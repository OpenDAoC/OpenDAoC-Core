using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ECSGameTimer>(ServiceObjectType.Timer, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                Diagnostics.StopPerfCounter(SERVICE_NAME);
                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static void ScheduleTimerAfterTask<T>(Task task, ContinuationAction<T> continuation, T argument, GameObject owner)
        {
            ContinuationActionTimerState<T> state = new(owner, continuation, argument);

            task.ContinueWith(static (task, state) =>
            {
                if (task.IsFaulted)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Async task failed", task.Exception);

                    return;
                }

                // We can't safely start a timer from within a task continuation, so we post it to the game loop.
                GameLoopService.PostAfterTick(static (s) => new ContinuationActionTimer<T>(s as ContinuationActionTimerState<T>), state);
            }, state);
        }

        private static void TickInternal(int index)
        {
            ECSGameTimer timer = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                timer = _list[index];

                if (ServiceUtils.ShouldTick(timer.NextTick))
                {
                    long startTick = GameLoop.GetRealTime();
                    timer.Tick();
                    long stopTick = GameLoop.GetRealTime();

                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Timer Callback: {timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
            }
        }

        public delegate bool ContinuationAction<T>(T argument);

        private class ContinuationActionTimerState<T>
        {
            public GameObject Owner { get; }
            public ContinuationAction<T> ContinuationAction { get; }
            public T Argument { get; }

            public ContinuationActionTimerState(GameObject owner, ContinuationAction<T> continuationAction, T argument)
            {
                Owner = owner;
                ContinuationAction = continuationAction;
                Argument = argument;
            }
        }

        private class ContinuationActionTimer<T> : ECSGameTimerWrapperBase
        {
            private ContinuationAction<T> _continuationAction;
            private T _argument;

            public ContinuationActionTimer(ContinuationActionTimerState<T> state) : base(state.Owner)
            {
                _continuationAction = state.ContinuationAction;
                _argument = state.Argument;
                Start(0);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                _continuationAction(_argument);
                return 0;
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

            if (Interval == 0)
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
