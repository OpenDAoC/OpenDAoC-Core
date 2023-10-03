using DOL.GS;

namespace DOL.AI
{
    public abstract class FSMState
    {
        public eFSMStateType StateType { get; protected set; }

        public FSMState() { }

        public abstract void Enter();
        public abstract void Exit();
        public abstract void Think();
    }
}
