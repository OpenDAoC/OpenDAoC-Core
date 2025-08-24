using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class TimerService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<ECSGameTimer> _list;

        public static TimerService Instance { get; }

        static TimerService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ECSGameTimer>(ServiceObjectType.Timer, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                return;
            }

            GameLoop.ExecuteForEach(_list, lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _list.Count);
        }

        private static void TickInternal(ECSGameTimer timer)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                if (GameServiceUtils.ShouldTick(timer.NextTick))
                {
                    long startTick = GameLoop.GetRealTime();
                    timer.Tick();
                    long stopTick = GameLoop.GetRealTime();

                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for Timer Callback: {timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, timer, timer.Owner);
            }
        }
    }

    public class ECSGameTimer : IServiceObject
    {
        public delegate int ECSTimerCallback(ECSGameTimer timer);

        public GameObject Owner { get; }
        public ECSTimerCallback Callback { private get; set; }
        public MethodInfo CallbackInfo => Callback?.GetMethodInfo();
        public int Interval { get; set; }
        public long NextTick { get; protected set; }
        public bool IsAlive { get; private set; }
        public int TimeUntilElapsed => (int) (NextTick - GameLoop.GameLoopTime);
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
            Start(interval);
        }

        public void Start()
        {
            // Use half-second intervals by default.
            Start(Interval <= 0 ? 500 : Interval);
        }

        public void Start(int interval)
        {
            Interval = interval;
            NextTick = GameLoop.GameLoopTime + interval;

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

            if (Interval <= 0)
            {
                Stop();
                return;
            }

            NextTick += Interval;
        }

        public PropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    lock (this)
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
