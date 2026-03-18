using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public class SchedulableServiceObjectArray<T> : ServiceObjectArray<T>
        where T : class, ISchedulableServiceObject
    {
        private readonly DrainArray<T> _itemsToSchedule = new();

        // Hierarchical timing wheel for O(1) suspended item scheduling.
        // Items sleeping longer than WHEEL_SIZE are parked in _farFuture.
        // 1 << 16 = 65,536 buckets.
        // 65,536 buckets * 50ms = 3,276,800ms = ~54.6 minutes.
        // If the game loop ticks faster than WHEEL_RESOLUTION_MS, items may be woken up
        private const int WHEEL_RESOLUTION_MS = 50;
        private const int WHEEL_BITS = 16;
        private const int WHEEL_SIZE = 1 << WHEEL_BITS;
        private const int WHEEL_MASK = WHEEL_SIZE - 1;

        private readonly List<T>[] _sleepWheel;
        private readonly Stack<List<T>> _listPool = new();
        private readonly PriorityQueue<T, long> _farFuture = new();
        private long _lastProcessedWheelTick = -1;
        private long _currentUpdateTick;

        public SchedulableServiceObjectArray(int capacity) : base(capacity)
        {
            _sleepWheel = new List<T>[WHEEL_SIZE];

            for (int i = 0; i < WHEEL_SIZE; i++)
                _sleepWheel[i] = new();
        }

        public override void Schedule(T item, long wakeUpTimeMs)
        {
            SchedulableServiceObjectId id = item.ServiceObjectId;
            long currentTick = GameLoop.GameLoopTime / WHEEL_RESOLUTION_MS;
            long targetTick = Math.Max(currentTick, wakeUpTimeMs / WHEEL_RESOLUTION_MS);
            id.ExpectedWakeTick = targetTick;
            _itemsToSchedule.Add(item);
        }

        protected override void ProcessItemsToRemove(long now)
        {
            _currentUpdateTick = now / WHEEL_RESOLUTION_MS;
            base.ProcessItemsToRemove(now);
        }

        protected override void ProcessItemsToAdd(long now)
        {
            WakeUpItems();

            if (_itemsToSchedule.Any)
                _itemsToSchedule.DrainTo(static (item, ctx) => ctx.ScheduleInternal(item), this);

            base.ProcessItemsToAdd(now);
        }

        protected override void AddToList(T item)
        {
            // Invalidate any sleep ticket. Handles both early manual re-adds and wake-ups.
            item.ServiceObjectId.ExpectedWakeTick = -1;
            base.AddToList(item);
        }

        private void ScheduleInternal(T item)
        {
            SchedulableServiceObjectId id = item.ServiceObjectId;

            if (!id.TryConsumeAction(ServiceObjectId.PendingAction.Schedule))
                return;

            long targetTick = Math.Max(id.ExpectedWakeTick, _lastProcessedWheelTick);

            if (targetTick - _lastProcessedWheelTick >= WHEEL_SIZE)
                _farFuture.Enqueue(item, targetTick);
            else
                _sleepWheel[(int) (targetTick & WHEEL_MASK)].Add(item);

            RemoveFromList(id);
            id.ExpectedWakeTick = targetTick;
            id.MoveTo(SchedulableServiceObjectId.SLEEPING_ID);
        }

        private void WakeUpItems()
        {
            if (_lastProcessedWheelTick == -1)
                _lastProcessedWheelTick = _currentUpdateTick;

            List<T> currentSwapList = _listPool.Count > 0 ? _listPool.Pop() : new();

            // Catch up on all missed ticks.
            while (_lastProcessedWheelTick <= _currentUpdateTick)
            {
                int bucketIndex = (int) (_lastProcessedWheelTick & WHEEL_MASK);
                List<T> bucket = _sleepWheel[bucketIndex];

                if (bucket.Count > 0)
                {
                    _sleepWheel[bucketIndex] = currentSwapList;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        T item = bucket[i];
                        SchedulableServiceObjectId id = item.ServiceObjectId;

                        // Do not wake if it's no longer sleeping or has a pending action.
                        if (id.ExpectedWakeTick != _lastProcessedWheelTick ||
                            !id.IsSleeping ||
                            id.PeekAction() is not ServiceObjectId.PendingAction.None)
                        {
                            continue;
                        }

                        AddToList(item);
                        id.MoveTo(LastValidIndex);
                    }

                    bucket.Clear();
                    currentSwapList = bucket;
                }

                _lastProcessedWheelTick++;

                // If the lowest bits are 0, we just completed a full cycle of the wheel.
                if ((_lastProcessedWheelTick & WHEEL_MASK) == 0)
                {
                    while (_farFuture.TryPeek(out T item, out long targetTick))
                    {
                        SchedulableServiceObjectId id = item.ServiceObjectId;

                        // Lazy deletion. Discard items that were removed, unset, or rescheduled.
                        if (id.PeekAction() is ServiceObjectId.PendingAction.Remove ||
                            id.Value == ServiceObjectId.UNSET_ID ||
                            id.ExpectedWakeTick != targetTick)
                        {
                            _farFuture.Dequeue();
                            continue;
                        }

                        // Check if remaining items are too far in the future.
                        if (targetTick - _lastProcessedWheelTick >= WHEEL_SIZE)
                            break;

                        _sleepWheel[(int) (targetTick & WHEEL_MASK)].Add(item);
                        _farFuture.Dequeue();
                    }
                }
            }

            _listPool.Push(currentSwapList);
        }
    }
}
