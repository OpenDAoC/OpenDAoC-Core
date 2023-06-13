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

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<ECSGameTimer> list = EntityManager.GetAll<ECSGameTimer>(EntityManager.EntityType.Timer);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.Timer) + 1, i =>
            {
                ECSGameTimer timer = list[i];

                if (timer == null)
                    return;

                try
                {
                    if (timer.NextTick < tick)
                    {
                        long startTick = GameTimer.GetTickCount();
                        timer.Tick();
                        long stopTick = GameTimer.GetTickCount();

                        if ((stopTick - startTick) > 25)
                            log.Warn($"Long TimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in TimerService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    public class ECSGameTimer
    {
        public delegate int ECSTimerCallback(ECSGameTimer timer);

        public GameObject TimerOwner { get; set; }
        public ECSTimerCallback Callback { get; set; }
        public int Interval { get; set; }
        public long StartTick { get; set; }
        public long NextTick => StartTick + Interval;
        public bool IsAlive => EntityManagerId != EntityManager.UNSET_ID;
        public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);
        public int EntityManagerId { get; set; } = EntityManager.UNSET_ID;
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

            if (!IsAlive)
                EntityManager.Add(EntityManager.EntityType.Timer, this);
        }

        public void Stop()
        {
            if (IsAlive)
                EntityManager.Remove(EntityManager.EntityType.Timer, EntityManagerId);
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
