using System;
using System.Runtime.CompilerServices;

namespace DOL.GS
{
    public class SchedulableServiceObjectArray<T> : ServiceObjectArray<T>
        where T : class, ISchedulableServiceObject
    {
        // Circular timing wheel with a priority queue for distant scheduled items.

        // Scheduling uses floor division, so items may wake up early.
        // Actual wake-up time also depends on the game loop's update frequency.
        private const int WHEEL_RESOLUTION_MS = 30;

        // 1 << 16 = 65,536 buckets.
        // 65,536 buckets * 30ms = 3,276,800ms = ~32.8 minutes.
        private const int WHEEL_BITS = 16;
        private const int WHEEL_SIZE = 1 << WHEEL_BITS;
        private const int WHEEL_MASK = WHEEL_SIZE - 1;

        private readonly ServiceObjectId[] _heads = new ServiceObjectId[WHEEL_SIZE];
        private long _lastProcessedWheelTick = -1;

        public SchedulableServiceObjectArray(int capacity) : base(capacity) { }

        private void Link(ServiceObjectId id, long tick)
        {
            int bucket = (int) (tick & WHEEL_MASK);
            ServiceObjectId head = _heads[bucket];
            id.SleepPrev = null;
            id.SleepNext = head;
            head?.SleepPrev = id;
            _heads[bucket] = id;
            id.SleepBucket = bucket;
        }

        private void Unlink(ServiceObjectId id)
        {
            ServiceObjectId prev = id.SleepPrev;
            ServiceObjectId next = id.SleepNext;

            if (prev != null)
                prev.SleepNext = next;
            else
                _heads[id.SleepBucket] = next;

            next?.SleepPrev = prev;
            id.SleepPrev = id.SleepNext = null;
            id.SleepBucket = -1;
        }

        private void WakeUpItems()
        {
            long currentUpdateTick = _currentUpdateNow / WHEEL_RESOLUTION_MS;

            if (_lastProcessedWheelTick == -1)
                _lastProcessedWheelTick = currentUpdateTick;

            // Catch up on all missed ticks.
            while (_lastProcessedWheelTick <= currentUpdateTick)
            {
                int bucket = (int) (_lastProcessedWheelTick & WHEEL_MASK);

                ServiceObjectId id = _heads[bucket];
                _heads[bucket] = null;

                while (id != null)
                {
                    ServiceObjectId next = id.SleepNext;
                    id.SleepNext = id.SleepPrev = null;
                    id.SleepBucket = -1;

                    if (id.PeekAction() is ServiceObjectId.PendingAction.None)
                    {
                        long target = id.NextTickMs / WHEEL_RESOLUTION_MS;

                        if (target > _lastProcessedWheelTick)
                            Link(id, Math.Min(target, _lastProcessedWheelTick + WHEEL_SIZE - 1));
                        else
                            AddToList(Unsafe.As<T>(id.Owner)); // Safety ensured by ServiceObjectStore.
                    }

                    id = next;
                }

                _lastProcessedWheelTick++;
            }
        }

        protected override bool TryPrepareAdd(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            // Already active, nothing pending, and it will still be in the current wheel bucket
            // (or past due) at the next update: AddInternal would end in a no-op AddToList anyway.
            if (id.IsRunning &&
                id.PeekAction() is ServiceObjectId.PendingAction.None &&
                id.NextTickMs / WHEEL_RESOLUTION_MS <= _currentUpdateNow / WHEEL_RESOLUTION_MS)
            {
                return false;
            }

            return id.TrySetAction(ServiceObjectId.PendingAction.Add);
        }

        protected override void ProcessItemsToAdd()
        {
            WakeUpItems();
            base.ProcessItemsToAdd();
        }

        protected override void RemoveInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!TryPrepareRemoveFromList(id))
                return;

            if (id.SleepBucket >= 0)
                Unlink(id);

            RemoveFromList(id);
            id.Unset();
        }

        protected override void AddInternal(T item)
        {
            ServiceObjectId id = item.ServiceObjectId;

            if (!TryPrepareAddToList(id))
                return;

            if (id.SleepBucket >= 0)
                Unlink(id);

            long wakeUp = id.NextTickMs;

            if (wakeUp - _currentUpdateNow <= 0)
            {
                AddToList(item);
                return;
            }

            long targetTick = wakeUp / WHEEL_RESOLUTION_MS;

            if (targetTick < _lastProcessedWheelTick)
            {
                AddToList(item);
                return;
            }

            RemoveFromList(id);
            id.Value = ServiceObjectId.SLEEPING_ID;
            Link(id, Math.Min(targetTick, _lastProcessedWheelTick + WHEEL_SIZE - 1));
        }
    }
}
