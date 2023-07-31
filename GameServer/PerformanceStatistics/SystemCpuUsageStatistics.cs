using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class SystemCpuUsagePercent : IPerformanceStatistic
    {
        private IPerformanceStatistic _processorTimeRatioStatistic;

        public SystemCpuUsagePercent()
        {
            _processorTimeRatioStatistic = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                           new PerformanceCounterStatistic("Processor", "% processor time", "_total") :
                                           new LinuxSystemCpuUsagePercent();
        }

        public double GetNextValue()
        {
            return _processorTimeRatioStatistic.GetNextValue();
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxSystemCpuUsagePercent : IPerformanceStatistic
    {
        private IPerformanceStatistic _processorTimeStatistic;
        private IPerformanceStatistic _idleTimeStatistic;

        public LinuxSystemCpuUsagePercent()
        {
            _processorTimeStatistic = new PerSecondStatistic(new LinuxTotalProcessorTimeInSeconds());
            _idleTimeStatistic = new PerSecondStatistic(new LinuxSystemIdleProcessorTimeInSeconds());
        }

        public double GetNextValue()
        {
            double cpuUsage = 1 - _idleTimeStatistic.GetNextValue() / _processorTimeStatistic.GetNextValue();
            return cpuUsage * 100;
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxTotalProcessorTimeInSeconds : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            double cpuTimeInSeconds = File.ReadAllText("/proc/stat").Split(Environment.NewLine).First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(c => Convert.ToInt64(c)).Aggregate(0L, (a, b) => a + b) * 0.001;
            return cpuTimeInSeconds;
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxSystemIdleProcessorTimeInSeconds : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            string cpuIdleTimeString = File.ReadAllText("/proc/stat").Split('\n').First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[4];
            double cpuIdleTimeInSeconds = Convert.ToInt64(cpuIdleTimeString) * 0.001;
            return cpuIdleTimeInSeconds;
        }
    }
}
