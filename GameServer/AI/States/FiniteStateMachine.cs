using System.Collections.Generic;
using Core.GS.Enums;

namespace Core.GS.AI;

public class FiniteStateMachine
{
    protected Dictionary<EFsmStateType, FsmState> _states = new();
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

    public virtual FsmState GetState(EFsmStateType stateType)
    {
        _states.TryGetValue(stateType, out FsmState state);
        return state;
    }

    public virtual void SetCurrentState(EFsmStateType stateType)
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