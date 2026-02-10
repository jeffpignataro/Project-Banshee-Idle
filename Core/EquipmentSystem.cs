using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BansheeIdle.Core;

/// <summary>
/// Equipment slots
/// </summary>
public enum EquipSlot
{
    Weapon,
    Helm,
    Body,
    Legs,
    Shield,
    Gloves,
    Boots,
    Ring,
    Amulet
}

/// <summary>
/// Stat bonuses from equipment
/// </summary>
public class EquipmentStats
{
    [JsonPropertyName("attack_bonus")]
    public int AttackBonus { get; set; } = 0;

    [JsonPropertyName("strength_bonus")]
    public int StrengthBonus { get; set; } = 0;

    [JsonPropertyName("defense_bonus")]
    public int DefenseBonus { get; set; } = 0;

    [JsonPropertyName("ranged_bonus")]
    public int RangedBonus { get; set; } = 0;
}

/// <summary>
/// Equipment requirements
/// </summary>
public class EquipmentRequirements
{
    [JsonPropertyName("attack_level")]
    public int AttackLevel { get; set; } = 1;

    [JsonPropertyName("defense_level")]
    public int DefenseLevel { get; set; } = 1;

    [JsonPropertyName("ranged_level")]
    public int RangedLevel { get; set; } = 1;
}

/// <summary>
/// Defines an equipment item
/// </summary>
public class EquipmentDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("slot")]
    public string Slot { get; set; } = "";

    [JsonPropertyName("stats")]
    public EquipmentStats Stats { get; set; } = new();

    [JsonPropertyName("requirements")]
    public EquipmentRequirements Requirements { get; set; } = new();

    [JsonPropertyName("value")]
    public int Value { get; set; } = 0;

    public EquipSlot GetSlot()
    {
        return Slot.ToLower() switch
        {
            "weapon" => EquipSlot.Weapon,
            "helm" => EquipSlot.Helm,
            "body" => EquipSlot.Body,
            "legs" => EquipSlot.Legs,
            "shield" => EquipSlot.Shield,
            "gloves" => EquipSlot.Gloves,
            "boots" => EquipSlot.Boots,
            "ring" => EquipSlot.Ring,
            "amulet" => EquipSlot.Amulet,
            _ => EquipSlot.Weapon
        };
    }
}

/// <summary>
/// Player's equipped items
/// </summary>
public class EquippedItems
{
    public Dictionary<EquipSlot, string> Slots { get; set; } = new();

    public EquippedItems()
    {
        // Initialize empty slots
        foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
        {
            Slots[slot] = "";
        }
    }

    public string GetEquipped(EquipSlot slot)
    {
        return Slots.TryGetValue(slot, out var itemId) ? itemId : "";
    }

    public void SetEquipped(EquipSlot slot, string itemId)
    {
        Slots[slot] = itemId;
    }

    public void Unequip(EquipSlot slot)
    {
        Slots[slot] = "";
    }
}

/// <summary>
/// Manages equipment and stat bonuses
/// </summary>
public class EquipmentManager
{
    private readonly InventoryManager _inventory;
    private readonly GameDatabase _db;

    public EquippedItems Equipped { get; set; } = new();

    public event Action<EquipmentDef, EquipSlot>? OnEquip;
    public event Action<EquipmentDef, EquipSlot>? OnUnequip;

    public EquipmentManager(InventoryManager inventory, GameDatabase db)
    {
        _inventory = inventory;
        _db = db;
    }

    /// <summary>
    /// Check if player meets requirements to equip an item
    /// </summary>
    public bool CanEquip(EquipmentDef equipment, GameState state)
    {
        var req = equipment.Requirements;

        if (state.Skills.TryGetValue("attack", out var attack) && attack.Level < req.AttackLevel)
            return false;

        if (state.Skills.TryGetValue("defense", out var defense) && defense.Level < req.DefenseLevel)
            return false;

        if (state.Skills.TryGetValue("ranged", out var ranged) && ranged.Level < req.RangedLevel)
            return false;

        return true;
    }

    /// <summary>
    /// Equip an item from inventory
    /// </summary>
    public bool EquipItem(string itemId, GameState state)
    {
        // Find equipment definition
        var equipment = _db.GetEquipment(itemId);
        if (equipment == null)
            return false;

        // Check if player has the item
        var inventoryItem = state.Inventory.Find(i => i.ItemId == itemId);
        if (inventoryItem == null || inventoryItem.Quantity < 1)
            return false;

        // Check requirements
        if (!CanEquip(equipment, state))
            return false;

        var slot = equipment.GetSlot();

        // Unequip existing item in that slot
        var currentEquipped = Equipped.GetEquipped(slot);
        if (!string.IsNullOrEmpty(currentEquipped))
        {
            UnequipItem(slot, state);
        }

        // Remove from inventory and equip
        _inventory.RemoveFromInventory(state, itemId, 1);
        Equipped.SetEquipped(slot, itemId);

        OnEquip?.Invoke(equipment, slot);
        return true;
    }

    /// <summary>
    /// Unequip an item and return it to inventory
    /// </summary>
    public bool UnequipItem(EquipSlot slot, GameState state)
    {
        var itemId = Equipped.GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId))
            return false;

        var equipment = _db.GetEquipment(itemId);
        if (equipment == null)
            return false;

        // Add back to inventory
        _inventory.AddToInventory(state, itemId, 1);
        Equipped.Unequip(slot);

        OnUnequip?.Invoke(equipment, slot);
        return true;
    }

    /// <summary>
    /// Calculate total stat bonuses from all equipped items
    /// </summary>
    public EquipmentStats GetTotalBonuses()
    {
        var total = new EquipmentStats();

        foreach (var slot in Equipped.Slots.Values)
        {
            if (string.IsNullOrEmpty(slot))
                continue;

            var equipment = _db.GetEquipment(slot);
            if (equipment == null)
                continue;

            total.AttackBonus += equipment.Stats.AttackBonus;
            total.StrengthBonus += equipment.Stats.StrengthBonus;
            total.DefenseBonus += equipment.Stats.DefenseBonus;
            total.RangedBonus += equipment.Stats.RangedBonus;
        }

        return total;
    }

    /// <summary>
    /// Update combat stats with equipment bonuses
    /// </summary>
    public void UpdateCombatStats(CombatStats combatStats)
    {
        var bonuses = GetTotalBonuses();
        combatStats.AttackBonus = bonuses.AttackBonus;
        combatStats.StrengthBonus = bonuses.StrengthBonus;
        combatStats.DefenseBonus = bonuses.DefenseBonus;
    }
}
