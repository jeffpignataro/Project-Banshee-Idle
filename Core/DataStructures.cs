namespace BansheeIdle.Core;

public class PlayerStats {
    public string Name { get; set; } = "Player";
    public long Gold { get; set; } = 0;
    // Potentially more global stats like Combat Level, HP, etc.
}

public class SkillData {
    public string Id { get; set; }
    public string Name { get; set; }
    public long Experience { get; set; } = 0;
    public int Level => CalculateLevel(Experience);

    private int CalculateLevel(long exp) {
        // Standard exponential curve logic here
        return 1; 
    }
}

public class InventoryItem {
    public string ItemId { get; set; }
    public int Quantity { get; set; }
}

public class GameState {
    public PlayerStats Stats { get; set; } = new();
    public Dictionary<string, SkillData> Skills { get; set; } = new();
    public List<InventoryItem> Bank { get; set; } = new();
}
