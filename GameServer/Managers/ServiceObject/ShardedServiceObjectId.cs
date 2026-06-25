using System.Threading;

namespace DOL.GS
{
    public class ShardedServiceObjectId : SchedulableServiceObjectId
    {
        private int _shardIndex = -1;

        public int ShardIndex
        {
            get => Volatile.Read(ref _shardIndex);
            set => Volatile.Write(ref _shardIndex, value);
        }

        public ShardedServiceObjectId(ServiceObjectType type) : base(type) { }

        public int TryAssignShardIndex(int shardIndex)
        {
            int currentShardIndex = Interlocked.CompareExchange(ref _shardIndex, shardIndex, -1);
            return currentShardIndex == -1 ? shardIndex : currentShardIndex;
        }
    }
}
