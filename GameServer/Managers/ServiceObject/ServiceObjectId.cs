namespace DOL.GS
{
    public class ServiceObjectId
    {
        public const int UNSET_ID = -1;
        public const int SLEEPING_ID = -2;

        private PendingAction _action = PendingAction.None;

        public IServiceObject Owner { get; }
        public ServiceObjectType Type { get; }
        public int Value { get; set; } = UNSET_ID;
        public bool IsRegistered => Value != UNSET_ID;
        public bool IsRunning => Value >= 0;

        // Schedulable properties.
        public long NextTickMs
        {
            get;
            set
            {
                field = value;

                // This prevents constructors/initializers from prematurely adding the object.
                if (IsRegistered)
                    ServiceObjectStore.Add(Owner);
            }
        }
        public ServiceObjectId SleepNext { get; set; }
        public ServiceObjectId SleepPrev  { get; set; }
        public int SleepBucket { get; set; } = -1;

        // Sharded properties.
        public int ShardIndex { get; set; } = -1;

        public ServiceObjectId(IServiceObject owner, ServiceObjectType type)
        {
            Owner = owner;
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

        public void Unset()
        {
            _action = PendingAction.None;
            Value = UNSET_ID;
        }

        public enum PendingAction
        {
            None,
            Add,
            Remove
        }
    }
}
