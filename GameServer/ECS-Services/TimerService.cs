using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public class TimerService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(TimerService);

        private static List<ECSGameTimer> _list;
        private static int _nonNullTimerCount;
        private static int _nullTimerCount;

        public static int DebugTickCount { get; set; } // Will print active brain count/array size info for debug purposes if superior to 0.
        private static bool Debug => DebugTickCount > 0;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (Debug)
            {
                _nonNullTimerCount = 0;
                _nullTimerCount = 0;
            }

            _list = EntityManager.UpdateAndGetAll<ECSGameTimer>(EntityManager.EntityType.Timer, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);

            if (Debug)
            {
                log.Debug($"==== Non-null timers in EntityManager array: {_nonNullTimerCount} | Null timers: {_nullTimerCount} | Total size: {_list.Count} ====");
                DebugTickCount--;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ECSGameTimer timer = _list[index];

            if (timer?.EntityManagerId.IsSet != true)
            {
                if (Debug)
                    Interlocked.Increment(ref _nullTimerCount);

                return;
            }

            if (Debug)
                Interlocked.Increment(ref _nonNullTimerCount);

            try
            {
                if (ServiceUtils.ShouldTickAdjust(ref timer.NextTick))
                {
                    long startTick = GameLoop.GetCurrentTime();
                    timer.Tick();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Timer Callback: {timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
            }
        }
    }

    public class ECSGameTimer : IManagedEntity
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
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.Timer, false);
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

            if (EntityManager.Add(this))
                IsAlive = true;
        }

        public void Stop()
        {
            if (EntityManager.Remove(this))
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
