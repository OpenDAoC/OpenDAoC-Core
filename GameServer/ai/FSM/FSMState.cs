using DOL.GS;

namespace DOL.AI
{
    public abstract class FSMState
    {
        public abstract eFSMStateType StateType { get; }

        public FSMState() { }

        public abstract void Enter();
        public abstract void Exit();
        public abstract void Think();
    }
}
