using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class SystemCpuUsagePercent : IPerformanceStatistic
    {
        private readonly IPerformanceStatistic _processorTimeRatioStatistic;

        public SystemCpuUsagePercent()
        {
            if (OperatingSystem.IsWindows())
                _processorTimeRatioStatistic = new PerformanceCounterStatistic("Processor", "% Processor Time", "_Total");
            else if (OperatingSystem.IsLinux())
                _processorTimeRatioStatistic = new LinuxSystemCpuUsagePercent();
            else
                throw new PlatformNotSupportedException("System CPU usage percent statistic is not supported on this platform.");
        }

        public double GetNextValue()
        {
            return _processorTimeRatioStatistic.GetNextValue();
        }
    }

    [SupportedOSPlatform("linux")]
    public class LinuxSystemCpuUsagePercent : IPerformanceStatistic
    {
        private long _previousIdleTime;
        private long _previousTotalTime;

        public LinuxSystemCpuUsagePercent()
        {
            GetNextValue();
        }

        public double GetNextValue()
        {
            long currentIdleTime;
            long currentTotalTime;

            try
            {
                string cpuLine = File.ReadLines("/proc/stat").First();

                var cpuNumbers = cpuLine.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                                        .Skip(1) 
                                        .Select(long.Parse)
                                        .ToList();

                currentIdleTime = cpuNumbers[3] + cpuNumbers[4];
                currentTotalTime = cpuNumbers.Sum();
            }
            catch (Exception)
            {
                return 0.0;
            }

            long totalTimeDelta = currentTotalTime - _previousTotalTime;
            long idleTimeDelta = currentIdleTime - _previousIdleTime;

            _previousTotalTime = currentTotalTime;
            _previousIdleTime = currentIdleTime;

            if (totalTimeDelta <= 0)
                return 0.0;

            double busyTime = totalTimeDelta - idleTimeDelta;
            double cpuUsagePercent = busyTime / totalTimeDelta * 100.0;
            return Math.Clamp(cpuUsagePercent, 0, 100);
        }
    }
}
