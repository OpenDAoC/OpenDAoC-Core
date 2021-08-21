using DOL.GS;
using FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




public class StandardMobFSM : FSM
{
    public StandardMobFSM() : base()
    {

    }

    public void Add(StandardMobFSMState state)
    {
        m_states.Add((int) state.ID, state);
    }

    public StandardMobFSMState GetState(StandardMobStateType key)
    {
        return (StandardMobFSMState)GetState((int) key);
    }

    public void SetCurrentState(StandardMobStateType stateKey)
    {
        State state = m_states[(int)stateKey];
        if (state != null)
        {
            SetCurrentState(state);
        }
    }
}

public enum StandardMobStateType
{
    IDLE = 0,
    WAKING_UP,
    AGGRO,
    ROAMING,
    RETURN_TO_SPAWN,
    PATROLLING,
    DEAD
}

public class StandardMobFSMState : State
{
    public StandardMobStateType ID { get { return _id; } }

    protected GameLiving _body = null;
    protected StandardMobStateType _id;

    public StandardMobFSMState(FSM fsm, GameLiving body) : base(fsm)
    {
        _body = body;
    }

    public void Enter()
    {
        base.Enter();
    }
    public void Exit()
    {
        base.Exit();
    }
    public void Think()
    {
        base.Think();
    }
}

public class StandardMobFSMState_IDLE : StandardMobFSMState
{
    public StandardMobFSMState_IDLE(FSM fsm, GameLiving living) : base(fsm, living)
    {
        _id = StandardMobStateType.IDLE;
    }

    public void Enter()
    {
        base.Enter();
    }

    public void Think()
    {
        //if DEAD, bail out of calc
        //if HP < 0, set state to DEAD

        //check for aggro
        //if aggro, set state to AGGRO

        base.Think();
    }
}


