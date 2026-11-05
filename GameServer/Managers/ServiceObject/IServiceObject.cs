namespace DOL.GS
{
    public interface IServiceObject
    {
        ServiceObjectId ServiceObjectId { get; }
    }

    public interface ISchedulableServiceObject : IServiceObject { }

    public interface IShardedServiceObject : ISchedulableServiceObject { }
}
