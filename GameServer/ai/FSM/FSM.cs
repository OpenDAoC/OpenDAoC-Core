using System.Collections.Generic;
using DOL.AI;
using DOL.GS;

public class FSM
{
    private Dictionary<eFSMStateType, FSMState> _states = new();
    private Dictionary<eFSMStateType, long> _stateLastThinkTimes = new();
    private FSMState _currentState;
    private bool _wasStateChanged;

    public FSM()
    {
        Initialize();
    }

    public void Add(FSMState state)
    {
        if (state == null)
            return;

        _states[state.StateType] = state;
        _stateLastThinkTimes[state.StateType] = 0;
    }

    public void ClearStates()
    {
        _states.Clear();
        _stateLastThinkTimes.Clear();
        Initialize();
    }

    public FSMState GetState(eFSMStateType stateType)
    {
        _states.TryGetValue(stateType, out FSMState state);
        return state;
    }

    public void SetCurrentState(eFSMStateType stateType)
    {
        if (_currentState.StateType == stateType)
            return;

        _currentState.Exit();

        if (!_states.TryGetValue(stateType, out _currentState))
            _currentState = NullState.Instance;

        _currentState.Enter();
        _wasStateChanged = true;
    }

    public FSMState GetCurrentState()
    {
        return _currentState;
    }

    public void Think()
    {
        long now = GameLoop.GameLoopTime;

        do
        {
            _stateLastThinkTimes[_currentState.StateType] = now;
            _wasStateChanged = false;
            _currentState.Think();
        } while (_wasStateChanged && _stateLastThinkTimes[_currentState.StateType] != now);
    }

    private void Initialize()
    {
        FSMState nullState = NullState.Instance;
        Add(nullState);
        _currentState = nullState;
    }

    private class NullState : FSMState
    {
        public override eFSMStateType StateType => eFSMStateType.NULL;
        public static NullState Instance { get; } = new();

        private NullState() { }

        public override void Enter() { }
        public override void Exit() { }
        public override void Think() { }
    }
}
