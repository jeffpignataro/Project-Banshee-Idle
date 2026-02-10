using Godot;
using System;
using BansheeIdle.Core;

namespace BansheeIdle.GUI;

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
    
    // Game State
    private SkillSystem _skillSystem;
    private InventoryManager _inventory;
    private string _currentSkill = "";
    private bool _isGathering = false;
    private double _gatherProgress = 0;
    private double _gatherTime = 2.0;
    private string _currentAction = "";

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
        _skillSystem = new SkillSystem();
        _inventory = new InventoryManager();
        
        // Connect button signals
        GetNode<Button>("HBoxContainer/Sidebar/WoodcuttingBtn").Pressed += () => ShowSkill("Woodcutting");
        GetNode<Button>("HBoxContainer/Sidebar/MiningBtn").Pressed += () => ShowSkill("Mining");
        GetNode<Button>("HBoxContainer/Sidebar/InventoryBtn").Pressed += ToggleInventory;
        GetNode<Button>("HBoxContainer/Sidebar/BankBtn").Pressed += ShowBank;
        GetNode<Button>("HBoxContainer/Sidebar/SaveBtn").Pressed += SaveGame;
        
        // Load saved game if exists
        LoadGame();
        
        _statusLabel.Text = "Welcome to Banshee Idle!";
    }

    public override void _Process(double delta)
    {
        if (_isGathering)
        {
            _gatherProgress += delta;
            _progressBar.Value = (_gatherProgress / _gatherTime) * 100.0;
            
            if (_gatherProgress >= _gatherTime)
            {
                CompleteGatherAction();
                _gatherProgress = 0;
            }
        }
    }

    private void ShowSkill(string skillName)
    {
        _currentSkill = skillName;
        _skillTitle.Text = skillName;
        UpdateSkillDisplay();
        PopulateActionGrid();
        _isGathering = false;
        _progressBar.Value = 0;
    }

    private void UpdateSkillDisplay()
    {
        int level = _skillSystem.GetLevel(_currentSkill);
        int xp = _skillSystem.GetXP(_currentSkill);
        int nextLevelXP = _skillSystem.GetXPForLevel(level + 1);
        _skillLevel.Text = $"Level: {level} | XP: {xp}/{nextLevelXP}";
    }

    private void PopulateActionGrid()
    {
        // Clear existing buttons
        foreach (Node child in _actionGrid.GetChildren())
        {
            child.QueueFree();
        }

        var actions = DataLoader.GetActionsForSkill(_currentSkill);
        int playerLevel = _skillSystem.GetLevel(_currentSkill);
        
        foreach (var action in actions)
        {
            var btn = new Button();
            btn.Text = $"{action.Name}\nLvl {action.LevelRequired}";
            btn.CustomMinimumSize = new Vector2(120, 60);
            
            if (playerLevel >= action.LevelRequired)
            {
                btn.Pressed += () => StartGatherAction(action);
            }
            else
            {
                btn.Disabled = true;
            }
            
            _actionGrid.AddChild(btn);
        }
    }

    private void StartGatherAction(SkillAction action)
    {
        _currentAction = action.Name;
        _gatherTime = action.TimeSeconds;
        _isGathering = true;
        _gatherProgress = 0;
        _statusLabel.Text = $"Gathering {action.Name}...";
    }

    private void CompleteGatherAction()
    {
        var action = DataLoader.GetAction(_currentSkill, _currentAction);
        if (action != null)
        {
            // Add XP
            _skillSystem.AddXP(_currentSkill, action.XPReward);
            
            // Add item to inventory
            _inventory.AddItem(action.ItemReward, 1);
            
            UpdateSkillDisplay();
            _statusLabel.Text = $"Got {action.ItemReward}! (+{action.XPReward} XP)";
            
            // Check for level up
            int newLevel = _skillSystem.GetLevel(_currentSkill);
            // Could show level up notification here
        }
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
        var items = _inventory.GetAllItems();
        foreach (var item in items)
        {
            _inventoryList.AddItem($"{item.Key} x{item.Value}");
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
            ["skills"] = _skillSystem.Serialize(),
            ["inventory"] = _inventory.Serialize()
        };
        
        var file = FileAccess.Open("user://savegame.json", FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(Json.Stringify(saveData));
            file.Close();
            _statusLabel.Text = "Game saved!";
        }
    }

    private void LoadGame()
    {
        if (FileAccess.FileExists("user://savegame.json"))
        {
            var file = FileAccess.Open("user://savegame.json", FileAccess.ModeFlags.Read);
            if (file != null)
            {
                var json = new Json();
                var result = json.Parse(file.GetAsText());
                file.Close();
                
                if (result == Error.Ok)
                {
                    var data = json.Data.AsGodotDictionary();
                    _skillSystem.Deserialize(data["skills"].AsGodotDictionary());
                    _inventory.Deserialize(data["inventory"].AsGodotDictionary());
                }
            }
        }
    }
}
