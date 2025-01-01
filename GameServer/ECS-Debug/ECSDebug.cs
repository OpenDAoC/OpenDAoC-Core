using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DOL.Events;
using DOL.GS;
using ECS.Debug;

namespace ECS.Debug
{
    public static class Diagnostics
    {
        private const string SERVICE_NAME = "Diagnostics";
        private static StreamWriter _perfStreamWriter;
        private static bool _streamWriterInitialized = false;
        private static readonly Lock _gameEventMgrNotifyLock = new();
        private static bool PerfCountersEnabled = false;
        private static bool stateMachineDebugEnabled = false;
        private static Dictionary<string, Stopwatch> PerfCounters = new();
        private static readonly Lock _perfCountersLock = new();
        private static bool GameEventMgrNotifyProfilingEnabled = false;
        private static int GameEventMgrNotifyTimerInterval = 0;
        private static long GameEventMgrNotifyTimerStartTick = 0;
        private static Stopwatch GameEventMgrNotifyStopwatch;
        private static Dictionary<string, List<double>> GameEventMgrNotifyTimes = new();

        public static void TogglePerfCounters(bool enabled)
        {
            if (enabled == false)
            {
                _perfStreamWriter.Close();
                _streamWriterInitialized = false;
            }

            PerfCountersEnabled = enabled;
        }

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            ReportPerfCounters();

            if (GameEventMgrNotifyProfilingEnabled)
            {
                if ((GameLoop.GetCurrentTime() - GameEventMgrNotifyTimerStartTick) > GameEventMgrNotifyTimerInterval)
                    ReportGameEventMgrNotifyTimes();
            }
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
            if (!PerfCountersEnabled)
                return;

            InitializeStreamWriter();
            Stopwatch stopwatch = Stopwatch.StartNew();
            lock(_perfCountersLock)
            {
                PerfCounters.TryAdd(uniqueID, stopwatch);
            }
        }

        public static void StopPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            lock (_perfCountersLock)
            {
                if (PerfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
                    stopwatch.Stop();
            }
        }

        private static void ReportPerfCounters()
        {
            if (!PerfCountersEnabled)
                return;

            // Report perf counters that were active this frame and then flush them.
            lock(_perfCountersLock)
            {
                if (PerfCounters.Count > 0)
                {
                    string logString = "[PerfCounters] ";

                    foreach (var counter in PerfCounters)
                    {
                        string counterName = counter.Key;
                        float elapsed = (float)counter.Value.Elapsed.TotalMilliseconds;
                        string elapsedString = elapsed.ToString();
                        elapsedString = Util.TruncateString(elapsedString, 4);
                        logString += $"{counterName} {elapsedString}ms | ";
                    }

                    _perfStreamWriter.WriteLine(logString);
                    PerfCounters.Clear();
                }
            }
        }

        public static void BeginGameEventMgrNotify()
        {
            if (!GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyStopwatch = Stopwatch.StartNew();
        }

        public static void EndGameEventMgrNotify(DOLEvent e)
        {
            if (!GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyStopwatch.Stop();

            lock (_gameEventMgrNotifyLock)
            {
                if (GameEventMgrNotifyTimes.TryGetValue(e.Name, out List<double> EventTimeValues))
                    EventTimeValues.Add(GameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
                else
                {
                    EventTimeValues = new();
                    EventTimeValues.Add(GameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
                    GameEventMgrNotifyTimes.TryAdd(e.Name, EventTimeValues);
                }
            }
        }

        public static void StartGameEventMgrNotifyTimeReporting(int IntervalMilliseconds)
        {
            if (GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyProfilingEnabled = true;
            GameEventMgrNotifyTimerInterval = IntervalMilliseconds;
            GameEventMgrNotifyTimerStartTick = GameLoop.GetCurrentTime();
        }

        public static void StopGameEventMgrNotifyTimeReporting()
        {
            if (!GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyProfilingEnabled = false;
            GameEventMgrNotifyTimes.Clear();
        }

        private static void ReportGameEventMgrNotifyTimes()
        {
            string ActualInterval = Util.TruncateString((GameLoop.GetCurrentTime() - GameEventMgrNotifyTimerStartTick).ToString(), 5);
            Console.WriteLine($"==== GameEventMgr Notify() Costs (Requested Interval: {GameEventMgrNotifyTimerInterval}ms | Actual Interval: {ActualInterval}ms) ====");

            lock (_gameEventMgrNotifyLock)
            {
                foreach (var NotifyData in GameEventMgrNotifyTimes)
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
                    Console.WriteLine($"{EventNameString} - # Calls: {NumValuesString} | Total: {TotalCostString}ms | Avg: {AvgCostString}ms | Min: {MinCostString}ms | Max: {MaxCostString}ms");
                }

                GameEventMgrNotifyTimes.Clear();
                GameEventMgrNotifyTimerStartTick = GameLoop.GetCurrentTime();
                Console.WriteLine("---------------------------------------------------------------------------");
            }
        }
    }
}

namespace DOL.GS.Commands
{
    [CmdAttribute(
    "&diag",
    ePrivLevel.GM,
    "Toggle server logging of performance diagnostics.",
    "/diag perf <on|off> to toggle performance diagnostics logging on server.",
    "/diag notify <on|off> <interval> to toggle GameEventMgr Notify profiling, where interval is the period of time in milliseconds during which to accumulate stats.",
    "/diag timer <tickcount> enables debugging of the TimerService for <tickcount> ticks and outputs to the server Console.",
    "/diag think <tickcount> enables debugging of the NPCThinkService for <tickcount> ticks and outputs to the server Console.",
    "/diag currentservicetick - returns the current service the gameloop tick is on; useful for debugging lagging/frozen server.")]
    public class ECSDiagnosticsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
                return;

            if (IsSpammingCommand(client.Player, "Diag"))
                return;

            if (client.Account.PrivLevel < 2)
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("currentservicetick"))
            {
                DisplayMessage(client, "Gameloop CurrentService Tick: " + GameLoop.CurrentServiceTick);
                return;
            }

            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("perf"))
            {
                if (args[2].ToLower().Equals("on"))
                {
                    Diagnostics.TogglePerfCounters(true);
                    DisplayMessage(client, "Performance diagnostics logging turned on. WARNING: This will spam the server logs.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    Diagnostics.TogglePerfCounters(false);
                    DisplayMessage(client, "Performance diagnostics logging turned off.");
                }
            }

            if (args[1].ToLower().Equals("notify"))
            {
                if (args[2].ToLower().Equals("on"))
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
                else if (args[2].ToLower().Equals("off"))
                {
                    Diagnostics.StopGameEventMgrNotifyTimeReporting();
                    DisplayMessage(client, "GameEventMgr Notify() logging turned off.");
                }
            }

            if (args[1].ToLower().Equals("timer"))
            {
                int tickcount = int.Parse(args[2]);
                if (tickcount <= 0)
                {
                    DisplayMessage(client, "Invalid tickcount argument. Please specify a positive integer value.");
                    return;
                }

                TimerService.DebugTickCount = tickcount;
                DisplayMessage(client, "Debugging next " + tickcount + " TimerService tick(s)");
            }

            if (args[1].ToLower().Equals("think"))
            {
                int tickcount = int.Parse(args[2]);
                if (tickcount <= 0)
                {
                    DisplayMessage(client, "Invalid tickcount argument. Please specify a positive integer value.");
                    return;
                }

                NpcService.DebugTickCount = tickcount;
                DisplayMessage(client, "Debugging next " + tickcount + " NPCThinkService tick(s)");
            }
        }
    }
}
