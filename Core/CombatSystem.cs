using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BansheeIdle.Core;

/// <summary>
/// Combat stats for a player or entity
/// </summary>
public class CombatStats
{
    public int Hitpoints { get; set; } = 10;
    public int MaxHitpoints { get; set; } = 10;
    public int Attack { get; set; } = 1;
    public int Strength { get; set; } = 1;
    public int Defense { get; set; } = 1;

    // Bonus stats from equipment
    public int AttackBonus { get; set; } = 0;
    public int StrengthBonus { get; set; } = 0;
    public int DefenseBonus { get; set; } = 0;

    public int TotalAttack => Attack + AttackBonus;
    public int TotalStrength => Strength + StrengthBonus;
    public int TotalDefense => Defense + DefenseBonus;

    public int MaxHit => (int)Math.Max(1, TotalStrength / 10.0 + 1);
}

/// <summary>
/// Current combat encounter state
/// </summary>
public class CombatEncounter
{
    public MonsterDef Monster { get; set; } = null!;
    public int MonsterCurrentHp { get; set; }
    public float PlayerAttackTimer { get; set; } = 0f;
    public float MonsterAttackTimer { get; set; } = 0f;
    public bool IsActive { get; set; } = false;
    public int MonstersKilled { get; set; } = 0;
}

/// <summary>
/// Combat system manager
/// </summary>
public class CombatManager
{
    private readonly InventoryManager _inventory;
    private readonly Random _random = new();

    public CombatStats PlayerStats { get; set; } = new();
    public CombatEncounter? CurrentEncounter { get; private set; }

    public event Action<int, bool>? OnDamageDealt; // damage, isPlayer
    public event Action<string>? OnCombatLog;
    public event Action<MonsterDef, List<LootDrop>>? OnMonsterKilled;
    public event Action<string, int>? OnSkillXpGained;

    public CombatManager(InventoryManager inventory)
    {
        _inventory = inventory;
    }

    /// <summary>
    /// Start combat with a monster
    /// </summary>
    public bool StartCombat(MonsterDef monster, GameState state)
    {
        if (CurrentEncounter?.IsActive == true)
            return false;

        // Check if player has enough HP
        if (PlayerStats.Hitpoints <= 0)
        {
            OnCombatLog?.Invoke("You need to restore your health before fighting!");
            return false;
        }

        CurrentEncounter = new CombatEncounter
        {
            Monster = monster,
            MonsterCurrentHp = monster.Hitpoints,
            IsActive = true,
            PlayerAttackTimer = 0f,
            MonsterAttackTimer = 0f,
            MonstersKilled = 0
        };

        OnCombatLog?.Invoke($"You engage {monster.Name} in combat!");
        return true;
    }

    /// <summary>
    /// Stop current combat
    /// </summary>
    public void StopCombat()
    {
        if (CurrentEncounter != null)
        {
            OnCombatLog?.Invoke("You retreat from combat.");
            CurrentEncounter.IsActive = false;
            CurrentEncounter = null;
        }
    }

    /// <summary>
    /// Process combat tick
    /// </summary>
    public void Tick(float delta, GameState state)
    {
        if (CurrentEncounter == null || !CurrentEncounter.IsActive)
            return;

        // Check if player is dead
        if (PlayerStats.Hitpoints <= 0)
        {
            OnCombatLog?.Invoke("You have been defeated!");
            HandlePlayerDeath();
            return;
        }

        // Player attack
        CurrentEncounter.PlayerAttackTimer += delta;
        if (CurrentEncounter.PlayerAttackTimer >= 2.4f) // Player attack speed
        {
            PerformPlayerAttack(state);
            CurrentEncounter.PlayerAttackTimer = 0f;
        }

        // Monster attack
        CurrentEncounter.MonsterAttackTimer += delta;
        if (CurrentEncounter.MonsterAttackTimer >= CurrentEncounter.Monster.AttackSpeed)
        {
            PerformMonsterAttack();
            CurrentEncounter.MonsterAttackTimer = 0f;
        }
    }

    private void PerformPlayerAttack(GameState state)
    {
        if (CurrentEncounter == null) return;

        // Simple hit chance calculation
        int attackRoll = _random.Next(0, PlayerStats.TotalAttack);
        int defenseRoll = _random.Next(0, 64); // Base defense

        if (attackRoll > defenseRoll)
        {
            // Hit!
            int damage = _random.Next(1, PlayerStats.MaxHit + 1);
            CurrentEncounter.MonsterCurrentHp -= damage;
            OnDamageDealt?.Invoke(damage, true);
            OnCombatLog?.Invoke($"You hit {CurrentEncounter.Monster.Name} for {damage} damage!");

            // Check if monster is dead
            if (CurrentEncounter.MonsterCurrentHp <= 0)
            {
                HandleMonsterDeath(state);
            }
        }
        else
        {
            // Miss
            OnCombatLog?.Invoke("You missed!");
        }
    }

    private void PerformMonsterAttack()
    {
        if (CurrentEncounter == null) return;

        // Simple hit chance calculation
        int attackRoll = _random.Next(0, CurrentEncounter.Monster.MaxHit * 5);
        int defenseRoll = _random.Next(0, PlayerStats.TotalDefense);

        if (attackRoll > defenseRoll)
        {
            // Hit!
            int damage = _random.Next(1, CurrentEncounter.Monster.MaxHit + 1);
            PlayerStats.Hitpoints = Math.Max(0, PlayerStats.Hitpoints - damage);
            OnDamageDealt?.Invoke(damage, false);
            OnCombatLog?.Invoke($"{CurrentEncounter.Monster.Name} hits you for {damage} damage!");
        }
        else
        {
            // Miss
            OnCombatLog?.Invoke($"{CurrentEncounter.Monster.Name} missed!");
        }
    }

    private void HandleMonsterDeath(GameState state)
    {
        if (CurrentEncounter == null) return;

        CurrentEncounter.MonstersKilled++;
        OnCombatLog?.Invoke($"You defeated {CurrentEncounter.Monster.Name}!");

        // Grant XP
        state.EnsureSkill("attack", "Attack");
        state.EnsureSkill("strength", "Strength");
        state.EnsureSkill("defense", "Defense");
        state.EnsureSkill("hitpoints", "Hitpoints");

        int xpPerStat = CurrentEncounter.Monster.XpReward / 4;
        state.Skills["attack"].Experience += xpPerStat;
        state.Skills["strength"].Experience += xpPerStat;
        state.Skills["defense"].Experience += xpPerStat;
        state.Skills["hitpoints"].Experience += xpPerStat;

        OnSkillXpGained?.Invoke("attack", xpPerStat);
        OnSkillXpGained?.Invoke("strength", xpPerStat);
        OnSkillXpGained?.Invoke("defense", xpPerStat);
        OnSkillXpGained?.Invoke("hitpoints", xpPerStat);

        // Roll loot
        var loot = RollLoot(CurrentEncounter.Monster);
        foreach (var drop in loot)
        {
            _inventory.AddToInventory(state, drop.ItemId, drop.Quantity);
            OnCombatLog?.Invoke($"You received {drop.Quantity}x {drop.ItemId}");
        }

        OnMonsterKilled?.Invoke(CurrentEncounter.Monster, loot);

        // Respawn monster for continuous combat
        CurrentEncounter.MonsterCurrentHp = CurrentEncounter.Monster.Hitpoints;
    }

    private List<LootDrop> RollLoot(MonsterDef monster)
    {
        var drops = new List<LootDrop>();

        foreach (var entry in monster.LootTable)
        {
            if (_random.NextDouble() <= entry.DropRate)
            {
                int quantity = _random.Next(entry.MinQuantity, entry.MaxQuantity + 1);
                drops.Add(new LootDrop
                {
                    ItemId = entry.ItemId,
                    Quantity = quantity
                });
            }
        }

        return drops;
    }

    private void HandlePlayerDeath()
    {
        OnCombatLog?.Invoke("You have died. Respawning at full health...");
        PlayerStats.Hitpoints = PlayerStats.MaxHitpoints;
        StopCombat();
    }

    /// <summary>
    /// Restore player HP (from food, etc)
    /// </summary>
    public void RestoreHp(int amount)
    {
        PlayerStats.Hitpoints = Math.Min(PlayerStats.MaxHitpoints, PlayerStats.Hitpoints + amount);
    }

    /// <summary>
    /// Update combat stats from skills
    /// </summary>
    public void UpdateCombatStats(GameState state)
    {
        if (state.Skills.TryGetValue("attack", out var attackSkill))
            PlayerStats.Attack = attackSkill.Level;

        if (state.Skills.TryGetValue("strength", out var strengthSkill))
            PlayerStats.Strength = strengthSkill.Level;

        if (state.Skills.TryGetValue("defense", out var defenseSkill))
            PlayerStats.Defense = defenseSkill.Level;

        if (state.Skills.TryGetValue("hitpoints", out var hpSkill))
        {
            int oldMax = PlayerStats.MaxHitpoints;
            PlayerStats.MaxHitpoints = 10 + (hpSkill.Level - 1);
            if (PlayerStats.MaxHitpoints > oldMax)
                PlayerStats.Hitpoints += (PlayerStats.MaxHitpoints - oldMax);
        }
    }
}

public class LootDrop
{
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
}
