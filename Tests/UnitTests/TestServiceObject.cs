namespace DOL.GS.Tests
{
    internal interface ITestServiceObject<TSelf> : IServiceObject<TSelf>
        where TSelf : class, ITestServiceObject<TSelf> { }

    internal interface ITestSchedulableServiceObject<TSelf> : ISchedulableServiceObject<TSelf>
        where TSelf : class, ITestSchedulableServiceObject<TSelf> { }

    internal interface ITestShardedServiceObject<TSelf> : IShardedServiceObject<TSelf>
        where TSelf : class, ITestShardedServiceObject<TSelf>
    {
        static ServiceObjectArrayBase<TSelf> IServiceObject<TSelf>.CreateArray(int capacity)
        {
            return new ShardedServiceObjectArray<TSelf>(capacity, TestExecutor.Instance);
        }
    }

    internal sealed class TestServiceObject : ITestServiceObject<TestServiceObject>
    {
        public ServiceObjectId ServiceObjectId { get; }

        public TestServiceObject()
        {
            ServiceObjectId = ServiceObjectFactory.CreateId(this);
        }

        public bool Register()
        {
            return ServiceObjectId.Register();
        }

        public bool Unregister()
        {
            return ServiceObjectId.Unregister();
        }
    }

    internal sealed class TestSchedulableServiceObject : ITestSchedulableServiceObject<TestSchedulableServiceObject>
    {
        public ServiceObjectId ServiceObjectId { get; }
        public SchedulableServiceObjectId<TestSchedulableServiceObject> Id => (SchedulableServiceObjectId<TestSchedulableServiceObject>) ServiceObjectId;

        public long NextTickMs
        {
            get => ServiceObjectId.NextTickMs;
            set => ServiceObjectId.NextTickMs = value;
        }

        public TestSchedulableServiceObject()
        {
            ServiceObjectId = ServiceObjectFactory.CreateId(this);
        }

        public bool Register()
        {
            return ServiceObjectId.Register();
        }

        public bool Unregister()
        {
            return ServiceObjectId.Unregister();
        }
    }

    internal sealed class TestShardedServiceObject : ITestShardedServiceObject<TestShardedServiceObject>
    {
        public ServiceObjectId ServiceObjectId { get; }
        public ShardedServiceObjectId<TestShardedServiceObject> Id => (ShardedServiceObjectId<TestShardedServiceObject>) ServiceObjectId;

        public long NextTickMs
        {
            get => ServiceObjectId.NextTickMs;
            set => ServiceObjectId.NextTickMs = value;
        }

        public TestShardedServiceObject()
        {
            ServiceObjectId = ServiceObjectFactory.CreateId(this);
        }

        public bool Register()
        {
            return ServiceObjectId.Register();
        }

        public bool Unregister()
        {
            return ServiceObjectId.Unregister();
        }
    }

    // Intentionally never published in ServiceObjectStore.
    internal sealed class TestUnregisteredServiceObject : ITestServiceObject<TestUnregisteredServiceObject>
    {
        public ServiceObjectId ServiceObjectId { get; }

        public TestUnregisteredServiceObject()
        {
            ServiceObjectId = ServiceObjectFactory.CreateId(this);
        }

        public bool Register()
        {
            return ServiceObjectId.Register();
        }

        public bool Unregister()
        {
            return ServiceObjectId.Unregister();
        }
    }
}
