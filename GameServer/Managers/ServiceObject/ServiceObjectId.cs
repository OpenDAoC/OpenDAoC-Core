namespace DOL.GS
{
    public class ServiceObjectId
    {
        public const int UNSET_ID = -1;

        private PendingAction _action = PendingAction.None;

        public int Value { get; private set; } = UNSET_ID;
        public ServiceObjectType Type { get; }

        public bool IsActive => Value >= 0;

        public ServiceObjectId(ServiceObjectType type)
        {
            Type = type;
        }

        public bool TrySetAction(PendingAction action)
        {
            if (_action == action)
                return false;

            _action = action;
            return true;
        }

        public bool TryConsumeAction(PendingAction expectedAction)
        {
            if (_action != expectedAction)
                return false;

            _action = PendingAction.None;
            return true;
        }

        public PendingAction PeekAction()
        {
            return _action;
        }

        public void MoveTo(int index)
        {
            Value = index;
        }

        public virtual void Unset()
        {
            _action = PendingAction.None;
            Value = UNSET_ID;
        }

        public enum PendingAction
        {
            None,
            Add,
            Schedule,
            Remove
        }
    }
}
