using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteStateMachine
{
    public class FSM
    {
        protected Dictionary<int, State> m_states = new Dictionary<int, State>();
        protected State m_currentState;

        public FSM()
        {

        }

        public void Add(int key, State state)
        {
            m_states.Add(key, state);
        }

        public void ClearStates()
        {
            m_states.Clear();
        }

        public State GetState(int key)
        {
            if (m_states.ContainsKey(key)){ return m_states[key];} 
            else { return null; }
            
        }

        public void SetCurrentState(State state)
        {
            if(m_currentState != null)
            {
                m_currentState.Exit();
            }

            m_currentState = state;
            if(m_currentState != null)
            {
                m_currentState.Enter();
            }
        }

        public State GetCurrentState()
        {
            return m_currentState;
        }

        public void Think()
        {
            if (m_currentState != null)
            {
                m_currentState.Think();
            }
        }

        public virtual void KillFSM()
        {
          
        }
    }
}
