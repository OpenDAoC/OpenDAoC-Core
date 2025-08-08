using System;
using System.IO;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class DiskTransfersPerSecondStatistic : IPerformanceStatistic
    {
        IPerformanceStatistic _performanceStatistic;

        public DiskTransfersPerSecondStatistic()
        {
            if (OperatingSystem.IsWindows())
                _performanceStatistic = new PerformanceCounterStatistic("PhysicalDisk", "Disk Transfers/sec", "_Total");
            else if (OperatingSystem.IsLinux())
                _performanceStatistic = new PerSecondStatistic(new LinuxTotalDiskTransfers());
            else
                throw new PlatformNotSupportedException("Disk transfers per second statistic is not supported on this platform.");
        }

        public double GetNextValue()
        {
            return _performanceStatistic.GetNextValue();
        }
    }

    [SupportedOSPlatform("linux")]
    public class LinuxTotalDiskTransfers : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            long transferCount = 0L;

            try
            {
                foreach (string line in File.ReadLines("/proc/diskstats"))
                {
                    string[] columns = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                    if (columns.Length < 14 || char.IsDigit(columns[2][^1]))
                        continue;

                    long readIO = Convert.ToInt64(columns[3]);
                    long writeIO = Convert.ToInt64(columns[7]);
                    long discardIO = 0L;

                    if (columns.Length >= 18)
                        discardIO = Convert.ToInt64(columns[14]);

                    transferCount += readIO + writeIO + discardIO;
                }
            }
            catch (Exception)
            {
                return transferCount;
            }

            return transferCount;
        }
    }
}
