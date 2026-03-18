namespace DOL.GS
{
    public class ShardedServiceObjectId : SchedulableServiceObjectId
    {
        public int ShardIndex { get; set; } = -1;

        public ShardedServiceObjectId(ServiceObjectType type) : base(type) { }
    }
}
