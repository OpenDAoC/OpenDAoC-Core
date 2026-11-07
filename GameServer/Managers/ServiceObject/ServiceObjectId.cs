namespace DOL.GS
{
    public abstract class ServiceObjectId
    {
        public const int UNSET_ID = -1;
        public const int SLEEPING_ID = -2;

        private PendingAction _action = PendingAction.None;

        public abstract IServiceObject Owner { get; }

        public int Value { get; set; } = UNSET_ID;
        public bool IsRegistered => Value != UNSET_ID;
        public bool IsRunning => Value >= 0;

        public long NextTickMs
        {
            get;
            set
            {
                field = value;
                Reschedule();
            }
        }

        private protected ServiceObjectId() { }

        public abstract bool Register();
        public abstract bool Unregister();

        // Called when the wake time of an already registered object changes.
        // Non schedulable objects ignore it.
        protected virtual void Reschedule() { }

        public static ServiceObjectId Create<T>(T owner) where T : class, IServiceObject<T>
        {
            return T.CreateId(owner);
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
            Remove,
            Move
        }
    }

    // ID: Basic functionalities.
    public class ServiceObjectId<T> : ServiceObjectId
        where T : class, IServiceObject<T>
    {
        public override T Owner { get; }

        public ServiceObjectId(T owner)
        {
            Owner = owner;
        }

        public override bool Register()
        {
            return ServiceObjectStore.GetArray<T, ServiceObjectId<T>>().Add(this);
        }

        public override bool Unregister()
        {
            return ServiceObjectStore.GetArray<T, ServiceObjectId<T>>().Remove(this);
        }
    }

    // Schedulable ID: Adds the timing wheel's intrusive list node.
    public class SchedulableServiceObjectId<T> : ServiceObjectId<T>
        where T : class, ISchedulableServiceObject<T>
    {
        public SchedulableServiceObjectId<T> SleepNext { get; set; }
        public SchedulableServiceObjectId<T> SleepPrev { get; set; }
        public int SleepBucket { get; set; } = -1;

        public SchedulableServiceObjectId(T owner) : base(owner) { }

        public override bool Register()
        {
            return ServiceObjectStore.GetArray<T, SchedulableServiceObjectId<T>>().Add(this);
        }

        public override bool Unregister()
        {
            return ServiceObjectStore.GetArray<T, SchedulableServiceObjectId<T>>().Remove(this);
        }

        protected override void Reschedule()
        {
            ServiceObjectStore.GetArray<T, SchedulableServiceObjectId<T>>().Reschedule(this);
        }
    }

    // Sharded ID: Adds the shard routing index.
    public class ShardedServiceObjectId<T> : SchedulableServiceObjectId<T>
        where T : class, IShardedServiceObject<T>
    {
        public int ShardIndex { get; set; } = -1;

        public ShardedServiceObjectId(T owner) : base(owner) { }

        public override bool Register()
        {
            return ServiceObjectStore.GetArray<T, ShardedServiceObjectId<T>>().Add(this);
        }

        public override bool Unregister()
        {
            return ServiceObjectStore.GetArray<T, ShardedServiceObjectId<T>>().Remove(this);
        }

        protected override void Reschedule()
        {
            ServiceObjectStore.GetArray<T, ShardedServiceObjectId<T>>().Reschedule(this);
        }
    }
}
