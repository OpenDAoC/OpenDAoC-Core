using FiniteStateMachine;

public abstract class State
{
    protected FSM m_fsm;
    public State(FSM fsm)
    {
        m_fsm = fsm;
    }

    public void Enter()
    {

    }
    public void Exit()
    {

    }

    public void Think()
    {

    }
}
