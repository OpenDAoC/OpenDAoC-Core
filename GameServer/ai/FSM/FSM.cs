using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI
{
    public class FSM
    {
        protected Dictionary<eFSMStateType, FSMState> _states = [];
        protected FSMState _state;

        public FSM() { }

        public virtual void Add(FSMState state)
        {
            _states[state.StateType] = state;
        }

        public virtual void ClearStates()
        {
            _states.Clear();
        }

        public virtual FSMState GetState(eFSMStateType stateType)
        {
            _states.TryGetValue(stateType, out FSMState state);
            return state;
        }

        public virtual void SetCurrentState(eFSMStateType stateType)
        {
            _state?.Exit();
            _states.TryGetValue(stateType, out _state);
            _state?.Enter();
        }

        public virtual FSMState GetCurrentState()
        {
            return _state;
        }

        public virtual void Think()
        {
            _state?.Think();
        }

        public virtual void KillFSM() { }
    }
}
