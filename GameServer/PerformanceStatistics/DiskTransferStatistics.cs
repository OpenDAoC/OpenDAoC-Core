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
                    string[] columns = line.Split([' '], 20, StringSplitOptions.RemoveEmptyEntries);

                    if (columns.Length < 14)
                        continue;

                    string deviceName = columns[2];

                    // Skip virtual, memory-based, and other unwanted devices.
                    if (deviceName.StartsWith("loop") || deviceName.StartsWith("ram") || deviceName.StartsWith("zram"))
                        continue;

                    // Check if the device is a whole block device. This correctly includes sda, nvme0n1, md0, etc., while excluding partitions.
                    if (!Directory.Exists($"/sys/block/{deviceName}"))
                        continue;

                    // Avoid double-counting RAID. If a device is part of an MD-RAID array, it will have an 'md' subdirectory.
                    // We skip these members and only count the top-level 'mdX' device itself (which passes the above check).
                    if (Directory.Exists($"/sys/block/{deviceName}/md"))
                        continue;

                    long readIO = Convert.ToInt64(columns[3]);
                    long writeIO = Convert.ToInt64(columns[7]);
                    long discardIO = 0L;

                    // Discards are in field 15 (index 14) on kernels 4.18+.
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
