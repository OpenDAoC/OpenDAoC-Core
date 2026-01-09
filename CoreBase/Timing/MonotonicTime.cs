using System.Diagnostics;

namespace DOL.Timing
{
    public static class MonotonicTime
    {
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;

        public static long NowMs => Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
    }
}
