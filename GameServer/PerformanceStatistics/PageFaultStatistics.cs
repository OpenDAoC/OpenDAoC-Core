using System;
using System.IO;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class PageFaultsPerSecondStatistic : IPerformanceStatistic
    {
        private readonly IPerformanceStatistic _performanceStatistic;

        public PageFaultsPerSecondStatistic()
        {
            if (OperatingSystem.IsWindows())
                _performanceStatistic = new PerformanceCounterStatistic("Memory", "Pages/sec", null);
            else if (OperatingSystem.IsLinux())
                _performanceStatistic = new PerSecondStatistic(new LinuxTotalMajorPageFaults());
            else
                throw new PlatformNotSupportedException("Page faults per second statistic is not supported on this platform.");
        }

        public double GetNextValue()
        {
            return _performanceStatistic.GetNextValue();
        }
    }

    [SupportedOSPlatform("linux")]
    public class LinuxTotalMajorPageFaults : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            try
            {
                const string key = "pgmajfault";

                foreach (string line in File.ReadLines("/proc/vmstat"))
                {
                    if (line.StartsWith(key))
                    {
                        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 2)
                            return Convert.ToInt64(parts[1]);
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return 0;
        }
    }
}
