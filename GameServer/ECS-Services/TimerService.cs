using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string SERVICE_NAME = "Timer Service";

        // Will print active brain count/array size info for debug purposes if superior to 0.
        public static int DebugTickCount;

        private static HashSet<ECSGameTimer> _activeTimers;
        private static Stack<ECSGameTimer> _timerToRemove;
        private static Stack<ECSGameTimer> _timerToAdd;
        private static readonly object _addTimerLockObject = new();
        private static readonly object _removeTimerLockObject = new();

        private static bool Debug => DebugTickCount > 0;

        static TimerService()
        {
            _activeTimers = new HashSet<ECSGameTimer>();
            _timerToAdd = new Stack<ECSGameTimer>();
            _timerToRemove = new Stack<ECSGameTimer>();
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            // Debug variables.
            Dictionary<string, int> timerToRemoveCallbacks = null;
            Dictionary<string, int> timerToAddCallbacks = null;
            int timerToRemoveCount = 0;
            int timerToAddCount = 0;

            // Check if need to debug, then setup vars.
            if (Debug && DebugTickCount > 0)
            {
                timerToRemoveCount = _timerToRemove.Count;
                timerToAddCount = _timerToAdd.Count;
                timerToRemoveCallbacks = new Dictionary<string, int>();
                timerToAddCallbacks = new Dictionary<string, int>();
            }

            long addRemoveStartTick = GameTimer.GetTickCount();

            lock (_removeTimerLockObject)
            {
                while (_timerToRemove.Count > 0)
                {
                    if (Debug && timerToRemoveCallbacks != null && _timerToRemove.Peek() != null && _timerToRemove.Peek().Callback != null)
                    {
                        string callbackMethodName = _timerToRemove.Peek().Callback.Method.DeclaringType + "." + _timerToRemove.Peek().Callback.Method.Name;
                        if (timerToRemoveCallbacks.ContainsKey(callbackMethodName))
                            timerToRemoveCallbacks[callbackMethodName]++;
                        else
                            timerToRemoveCallbacks.Add(callbackMethodName, 1);
                    }

                    if (_activeTimers.Contains(_timerToRemove.Peek()))
                        _activeTimers.Remove(_timerToRemove.Pop());
                    else
                        _timerToRemove.Pop();
                }
            }

            long addRemoveStopTick = GameTimer.GetTickCount();

            if ((addRemoveStopTick - addRemoveStartTick) > 25)
                log.Warn($"Long TimerService Remove Timers Time: {addRemoveStopTick - addRemoveStartTick}ms");

            addRemoveStartTick = GameTimer.GetTickCount();

            lock (_addTimerLockObject)
            {
                while (_timerToAdd.Count > 0)
                {
                    if (Debug && timerToAddCallbacks != null && _timerToAdd.Peek() != null && _timerToAdd.Peek().Callback != null)
                    {
                        string callbackMethodName = _timerToAdd.Peek().Callback.Method.DeclaringType + "." + _timerToAdd.Peek().Callback.Method.Name;
                        if (timerToAddCallbacks.ContainsKey(callbackMethodName))
                            timerToAddCallbacks[callbackMethodName]++;
                        else
                            timerToAddCallbacks.Add(callbackMethodName, 1);
                    }

                    if (!_activeTimers.Contains(_timerToAdd.Peek()))
                        _activeTimers.Add(_timerToAdd.Pop());
                    else
                        _timerToAdd.Pop();
                }
            }

            addRemoveStopTick = GameTimer.GetTickCount();

            if ((addRemoveStopTick - addRemoveStartTick) > 25)
                log.Warn($"Long TimerService Add Timers Time: {addRemoveStopTick - addRemoveStartTick}ms");

            //Console.WriteLine($"timer size {ActiveTimers.Count}");
            /*
            if (debugTick + 1000 < tick)
            {
                Console.WriteLine($"timer size {ActiveTimers.Count}");
                debugTick = tick;
            }*/

            Parallel.ForEach(_activeTimers.ToArray(), timer =>
            {
                try
                {
                    if (timer != null && timer.NextTick < tick)
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
                    log.Error($"Critical error encountered in Timer Service: {e}");
                }
            });

            // Output Debug info.
            if (Debug && timerToRemoveCallbacks != null && timerToAddCallbacks != null)
            {
                log.Debug($"==== TimerService Debug - Total ActiveTimers: {_activeTimers.Count} ====");
                log.Debug($"==== TimerService RemoveTimer Top 10 Callback Methods. Total TimerToRemove Count: {timerToRemoveCount} ====");

                foreach (var callbacks in timerToRemoveCallbacks.OrderByDescending(callback => callback.Value).Take(10))
                    log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");

                log.Debug($"==== TimerService AddTimer Top 10 Callback Methods. Total TimerToAdd Count: {timerToAddCount} ====");

                foreach (var callbacks in timerToAddCallbacks.OrderByDescending(callback => callback.Value).Take(10))
                    log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");

                log.Debug("---------------------------------------------------------------------------");

                if (DebugTickCount > 1)
                    DebugTickCount--;
                else;
                    DebugTickCount = 0;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        // Currently identical to 'AddExistingTimer'.
        public static void AddTimer(ECSGameTimer newTimer)
        {
            lock (_addTimerLockObject)
            {
                _timerToAdd?.Push(newTimer);
            }
        }

        // Adds timer to the TimerToAdd Stack without checking it already exists. Helpful if the timer is being removed and then added again in same tick.
        // The Tick() method will still check for duplicate timer in ActiveTimers.
        public static void AddExistingTimer(ECSGameTimer newTimer)
        {
            lock (_addTimerLockObject)
            {
                _timerToAdd?.Push(newTimer);
            }
        }

        public static void RemoveTimer(ECSGameTimer timerToRemove)
        {
            lock (_removeTimerLockObject)
            {
                _timerToRemove?.Push(timerToRemove);
            }
        }

        public static bool HasActiveTimer(ECSGameTimer timer)
        {
            List<ECSGameTimer> currentTimers = new();
            List<ECSGameTimer> timerAdds = new();
            lock (_addTimerLockObject)
            {
                currentTimers = _activeTimers.ToList();
                timerAdds = _timerToAdd.ToList();
            }

            return currentTimers.Contains(timer) || timerAdds.Contains(timer);
        }
    }

    public class ECSGameTimer
    {
        /// <summary>
        /// This delegate is the callback function for the ECS Timer
        /// </summary>
        public delegate int ECSTimerCallback(ECSGameTimer timer);

        public GameObject TimerOwner;
        public ECSTimerCallback Callback;
        public int Interval;
        public long StartTick;
        public long NextTick => StartTick + Interval;
        public bool IsAlive => TimerService.HasActiveTimer(this);
        public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);

        /// <summary>
        /// Holds properties for this region timer
        /// </summary>
        private PropertyCollection m_properties;

        public ECSGameTimer(GameObject target)
        {
            TimerOwner = target;
        }

        public ECSGameTimer(GameObject target, ECSTimerCallback callback, int interval)
        {
            TimerOwner = target;
            Callback = callback;
            Interval = interval;
            Start();
        }

        public ECSGameTimer(GameObject target, ECSTimerCallback callback)
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
            StartTick = GameLoop.GameLoopTime;
            Interval = interval;
            TimerService.AddTimer(this);
        }

        public void StartExistingTimer(int interval)
        {
            StartTick = GameLoop.GameLoopTime;
            Interval = interval;
            TimerService.AddExistingTimer(this);
        }

        public void Stop()
        {
            TimerService.RemoveTimer(this);
        }

        public void Tick()
        {
            StartTick = GameLoop.GameLoopTime;
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
