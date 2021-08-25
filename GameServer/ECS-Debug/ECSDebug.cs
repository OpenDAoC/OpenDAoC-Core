using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ECS.Debug
{
    public static class Diagnostics
    {
        private static bool PerfCountersEnabled = false;
        private static Dictionary<string, System.Diagnostics.Stopwatch> PerfCounters = new Dictionary<string, System.Diagnostics.Stopwatch>();

        public static void TogglePerfCounters(bool enabled)
        {
            PerfCountersEnabled = enabled;
        }
        
        public static void Tick()
        {
            ReportPerfCounters();
        }

        public static void StartPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            PerfCounters.Add(uniqueID, stopwatch);
        }

        public static void StopPerfCounter(string uniqueID)
        {
            if (!PerfCountersEnabled)
                return;

            System.Diagnostics.Stopwatch stopwatch;
            if (PerfCounters.TryGetValue(uniqueID, out stopwatch))
            {
                stopwatch.Stop();
            }
        }

        private static void ReportPerfCounters()
        {
            if (!PerfCountersEnabled)
                return;

            // Report perf counters that were active this frame and then flush them.
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
                Console.WriteLine(logString);
                PerfCounters.Clear();
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
    "/diag perf <on|off> to toggle performance diagnostics logging on server.")]
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
        }
    }
}