using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BansheeIdle.Core;

/// <summary>
/// Crafting action that consumes ingredients and produces items
/// </summary>
public class CraftingAction : ISkillAction
{
    private readonly RecipeDef _recipe;
    private readonly GameDatabase _db;

    public string ActionId => _recipe.Id;
    public string SkillId => _recipe.Skill;
    public float TimeToComplete => _recipe.Time;
    public int RequiredLevel => _recipe.ReqLevel;
    public RecipeDef Recipe => _recipe;

    public event Action<string, int>? OnLevelUp;
    public event Action<RecipeDef>? OnCraftComplete;

    public CraftingAction(RecipeDef recipe, GameDatabase db)
    {
        _recipe = recipe;
        _db = db;
    }

    public bool CanPerform(GameState state)
    {
        // Check skill level
        if (!state.Skills.TryGetValue(_recipe.Skill, out var skill))
            return false;
        if (skill.Level < _recipe.ReqLevel)
            return false;

        // Check ingredients in inventory
        foreach (var ingredient in _recipe.Ingredients)
        {
            var item = state.Inventory.Find(i => i.ItemId == ingredient.ItemId);
            if (item == null || item.Quantity < ingredient.Quantity)
                return false;
        }

        return true;
    }

    public void OnComplete(GameState state, InventoryManager inventory)
    {
        // Consume ingredients
        foreach (var ingredient in _recipe.Ingredients)
        {
            inventory.RemoveFromInventory(state, ingredient.ItemId, ingredient.Quantity);
        }

        // Grant XP
        if (state.Skills.TryGetValue(_recipe.Skill, out var skill))
        {
            int oldLevel = skill.Level;
            skill.Experience += _recipe.Xp;
            int newLevel = skill.Level;
            if (newLevel > oldLevel)
                OnLevelUp?.Invoke(_recipe.Skill, newLevel);
        }

        // Add output to inventory
        if (!string.IsNullOrEmpty(_recipe.OutputItem))
        {
            inventory.AddToInventory(state, _recipe.OutputItem, _recipe.OutputQuantity);
        }

        OnCraftComplete?.Invoke(_recipe);
    }
}

/// <summary>
/// Manages crafting operations
/// </summary>
public class CraftingManager
{
    private CraftingAction? _currentAction;
    private float _timer;
    private readonly InventoryManager _inventory;

    public CraftingAction? CurrentAction => _currentAction;
    public float Timer => _timer;
    public float Progress => _currentAction != null ? _timer / _currentAction.TimeToComplete : 0f;

    public event Action<string, int>? OnLevelUp;
    public event Action<RecipeDef>? OnCraftComplete;

    public CraftingManager(InventoryManager inventory)
    {
        _inventory = inventory;
    }

    /// <summary>
    /// Start crafting a recipe
    /// </summary>
    public bool StartCrafting(CraftingAction action, GameState state)
    {
        if (!action.CanPerform(state))
            return false;

        _currentAction = action;
        _timer = 0;

        // Wire up events
        action.OnLevelUp += (skill, level) => OnLevelUp?.Invoke(skill, level);
        action.OnCraftComplete += (recipe) => OnCraftComplete?.Invoke(recipe);

        return true;
    }

    /// <summary>
    /// Stop current crafting
    /// </summary>
    public void StopCrafting()
    {
        _currentAction = null;
        _timer = 0;
    }

    /// <summary>
    /// Process crafting tick
    /// </summary>
    public void Tick(float delta, GameState state)
    {
        if (_currentAction == null) return;

        _timer += delta;
        if (_timer >= _currentAction.TimeToComplete)
        {
            _currentAction.OnComplete(state, _inventory);
            _timer = 0;

            // Check if we can still perform the action (ingredients might be depleted)
            if (!_currentAction.CanPerform(state))
                StopCrafting();
        }
    }

    /// <summary>
    /// Check if a recipe can be crafted
    /// </summary>
    public bool CanCraft(RecipeDef recipe, GameState state)
    {
        var action = new CraftingAction(recipe, null!);
        return action.CanPerform(state);
    }
}
