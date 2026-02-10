using BansheeIdle.Core;

const string DatabasePath = "Data/Database.json";
const string SavePath = "savegame.json";
const float TickRate = 0.5f; // seconds per tick

// Load game database
GameDatabase db = DataLoader.LoadDatabase(DatabasePath);

// Load or create new game
GameState state = DataLoader.LoadGame(SavePath) ?? DataLoader.InitializeNewGame(db);

var inventory = new InventoryManager();
var skillManager = new SkillManager(inventory);

// Wire up events
skillManager.OnLevelUp += (skill, level) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n*** LEVEL UP! {skill} is now level {level}! ***");
    Console.ResetColor();
};

skillManager.OnActionComplete += (def) =>
{
    var item = db.GetItem(def.Loot);
    string itemName = item?.Name ?? def.Loot;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  + {itemName} (+{def.Xp} XP)");
    Console.ResetColor();
};

Console.WriteLine("=== Project Banshee-Idle ===");
Console.WriteLine($"Welcome, {state.Stats.Name}!\n");

bool running = true;
while (running)
{
    ShowMainMenu();
    string? input = Console.ReadLine()?.Trim();

    switch (input)
    {
        case "1": SkillMenu("woodcutting", "Woodcutting"); break;
        case "2": SkillMenu("mining", "Mining"); break;
        case "3": ShowInventory(); break;
        case "4": ShowBank(); break;
        case "5": BankMenu(); break;
        case "6": ShowStats(); break;
        case "7":
            DataLoader.SaveGame(state, SavePath);
            Console.WriteLine("Game saved.");
            break;
        case "8":
            DataLoader.SaveGame(state, SavePath);
            Console.WriteLine("Game saved. Goodbye!");
            running = false;
            break;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

// --- Menu functions ---

void ShowMainMenu()
{
    Console.WriteLine("\n--- Main Menu ---");
    Console.WriteLine("1) Woodcutting");
    Console.WriteLine("2) Mining");
    Console.WriteLine("3) Inventory");
    Console.WriteLine("4) Bank");
    Console.WriteLine("5) Deposit/Withdraw");
    Console.WriteLine("6) Stats");
    Console.WriteLine("7) Save");
    Console.WriteLine("8) Save & Quit");
    Console.Write("> ");
}

void SkillMenu(string skillId, string skillName)
{
    var actions = db.GetActionsForSkill(skillId);
    var skill = state.Skills.GetValueOrDefault(skillId);
    int level = skill?.Level ?? 1;
    long xp = skill?.Experience ?? 0;

    Console.WriteLine($"\n--- {skillName} (Level {level}, XP: {xp}) ---");
    Console.WriteLine("Available actions:");

    for (int i = 0; i < actions.Count; i++)
    {
        var a = actions[i];
        var item = db.GetItem(a.Loot);
        string itemName = item?.Name ?? a.Loot;
        string status = level >= a.ReqLevel ? "" : " [LOCKED]";
        Console.WriteLine($"  {i + 1}) {itemName} (Lvl {a.ReqLevel}, {a.Xp} XP, {a.Time}s){status}");
    }
    Console.WriteLine($"  0) Back");
    Console.Write("> ");

    string? choice = Console.ReadLine()?.Trim();
    if (!int.TryParse(choice, out int idx) || idx == 0)
        return;

    idx--;
    if (idx < 0 || idx >= actions.Count)
    {
        Console.WriteLine("Invalid choice.");
        return;
    }

    var actionDef = actions[idx];
    var action = new GatheringAction(actionDef, db);

    if (!skillManager.SetAction(action, state))
    {
        Console.WriteLine($"Cannot perform this action. Requires level {actionDef.ReqLevel}.");
        return;
    }

    var itemDef = db.GetItem(actionDef.Loot);
    string lootName = itemDef?.Name ?? actionDef.Loot;
    Console.WriteLine($"\nGathering {lootName}... (Press Enter to stop)\n");

    RunActionLoop();
}

void RunActionLoop()
{
    // Run action ticks until user presses a key
    while (skillManager.CurrentAction != null)
    {
        if (Console.KeyAvailable)
        {
            Console.ReadKey(true);
            skillManager.StopAction();
            Console.WriteLine("Stopped.");
            break;
        }

        Thread.Sleep((int)(TickRate * 1000));
        skillManager.Tick(TickRate, state);

        // Show progress bar
        int barWidth = 20;
        int filled = (int)(skillManager.Progress * barWidth);
        string bar = new string('#', filled) + new string('-', barWidth - filled);
        Console.Write($"\r  [{bar}] {skillManager.Progress * 100:F0}%  ");
    }
    Console.WriteLine();
}

void ShowInventory()
{
    Console.WriteLine($"\n--- Inventory ({state.Inventory.Count}/{InventoryManager.MaxInventorySlots}) ---");
    if (state.Inventory.Count == 0)
    {
        Console.WriteLine("  (empty)");
        return;
    }
    foreach (var item in state.Inventory)
    {
        var def = db.GetItem(item.ItemId);
        string name = def?.Name ?? item.ItemId;
        Console.WriteLine($"  {name} x{item.Quantity} (value: {def?.Value ?? 0} gp each)");
    }
}

void ShowBank()
{
    Console.WriteLine($"\n--- Bank ({state.Bank.Count}/{InventoryManager.MaxBankSlots}) ---");
    if (state.Bank.Count == 0)
    {
        Console.WriteLine("  (empty)");
        return;
    }
    foreach (var item in state.Bank)
    {
        var def = db.GetItem(item.ItemId);
        string name = def?.Name ?? item.ItemId;
        Console.WriteLine($"  {name} x{item.Quantity}");
    }
}

void BankMenu()
{
    Console.WriteLine("\n--- Bank Operations ---");
    Console.WriteLine("1) Deposit all inventory");
    Console.WriteLine("2) Sell all inventory");
    Console.WriteLine("0) Back");
    Console.Write("> ");

    string? choice = Console.ReadLine()?.Trim();
    switch (choice)
    {
        case "1":
            if (inventory.DepositAll(state))
                Console.WriteLine("All items deposited to bank.");
            else
                Console.WriteLine("Some items could not be deposited (bank full?).");
            break;
        case "2":
            var items = new List<InventoryItem>(state.Inventory);
            long totalGold = 0;
            foreach (var item in items)
            {
                var def = db.GetItem(item.ItemId);
                if (def != null)
                    totalGold += (long)def.Value * item.Quantity;
                inventory.SellItem(state, db, item.ItemId, item.Quantity);
            }
            Console.WriteLine($"Sold all items for {totalGold} gold. Total gold: {state.Stats.Gold}");
            break;
    }
}

void ShowStats()
{
    Console.WriteLine($"\n--- {state.Stats.Name}'s Stats ---");
    Console.WriteLine($"Gold: {state.Stats.Gold}");
    Console.WriteLine("Skills:");
    foreach (var (id, skill) in state.Skills)
    {
        long nextLevelXp = SkillData.XpForLevel(skill.Level + 1);
        Console.WriteLine($"  {skill.Name}: Level {skill.Level} ({skill.Experience}/{nextLevelXp} XP)");
    }
}
