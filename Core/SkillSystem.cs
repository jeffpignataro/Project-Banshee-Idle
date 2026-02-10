namespace BansheeIdle.Core;

public interface ISkillAction
{
    string ActionId { get; }
    string SkillId { get; }
    float TimeToComplete { get; }
    int RequiredLevel { get; }
    bool CanPerform(GameState state);
    void OnComplete(GameState state, InventoryManager inventory);
}

/// <summary>
/// A generic data-driven skill action. Works for any gathering skill
/// by reading its parameters from an ActionDef.
/// </summary>
public class GatheringAction : ISkillAction
{
    private readonly ActionDef _def;
    private readonly GameDatabase _db;

    public string ActionId => _def.Id;
    public string SkillId => _def.Skill;
    public float TimeToComplete => _def.Time;
    public int RequiredLevel => _def.ReqLevel;

    public GatheringAction(ActionDef def, GameDatabase db)
    {
        _def = def;
        _db = db;
    }

    public bool CanPerform(GameState state)
    {
        if (!state.Skills.TryGetValue(_def.Skill, out var skill))
            return false;
        return skill.Level >= _def.ReqLevel;
    }

    public void OnComplete(GameState state, InventoryManager inventory)
    {
        // Grant XP
        if (state.Skills.TryGetValue(_def.Skill, out var skill))
        {
            int oldLevel = skill.Level;
            skill.Experience += _def.Xp;
            int newLevel = skill.Level;
            if (newLevel > oldLevel)
                OnLevelUp?.Invoke(_def.Skill, newLevel);
        }

        // Add loot to inventory
        if (!string.IsNullOrEmpty(_def.Loot))
        {
            inventory.AddToInventory(state, _def.Loot, 1);
        }

        OnActionComplete?.Invoke(_def);
    }

    public event Action<string, int>? OnLevelUp;
    public event Action<ActionDef>? OnActionComplete;
}

public class SkillManager
{
    private ISkillAction? _currentAction;
    private float _timer;
    private readonly InventoryManager _inventory;

    public ISkillAction? CurrentAction => _currentAction;
    public float Timer => _timer;
    public float Progress => _currentAction != null ? _timer / _currentAction.TimeToComplete : 0f;

    public event Action<string, int>? OnLevelUp;
    public event Action<ActionDef>? OnActionComplete;

    public SkillManager(InventoryManager inventory)
    {
        _inventory = inventory;
    }

    public bool SetAction(ISkillAction action, GameState state)
    {
        if (!action.CanPerform(state))
            return false;

        _currentAction = action;
        _timer = 0;

        // Wire up events from gathering actions
        if (action is GatheringAction ga)
        {
            ga.OnLevelUp += (skill, level) => OnLevelUp?.Invoke(skill, level);
            ga.OnActionComplete += (def) => OnActionComplete?.Invoke(def);
        }

        return true;
    }

    public void StopAction()
    {
        _currentAction = null;
        _timer = 0;
    }

    public void Tick(float delta, GameState state)
    {
        if (_currentAction == null) return;

        _timer += delta;
        if (_timer >= _currentAction.TimeToComplete)
        {
            _currentAction.OnComplete(state, _inventory);
            _timer = 0;

            // Check if we can still perform the action (level requirement may have changed)
            if (!_currentAction.CanPerform(state))
                StopAction();
        }
    }
}
