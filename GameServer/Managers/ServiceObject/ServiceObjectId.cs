namespace DOL.GS
{
    public class ServiceObjectId
    {
        public const int UNSET_ID = -1;

        private PendingAction _action = PendingAction.None;

        public int Value { get; private set; } = UNSET_ID;
        public ServiceObjectType Type { get; }

        public bool IsRegistered => Value != UNSET_ID;
        public bool IsRunning => Value >= 0;
        public bool IsDormant => IsRegistered && !IsRunning;

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

        public virtual void MoveTo(int index)
        {
            Value = index;
        }

        public void Unset()
        {
            _action = PendingAction.None;
            MoveTo(UNSET_ID); // Ensure MoveTo overrides are called.
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
