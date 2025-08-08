using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class CurrentProcessCpuUsagePercentStatistic : IPerformanceStatistic
    {
        private readonly IPerformanceStatistic _processorTimeRatioStatistic;

        public CurrentProcessCpuUsagePercentStatistic()
        {
            if (OperatingSystem.IsWindows())
                _processorTimeRatioStatistic = new WindowsProcessCpuUsagePercentStatistic();
            else if (OperatingSystem.IsLinux())
                _processorTimeRatioStatistic = new LinuxCurrentProcessUsagePercentStatistic();
            else
                throw new PlatformNotSupportedException("Current process CPU usage percent statistic is not supported on this platform.");
        }

        public double GetNextValue()
        {
            return _processorTimeRatioStatistic.GetNextValue();
        }
    }

    [SupportedOSPlatform("windows")]
    public class WindowsProcessCpuUsagePercentStatistic : IPerformanceStatistic
    {
        private readonly Process _process;
        private readonly IPerformanceStatistic _performanceCounter;

        public WindowsProcessCpuUsagePercentStatistic()
        {
            // NOTE: This can be ambiguous if multiple processes share the same name.
            _process = Process.GetCurrentProcess();
            _performanceCounter = new PerformanceCounterStatistic("Process", "% Processor Time", _process.ProcessName);
        }

        public double GetNextValue()
        {
            return _performanceCounter.GetNextValue() / Environment.ProcessorCount;
        }
    }

    [SupportedOSPlatform("linux")]
    public class LinuxCurrentProcessUsagePercentStatistic : IPerformanceStatistic
    {
        private long _previousTotalSystemTime;
        private long _previousProcessTime;

        public LinuxCurrentProcessUsagePercentStatistic()
        {
            GetNextValue();
        }

        public double GetNextValue()
        {
            long currentTotalSystemTime;
            long currentProcessTime;

            try
            {
                string systemStatLine = File.ReadLines("/proc/stat").First();

                var systemNumbers = systemStatLine.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                                                  .Skip(1)
                                                  .Select(long.Parse);
                currentTotalSystemTime = systemNumbers.Sum();

                string processStatContent = File.ReadAllText($"/proc/{Environment.ProcessId}/stat");
                int lastParen = processStatContent.LastIndexOf(')');
                string[] processStatParts = processStatContent[(lastParen + 2)..].Split(' ');

                long utime = long.Parse(processStatParts[11]);
                long stime = long.Parse(processStatParts[12]);
                currentProcessTime = utime + stime;
            }
            catch (Exception)
            {
                return 0.0;
            }

            long totalSystemDelta = currentTotalSystemTime - _previousTotalSystemTime;
            long processDelta = currentProcessTime - _previousProcessTime;

            _previousTotalSystemTime = currentTotalSystemTime;
            _previousProcessTime = currentProcessTime;

            if (totalSystemDelta <= 0)
                return 0.0;

            double cpuUsage = (double) processDelta / totalSystemDelta * 100.0;
            return Math.Clamp(cpuUsage, 0, 100);
        }
    }
}
