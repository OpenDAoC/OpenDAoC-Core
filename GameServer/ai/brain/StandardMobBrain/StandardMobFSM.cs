using DOL.AI;
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

    public void Add(StandardMobState state)
    {
        m_states.Add((int) state.ID, state);
    }

    public StandardMobState GetState(eFSMStateType key)
    {
        return (StandardMobState)GetState((int) key);
    }

    public void SetCurrentState(eFSMStateType stateKey)
    {
        State state = m_states[(int)stateKey];
        if (state != null)
        {
            SetCurrentState(state);
        }
    }

    public new void Think()
    {
        base.Think();
    }

    public override void KillFSM()
    {
        SetCurrentState(eFSMStateType.DEAD);
        base.KillFSM();
    }
}



