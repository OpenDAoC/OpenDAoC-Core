using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace DOL.GS
{
    public class AuxTimerService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "Aux Timer Service";

        // Will print active brain count/array size info for debug purposes if superior to 0.
        public static int DebugTickCount;

        private static List<AuxECSGameTimer> _activeTimers;
        private static Stack<AuxECSGameTimer> _timerToRemove;
        private static Stack<AuxECSGameTimer> _timerToAdd;
        private static readonly object _addTimerLockObject = new();
        private static readonly object _removeTimerLockObject = new();

        private static bool Debug => DebugTickCount > 0;

        static AuxTimerService()
        {
            _activeTimers = new List<AuxECSGameTimer>();
            _timerToAdd = new Stack<AuxECSGameTimer>();
            _timerToRemove = new Stack<AuxECSGameTimer>();
        }

        public static void Tick(long tick)
        {
            // Diagnostics.StartPerfCounter(SERVICE_NAME);

            // Debug variables.
            Dictionary<string, int> TimerToRemoveCallbacks = null;
            Dictionary<string, int> TimerToAddCallbacks = null;
            int TimerToRemoveCount = 0;
            int TimerToAddCount = 0;

            // Check if need to debug, then setup vars.
            if (Debug && DebugTickCount > 0)
            {
                TimerToRemoveCount = _timerToRemove.Count;
                TimerToAddCount = _timerToAdd.Count;
                TimerToRemoveCallbacks = new Dictionary<string, int>();
                TimerToAddCallbacks = new Dictionary<string, int>();
            }

            while (_timerToRemove.Count > 0)
            {
                lock (_removeTimerLockObject)
                {
                    if (Debug && TimerToRemoveCallbacks != null && _timerToRemove.Peek() != null && _timerToRemove.Peek().Callback != null)
                    {
                        string callbackMethodName = _timerToRemove.Peek().Callback.Method.Name;
                        if (TimerToRemoveCallbacks.ContainsKey(callbackMethodName))
                            TimerToRemoveCallbacks[callbackMethodName]++;
                        else
                            TimerToRemoveCallbacks.Add(callbackMethodName, 1);
                    }

                    if (_activeTimers.Contains(_timerToRemove.Peek()))
                        _activeTimers.Remove(_timerToRemove.Pop());
                    else
                        _timerToRemove.Pop();
                }
            }

            while (_timerToAdd.Count > 0)
            {
                lock (_addTimerLockObject)
                {
                    if (Debug && TimerToAddCallbacks != null && _timerToAdd.Peek() != null && _timerToAdd.Peek().Callback != null)
                    {
                        string callbackMethodName = _timerToAdd.Peek().Callback.Method.Name;
                        if (TimerToAddCallbacks.ContainsKey(callbackMethodName))
                            TimerToAddCallbacks[callbackMethodName]++;
                        else
                            TimerToAddCallbacks.Add(callbackMethodName, 1);
                    }

                    if (!_activeTimers.Contains(_timerToAdd.Peek()))
                        _activeTimers.Add(_timerToAdd.Pop());
                    else
                        _timerToAdd.Pop();
                }
            }

            //Console.WriteLine($"timer size {ActiveTimers.Count}");
            /*
            if (debugTick + 1000 < tick)
            {
                Console.WriteLine($"timer size {ActiveTimers.Count}");
                debugTick = tick;
            }*/

            Parallel.ForEach(_activeTimers, timer =>
            {
                if (timer != null && timer.NextTick < tick)
                {
                    long startTick = GameTimer.GetTickCount();
                    timer.Tick();
                    long stopTick = GameTimer.GetTickCount();
                    if ((stopTick - startTick) > 25)
                        log.Warn($"Long AuxTimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
                }
            });

            // Output debug info.
            if (Debug && TimerToRemoveCallbacks != null && TimerToAddCallbacks != null)
            {
                log.Debug($"==== AuxTimerService Debug - Total ActiveTimers: {_activeTimers.Count} ====");
                log.Debug($"==== AuxTimerService RemoveTimer Top 5 Callback Methods. Total TimerToRemove Count: {TimerToRemoveCount} ====");

                foreach (var callbacks in TimerToRemoveCallbacks.OrderByDescending(callback => callback.Value).Take(5))
                {
                    log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
                }

                log.Debug($"==== AuxTimerService AddTimer Top 5 Callback Methods. Total TimerToAdd Count: {TimerToAddCount} ====");

                foreach (var callbacks in TimerToAddCallbacks.OrderByDescending(callback => callback.Value).Take(5))
                {
                    log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
                }

                log.Debug("---------------------------------------------------------------------------");

                if (DebugTickCount > 1)
                    DebugTickCount--;
                else;
                    DebugTickCount = 0;
            }

            // Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        // Currently identical to 'AddExistingTimer'.
        public static void AddTimer(AuxECSGameTimer newTimer)
        {
            lock (_addTimerLockObject)
            {
                _timerToAdd?.Push(newTimer);
            }
        }

        // Adds timer to the TimerToAdd Stack without checking it already exists. Helpful if the timer is being removed and then added again in same tick.
        // The Tick() method will still check for duplicate timer in ActiveTimers.
        public static void AddExistingTimer(AuxECSGameTimer newTimer)
        {
            lock (_addTimerLockObject)
            {
                _timerToAdd?.Push(newTimer);
            }
        }

        public static void RemoveTimer(AuxECSGameTimer timerToRemove)
        {
            lock (_removeTimerLockObject)
            {
                if (_activeTimers.Contains(timerToRemove))
                {
                    _timerToRemove?.Push(timerToRemove);
                }
            }
        }

        public static bool HasActiveTimer(AuxECSGameTimer timer)
        {
            return _activeTimers.Contains(timer) || _timerToAdd.Contains(timer);
        }
    }

    public class AuxECSGameTimer
    {
        /// <summary>
        /// This delegate is the callback function for the ECS Timer
        /// </summary>
        public delegate int AuxECSTimerCallback(AuxECSGameTimer timer);

        public GameObject TimerOwner;
        public AuxECSTimerCallback Callback;
        public int Interval;
        public long StartTick;
        public long NextTick => StartTick + Interval;
        public bool IsAlive => AuxTimerService.HasActiveTimer(this);
        public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);

        /// <summary>
        /// Holds properties for this region timer
        /// </summary>
        private PropertyCollection m_properties;

        public AuxECSGameTimer(GameObject target)
        {
            TimerOwner = target;
        }

        public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback, int interval)
        {
            TimerOwner = target;
            Callback = callback;
            Interval = interval;
            Start();
        }

        public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback)
        {
            TimerOwner = target;
            Callback = callback;
        }

        public void Start()
        {
            if (Interval <= 0)
                Start(500); // Use half-second intervals by default.
            else
                Start(Interval);
        }

        public void Start(int interval)
        {
            StartTick = AuxGameLoop.GameLoopTime;
            Interval = interval;
            AuxTimerService.AddTimer(this);
        }

        public void StartExistingTimer(int interval)
        {
            StartTick = AuxGameLoop.GameLoopTime;
            Interval = interval;
            AuxTimerService.AddExistingTimer(this);
        }

        public void Stop()
        {
            AuxTimerService.RemoveTimer(this);
        }

        public void Tick()
        {
            StartTick = AuxGameLoop.GameLoopTime;
            if (Callback != null)
            {
                Interval = Callback.Invoke(this);
            }

            if (Interval == 0)
                Stop();
        }

        public PropertyCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    lock (this)
                    {
                        if (m_properties == null)
                        {
                            PropertyCollection properties = new PropertyCollection();
                            Thread.MemoryBarrier();
                            m_properties = properties;
                        }
                    }
                }

                return m_properties;
            }
        }
    }
}
