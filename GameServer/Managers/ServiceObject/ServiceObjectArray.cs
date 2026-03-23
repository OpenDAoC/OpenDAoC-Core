using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // Manages a contiguous array of active items.
    public class ServiceObjectArray<T> : ServiceObjectArrayBase<T>
        where T : class, IServiceObject
    {
        private readonly DrainArray<T> _itemsToAdd = new();
        private readonly DrainArray<T> _itemsToRemove = new();
        private bool _updating;
        private int _lastValidIndex = -1;

        public override bool IsSharded => false;
        public override List<T> Items { get; }
        public override int LastValidIndex => _lastValidIndex;
        public override List<T>[] Shards => null;
        public override int[] ShardStartIndices => null;
        public override int TotalValidCount => LastValidIndex + 1;

        public ServiceObjectArray(int capacity)
        {
            Items = new(capacity);
        }

        public override void Add(T item)
        {
            _itemsToAdd.Add(item);
        }

        public override void Schedule(T item, long wakeUpTimeMs) { }

        public override void Remove(T item)
        {
            _itemsToRemove.Add(item);
        }

        public override void Update(long now)
        {
            if (Interlocked.Exchange(ref _updating, true) != false)
                throw new InvalidOperationException($"{typeof(T)} is already being updated.");

            try
            {
                ProcessItemsToRemove(now);
                ProcessItemsToAdd(now);
            }
            finally
            {
                _updating = false;
            }
        }

        protected virtual void ProcessItemsToRemove(long now)
        {
            if (_itemsToRemove.Any)
                _itemsToRemove.DrainTo(static (item, ctx) => ctx.RemoveInternal(item), this);
        }

        protected virtual void ProcessItemsToAdd(long now)
        {
            if (_itemsToAdd.Any)
                _itemsToAdd.DrainTo(static (item, ctx) => ctx.AddInternal(item), this);
        }

        private void RemoveInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!id.TryConsumeAction(ServiceObjectId.PendingAction.Remove))
                return;

            RemoveFromList(id);
        }

        private void AddInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!id.TryConsumeAction(ServiceObjectId.PendingAction.Add))
                return;

            AddToList(item);
        }

        protected void RemoveFromList(ServiceObjectId id)
        {
            int idValue = id.Value;

            if (idValue >= 0 && idValue <= LastValidIndex)
            {
                // Swap the item being removed with the item at the end.
                if (idValue == LastValidIndex)
                    Items[LastValidIndex] = null;
                else
                {
                    T lastItem = Items[LastValidIndex];
                    Items[idValue] = lastItem;
                    lastItem.ServiceObjectId.MoveTo(idValue);
                    Items[LastValidIndex] = null;
                }

                _lastValidIndex--;
            }

            id.Unset();
        }

        protected virtual void AddToList(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (id.IsActive)
                return;

            if (++_lastValidIndex >= Items.Capacity)
            {
                int newCapacity = (int) (Items.Capacity * 1.2);

                if (Items.Capacity == newCapacity)
                    newCapacity++;

                Items.Resize(newCapacity);
            }

            if (LastValidIndex >= Items.Count)
                Items.Add(item);
            else
                Items[LastValidIndex] = item;

            id.MoveTo(LastValidIndex);
        }
    }
}
