using System.Text.Json;

namespace BansheeIdle.Core;

public static class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static GameDatabase LoadDatabase(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameDatabase>(json, JsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize database.");
    }

    public static GameState InitializeNewGame(GameDatabase db)
    {
        var state = new GameState();
        foreach (var skill in db.Skills)
            state.EnsureSkill(skill.Id, skill.Name);
        return state;
    }

    public static void SaveGame(GameState state, string path)
    {
        string json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static GameState? LoadGame(string path)
    {
        if (!File.Exists(path))
            return null;
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameState>(json, JsonOptions);
    }
}
