using Core.GS.Enums;

namespace Core.GS.AI.States;

public abstract class FsmState
{
    public EFsmStateType StateType { get; protected set; }

    public FsmState() { }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Think();
}