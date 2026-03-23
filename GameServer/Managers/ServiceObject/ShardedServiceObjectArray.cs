using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ShardedServiceObjectArray<T> : ServiceObjectArrayBase<T>
        where T : class, IShardedServiceObject
    {
        private readonly List<SchedulableServiceObjectArray<T>> _shards;
        private readonly List<T>[] _shardsView;
        private readonly int[] _shardStartIndices;
        private int _roundRobinCounter;
        private int _totalValidCount;

        public override bool IsSharded => true;
        public override List<T> Items => null;
        public override int LastValidIndex => TotalValidCount - 1;
        public override List<T>[] Shards => _shardsView;
        public override int[] ShardStartIndices => _shardStartIndices;
        public override int TotalValidCount => _totalValidCount;

        public ShardedServiceObjectArray(int initialCapacity)
        {
            int shardCount = GameLoop.DegreeOfParallelism;
            _shards = new(shardCount);
            _shardsView = new List<T>[shardCount];

            int capacityPerShard = initialCapacity / shardCount;

            for (int i = 0; i < shardCount; i++)
            {
                _shards.Add(new(capacityPerShard));
                _shardsView[i] = _shards[i].Items;
            }

            _shardStartIndices = new int[shardCount];
        }

        private SchedulableServiceObjectArray<T> RouteItem(T item)
        {
            ShardedServiceObjectId id = item.ServiceObjectId;

            if (id.ShardIndex == -1)
                id.ShardIndex = (int) ((uint) Interlocked.Increment(ref _roundRobinCounter) % _shards.Count);

            return _shards[id.ShardIndex];
        }

        public override void Add(T item)
        {
            RouteItem(item).Add(item);
        }

        public override void Schedule(T item, long wakeUpTimeMs)
        {
            RouteItem(item).Schedule(item, wakeUpTimeMs);
        }

        public override void Remove(T item)
        {
            if (item.ServiceObjectId.ShardIndex == -1)
                return;

            RouteItem(item).Remove(item);
        }

        public override void Update(long now)
        {
            GameLoop.ExecuteForEach(_shards, _shards.Count, static shard => shard.Update(GameLoop.GameLoopTime));

            int currentTotal = 0;

            for (int i = 0; i < Shards.Length; i++)
            {
                _shardStartIndices[i] = currentTotal;
                currentTotal += _shards[i].LastValidIndex + 1;
            }

            _totalValidCount = currentTotal;
        }
    }
}
