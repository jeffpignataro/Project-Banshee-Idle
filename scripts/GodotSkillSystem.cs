using Godot;
using System;
using System.Collections.Generic;

namespace BansheeIdle.Core;

/// <summary>
/// Godot-compatible wrapper for skill progression with serialization
/// </summary>
public class SkillSystem
{
    private Dictionary<string, int> _xp = new();
    
    // RuneScape-style XP curve constants
    private const double XP_BASE = 50.0;
    private const double XP_MULT = 1.1;

    public int GetXP(string skill)
    {
        return _xp.TryGetValue(skill, out int xp) ? xp : 0;
    }

    public int GetLevel(string skill)
    {
        int xp = GetXP(skill);
        return XPToLevel(xp);
    }

    public void AddXP(string skill, int amount)
    {
        if (!_xp.ContainsKey(skill))
            _xp[skill] = 0;
        _xp[skill] += amount;
    }

    public int GetXPForLevel(int level)
    {
        if (level <= 1) return 0;
        double total = 0;
        for (int i = 1; i < level; i++)
        {
            total += Math.Floor(XP_BASE * Math.Pow(XP_MULT, i));
        }
        return (int)total;
    }

    private int XPToLevel(int xp)
    {
        int level = 1;
        int xpNeeded = 0;
        while (level < 99)
        {
            xpNeeded += (int)Math.Floor(XP_BASE * Math.Pow(XP_MULT, level));
            if (xp < xpNeeded) break;
            level++;
        }
        return level;
    }

    public Godot.Collections.Dictionary Serialize()
    {
        var data = new Godot.Collections.Dictionary();
        foreach (var kvp in _xp)
        {
            data[kvp.Key] = kvp.Value;
        }
        return data;
    }

    public void Deserialize(Godot.Collections.Dictionary data)
    {
        _xp.Clear();
        foreach (var key in data.Keys)
        {
            _xp[key.ToString()] = (int)data[key];
        }
    }
}

/// <summary>
/// Godot-compatible inventory manager
/// </summary>
public class InventoryManager
{
    private Dictionary<string, int> _items = new();
    private const int MAX_SLOTS = 28;

    public void AddItem(string itemName, int quantity)
    {
        if (!_items.ContainsKey(itemName))
            _items[itemName] = 0;
        _items[itemName] += quantity;
    }

    public bool RemoveItem(string itemName, int quantity)
    {
        if (!_items.ContainsKey(itemName) || _items[itemName] < quantity)
            return false;
        _items[itemName] -= quantity;
        if (_items[itemName] <= 0)
            _items.Remove(itemName);
        return true;
    }

    public int GetItemCount(string itemName)
    {
        return _items.TryGetValue(itemName, out int count) ? count : 0;
    }

    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(_items);
    }

    public Godot.Collections.Dictionary Serialize()
    {
        var data = new Godot.Collections.Dictionary();
        foreach (var kvp in _items)
        {
            data[kvp.Key] = kvp.Value;
        }
        return data;
    }

    public void Deserialize(Godot.Collections.Dictionary data)
    {
        _items.Clear();
        foreach (var key in data.Keys)
        {
            _items[key.ToString()] = (int)data[key];
        }
    }
}

/// <summary>
/// Simple skill action data
/// </summary>
public class SkillAction
{
    public string Name { get; set; } = "";
    public int LevelRequired { get; set; }
    public double TimeSeconds { get; set; }
    public int XPReward { get; set; }
    public string ItemReward { get; set; } = "";
}

/// <summary>
/// Static data loader for skill actions
/// </summary>
public static class DataLoader
{
    private static readonly Dictionary<string, List<SkillAction>> _actions = new()
    {
        ["Woodcutting"] = new List<SkillAction>
        {
            new() { Name = "Normal Tree", LevelRequired = 1, TimeSeconds = 2.0, XPReward = 25, ItemReward = "Normal Logs" },
            new() { Name = "Oak Tree", LevelRequired = 15, TimeSeconds = 3.0, XPReward = 37, ItemReward = "Oak Logs" },
            new() { Name = "Willow Tree", LevelRequired = 30, TimeSeconds = 4.0, XPReward = 68, ItemReward = "Willow Logs" },
            new() { Name = "Maple Tree", LevelRequired = 45, TimeSeconds = 5.0, XPReward = 100, ItemReward = "Maple Logs" },
            new() { Name = "Yew Tree", LevelRequired = 60, TimeSeconds = 6.0, XPReward = 175, ItemReward = "Yew Logs" },
            new() { Name = "Magic Tree", LevelRequired = 75, TimeSeconds = 8.0, XPReward = 250, ItemReward = "Magic Logs" },
        },
        ["Mining"] = new List<SkillAction>
        {
            new() { Name = "Copper Rock", LevelRequired = 1, TimeSeconds = 2.0, XPReward = 18, ItemReward = "Copper Ore" },
            new() { Name = "Tin Rock", LevelRequired = 1, TimeSeconds = 2.0, XPReward = 18, ItemReward = "Tin Ore" },
            new() { Name = "Iron Rock", LevelRequired = 15, TimeSeconds = 3.0, XPReward = 35, ItemReward = "Iron Ore" },
            new() { Name = "Coal Rock", LevelRequired = 30, TimeSeconds = 4.0, XPReward = 50, ItemReward = "Coal" },
            new() { Name = "Gold Rock", LevelRequired = 40, TimeSeconds = 5.0, XPReward = 65, ItemReward = "Gold Ore" },
            new() { Name = "Mithril Rock", LevelRequired = 55, TimeSeconds = 6.0, XPReward = 80, ItemReward = "Mithril Ore" },
            new() { Name = "Adamant Rock", LevelRequired = 70, TimeSeconds = 7.0, XPReward = 95, ItemReward = "Adamantite Ore" },
            new() { Name = "Runite Rock", LevelRequired = 85, TimeSeconds = 10.0, XPReward = 125, ItemReward = "Runite Ore" },
        }
    };

    public static List<SkillAction> GetActionsForSkill(string skillName)
    {
        return _actions.TryGetValue(skillName, out var actions) ? actions : new List<SkillAction>();
    }

    public static SkillAction? GetAction(string skillName, string actionName)
    {
        var actions = GetActionsForSkill(skillName);
        return actions.Find(a => a.Name == actionName);
    }
}
