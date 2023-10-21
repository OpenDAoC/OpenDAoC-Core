using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI;

public class FiniteStateMachine
{
    protected Dictionary<EFSMStateType, FsmState> _states = new();
    protected FsmState _state;

    public FiniteStateMachine() { }

    public virtual void Add(FsmState state)
    {
        _states.Add(state.StateType, state);
    }

    public virtual void ClearStates()
    {
        _states.Clear();
    }

    public virtual FsmState GetState(EFSMStateType stateType)
    {
        _states.TryGetValue(stateType, out FsmState state);
        return state;
    }

    public virtual void SetCurrentState(EFSMStateType stateType)
    {
        if (_state != null)
            _state.Exit();

        _states.TryGetValue(stateType, out _state);

        if (_state != null)
            _state.Enter();
    }

    public virtual FsmState GetCurrentState()
    {
        return _state;
    }

    public virtual void Think()
    {
        if (_state != null)
            _state.Think();
    }

    public virtual void KillFSM() { }
}