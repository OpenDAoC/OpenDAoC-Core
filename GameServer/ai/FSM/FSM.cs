using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI
{
    public class FSM
    {
        protected Dictionary<eFSMStateType, FSMState> _states = [];
        protected FSMState _state;

        public FSM() { }

        public void Add(FSMState state)
        {
            _states[state.StateType] = state;
        }

        public void ClearStates()
        {
            _states.Clear();
        }

        public FSMState GetState(eFSMStateType stateType)
        {
            _states.TryGetValue(stateType, out FSMState state);
            return state;
        }

        public void SetCurrentState(eFSMStateType stateType)
        {
            _state?.Exit();
            _states.TryGetValue(stateType, out _state);
            _state?.Enter();
        }

        public FSMState GetCurrentState()
        {
            return _state;
        }

        public void Think()
        {
            _state?.Think();
        }
    }
}
