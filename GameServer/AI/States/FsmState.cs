using DOL.GS;

namespace DOL.AI;

public abstract class FsmState
{
    public EFSMStateType StateType { get; protected set; }

    public FsmState() { }

    public abstract void Enter();
    public abstract void Exit();
    public abstract void Think();
}