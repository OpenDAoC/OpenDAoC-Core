using System.Collections.Generic;
using DOL.AI;
using DOL.GS;

public class FSM
{
    private Dictionary<eFSMStateType, FSMState> _states = new();
    private Dictionary<eFSMStateType, long> _stateLastThinkTimes = new();
    private FSMState _currentState;

    public FSM() { }

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
    }

    public FSMState GetState(eFSMStateType stateType)
    {
        _states.TryGetValue(stateType, out FSMState state);
        return state;
    }

    public void SetCurrentState(eFSMStateType stateType)
    {
        if (_currentState != null)
        {
            // Prevent unnecessary re-entry into the exact same state.
            if (_currentState.StateType == stateType)
                return;

            _currentState.Exit();
        }

        if (!_states.TryGetValue(stateType, out _currentState))
            return;

        _currentState.Enter();

        // Think immediately if NpcService is ticking and game loop time has advanced since the last think.
        if (!NpcService.IsTicking)
            return;

        long now = GameLoop.GameLoopTime;

        if (_stateLastThinkTimes[stateType] == now)
            return;

        _stateLastThinkTimes[stateType] = now;
        _currentState.Think();
    }

    public FSMState GetCurrentState()
    {
        return _currentState;
    }

    public void Think()
    {
        if (_currentState == null)
            return;

        _stateLastThinkTimes[_currentState.StateType] = GameLoop.GameLoopTime;
        _currentState.Think();
    }
}
