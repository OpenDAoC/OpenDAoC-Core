using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using DOL.Events;
using DOL.GS;
using DOL.Logging;
using ECS.Debug;

namespace ECS.Debug
{
    public static class Diagnostics
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Perf counter fields.
        private static Dictionary<string, Stopwatch> _perfCounters = new();
        private static bool _perfCountersEnabled;
        private static long _perfCountersEndTick;
        private static StreamWriter _perfStreamWriter;
        private static bool _perfStreamWriterInitialized;
        private static readonly Lock _perfCountersLock = new();

        // GameEventMgr Notify profiling fields.
        private static Dictionary<string, List<double>> _gameEventMgrNotifyTimes = new();
        private static bool _gameEventMgrNotifyProfilingEnabled;
        private static int _gameEventMgrNotifyTimerInterval;
        private static long _gameEventMgrNotifyTimerStartTick;
        private static Stopwatch _gameEventMgrNotifyStopwatch;
        private static readonly Lock _gameEventMgrNotifyLock = new();

        // State management for delayed start/stop.
        private static StateChangeRequest _perfCountersStateRequest = StateChangeRequest.None;
        private static bool _serviceObjectCountRequest;
        private static StateChangeRequest _notifyProfilingStateRequest = StateChangeRequest.None;
        private static int _notifyProfilingIntervalRequest;

        public static bool CheckServiceObjectCount { get; private set; }
        public static int LongTickThreshold { get; private set; } = 25;

        public static void PrintServiceObjectCount(string serviceName, ref int nonNull, int total)
        {
            log.Debug($"==== {FormatCount(nonNull),-4} / {FormatCount(total),4} non-null objects in {serviceName}'s list ====");
            nonNull = 0;

            static string FormatCount(int count)
            {
                return count >= 1000000 ? (count / 1000000.0).ToString("G3") + "M" :
                    count >= 1000 ? (count / 1000.0).ToString("G3") + "K" :
                    count.ToString();
            }
        }

        public static void Tick()
        {
            try
            {
                ReportPerfCounters();
                CheckServiceObjectCount = false;
                ReportGameEventMgrNotifyTimes();

                // Handle new requests to change diagnostic states for the next tick.
                HandlePendingRequests();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("An error occurred during diagnostics tick processing.", e);
            }
        }

        public static void StartPerfCounter(string uniqueID)
        {
            if (!_perfCountersEnabled)
                return;

            InitializeStreamWriter();

            lock (_perfCountersLock)
            {
                if (_perfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
                    stopwatch.Restart();
                else
                    _perfCounters.Add(uniqueID, Stopwatch.StartNew());
            }

            static void InitializeStreamWriter()
            {
                if (_perfStreamWriterInitialized)
                    return;

                string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Perf_{DateTime.Now.ToFileTime()}.log");
                _perfStreamWriter = new(_filePath, false);
                _perfStreamWriterInitialized = true;
            }
        }

        public static void StopPerfCounter(string uniqueID)
        {
            if (!_perfCountersEnabled)
                return;

            lock (_perfCountersLock)
            {
                if (_perfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
                    stopwatch.Stop();
            }
        }

        public static void BeginGameEventMgrNotify()
        {
            if (!_gameEventMgrNotifyProfilingEnabled)
                return;

            _gameEventMgrNotifyStopwatch = Stopwatch.StartNew();
        }

        public static void EndGameEventMgrNotify(DOLEvent e)
        {
            if (!_gameEventMgrNotifyProfilingEnabled)
                return;

            _gameEventMgrNotifyStopwatch.Stop();

            lock (_gameEventMgrNotifyLock)
            {
                if (_gameEventMgrNotifyTimes.TryGetValue(e.Name, out List<double> EventTimeValues))
                    EventTimeValues.Add(_gameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
                else
                {
                    EventTimeValues = [_gameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds];
                    _gameEventMgrNotifyTimes.TryAdd(e.Name, EventTimeValues);
                }
            }
        }

        public static void RequestPerfCounters(bool enable, long endTick = long.MaxValue)
        {
            _perfCountersStateRequest = enable ? StateChangeRequest.Start : StateChangeRequest.Stop;
            _perfCountersEndTick = endTick;
        }

        public static void RequestServiceObjectCount()
        {
            _serviceObjectCountRequest = true;
        }

        public static void RequestGameEventMgrNotifyTimeReporting(bool enable, int intervalMilliseconds = 0)
        {
            if (enable)
            {
                _notifyProfilingIntervalRequest = intervalMilliseconds;
                _notifyProfilingStateRequest = StateChangeRequest.Start;
            }
            else
            {
                _notifyProfilingStateRequest = StateChangeRequest.Stop;
            }
        }

        private static void HandlePendingRequests()
        {
            // Perf counters state change.
            if (_perfCountersStateRequest is not StateChangeRequest.None)
            {
                if (_perfCountersStateRequest is StateChangeRequest.Start)
                    _perfCountersEnabled = true;
                else
                {
                    if (_perfStreamWriterInitialized)
                    {
                        _perfStreamWriter.Close();
                        _perfStreamWriterInitialized = false;
                    }

                    _perfCounters.Clear();
                    _perfCountersEnabled = false;
                }

                _perfCountersStateRequest = StateChangeRequest.None;
            }

            // GameEventMgr Notify profiling state change.
            if (_notifyProfilingStateRequest is not StateChangeRequest.None)
            {
                if (_notifyProfilingStateRequest is StateChangeRequest.Start)
                {
                    if (!_gameEventMgrNotifyProfilingEnabled)
                    {
                        _gameEventMgrNotifyProfilingEnabled = true;
                        _gameEventMgrNotifyTimerInterval = _notifyProfilingIntervalRequest;
                        _gameEventMgrNotifyTimerStartTick = GameLoop.GetRealTime();
                    }
                }
                else
                {
                    if (_gameEventMgrNotifyProfilingEnabled)
                    {
                        _gameEventMgrNotifyProfilingEnabled = false;
                        _gameEventMgrNotifyTimes.Clear();
                    }
                }

                _notifyProfilingStateRequest = StateChangeRequest.None;
            }

            // Service object count request.
            if (_serviceObjectCountRequest)
            {
                CheckServiceObjectCount = true;
                _serviceObjectCountRequest = false;
            }
        }

        private static void ReportPerfCounters()
        {
            if (!_perfCountersEnabled)
                return;

            lock (_perfCountersLock)
            {
                if (_perfCounters.Count > 0)
                {
                    string logString = string.Empty;

                    foreach (var counter in _perfCounters)
                        logString += $"{counter.Key} {counter.Value.Elapsed.TotalMilliseconds:0.##}ms | ";

                    _perfStreamWriter.WriteLine($"[PerfCounters] {logString}");
                }

                if (ServiceUtils.ShouldTick(_perfCountersEndTick))
                    _perfCountersStateRequest = StateChangeRequest.Stop;
            }
        }

        private static void ReportGameEventMgrNotifyTimes()
        {
            if (!_gameEventMgrNotifyProfilingEnabled || GameLoop.GetRealTime() - _gameEventMgrNotifyTimerStartTick <= _gameEventMgrNotifyTimerInterval)
                return;

            string actualInterval = Util.TruncateString((GameLoop.GetRealTime() - _gameEventMgrNotifyTimerStartTick).ToString(), 5);
            log.Debug($"==== GameEventMgr Notify() Costs (Requested Interval: {_gameEventMgrNotifyTimerInterval}ms | Actual Interval: {actualInterval}ms) ====");

            lock (_gameEventMgrNotifyLock)
            {
                foreach (var NotifyData in _gameEventMgrNotifyTimes)
                {
                    List<double> EventTimeValues = NotifyData.Value;
                    string EventNameString = NotifyData.Key.PadRight(30);
                    double TotalCost = 0;
                    double MinCost = double.MaxValue;
                    double MaxCost = 0;

                    foreach (double time in EventTimeValues)
                    {
                        TotalCost += time;

                        if (time < MinCost)
                            MinCost = time;

                        if (time > MaxCost)
                            MaxCost = time;
                    }

                    int NumValues = EventTimeValues.Count;
                    double AvgCost = TotalCost / NumValues;
                    string NumValuesString = NumValues.ToString().PadRight(4);
                    string TotalCostString = Util.TruncateString(TotalCost.ToString(), 5);
                    string MinCostString = Util.TruncateString(MinCost.ToString(), 5);
                    string MaxCostString = Util.TruncateString(MaxCost.ToString(), 5);
                    string AvgCostString = Util.TruncateString(AvgCost.ToString(), 5);
                    log.Debug($"{EventNameString} - # Calls: {NumValuesString} | Total: {TotalCostString}ms | Avg: {AvgCostString}ms | Min: {MinCostString}ms | Max: {MaxCostString}ms");
                }

                _gameEventMgrNotifyTimes.Clear();
                _gameEventMgrNotifyTimerStartTick = GameLoop.GetRealTime();
                log.Debug("---------------------------------------------------------------------------");
            }
        }

        private enum StateChangeRequest
        {
            None,
            Start,
            Stop
        }
    }
}

namespace DOL.GS.Commands
{
    [Cmd(
    "&diag",
    ePrivLevel.GM,
    "Toggle server logging of performance diagnostics.",
    "/diag perf <on|off> [duration] to toggle performance diagnostics logging on server with an optional duration (in minutes).",
    "/diag notify <on|off> <interval> to toggle GameEventMgr Notify profiling, where interval is the period of time in milliseconds during which to accumulate stats.",
    "/diag object to count non-null service objects in ServiceObjectStore arrays.")]
    public class ECSDiagnosticsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
                return;

            if (IsSpammingCommand(client.Player, "diag"))
                return;

            if ((ePrivLevel) client.Account.PrivLevel < ePrivLevel.GM)
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            string subCommand = args[1].ToLowerInvariant();

            switch (subCommand)
            {
                case "object":
                {
                    Diagnostics.RequestServiceObjectCount();
                    DisplayMessage(client, "Service object count scheduled for next tick.");
                    break;
                }
                case "perf":
                {
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        if (args.Length > 3 && int.TryParse(args[3], out int duration) && duration > 0)
                        {
                            Diagnostics.RequestPerfCounters(true, GameLoop.GameLoopTime + duration * 60000);
                            DisplayMessage(client, $"Performance diagnostics logging turned on for {duration} minutes.");
                        }
                        else
                        {
                            Diagnostics.RequestPerfCounters(true);
                            DisplayMessage(client, "Performance diagnostics logging turned on.");
                        }
                    }
                    else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        Diagnostics.RequestPerfCounters(false);
                        DisplayMessage(client, "Performance diagnostics logging turned off.");
                    }
                    else
                        DisplaySyntax(client);

                    break;
                }
                case "notify":
                {
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        if (args.Length < 4 || !int.TryParse(args[3], out int interval) || interval <= 0)
                        {
                            DisplayMessage(client, "Invalid interval argument. Please specify a positive value in milliseconds.");
                            return;
                        }

                        Diagnostics.RequestGameEventMgrNotifyTimeReporting(true, interval);
                        DisplayMessage(client, "GameEventMgr Notify() logging turned on.");
                    }
                    else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        Diagnostics.RequestGameEventMgrNotifyTimeReporting(false);
                        DisplayMessage(client, "GameEventMgr Notify() logging turned off.");
                    }
                    else
                        DisplaySyntax(client);

                    break;
                }
                default:
                {
                    DisplaySyntax(client);
                    break;
                }
            }
        }
    }
}
