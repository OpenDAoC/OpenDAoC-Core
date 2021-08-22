using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ECS.Debug
{
    public static class Diagnostics
    {
        private static bool PerfCountersEnabled = false; // [Takii] Change if you want perf stats in the logs every tick. Will probably be moved to being enabled via a console command instead.
        private static Dictionary<string, System.Diagnostics.Stopwatch> PerfCounters = new Dictionary<string, System.Diagnostics.Stopwatch>();

        static Diagnostics()
        {
#if !DEBUG            
            PerfCountersEnabled = false;
#endif
        }

        public static void Tick()
        {
#if !DEBUG
            return;
#endif
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