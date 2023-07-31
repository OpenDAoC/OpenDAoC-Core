using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class PageFaultsPerSecondStatistic : IPerformanceStatistic
    {
        IPerformanceStatistic _performanceStatistic;

        public PageFaultsPerSecondStatistic()
        {
            _performanceStatistic = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                    new PerformanceCounterStatistic("Memory", "Pages/sec", null) :
                                    new LinuxPageFaultsPerSecondStatistic();
        }

        public double GetNextValue()
        {
            return _performanceStatistic.GetNextValue();
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxPageFaultsPerSecondStatistic : IPerformanceStatistic
    {
        private IPerformanceStatistic _memoryFaultsPerSecondStatistic;

        public LinuxPageFaultsPerSecondStatistic()
        {
            _memoryFaultsPerSecondStatistic = new PerSecondStatistic(new LinuxTotalPageFaults());
        }

        public double GetNextValue()
        {
            return _memoryFaultsPerSecondStatistic.GetNextValue();
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxTotalPageFaults : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            return Convert.ToInt64(File.ReadAllText("/proc/vmstat").Split(Environment.NewLine).Where(s => s.StartsWith("pgfault")).First().Split(' ')[1]);
        }
    }
}
