using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using ECS.Debug;
using log4net;
using log4net.Core;

namespace ECS.Debug
{
    public static class Diagnostics
    {
        private static readonly ILog log = LogManager.GetLogger("Performance");

        //Create FileStream and append to it as needed
        private static StreamWriter _perfStreamWriter;
        private static bool _streamWriterInitialized = false;

        private static object _GameEventMgrNotifyLock = new object();
        private static bool PerfCountersEnabled = false;
        private static bool stateMachineDebugEnabled = false;
        private static bool aggroDebugEnabled = false;
        private static Dictionary<string, System.Diagnostics.Stopwatch> PerfCounters = new Dictionary<string, System.Diagnostics.Stopwatch>();

        private static object _PerfCountersLock = new object();

        private static bool GameEventMgrNotifyProfilingEnabled = false;
        private static int GameEventMgrNotifyTimerInterval = 0;
        private static long GameEventMgrNotifyTimerStartTick = 0;
        private static System.Diagnostics.Stopwatch GameEventMgrNotifyStopwatch;
        private static Dictionary<string, List<double>> GameEventMgrNotifyTimes = new Dictionary<string, List<double>>();

        public static bool StateMachineDebugEnabled { get => stateMachineDebugEnabled; private set => stateMachineDebugEnabled = value; }
        public static bool AggroDebugEnabled { get => aggroDebugEnabled; private set => aggroDebugEnabled = value; }

        public static void TogglePerfCounters(bool enabled)
        {
            if (enabled == false)
            {
                _perfStreamWriter.Close();
                _streamWriterInitialized = false;
            }
            PerfCountersEnabled = enabled;

        }

        public static void ToggleStateMachineDebug(bool enabled)
        {
            StateMachineDebugEnabled = enabled;
        }

        public static void ToggleAggroDebug(bool enabled)
        {
            AggroDebugEnabled = enabled;
        }


        public static void Tick()
        {
            ReportPerfCounters();

            if (GameEventMgrNotifyProfilingEnabled)
            {
                if ((DOL.GS.GameTimer.GetTickCount() - GameEventMgrNotifyTimerStartTick) > GameEventMgrNotifyTimerInterval)
                {
                    ReportGameEventMgrNotifyTimes();
                }
            }
        }

        private static void InitializeStreamWriter()
        {
            if (_streamWriterInitialized)
                return;
            else
            {
                var _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PerfLog" + DateTime.Now.ToFileTime());
                _perfStreamWriter = new StreamWriter(_filePath, false);
                _streamWriterInitialized = true;
            }
        }

        public static void StartPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            InitializeStreamWriter();
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            lock(_PerfCountersLock)
            {
                PerfCounters.TryAdd(uniqueID, stopwatch);
            }
        }

        public static void StopPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            System.Diagnostics.Stopwatch stopwatch;
            lock(_PerfCountersLock)
            {
                if (PerfCounters.TryGetValue(uniqueID, out stopwatch))
                {
                    stopwatch.Stop();
                }
            }
        }

        private static void ReportPerfCounters()
        {
            if (!PerfCountersEnabled)
                return;

            // Report perf counters that were active this frame and then flush them.
            lock(_PerfCountersLock)
            {
                if (PerfCounters.Count > 0)
                {
                    string logString = "[PerfCounters] ";

                    foreach (var counter in PerfCounters)
                    {
                        var counterName = counter.Key;
                        var elapsed = (float)counter.Value.Elapsed.TotalMilliseconds;
                        string elapsedString = elapsed.ToString();
                        elapsedString = DOL.GS.Util.TruncateString(elapsedString, 4);
                        logString += ($"{counterName} {elapsedString}ms | ");
                    }
                    //Console.WriteLine(logString);
                    //log.Logger.Log(typeof(Diagnostics), Level.Info, logString, null);
                    //log.Info(logString);
                    _perfStreamWriter.WriteLine(logString);
                    PerfCounters.Clear();
                }
            }
        }

        public static void BeginGameEventMgrNotify()
        {
            if (!GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyStopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public static void EndGameEventMgrNotify(DOLEvent e)
        {
            if (!GameEventMgrNotifyProfilingEnabled)
                return;

            GameEventMgrNotifyStopwatch.Stop();

            lock (_GameEventMgrNotifyLock)
            {
                List<double> EventTimeValues;
                if (GameEventMgrNotifyTimes.TryGetValue(e.Name, out EventTimeValues))
                {
                    EventTimeValues.Add(GameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    EventTimeValues = new List<double>();
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
            GameEventMgrNotifyTimerStartTick = DOL.GS.GameTimer.GetTickCount();
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
            string ActualInterval = DOL.GS.Util.TruncateString((DOL.GS.GameTimer.GetTickCount() - GameEventMgrNotifyTimerStartTick).ToString(), 5);

            Console.WriteLine($"==== GameEventMgr Notify() Costs (Requested Interval: {GameEventMgrNotifyTimerInterval}ms | Actual Interval: {ActualInterval}ms) ====");

            lock (_GameEventMgrNotifyLock)
            {
                foreach (var NotifyData in GameEventMgrNotifyTimes)
                {
                    List<double> EventTimeValues = NotifyData.Value;

                    string EventNameString = NotifyData.Key.PadRight(30);
                    double TotalCost = 0;
                    double MinCost = 0;
                    double MaxCost = 0;

                    foreach (var time in EventTimeValues)
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
                    string TotalCostString = DOL.GS.Util.TruncateString(TotalCost.ToString(), 5);
                    string MinCostString = DOL.GS.Util.TruncateString(MinCost.ToString(), 5);
                    string MaxCostString = DOL.GS.Util.TruncateString(MaxCost.ToString(), 5);
                    string AvgCostString = DOL.GS.Util.TruncateString(AvgCost.ToString(), 5);

                    Console.WriteLine($"{EventNameString} - # Calls: {NumValuesString} | Total: {TotalCostString}ms | Avg: {AvgCostString}ms | Min: {MinCostString}ms | Max: {MaxCostString}ms");
                }

                GameEventMgrNotifyTimes.Clear();
                GameEventMgrNotifyTimerStartTick = DOL.GS.GameTimer.GetTickCount();
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
    "/diag currentservicetick - returns the current service the gameloop tick is on; useful for debugging lagging/frozen server.")]
    public class ECSDiagnosticsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
            {
                return;
            }

            if (IsSpammingCommand(client.Player, "Diag"))
            {
                return;
            }

            // extra check to disallow all but server GM's
            if (client.Account.PrivLevel < 2)
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("currentservicetick"))
            {
                DisplayMessage(client, "Gameloop CurrentService Tick: " + GameLoop.currentServiceTick);
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
                    ECS.Debug.Diagnostics.TogglePerfCounters(true);
                    DisplayMessage(client, "Performance diagnostics logging turned on. WARNING: This will spam the server logs.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    ECS.Debug.Diagnostics.TogglePerfCounters(false);
                    DisplayMessage(client, "Performance diagnostics logging turned off.");
                }
            }

            if (args[1].ToLower().Equals("notify"))
            {
                if (args[2].ToLower().Equals("on"))
                {
                    int interval = Int32.Parse(args[3]);
                    if (interval <= 0)
                    {
                        DisplayMessage(client, "Invalid interval argument. Please specify a value in milliseconds.");
                        return;
                    }

                    ECS.Debug.Diagnostics.StartGameEventMgrNotifyTimeReporting(interval);
                    DisplayMessage(client, "GameEventMgr Notify() logging turned on. WARNING: This will spam the server logs.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    ECS.Debug.Diagnostics.StopGameEventMgrNotifyTimeReporting();
                    DisplayMessage(client, "GameEventMgr Notify() logging turned off.");
                }
            }

            if (args[1].ToLower().Equals("timer"))
            {
                int tickcount = Int32.Parse(args[2]);
                if (tickcount <= 0)
                {
                    DisplayMessage(client, "Invalid tickcount argument. Please specify a positive interger value.");
                    return;
                }

                TimerService.debugTimer = true;
                TimerService.debugTimerTickCount = tickcount;
                DisplayMessage(client, "Debugging next " + tickcount + " TimerService tick(s)");
            }
        }
    }

    // This should be moved outside of this file if we want this as a real player-facing feature.
    [CmdAttribute(
        "&charstats",
        ePrivLevel.GM,
        "Shows normally hidden character stats.")]
    public class AtlasCharStatsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            var messages = new List<string>();
            string header = "Hidden Character Stats";

            GamePlayer player = client.Player;

            InventoryItem lefthand = null;
            lefthand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);

            // Block Chance
            if (player.HasAbility(Abilities.Shield))
            {
                if (lefthand == null)
                {
                    messages.Add($"Block Chance: No Shield Equipped!");
                }
                else
                {
                    double blockChance = player.GetBlockChance();
                    messages.Add($"Block Chance: {blockChance}%");
                }
            }

            // Parry Chance
            if (player.HasSpecialization(Specs.Parry))
            {
                double parryChance = player.GetParryChance();
                messages.Add($"Parry Chance: {parryChance}%");
            }

            // Evade Chance
            if (player.HasAbility(Abilities.Evade))
            {
                double evadeChance = player.GetEvadeChance();
                messages.Add($"Evade Chance: {evadeChance}%");
            }

            // Melee Crit Chance
            int meleeCritChance = player.GetModified(eProperty.CriticalMeleeHitChance);
            messages.Add($"Melee Crit Chance: {meleeCritChance}%");

            // Spell Crit Chance
            int spellCritChance = player.GetModified(eProperty.CriticalSpellHitChance);
            messages.Add($"Spell Crit Chance: {spellCritChance}");

            // Spell Casting Speed Bonus
            int spellCastSpeed = player.GetModified(eProperty.CastingSpeed);
            messages.Add($"Spell Casting Speed Bonus: {spellCastSpeed}%");

            // Heal Crit Chance
            int healCritChance = player.GetModified(eProperty.CriticalHealHitChance);
            messages.Add($"Heal Crit Chance: {healCritChance}%");

            // Archery Crit Chance
            if (player.HasSpecialization(Specs.Archery)
                || player.HasSpecialization(Specs.CompositeBow)
                || player.HasSpecialization(Specs.RecurveBow)
                || player.HasSpecialization(Specs.ShortBow)
                || player.HasSpecialization(Specs.Crossbow)
                || player.HasSpecialization(Specs.Longbow))
            {
                int archeryCritChance = player.GetModified(eProperty.CriticalArcheryHitChance);
                messages.Add($"Archery Crit Chance: {archeryCritChance}%");
            }

            // Finalize
            player.Out.SendCustomTextWindow(header, messages);
        }
    }

    [CmdAttribute(
    "&fsm",
    ePrivLevel.GM,
    "Toggle server logging of mob FSM states.",
    "/fsm debug <on|off> to toggle performance diagnostics logging on server.")]
    public class StateMachineCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
            {
                return;
            }

            if (IsSpammingCommand(client.Player, "fsm"))
            {
                return;
            }

            // extra check to disallow all but server GM's
            if (client.Account.PrivLevel < 2)
                return;

            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("debug"))
            {
                if (args[2].ToLower().Equals("on"))
                {
                    ECS.Debug.Diagnostics.ToggleStateMachineDebug(true);
                    DisplayMessage(client, "Mob state logging turned on.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    ECS.Debug.Diagnostics.ToggleStateMachineDebug(false);
                    DisplayMessage(client, "Mob state logging turned off.");
                }
            }
        }
    }

    [CmdAttribute(
    "&aggro",
    ePrivLevel.GM,
    "Toggle server logging of mob aggro tables.",
    "/aggro debug <on|off> to toggle mob aggro logging on server.")]
    public class AggroCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
            {
                return;
            }

            if (IsSpammingCommand(client.Player, "aggro"))
            {
                return;
            }

            // extra check to disallow all but server GM's
            if (client.Account.PrivLevel < 2)
                return;

            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("debug"))
            {
                if (args[2].ToLower().Equals("on"))
                {
                    ECS.Debug.Diagnostics.ToggleAggroDebug(true);
                    DisplayMessage(client, "Mob aggro logging turned on.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    ECS.Debug.Diagnostics.ToggleAggroDebug(false);
                    DisplayMessage(client, "Mob aggro logging turned off.");
                }
            }
        }
    }
}
