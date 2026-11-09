namespace DOL.GS
{
    public interface IServiceObject
    {
        ServiceObjectId ServiceObjectId { get; }
    }

    public static class ServiceObjectFactory
    {
        public static ServiceObjectId CreateId<T>(T owner)
            where T : class, IServiceObject<T>
        {
            return T.CreateId(owner);
        }
    }

    public interface IServiceObject<TSelf> : IServiceObject
        where TSelf : class, IServiceObject<TSelf>
    {
        static virtual ServiceObjectId CreateId(TSelf owner)
        {
            return new ServiceObjectId<TSelf>(owner);
        }

        static virtual ServiceObjectArrayBase<TSelf> CreateArray(int capacity)
        {
            return new ServiceObjectArray<TSelf>(capacity);
        }
    }
 
    public interface ISchedulableServiceObject<TSelf> : IServiceObject<TSelf>
        where TSelf : class, ISchedulableServiceObject<TSelf>
    {
        static ServiceObjectId IServiceObject<TSelf>.CreateId(TSelf owner)
        {
            return new SchedulableServiceObjectId<TSelf>(owner);
        }

        static ServiceObjectArrayBase<TSelf> IServiceObject<TSelf>.CreateArray(int capacity)
        {
            return new SchedulableServiceObjectArray<TSelf>(capacity);
        }
    }

    public interface IShardedServiceObject<TSelf> : ISchedulableServiceObject<TSelf>
        where TSelf : class, IShardedServiceObject<TSelf>
    {
        static ServiceObjectId IServiceObject<TSelf>.CreateId(TSelf owner)
        {
            return new ShardedServiceObjectId<TSelf>(owner);
        }

        static ServiceObjectArrayBase<TSelf> IServiceObject<TSelf>.CreateArray(int capacity)
        {
            return new ShardedServiceObjectArray<TSelf>(capacity, GameLoopExecutor.Instance);
        }
    }
}
