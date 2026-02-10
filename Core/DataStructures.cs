using System.Text.Json.Serialization;

namespace BansheeIdle.Core;

public class PlayerStats
{
    public string Name { get; set; } = "Player";
    public long Gold { get; set; } = 0;
}

public class SkillData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public long Experience { get; set; } = 0;
    public int Level => CalculateLevel(Experience);

    // RuneScape-style exponential leveling curve.
    // Each level requires roughly 10% more total XP than the last.
    public static int CalculateLevel(long xp)
    {
        int level = 1;
        long total = 0;
        for (int l = 1; l < 99; l++)
        {
            total += (long)(l + 300 * Math.Pow(2, l / 7.0)) / 4;
            if (xp < total)
                return level;
            level = l + 1;
        }
        return 99;
    }

    public static long XpForLevel(int level)
    {
        if (level <= 1) return 0;
        long total = 0;
        for (int l = 1; l < level; l++)
            total += (long)(l + 300 * Math.Pow(2, l / 7.0)) / 4;
        return total;
    }
}

public class InventoryItem
{
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
}

public class GameState
{
    public PlayerStats Stats { get; set; } = new();
    public Dictionary<string, SkillData> Skills { get; set; } = new();
    public List<InventoryItem> Inventory { get; set; } = new();
    public List<InventoryItem> Bank { get; set; } = new();

    public void EnsureSkill(string id, string name)
    {
        if (!Skills.ContainsKey(id))
            Skills[id] = new SkillData { Id = id, Name = name };
    }
}

// --- Database definition models (loaded from Database.json) ---

public class ItemDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class SkillDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class ActionDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("skill")]
    public string Skill { get; set; } = "";

    [JsonPropertyName("req_level")]
    public int ReqLevel { get; set; }

    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("loot")]
    public string Loot { get; set; } = "";

    [JsonPropertyName("time")]
    public float Time { get; set; } = 3.0f;
}

public class GameDatabase
{
    [JsonPropertyName("items")]
    public List<ItemDef> Items { get; set; } = new();

    [JsonPropertyName("skills")]
    public List<SkillDef> Skills { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<ActionDef> Actions { get; set; } = new();

    public ItemDef? GetItem(string id) => Items.Find(i => i.Id == id);
    public SkillDef? GetSkill(string id) => Skills.Find(s => s.Id == id);
    public ActionDef? GetAction(string id) => Actions.Find(a => a.Id == id);
    public List<ActionDef> GetActionsForSkill(string skillId) => Actions.FindAll(a => a.Skill == skillId);
}
