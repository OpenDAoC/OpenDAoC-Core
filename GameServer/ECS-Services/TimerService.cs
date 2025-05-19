using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public class TimerService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(TimerService);
        private static List<ECSGameTimer> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = ServiceObjectStore.UpdateAndGetAll<ECSGameTimer>(ServiceObjectType.Timer, out int lastValidIndex);
            GameLoop.Work(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ECSGameTimer timer = _list[index];

            if (timer?.ServiceObjectId.IsSet != true)
                return;

            if (Diagnostics.CheckEntityCounts)
                Interlocked.Increment(ref _entityCount);

            try
            {
                if (ServiceUtils.ShouldTickAdjust(ref timer.NextTick))
                {
                    long startTick = GameLoop.GetCurrentTime();
                    timer.Tick();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Timer Callback: {timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
            }
        }
    }

    public class ECSGameTimer : IServiceObject
    {
        public delegate int ECSTimerCallback(ECSGameTimer timer);

        private long _nextTick;

        public GameObject Owner { get; }
        public ECSTimerCallback Callback { private get; set; }
        public MethodInfo CallbackInfo => Callback?.GetMethodInfo();
        public int Interval { get; set; }
        public ref long NextTick => ref _nextTick;
        public bool IsAlive { get; private set; }
        public int TimeUntilElapsed => (int) (_nextTick - GameLoop.GameLoopTime);
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.Timer);
        private PropertyCollection _properties;

        public ECSGameTimer(GameObject timerOwner)
        {
            Owner = timerOwner;
        }

        public ECSGameTimer(GameObject timerOwner, ECSTimerCallback callback)
        {
            Owner = timerOwner;
            Callback = callback;
        }

        public ECSGameTimer(GameObject timerOwner, ECSTimerCallback callback, int interval)
        {
            Owner = timerOwner;
            Callback = callback;
            Interval = interval;
            Start();
        }

        public void Start()
        {
            // Use half-second intervals by default.
            Start(Interval <= 0 ? 500 : Interval);
        }

        public void Start(int interval)
        {
            Interval = interval;
            _nextTick = GameLoop.GameLoopTime + interval;

            if (ServiceObjectStore.Add(this))
                IsAlive = true;
        }

        public void Stop()
        {
            if (ServiceObjectStore.Remove(this))
                IsAlive = false;
        }

        public void Tick()
        {
            if (Callback != null)
                Interval = Callback.Invoke(this);

            if (Interval == 0)
            {
                Stop();
                return;
            }

            _nextTick += Interval;
        }

        public PropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    lock(this)
                    {
                        _properties ??= new();
                    }
                }

                return _properties;
            }
        }
    }

    public abstract class ECSGameTimerWrapperBase : ECSGameTimer
    {
        public ECSGameTimerWrapperBase(GameObject owner) : base(owner)
        {
            Callback = new ECSTimerCallback(OnTick);
        }

        protected abstract int OnTick(ECSGameTimer timer);
    }
}
