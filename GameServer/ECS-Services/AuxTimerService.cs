using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace DOL.GS
{
    public class AuxTimerService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "AuxTimerService";

        public static void Tick(long tick)
        {
            // Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<AuxECSGameTimer> list = EntityManager.UpdateAndGetAll<AuxECSGameTimer>(EntityManager.EntityType.AuxTimer, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                AuxECSGameTimer timer = list[i];

                if (timer == null)
                    return;

                try
                {
                    if (timer.NextTick < tick)
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        timer.Tick();
                        long stopTick = GameLoop.GetCurrentTime();

                        if ((stopTick - startTick) > 25)
                            log.Warn($"Long AuxTimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in AuxTimerService: {e}");
                }
            });

            // Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    public class AuxECSGameTimer : IManagedEntity
    {
        /// <summary>
        /// This delegate is the callback function for the ECS Timer
        /// </summary>
        public delegate int AuxECSTimerCallback(AuxECSGameTimer timer);

        public GameObject TimerOwner { get; set; }
        public AuxECSTimerCallback Callback { get; set; }
        public int Interval { get; set; }
        public long StartTick { get; set; }
        public long NextTick => StartTick + Interval;
        public bool IsAlive { get; set; }
        public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);
        public EntityManagerId EntityManagerId { get; set; } = new();
        private PropertyCollection _properties;

        public AuxECSGameTimer(GameObject target)
        {
            TimerOwner = target;
        }

        public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback)
        {
            TimerOwner = target;
            Callback = callback;
        }

        public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback, int interval)
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
            StartTick = AuxGameLoop.GameLoopTime;
            Interval = interval;

            if (EntityManager.Add(EntityManager.EntityType.AuxTimer, this))
                IsAlive = true;
        }

        public void Stop()
        {
            if (EntityManager.Remove(EntityManager.EntityType.AuxTimer, this))
               IsAlive = false;
        }

        public void Tick()
        {
            StartTick = AuxGameLoop.GameLoopTime;

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
