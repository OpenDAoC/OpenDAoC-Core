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
        private const string SERVICE_NAME = "TimerService";

        private static int _nonNullTimerCount;
        private static int _nullTimerCount;

        public static int DebugTickCount { get; set; } // Will print active brain count/array size info for debug purposes if superior to 0.
        private static bool Debug => DebugTickCount > 0;

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (Debug)
            {
                _nonNullTimerCount = 0;
                _nullTimerCount = 0;
            }

            List<ECSGameTimer> list = EntityManager.UpdateAndGetAll<ECSGameTimer>(EntityManager.EntityType.Timer, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                ECSGameTimer timer = list[i];

                if (timer == null)
                {
                    if (Debug)
                        Interlocked.Increment(ref _nullTimerCount);

                    return;
                }

                if (Debug)
                    Interlocked.Increment(ref _nonNullTimerCount);

                try
                {
                    if (timer.NextTick < tick)
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        timer.Tick();
                        long stopTick = GameLoop.GetCurrentTime();

                        if ((stopTick - startTick) > 25)
                            log.Warn($"Long TimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in TimerService: {e}");
                }
            });

            // Output debug info.
            if (Debug)
            {
                log.Debug($"==== Non-null timers in EntityManager array: {_nonNullTimerCount} | Null timers: {_nullTimerCount} | Total size: {list.Count} ====");
                DebugTickCount--;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    public class ECSGameTimer : IManagedEntity
    {
        public delegate int ECSTimerCallback(ECSGameTimer timer);

        public GameObject TimerOwner { get; set; }
        public ECSTimerCallback Callback { get; set; }
        public int Interval { get; set; }
        public long StartTick { get; set; }
        public long NextTick => StartTick + Interval;
        public bool IsAlive { get; set; }
        public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);
        public EntityManagerId EntityManagerId { get; set; } = new();
        private PropertyCollection _properties;

        public ECSGameTimer(GameObject target)
        {
            TimerOwner = target;
        }

        public ECSGameTimer(GameObject target, ECSTimerCallback callback)
        {
            TimerOwner = target;
            Callback = callback;
        }

        public ECSGameTimer(GameObject target, ECSTimerCallback callback, int interval)
        {
            TimerOwner = target;
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
            StartTick = GameLoop.GameLoopTime;
            Interval = interval;
            IsAlive = true;

            if (EntityManager.Add(EntityManager.EntityType.Timer, this))
                IsAlive = true;
        }

        public void Stop()
        {
            if (EntityManager.Remove(EntityManager.EntityType.Timer, this))
               IsAlive = false;
        }

        public void Tick()
        {
            StartTick = GameLoop.GameLoopTime;

            if (Callback != null)
                Interval = Callback.Invoke(this);

            if (Interval == 0)
                Stop();
        }

        public PropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    lock (this)
                    {
                        if (_properties == null)
                        {
                            PropertyCollection properties = new PropertyCollection();
                            Thread.MemoryBarrier();
                            _properties = properties;
                        }
                    }
                }

                return _properties;
            }
        }
    }
}
