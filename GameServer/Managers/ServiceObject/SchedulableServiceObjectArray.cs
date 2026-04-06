using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public class SchedulableServiceObjectArray<T> : ServiceObjectArray<T>
        where T : class, ISchedulableServiceObject
    {
        private readonly DrainArray<ScheduleRequest> _itemsToSchedule = new();

        // Hierarchical timing wheel for O(1) suspended item scheduling.
        // Items sleeping longer than WHEEL_SIZE are parked in _farFuture.
        // 1 << 16 = 65,536 buckets.
        // 65,536 buckets * 50ms = 3,276,800ms = ~54.6 minutes.
        // If the game loop ticks faster than WHEEL_RESOLUTION_MS, items may be woken up
        private const int WHEEL_RESOLUTION_MS = 50;
        private const int WHEEL_BITS = 16;
        private const int WHEEL_SIZE = 1 << WHEEL_BITS;
        private const int WHEEL_MASK = WHEEL_SIZE - 1;
        private const int SKIP_SCHEDULE_THRESHOLD_TICK = 30;

        private readonly List<SleepTicket>[] _sleepWheel;
        private readonly Stack<List<SleepTicket>> _listPool = new();
        private readonly PriorityQueue<SleepTicket, long> _farFuture = new();
        private long _lastProcessedWheelTick = -1;
        private long _currentUpdateTick;

        public SchedulableServiceObjectArray(int capacity) : base(capacity)
        {
            _sleepWheel = new List<SleepTicket>[WHEEL_SIZE];

            for (int i = 0; i < WHEEL_SIZE; i++)
                _sleepWheel[i] = new();
        }

        public override void Schedule(T item, long wakeUpTimeMs)
        {
            long now = GameLoop.GameLoopTime;
            SchedulableServiceObjectId id = item.ServiceObjectId;

            // Bypass the wheel entirely if wakeUpTimeMs is close.
            if (wakeUpTimeMs - now <= SKIP_SCHEDULE_THRESHOLD_TICK * GameLoop.TickDuration)
            {
                if (id.IsRunning)
                {
                    id.TryConsumeAction(ServiceObjectId.PendingAction.Schedule);
                    return;
                }

                if (id.TrySetAction(ServiceObjectId.PendingAction.Add))
                    Add(item);

                return;
            }

            long currentTick = now / WHEEL_RESOLUTION_MS;
            long targetTick = Math.Max(currentTick, wakeUpTimeMs / WHEEL_RESOLUTION_MS);
            _itemsToSchedule.Add(new(item, targetTick));
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
                _itemsToSchedule.DrainTo(static (req, ctx) => ctx.ScheduleInternal(req), this);

            base.ProcessItemsToAdd(now);
        }

        private void ScheduleInternal(ScheduleRequest request)
        {
            T item = request.Item;
            SchedulableServiceObjectId id = item.ServiceObjectId;
            long targetTick = request.TargetTick;

            if (!id.TryConsumeAction(ServiceObjectId.PendingAction.Schedule) ||
                targetTick < _lastProcessedWheelTick)
            {
                return;
            }

            RemoveFromList(id);
            id.MoveTo(SchedulableServiceObjectId.SLEEPING_ID);
            SleepTicket ticket = new(item, id.SleepToken);

            if (targetTick - _lastProcessedWheelTick >= WHEEL_SIZE)
                _farFuture.Enqueue(ticket, targetTick);
            else
                _sleepWheel[(int) (targetTick & WHEEL_MASK)].Add(ticket);
        }

        private void WakeUpItems()
        {
            if (_lastProcessedWheelTick == -1)
                _lastProcessedWheelTick = _currentUpdateTick;

            List<SleepTicket> currentSwapList = _listPool.Count > 0 ? _listPool.Pop() : new();

            // Catch up on all missed ticks.
            while (_lastProcessedWheelTick <= _currentUpdateTick)
            {
                int bucketIndex = (int) (_lastProcessedWheelTick & WHEEL_MASK);
                List<SleepTicket> bucket = _sleepWheel[bucketIndex];

                if (bucket.Count > 0)
                {
                    _sleepWheel[bucketIndex] = currentSwapList;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        SleepTicket ticket = bucket[i];
                        T item = ticket.Item;
                        SchedulableServiceObjectId id = item.ServiceObjectId;

                        // Do not wake if the sleep token changed or if there's a pending action.
                        if (id.SleepToken != ticket.Token ||
                            id.PeekAction() is not ServiceObjectId.PendingAction.None)
                        {
                            continue;
                        }

                        AddToList(item);
                    }

                    bucket.Clear();
                    currentSwapList = bucket;
                }

                _lastProcessedWheelTick++;

                // If the lowest bits are 0, we just completed a full cycle of the wheel.
                if ((_lastProcessedWheelTick & WHEEL_MASK) == 0)
                {
                    while (_farFuture.TryPeek(out SleepTicket ticket, out long targetTick))
                    {
                        T item = ticket.Item;
                        SchedulableServiceObjectId id = item.ServiceObjectId;

                        // Lazy deletion.
                        if (id.SleepToken != ticket.Token ||
                            id.PeekAction() is ServiceObjectId.PendingAction.Remove)
                        {
                            _farFuture.Dequeue();
                            continue;
                        }

                        // Check if remaining items are too far in the future.
                        if (targetTick - _lastProcessedWheelTick >= WHEEL_SIZE)
                            break;

                        _sleepWheel[(int) (targetTick & WHEEL_MASK)].Add(ticket);
                        _farFuture.Dequeue();
                    }
                }
            }

            _listPool.Push(currentSwapList);
        }

        private readonly record struct ScheduleRequest(T Item, long TargetTick);

        private readonly record struct SleepTicket(T Item, long Token);
    }
}
