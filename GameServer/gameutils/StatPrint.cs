using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PerformanceStatistics;

namespace DOL.GS
{
    public class StatPrint
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static volatile Timer _timer;

        private static long _prevGen0;
        private static long _prevGen1;
        private static long _prevGen2;
        private static IPerformanceStatistic _systemCpuUsagePercent;
        private static IPerformanceStatistic _programCpuUsagePercent;
        private static IPerformanceStatistic _pageFaultsPerSecond;
        private static IPerformanceStatistic _diskTransfersPerSecond;
        private static readonly Lock _lock  = new();

        public static bool Init()
        {
            if (ServerProperties.Properties.STATPRINT_FREQUENCY <= 0)
                return true;

            try
            {
                _timer = new(new TimerCallback(PrintStats), null, 10000, 0);
                _systemCpuUsagePercent = TryToCreateStatistic(() => new SystemCpuUsagePercent());
                _programCpuUsagePercent = TryToCreateStatistic(() => new CurrentProcessCpuUsagePercentStatistic());
                _pageFaultsPerSecond = TryToCreateStatistic(() => new PageFaultsPerSecondStatistic());
                _diskTransfersPerSecond = TryToCreateStatistic(() => new DiskTransfersPerSecondStatistic());
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                return false;
            }

            return true;
        }

        public static void PrintStats(object state)
        {
            int PADDING = 27;

            try
            {
                if (!log.IsInfoEnabled)
                    return;

                ThreadPriority oldPriority = Thread.CurrentThread.Priority;
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                // Memory usage
                long memUsedMb = GC.GetTotalMemory(false) / 1024 / 1024;
                long memCommittedMb = GC.GetGCMemoryInfo().TotalCommittedBytes / 1024 / 1024;

                // GC Collection counts (delta since last check)
                long gen0Total = GC.CollectionCount(0);
                long gen1Total = GC.CollectionCount(1);
                long gen2Total = GC.CollectionCount(2);
                long gen0Delta = gen0Total - _prevGen0;
                long gen1Delta = gen1Total - _prevGen1;
                long gen2Delta = gen2Total - _prevGen2;
                _prevGen0 = gen0Total;
                _prevGen1 = gen1Total;
                _prevGen2 = gen2Total;

                // Thread pool info.
                ThreadPool.GetMaxThreads(out int maxWorkers, out int maxIocp);
                ThreadPool.GetAvailableThreads(out int availableWorkers, out int availableIocp);
                int usedWorkers = maxWorkers - availableWorkers;
                int usedIocp = maxIocp - availableIocp;

                // Application-specific stats.
                int clientCount = ClientService.Instance.ClientCount;
                int globalHandlers = GameEventMgr.NumGlobalHandlers;
                int objectHandlers = GameEventMgr.NumObjectHandlers;
                long sendBufferPoolExhaustedCount = PacketProcessor.SendBufferPoolExhaustedCount;

                // Game loop average TPS.
                List<(int, double)> averageTps = GameLoop.GetAverageTps();

                // Performance counters.
                double procCpu = _programCpuUsagePercent?.GetNextValue() ?? 0.0;
                double sysCpu = _systemCpuUsagePercent?.GetNextValue() ?? 0.0;
                double pageFaults = _pageFaultsPerSecond?.GetNextValue() ?? 0.0;
                double diskTransfers = _diskTransfersPerSecond?.GetNextValue() ?? 0.0;

                StringBuilder stats = new(512);

                stats.AppendLine();
                stats.AppendLine("[System]");
                stats.AppendLine($"  {"CPU (process/system):".PadRight(PADDING)} {procCpu:F1}% / {sysCpu:F1}%");
                stats.AppendLine($"  {"Memory (used/commit):".PadRight(PADDING)} {memUsedMb} MB / {memCommittedMb} MB");
                stats.AppendLine($"  {"Page faults/sec:".PadRight(PADDING)} {pageFaults:F1}");
                stats.AppendLine($"  {"Disk transfers/sec:".PadRight(PADDING)} {diskTransfers:F1}");

                stats.AppendLine("[Application]");
                stats.AppendLine($"  {"Clients:".PadRight(PADDING)} {clientCount}");
                stats.AppendLine($"  {"Event handlers (G/O):".PadRight(PADDING)} {globalHandlers} / {objectHandlers}");

                if (averageTps.Count != 0)
                {
                    stats.Append("  ");
                    StringBuilder labelBuilder = new("Game loop TPS (");

                    for (int i = averageTps.Count - 1; i >= 0; i--)
                    {
                        double interval = averageTps[i].Item1 / 1000.0;
                        labelBuilder.Append($"{interval}");

                        if (i != 0)
                            labelBuilder.Append('/');
                    }

                    labelBuilder.Append(")s:");
                    stats.Append($"{labelBuilder.ToString().PadRight(PADDING)} ");

                    for (int i = averageTps.Count - 1; i >= 0; i--)
                    {
                        double percentOfTarget = averageTps[i].Item2 / (10.0 / GameLoop.TickDuration);
                        stats.Append($"{percentOfTarget:F1}%");

                        if (i != 0)
                            stats.Append(" / ");
                    }

                    stats.AppendLine();
                }
                else
                    stats.AppendLine("  No TPS data available.");

                if (sendBufferPoolExhaustedCount > 0)
                    stats.AppendLine($"  {"Packet pool misses:".PadRight(PADDING)} {sendBufferPoolExhaustedCount}");

                stats.AppendLine("[.NET]");
                stats.AppendLine($"  {"Workers (used/max):".PadRight(PADDING)} {usedWorkers} / {maxWorkers}");
                stats.AppendLine($"  {"IOCP (used/max):".PadRight(PADDING)} {usedIocp} / {maxIocp}");
                stats.Append($"  {"GC ƒ¢ (0/1/2):".PadRight(PADDING)} {gen0Delta} / {gen1Delta} / {gen2Delta}");

                log.Info(stats.ToString());
                Thread.CurrentThread.Priority = oldPriority;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Stats log callback failed", e);
            }
            finally
            {
                lock (_lock)
                {
                    _timer?.Change(ServerProperties.Properties.STATPRINT_FREQUENCY, 0);
                }
            }
        }

        private static IPerformanceStatistic TryToCreateStatistic(Func<IPerformanceStatistic> createFunc)
        {
            try
            {
                return createFunc();
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error(ex);

                return DummyPerformanceStatistic.Instance;
            }
        }
    }
}
