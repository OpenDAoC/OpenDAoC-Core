using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DOL.Database;
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
        private static object _GameEventMgrNotifyLock = new();
        private static bool PerfCountersEnabled = false;
        private static bool stateMachineDebugEnabled = false;
        private static bool aggroDebugEnabled = false;
        private static Dictionary<string, Stopwatch> PerfCounters = new();
        private static object _PerfCountersLock = new();
        private static bool GameEventMgrNotifyProfilingEnabled = false;
        private static int GameEventMgrNotifyTimerInterval = 0;
        private static long GameEventMgrNotifyTimerStartTick = 0;
        private static Stopwatch GameEventMgrNotifyStopwatch;
        private static Dictionary<string, List<double>> GameEventMgrNotifyTimes = new();

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
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            ReportPerfCounters();

            if (GameEventMgrNotifyProfilingEnabled)
            {
                if ((GameTimer.GetTickCount() - GameEventMgrNotifyTimerStartTick) > GameEventMgrNotifyTimerInterval)
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
            lock(_PerfCountersLock)
            {
                PerfCounters.TryAdd(uniqueID, stopwatch);
            }
        }

        public static void StopPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            lock (_PerfCountersLock)
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
            lock(_PerfCountersLock)
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

            lock (_GameEventMgrNotifyLock)
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
            GameEventMgrNotifyTimerStartTick = GameTimer.GetTickCount();
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
            string ActualInterval = Util.TruncateString((GameTimer.GetTickCount() - GameEventMgrNotifyTimerStartTick).ToString(), 5);
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
                GameEventMgrNotifyTimerStartTick = GameTimer.GetTickCount();
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

                NPCThinkService.DebugTickCount = tickcount;
                DisplayMessage(client, "Debugging next " + tickcount + " NPCThinkService tick(s)");
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
            List<string> messages = new();
            string header = "Hidden Character Stats";
            GamePlayer player = client.Player;
            InventoryItem lefthand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);

            // Block chance.
            if (player.HasAbility(Abilities.Shield))
            {
                if (lefthand == null)
                    messages.Add($"Block Chance: No Shield Equipped!");
                else
                {
                    double blockChance = player.GetBlockChance();
                    messages.Add($"Block Chance: {blockChance}%");
                }
            }

            // Parry chance.
            if (player.HasSpecialization(Specs.Parry))
            {
                double parryChance = player.GetParryChance();
                messages.Add($"Parry Chance: {parryChance}%");
            }

            // Evade chance.
            if (player.HasAbility(Abilities.Evade))
            {
                double evadeChance = player.GetEvadeChance();
                messages.Add($"Evade Chance: {evadeChance}%");
            }

            // Melee crit chance.
            int meleeCritChance = player.GetModified(eProperty.CriticalMeleeHitChance);
            messages.Add($"Melee Crit Chance: {meleeCritChance}%");

            // Spell crit chance
            int spellCritChance = player.GetModified(eProperty.CriticalSpellHitChance);
            messages.Add($"Spell Crit Chance: {spellCritChance}");

            // Spell casting speed bonus.
            int spellCastSpeed = player.GetModified(eProperty.CastingSpeed);
            messages.Add($"Spell Casting Speed Bonus: {spellCastSpeed}%");

            // Heal crit chance.
            int healCritChance = player.GetModified(eProperty.CriticalHealHitChance);
            messages.Add($"Heal Crit Chance: {healCritChance}%");

            // Archery crit chance.
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

            // Finalize.
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
                return;

            if (IsSpammingCommand(client.Player, "fsm"))
                return;

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
                    Diagnostics.ToggleStateMachineDebug(true);
                    DisplayMessage(client, "Mob state logging turned on.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    Diagnostics.ToggleStateMachineDebug(false);
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
                return;

            if (IsSpammingCommand(client.Player, "aggro"))
                return;

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
                    Diagnostics.ToggleAggroDebug(true);
                    DisplayMessage(client, "Mob aggro logging turned on.");
                }
                else if (args[2].ToLower().Equals("off"))
                {
                    Diagnostics.ToggleAggroDebug(false);
                    DisplayMessage(client, "Mob aggro logging turned off.");
                }
            }
        }
    }
}
