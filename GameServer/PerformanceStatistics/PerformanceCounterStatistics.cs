using System.Diagnostics;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    [SupportedOSPlatform("windows")]
    public class PerformanceCounterStatistic : IPerformanceStatistic
    {
        private PerformanceCounter _performanceCounter;

        public PerformanceCounterStatistic(string categoryName, string counterName, string instanceName)
        {
            _performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName);
            _performanceCounter.NextValue();
        }

        public double GetNextValue()
        {
            return _performanceCounter.NextValue();
        }
    }
}
