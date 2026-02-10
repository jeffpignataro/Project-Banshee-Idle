using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using BansheeIdle.Core;

public partial class MainController : Control
{
    // UI References
    private Label _skillTitle;
    private Label _skillLevel;
    private ProgressBar _progressBar;
    private GridContainer _actionGrid;
    private Label _statusLabel;
    private PanelContainer _inventoryPanel;
    private ItemList _inventoryList;

    // Core Systems
    private GameDatabase _database;
    private GameState _state;
    private InventoryManager _inventory;
    private CombatManager _combat;
    private CraftingManager _crafting;
    private EquipmentManager _equipment;

    // Current state
    private enum ViewMode { Skill, Combat, Equipment, Inventory, Bank }
    private ViewMode _currentView = ViewMode.Skill;
    private string _currentSkillId = "";
    private ISkillAction _currentAction;
    private float _actionTimer = 0f;

    public override void _Ready()
    {
        // Get UI references
        _skillTitle = GetNode<Label>("HBoxContainer/MainPanel/VBoxContainer/SkillTitle");
        _skillLevel = GetNode<Label>("HBoxContainer/MainPanel/VBoxContainer/SkillLevel");
        _progressBar = GetNode<ProgressBar>("HBoxContainer/MainPanel/VBoxContainer/ProgressBar");
        _actionGrid = GetNode<GridContainer>("HBoxContainer/MainPanel/VBoxContainer/ActionGrid");
        _statusLabel = GetNode<Label>("HBoxContainer/MainPanel/VBoxContainer/StatusLabel");
        _inventoryPanel = GetNode<PanelContainer>("HBoxContainer/InventoryPanel");
        _inventoryList = GetNode<ItemList>("HBoxContainer/InventoryPanel/InventoryList");

        // Initialize game systems
        LoadDatabase();
        _state = new GameState();
        _inventory = new InventoryManager();
        _combat = new CombatManager(_inventory);
        _crafting = new CraftingManager(_inventory);
        _equipment = new EquipmentManager(_inventory, _database);

        // Wire up events
        _combat.OnCombatLog += (msg) => _statusLabel.Text = msg;
        _combat.OnSkillXpGained += OnSkillXpGained;
        _crafting.OnLevelUp += OnLevelUp;
        _crafting.OnCraftComplete += OnCraftComplete;
        _equipment.OnEquip += (equip, slot) => _statusLabel.Text = $"Equipped {equip.Name}";
        _equipment.OnUnequip += (equip, slot) => _statusLabel.Text = $"Unequipped {equip.Name}";

        // Connect button signals
        GetNode<Button>("HBoxContainer/Sidebar/WoodcuttingBtn").Pressed += () => ShowSkill("woodcutting");
        GetNode<Button>("HBoxContainer/Sidebar/MiningBtn").Pressed += () => ShowSkill("mining");
        GetNode<Button>("HBoxContainer/Sidebar/SmithingBtn").Pressed += () => ShowSkill("smithing");
        GetNode<Button>("HBoxContainer/Sidebar/FletchingBtn").Pressed += () => ShowSkill("fletching");
        GetNode<Button>("HBoxContainer/Sidebar/CombatBtn").Pressed += ShowCombat;
        GetNode<Button>("HBoxContainer/Sidebar/EquipmentBtn").Pressed += ShowEquipment;
        GetNode<Button>("HBoxContainer/Sidebar/InventoryBtn").Pressed += ToggleInventory;
        GetNode<Button>("HBoxContainer/Sidebar/BankBtn").Pressed += ShowBank;
        GetNode<Button>("HBoxContainer/Sidebar/SaveBtn").Pressed += SaveGame;

        // Initialize default skills
        _state.EnsureSkill("woodcutting", "Woodcutting");
        _state.EnsureSkill("mining", "Mining");
        _state.EnsureSkill("smithing", "Smithing");
        _state.EnsureSkill("fletching", "Fletching");
        _state.EnsureSkill("attack", "Attack");
        _state.EnsureSkill("strength", "Strength");
        _state.EnsureSkill("defense", "Defense");
        _state.EnsureSkill("hitpoints", "Hitpoints");

        LoadGame();
        _statusLabel.Text = "Welcome to Banshee Idle!";
    }

    public override void _Process(double delta)
    {
        float deltaF = (float)delta;

        // Process active systems
        _combat.Tick(deltaF, _state);
        _crafting.Tick(deltaF, _state);

        // Update progress bar based on current activity
        if (_combat.CurrentEncounter?.IsActive == true)
        {
            var enc = _combat.CurrentEncounter;
            float progress = (enc.MonsterCurrentHp / (float)enc.Monster.Hitpoints) * 100f;
            _progressBar.Value = 100f - progress;
        }
        else if (_crafting.CurrentAction != null)
        {
            _progressBar.Value = _crafting.Progress * 100f;
        }
        else if (_currentAction != null)
        {
            _actionTimer += deltaF;
            _progressBar.Value = (_actionTimer / _currentAction.TimeToComplete) * 100f;

            if (_actionTimer >= _currentAction.TimeToComplete)
            {
                CompleteAction();
                _actionTimer = 0;
            }
        }
    }

    private void LoadDatabase()
    {
        string dbPath = "res://Data/Database.json";
        if (Godot.FileAccess.FileExists(dbPath))
        {
            using var file = Godot.FileAccess.Open(dbPath, Godot.FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            _database = JsonSerializer.Deserialize<GameDatabase>(json);
        }
        else
        {
            GD.PrintErr("Database.json not found!");
            _database = new GameDatabase();
        }
    }

    private void ShowSkill(string skillId)
    {
        _currentView = ViewMode.Skill;
        _currentSkillId = skillId;
        _currentAction = null;

        // Stop other activities
        _combat.StopCombat();
        _crafting.StopCrafting();

        var skillDef = _database.GetSkill(skillId);
        _skillTitle.Text = skillDef?.Name ?? skillId;
        UpdateSkillDisplay();
        PopulateSkillActions();
    }

    private void ShowCombat()
    {
        _currentView = ViewMode.Combat;
        _currentAction = null;
        _crafting.StopCrafting();

        _skillTitle.Text = "Combat";
        UpdateCombatDisplay();
        PopulateCombatActions();
    }

    private void ShowEquipment()
    {
        _currentView = ViewMode.Equipment;
        _currentAction = null;
        _combat.StopCombat();
        _crafting.StopCrafting();

        _skillTitle.Text = "Equipment";
        _skillLevel.Text = $"HP: {_combat.PlayerStats.Hitpoints}/{_combat.PlayerStats.MaxHitpoints}";
        PopulateEquipmentGrid();
    }

    private void UpdateSkillDisplay()
    {
        if (_state.Skills.TryGetValue(_currentSkillId, out var skill))
        {
            long nextLevelXp = SkillData.XpForLevel(skill.Level + 1);
            _skillLevel.Text = $"Level: {skill.Level} | XP: {skill.Experience}/{nextLevelXp}";
        }
    }

    private void UpdateCombatDisplay()
    {
        var stats = _combat.PlayerStats;
        _skillLevel.Text = $"HP: {stats.Hitpoints}/{stats.MaxHitpoints} | ATT: {stats.TotalAttack} | STR: {stats.TotalStrength} | DEF: {stats.TotalDefense}";

        // Update combat stats from skills
        _combat.UpdateCombatStats(_state);
        _equipment.UpdateCombatStats(_combat.PlayerStats);
    }

    private void PopulateSkillActions()
    {
        ClearActionGrid();

        var actions = _database.GetActionsForSkill(_currentSkillId);
        var recipes = _database.GetRecipesForSkill(_currentSkillId);

        int playerLevel = _state.Skills.TryGetValue(_currentSkillId, out var skill) ? skill.Level : 1;

        // Add gathering actions
        foreach (var actionDef in actions)
        {
            var btn = new Button();
            var itemName = _database.GetItem(actionDef.Loot)?.Name ?? actionDef.Loot;
            btn.Text = $"{itemName}\nLvl {actionDef.ReqLevel}\n{actionDef.Xp} XP";
            btn.CustomMinimumSize = new Vector2(120, 60);

            if (playerLevel >= actionDef.ReqLevel)
            {
                var action = new GatheringAction(actionDef, _database);
                btn.Pressed += () => StartAction(action);
            }
            else
            {
                btn.Disabled = true;
            }

            _actionGrid.AddChild(btn);
        }

        // Add crafting recipes
        foreach (var recipe in recipes)
        {
            var btn = new Button();
            btn.Text = $"{recipe.Name}\nLvl {recipe.ReqLevel}\n{recipe.Xp} XP";
            btn.CustomMinimumSize = new Vector2(120, 60);

            if (playerLevel >= recipe.ReqLevel)
            {
                btn.Pressed += () => StartCrafting(recipe);
            }
            else
            {
                btn.Disabled = true;
            }

            _actionGrid.AddChild(btn);
        }
    }

    private void PopulateCombatActions()
    {
        ClearActionGrid();

        foreach (var monster in _database.Monsters)
        {
            var btn = new Button();
            btn.Text = $"{monster.Name}\nLvl {monster.CombatLevel}\nHP: {monster.Hitpoints}";
            btn.CustomMinimumSize = new Vector2(120, 60);
            btn.Pressed += () => StartCombat(monster);
            _actionGrid.AddChild(btn);
        }
    }

    private void PopulateEquipmentGrid()
    {
        ClearActionGrid();

        // Show equipped items
        var equipped = _equipment.Equipped;
        foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
        {
            var itemId = equipped.GetEquipped(slot);
            var label = new Label();
            label.Text = $"{slot}: {(string.IsNullOrEmpty(itemId) ? "Empty" : itemId)}";
            _actionGrid.AddChild(label);

            if (!string.IsNullOrEmpty(itemId))
            {
                var unequipBtn = new Button();
                unequipBtn.Text = "Unequip";
                unequipBtn.Pressed += () => _equipment.UnequipItem(slot, _state);
                _actionGrid.AddChild(unequipBtn);
            }
        }

        // Show equippable items from inventory
        var separator = new HSeparator();
        _actionGrid.AddChild(separator);

        foreach (var item in _state.Inventory)
        {
            var equipDef = _database.GetEquipment(item.ItemId);
            if (equipDef != null)
            {
                var btn = new Button();
                btn.Text = $"Equip {equipDef.Name}";
                btn.CustomMinimumSize = new Vector2(120, 40);
                btn.Pressed += () => _equipment.EquipItem(item.ItemId, _state);
                _actionGrid.AddChild(btn);
            }
        }
    }

    private void ClearActionGrid()
    {
        foreach (Node child in _actionGrid.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void StartAction(ISkillAction action)
    {
        if (!action.CanPerform(_state))
        {
            _statusLabel.Text = "Cannot perform this action!";
            return;
        }

        _currentAction = action;
        _actionTimer = 0;
        _statusLabel.Text = $"Gathering...";
    }

    private void StartCrafting(RecipeDef recipe)
    {
        var action = new CraftingAction(recipe, _database);
        if (_crafting.StartCrafting(action, _state))
        {
            _statusLabel.Text = $"Crafting {recipe.Name}...";
        }
        else
        {
            _statusLabel.Text = "Missing ingredients or level requirement!";
        }
    }

    private void StartCombat(MonsterDef monster)
    {
        if (_combat.StartCombat(monster, _state))
        {
            _statusLabel.Text = $"Fighting {monster.Name}...";
        }
    }

    private void CompleteAction()
    {
        _currentAction.OnComplete(_state, _inventory);

        if (_state.Skills.TryGetValue(_currentSkillId, out var skill))
        {
            _statusLabel.Text = $"Action complete! (+{_currentAction.RequiredLevel} XP)";
        }

        UpdateSkillDisplay();

        // Continue if still possible
        if (!_currentAction.CanPerform(_state))
        {
            _currentAction = null;
            _progressBar.Value = 0;
        }
    }

    private void OnSkillXpGained(string skillId, int xp)
    {
        GD.Print($"{skillId} gained {xp} XP");
    }

    private void OnLevelUp(string skillId, int newLevel)
    {
        _statusLabel.Text = $"Level up! {skillId} is now level {newLevel}!";
    }

    private void OnCraftComplete(RecipeDef recipe)
    {
        _statusLabel.Text = $"Crafted {recipe.Name}!";
    }

    private void ToggleInventory()
    {
        _inventoryPanel.Visible = !_inventoryPanel.Visible;
        if (_inventoryPanel.Visible)
        {
            RefreshInventoryDisplay();
        }
    }

    private void RefreshInventoryDisplay()
    {
        _inventoryList.Clear();
        foreach (var item in _state.Inventory)
        {
            var itemDef = _database.GetItem(item.ItemId);
            string name = itemDef?.Name ?? item.ItemId;
            _inventoryList.AddItem($"{name} x{item.Quantity}");
        }
    }

    private void ShowBank()
    {
        _statusLabel.Text = "Bank coming soon!";
    }

    private void SaveGame()
    {
        var saveData = new Godot.Collections.Dictionary
        {
            ["stats"] = SerializeStats(),
            ["skills"] = SerializeSkills(),
            ["inventory"] = SerializeInventory(),
            ["equipment"] = SerializeEquipment(),
            ["combat_stats"] = SerializeCombatStats()
        };

        var file = Godot.FileAccess.Open("user://savegame.json", Godot.FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(Json.Stringify(saveData));
            file.Close();
            _statusLabel.Text = "Game saved!";
        }
    }

    private void LoadGame()
    {
        if (Godot.FileAccess.FileExists("user://savegame.json"))
        {
            var file = Godot.FileAccess.Open("user://savegame.json", Godot.FileAccess.ModeFlags.Read);
            if (file != null)
            {
                var json = new Json();
                var result = json.Parse(file.GetAsText());
                file.Close();

                if (result == Error.Ok)
                {
                    var data = json.Data.AsGodotDictionary();
                    if (data.ContainsKey("skills")) DeserializeSkills(data["skills"].AsGodotDictionary());
                    if (data.ContainsKey("inventory")) DeserializeInventory(data["inventory"].AsGodotArray());
                    if (data.ContainsKey("equipment")) DeserializeEquipment(data["equipment"].AsGodotDictionary());
                    if (data.ContainsKey("combat_stats")) DeserializeCombatStats(data["combat_stats"].AsGodotDictionary());
                }
            }
        }
    }

    private Godot.Collections.Dictionary SerializeStats()
    {
        return new Godot.Collections.Dictionary
        {
            ["name"] = _state.Stats.Name,
            ["gold"] = _state.Stats.Gold
        };
    }

    private Godot.Collections.Dictionary SerializeSkills()
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var skill in _state.Skills)
        {
            dict[skill.Key] = new Godot.Collections.Dictionary
            {
                ["id"] = skill.Value.Id,
                ["name"] = skill.Value.Name,
                ["xp"] = skill.Value.Experience
            };
        }
        return dict;
    }

    private Godot.Collections.Array SerializeInventory()
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in _state.Inventory)
        {
            arr.Add(new Godot.Collections.Dictionary
            {
                ["item_id"] = item.ItemId,
                ["quantity"] = item.Quantity
            });
        }
        return arr;
    }

    private Godot.Collections.Dictionary SerializeEquipment()
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var slot in _equipment.Equipped.Slots)
        {
            dict[slot.Key.ToString()] = slot.Value;
        }
        return dict;
    }

    private Godot.Collections.Dictionary SerializeCombatStats()
    {
        var stats = _combat.PlayerStats;
        return new Godot.Collections.Dictionary
        {
            ["hitpoints"] = stats.Hitpoints,
            ["max_hitpoints"] = stats.MaxHitpoints
        };
    }

    private void DeserializeSkills(Godot.Collections.Dictionary data)
    {
        foreach (var key in data.Keys)
        {
            var skillData = data[key].AsGodotDictionary();
            var skill = new SkillData
            {
                Id = skillData["id"].ToString(),
                Name = skillData["name"].ToString(),
                Experience = Convert.ToInt64(skillData["xp"])
            };
            _state.Skills[key.ToString()] = skill;
        }
    }

    private void DeserializeInventory(Godot.Collections.Array data)
    {
        _state.Inventory.Clear();
        foreach (var itemData in data)
        {
            var item = itemData.AsGodotDictionary();
            _state.Inventory.Add(new InventoryItem
            {
                ItemId = item["item_id"].ToString(),
                Quantity = Convert.ToInt32(item["quantity"])
            });
        }
    }

    private void DeserializeEquipment(Godot.Collections.Dictionary data)
    {
        foreach (var key in data.Keys)
        {
            if (Enum.TryParse<EquipSlot>(key.ToString(), out var slot))
            {
                _equipment.Equipped.SetEquipped(slot, data[key].ToString());
            }
        }
    }

    private void DeserializeCombatStats(Godot.Collections.Dictionary data)
    {
        _combat.PlayerStats.Hitpoints = Convert.ToInt32(data["hitpoints"]);
        _combat.PlayerStats.MaxHitpoints = Convert.ToInt32(data["max_hitpoints"]);
    }
}
