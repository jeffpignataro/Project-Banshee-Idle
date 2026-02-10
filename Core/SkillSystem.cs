namespace BansheeIdle.Core;

public interface ISkillAction {
    string ActionId { get; }
    float TimeToComplete { get; }
    void OnComplete(GameState state);
}

public class WoodcuttingAction : ISkillAction {
    public string ActionId => "wc_tree";
    public float TimeToComplete => 3.0f;
    
    public void OnComplete(GameState state) {
        // Logic for adding logs to bank and giving XP
    }
}

public class SkillManager {
    private ISkillAction _currentAction;
    private float _timer;

    public void Tick(float delta, GameState state) {
        if (_currentAction == null) return;

        _timer += delta;
        if (_timer >= _currentAction.TimeToComplete) {
            _currentAction.OnComplete(state);
            _timer = 0;
        }
    }

    public void SetAction(ISkillAction action) {
        _currentAction = action;
        _timer = 0;
    }
}
