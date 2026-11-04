using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public abstract class ServiceObjectArrayBase<T> : IServiceObjectArray
        where T : class, IServiceObject
    {
        protected long _currentUpdateNow;

        // Flat array API.
        public abstract List<T> Items { get; }
        public abstract int LastValidIndex { get; }

        // Sharded array API.
        public abstract List<T>[] Shards { get; }
        public abstract int[] ShardStartIndices { get; }
        public abstract int TotalValidCount { get; }

        protected abstract void Add(T item);
        protected abstract void Remove(T item);
        protected abstract ServiceObjectView<T> UpdateAndGetView();

        protected abstract bool TryPrepareAdd(T item);
        protected abstract bool TryPrepareRemove(T item);

        public virtual ServiceObjectView<T> UpdateAndGetView(long now)
        {
            _currentUpdateNow = now;
            return UpdateAndGetView();
        }

        protected static void Add(ServiceObjectArrayBase<T> target, T item)
        {
            target.Add(item);
        }

        protected static void Remove(ServiceObjectArrayBase<T> target, T item)
        {
            target.Remove(item);
        }

        protected static bool TryPrepareAdd(ServiceObjectArrayBase<T> target, T item)
        {
            return target.TryPrepareAdd(item);
        }

        protected static bool TryPrepareRemove(ServiceObjectArrayBase<T> target, T item)
        {
            return target.TryPrepareRemove(item);
        }

        bool IServiceObjectArray.Add(IServiceObject obj)
        {
            T item = (T) obj;

            if (!TryPrepareAdd(item))
                return false;

            Add(item);
            return true;
        }

        bool IServiceObjectArray.Remove(IServiceObject obj)
        {
            T item = (T) obj;

            if (!TryPrepareRemove(item))
                return false;

            Remove(item);
            return true;
        }
    }

    public readonly struct ServiceObjectView<T> where T : class, IServiceObject
    {
        public readonly bool IsSharded;
        public readonly int TotalValidCount;
        public readonly List<T> Items;
        public readonly List<T>[] Shards;
        public readonly int[] ShardStartIndices;

        public ServiceObjectView(List<T> items, int totalValidCount)
        {
            IsSharded = false;
            Items = items;
            TotalValidCount = totalValidCount;
            Shards = null;
            ShardStartIndices = null;
        }

        public ServiceObjectView(List<T>[] shards, int[] shardStartIndices, int totalValidCount)
        {
            IsSharded = true;
            Shards = shards;
            ShardStartIndices = shardStartIndices;
            TotalValidCount = totalValidCount;
            Items = null;
        }

        public void ExecuteForEach(Action<T> action)
        {
            if (TotalValidCount <= 0)
                return;

            if (IsSharded)
                GameLoop.ExecuteForEachSharded(Shards, ShardStartIndices, TotalValidCount, action);
            else
                GameLoop.ExecuteForEach(Items, TotalValidCount, action);
        }
    }
}
