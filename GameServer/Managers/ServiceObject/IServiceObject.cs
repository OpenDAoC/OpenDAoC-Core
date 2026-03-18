namespace DOL.GS
{
    public interface IServiceObject
    {
        ServiceObjectId ServiceObjectId { get; }
    }

    public interface ISchedulableServiceObject : IServiceObject
    {
        new SchedulableServiceObjectId ServiceObjectId { get; }
    }

    public interface IShardedServiceObject : ISchedulableServiceObject
    {
        new ShardedServiceObjectId ServiceObjectId { get; }
    }
}
