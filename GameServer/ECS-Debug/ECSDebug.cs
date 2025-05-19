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

        private const string SERVICE_NAME = nameof(Diagnostics);
        private static StreamWriter _perfStreamWriter;
        private static bool _streamWriterInitialized;
        private static readonly Lock _gameEventMgrNotifyLock = new();
        private static bool _perfCountersEnabled;
        private static Dictionary<string, Stopwatch> _perfCounters = new();
        private static readonly Lock _perfCountersLock = new();
        private static bool _gameEventMgrNotifyProfilingEnabled ;
        private static int _gameEventMgrNotifyTimerInterval;
        private static long _gameEventMgrNotifyTimerStartTick;
        private static Stopwatch _gameEventMgrNotifyStopwatch;
        private static Dictionary<string, List<double>> _gameEventMgrNotifyTimes = new();
        private static int _checkEntityCountTicks;

        public static bool CheckEntityCounts => _checkEntityCountTicks > 0;
        public static bool RequestCheckEntityCounts { get; set; }
        public static int LongTickThreshold { get; set; } = 25;

        public static void PrintEntityCount(string serviceName, ref int nonNull, int total)
        {
            log.Debug($"==== {FormatCount(nonNull),-4} / {FormatCount(total),4} non-null entities in {serviceName}'s list ====");
            nonNull = 0;

            static string FormatCount(int count)
            {
                return count >= 1000000 ? (count / 1000000.0).ToString("G3") + "M" :
                    count >= 1000 ? (count / 1000.0).ToString("G3") + "K" :
                    count.ToString();
            }
        }

        public static void TogglePerfCounters(bool enabled)
        {
            if (enabled == false)
            {
                _perfStreamWriter.Close();
                _streamWriterInitialized = false;
            }

            _perfCountersEnabled = enabled;
        }

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            ReportPerfCounters();

            if (_gameEventMgrNotifyProfilingEnabled)
            {
                if ((GameLoop.GetCurrentTime() - _gameEventMgrNotifyTimerStartTick) > _gameEventMgrNotifyTimerInterval)
                    ReportGameEventMgrNotifyTimes();
            }

            // Delay by one tick to account for the fact that this was most likely requested from ClientService.
            if (RequestCheckEntityCounts)
            {
                _checkEntityCountTicks = 1;
                RequestCheckEntityCounts = false;
            }
            else if (CheckEntityCounts)
                _checkEntityCountTicks--;
        }

        private static void InitializeStreamWriter()
        {
            if (_streamWriterInitialized)
                return;
            else
            {
                string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PerfLog" + DateTime.Now.ToFileTime());
                _perfStreamWriter = new StreamWriter(_filePath, false);
                _streamWriterInitialized = true;
            }
        }

        public static void StartPerfCounter(string uniqueID)
        {
            if (!_perfCountersEnabled)
                return;

            InitializeStreamWriter();
            Stopwatch stopwatch = Stopwatch.StartNew();
            lock(_perfCountersLock)
            {
                _perfCounters.TryAdd(uniqueID, stopwatch);
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

        private static void ReportPerfCounters()
        {
            if (!_perfCountersEnabled)
                return;

            // Report perf counters that were active this frame and then flush them.
            lock(_perfCountersLock)
            {
                if (_perfCounters.Count > 0)
                {
                    string logString = "[PerfCounters] ";

                    foreach (var counter in _perfCounters)
                    {
                        string counterName = counter.Key;
                        float elapsed = (float)counter.Value.Elapsed.TotalMilliseconds;
                        string elapsedString = elapsed.ToString();
                        elapsedString = Util.TruncateString(elapsedString, 4);
                        logString += $"{counterName} {elapsedString}ms | ";
                    }

                    _perfStreamWriter.WriteLine(logString);
                    _perfCounters.Clear();
                }
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
                    EventTimeValues = new();
                    EventTimeValues.Add(_gameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
                    _gameEventMgrNotifyTimes.TryAdd(e.Name, EventTimeValues);
                }
            }
        }

        public static void StartGameEventMgrNotifyTimeReporting(int IntervalMilliseconds)
        {
            if (_gameEventMgrNotifyProfilingEnabled)
                return;

            _gameEventMgrNotifyProfilingEnabled = true;
            _gameEventMgrNotifyTimerInterval = IntervalMilliseconds;
            _gameEventMgrNotifyTimerStartTick = GameLoop.GetCurrentTime();
        }

        public static void StopGameEventMgrNotifyTimeReporting()
        {
            if (!_gameEventMgrNotifyProfilingEnabled)
                return;

            _gameEventMgrNotifyProfilingEnabled = false;
            _gameEventMgrNotifyTimes.Clear();
        }

        private static void ReportGameEventMgrNotifyTimes()
        {
            string ActualInterval = Util.TruncateString((GameLoop.GetCurrentTime() - _gameEventMgrNotifyTimerStartTick).ToString(), 5);
            log.Debug($"==== GameEventMgr Notify() Costs (Requested Interval: {_gameEventMgrNotifyTimerInterval}ms | Actual Interval: {ActualInterval}ms) ====");

            lock (_gameEventMgrNotifyLock)
            {
                foreach (var NotifyData in _gameEventMgrNotifyTimes)
                {
                    List<double> EventTimeValues = NotifyData.Value;
                    string EventNameString = NotifyData.Key.PadRight(30);
                    double TotalCost = 0;
                    double MinCost = 0;
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
                _gameEventMgrNotifyTimerStartTick = GameLoop.GetCurrentTime();
                log.Debug("---------------------------------------------------------------------------");
            }
        }
    }
}

namespace DOL.GS.Commands
{
    [Cmd(
    "&diag",
    ePrivLevel.GM,
    "Toggle server logging of performance diagnostics.",
    "/diag perf <on|off> to toggle performance diagnostics logging on server.",
    "/diag notify <on|off> <interval> to toggle GameEventMgr Notify profiling, where interval is the period of time in milliseconds during which to accumulate stats.",
    "/diag timer <tickcount> enables debugging of the TimerService for <tickcount> ticks and outputs to the server Console.",
    "/diag entity to count non-null service objects in ServiceObjectStore arrays")]
    public class ECSDiagnosticsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
                return;

            if (IsSpammingCommand(client.Player, "Diag"))
                return;

            if ((ePrivLevel) client.Account.PrivLevel < ePrivLevel.GM)
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].Equals("entity", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RequestCheckEntityCounts = true;
                DisplayMessage(client, "Counting entities...");
                return;
            }

            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].Equals("perf", StringComparison.OrdinalIgnoreCase))
            {
                if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    Diagnostics.TogglePerfCounters(true);
                    DisplayMessage(client, "Performance diagnostics logging turned on. WARNING: This will spam the server logs.");
                }
                else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    Diagnostics.TogglePerfCounters(false);
                    DisplayMessage(client, "Performance diagnostics logging turned off.");
                }
            }

            if (args[1].Equals("notify", StringComparison.OrdinalIgnoreCase))
            {
                if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    int interval = int.Parse(args[3]);
                    if (interval <= 0)
                    {
                        DisplayMessage(client, "Invalid interval argument. Please specify a value in milliseconds.");
                        return;
                    }

                    Diagnostics.StartGameEventMgrNotifyTimeReporting(interval);
                    DisplayMessage(client, "GameEventMgr Notify() logging turned on. WARNING: This will spam the server logs.");
                }
                else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    Diagnostics.StopGameEventMgrNotifyTimeReporting();
                    DisplayMessage(client, "GameEventMgr Notify() logging turned off.");
                }
            }
        }
    }
}
