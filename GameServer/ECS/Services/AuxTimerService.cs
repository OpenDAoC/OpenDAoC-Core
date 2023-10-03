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
        private const string SERVICE_NAME = nameof(AuxTimerService);

        public static void Tick(long tick)
        {
            // Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<AuxECSGameTimer> list = EntityManager.UpdateAndGetAll<AuxECSGameTimer>(EntityManager.EntityType.AuxTimer, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                AuxECSGameTimer timer = list[i];

                try
                {
                    if (timer?.EntityManagerId.IsSet != true)
                        return;

                    if (timer.NextTick < tick)
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        timer.Tick();
                        long stopTick = GameLoop.GetCurrentTime();

                        if (stopTick - startTick > 25)
                            log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                    }
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
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

        public GameObject Owner { get; set; }
        public AuxECSTimerCallback Callback { get; set; }
        public int Interval { get; set; }
        public long StartTick { get; set; }
        public long NextTick => StartTick + Interval;
        public bool IsAlive { get; set; }
        public int TimeUntilElapsed => (int) (StartTick + Interval - AuxGameLoop.GameLoopTime);
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.AuxTimer, false);
        private PropertyCollection _properties;

        public AuxECSGameTimer(GameObject owner)
        {
            Owner = owner;
        }

        public AuxECSGameTimer(GameObject owner, AuxECSTimerCallback callback)
        {
            Owner = owner;
            Callback = callback;
        }

        public AuxECSGameTimer(GameObject owner, AuxECSTimerCallback callback, int interval)
        {
            Owner = owner;
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

    public abstract class AuxECSGameTimerWrapperBase : AuxECSGameTimer
    {
        public AuxECSGameTimerWrapperBase(GameObject owner) : base(owner)
        {
            Owner = owner;
            Callback = new AuxECSTimerCallback(OnTick);
        }

        protected abstract int OnTick(AuxECSGameTimer timer);
    }
}
