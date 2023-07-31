using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DOL.GS.PerformanceStatistics
{
    public class DiskTransfersPerSecondStatistic : IPerformanceStatistic
    {
        IPerformanceStatistic _performanceStatistic;

        public DiskTransfersPerSecondStatistic()
        {
            _performanceStatistic = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                    new PerformanceCounterStatistic("PhysicalDisk", "Disk Transfers/sec", "_Total") :
                                    new PerSecondStatistic(new LinuxTotalDiskTransfers());
        }

        public double GetNextValue()
        {
            return _performanceStatistic.GetNextValue();
        }
    }

#if NET
    [UnsupportedOSPlatform("Windows")]
#endif
    public class LinuxTotalDiskTransfers : IPerformanceStatistic
    {
        public double GetNextValue()
        {
            string diskStats = File.ReadAllText("/proc/diskstats");
            long transferCount = 0L;

            foreach (string line in diskStats.Split('\n'))
            {
                string[] columns = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (columns.Length < 14)
                    continue;

                if (char.IsDigit(columns[2][^1]))
                    continue;

                long readIO = Convert.ToInt64(columns[3]);
                long writeIO = Convert.ToInt64(columns[7]);
                long discardIO = 0L;

                if (columns.Length >= 18)
                    discardIO = Convert.ToInt64(columns[14]);

                transferCount += readIO + writeIO + discardIO;
            }

            return transferCount;
        }
    }
}
