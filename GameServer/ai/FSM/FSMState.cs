using DOL.GS;

namespace DOL.AI
{
    public abstract class FSMState
    {
        public eFSMStateType StateType { get; }

        public FSMState(eFSMStateType stateType)
        {
            StateType = stateType;
        }

        public abstract void Enter();
        public abstract void Exit();
        public abstract void Think();
    }
}
