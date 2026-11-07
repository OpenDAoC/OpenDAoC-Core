using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PerformanceStatistics;
using DOL.Logging;

namespace DOL.GS
{
    public class StatPrint
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static StringBuilder _statsBuilder;
        private static Timer _timer;

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

            Task.Run(() =>
            {
                try
                {
                    _systemCpuUsagePercent = TryToCreateStatistic(() => new SystemCpuUsagePercent());
                    _programCpuUsagePercent = TryToCreateStatistic(() => new CurrentProcessCpuUsagePercentStatistic());
                    _pageFaultsPerSecond = TryToCreateStatistic(() => new PageFaultsPerSecondStatistic());
                    _diskTransfersPerSecond = TryToCreateStatistic(() => new DiskTransfersPerSecondStatistic());

                    _statsBuilder = new();
                    _timer = new(PrintStats, null, Timeout.Infinite, 0);
                    _timer.Change(0, 0);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error(e);
                }
            });

            return true;
        }

        private static void PrintStats(object state)
        {
            if (!log.IsInfoEnabled)
                return;

            ThreadPriority oldPriority = Thread.CurrentThread.Priority;

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                lock (_lock)
                {
                    PrintStatsImpl();
                }
            }
            finally
            {
                _timer?.Change(ServerProperties.Properties.STATPRINT_FREQUENCY, 0);
                Thread.CurrentThread.Priority = oldPriority;
            }
        }

        private static void PrintStatsImpl()
        {
            const int PADDING = 27;

            try
            {
                // Memory usage.
                long memUsedMb = GC.GetTotalMemory(false) / 1024 / 1024;
                long memCommittedMb = GC.GetGCMemoryInfo().TotalCommittedBytes / 1024 / 1024;

                // GC Collection counts (delta since last check).
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
                long tcpSendArgsPoolMisses = PacketProcessor.TcpSendArgsPoolMisses;
                long udpSendArgsPoolMisses = PacketProcessor.UdpSendArgsPoolMisses;
                long udpPacketsReceived = GameServer.Instance.UdpPacketsReceived;
                long udpPacketsDropped = GameServer.Instance.UdpPacketsDropped;
                long udpReceiveErrors = GameServer.Instance.UdpReceiveErrors;
                long udpBufferReturnFailures = GameServer.Instance.UdpBufferReturnFailures;
                long udpInvalidSize = GameServer.UdpInvalidSizeCount;
                long udpInvalidChecksum = GameServer.UdpInvalidChecksumCount;
                long udpInvalidSession = GameServer.UdpInvalidSessionCount;
                long udpSourceMismatch = GameServer.UdpSourceMismatchCount;
                long udpEndpointMismatch = GameServer.UdpEndpointMismatchCount;

                // Game loop average TPS.
                List<(int, double)> averageTps = GameLoop.GetAverageTps();

                // Performance counters.
                double procCpu = _programCpuUsagePercent?.GetNextValue() ?? 0.0;
                double sysCpu = _systemCpuUsagePercent?.GetNextValue() ?? 0.0;
                double pageFaults = _pageFaultsPerSecond?.GetNextValue() ?? 0.0;
                double diskTransfers = _diskTransfersPerSecond?.GetNextValue() ?? 0.0;

                _statsBuilder.Clear();

                _statsBuilder.AppendLine();
                _statsBuilder.AppendLine("[System]");
                _statsBuilder.AppendLine($"  {"CPU (process/system):",-PADDING} {procCpu:F1}% / {sysCpu:F1}%");
                _statsBuilder.AppendLine($"  {"Memory (used/commit):",-PADDING} {memUsedMb} MB / {memCommittedMb} MB");
                _statsBuilder.AppendLine($"  {"Page faults/sec:",-PADDING} {pageFaults:F1}");
                _statsBuilder.AppendLine($"  {"Disk transfers/sec:",-PADDING} {diskTransfers:F1}");

                _statsBuilder.AppendLine("[GameServer]");
                _statsBuilder.AppendLine($"  {"Clients:",-PADDING} {clientCount}");
                _statsBuilder.AppendLine($"  {"Event handlers (G/O):",-PADDING} {globalHandlers} / {objectHandlers}");

                if (averageTps.Count != 0)
                {
                    _statsBuilder.Append("  ");
                    StringBuilder labelBuilder = new("Game loop TPS (");

                    for (int i = averageTps.Count - 1; i >= 0; i--)
                    {
                        double interval = averageTps[i].Item1 / 1000.0;
                        labelBuilder.Append($"{interval}");

                        if (i != 0)
                            labelBuilder.Append('/');
                    }

                    labelBuilder.Append(")s:");
                    _statsBuilder.Append($"{labelBuilder,-PADDING} ");

                    for (int i = averageTps.Count - 1; i >= 0; i--)
                    {
                        double percentOfTarget = averageTps[i].Item2 / (10 / GameLoop.TickDuration);
                        _statsBuilder.Append($"{percentOfTarget:F1}%");

                        if (i != 0)
                            _statsBuilder.Append(" / ");
                    }

                    _statsBuilder.AppendLine();
                }
                else
                    _statsBuilder.AppendLine("  No TPS data available.");

                _statsBuilder.AppendLine("[Network]");

                if (tcpSendArgsPoolMisses > 0)
                    _statsBuilder.AppendLine($"  {"TCP send args pool misses:",-PADDING} {tcpSendArgsPoolMisses}");

                if (udpSendArgsPoolMisses > 0)
                    _statsBuilder.AppendLine($"  {"UDP send args pool misses:",-PADDING} {udpSendArgsPoolMisses}");

                if (udpPacketsReceived > 0 || udpPacketsDropped > 0 || udpReceiveErrors > 0)
                    _statsBuilder.AppendLine($"  {"UDP recv/drop/err:",-PADDING} {udpPacketsReceived} / {udpPacketsDropped} / {udpReceiveErrors}");

                if (udpInvalidSize > 0 || udpInvalidChecksum > 0 || udpInvalidSession > 0)
                    _statsBuilder.AppendLine($"  {"UDP invalid (size/crc/ses):",-PADDING} {udpInvalidSize} / {udpInvalidChecksum} / {udpInvalidSession}");

                if (udpSourceMismatch > 0 || udpEndpointMismatch > 0)
                    _statsBuilder.AppendLine($"  {"UDP mismatch (spoof/ep):",-PADDING} {udpSourceMismatch} / {udpEndpointMismatch}");

                if (udpBufferReturnFailures > 0)
                    _statsBuilder.AppendLine($"  {"UDP buffer return fails:",-PADDING} {udpBufferReturnFailures}");

                _statsBuilder.AppendLine("[.NET]");
                _statsBuilder.AppendLine($"  {"Workers (used/max):",-PADDING} {usedWorkers} / {maxWorkers}");
                _statsBuilder.AppendLine($"  {"IOCP (used/max):",-PADDING} {usedIocp} / {maxIocp}");
                _statsBuilder.Append($"  {"GC (0/1/2):",-PADDING} {gen0Delta} / {gen1Delta} / {gen2Delta}");

                log.Info(_statsBuilder.ToString());
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Stats log callback failed", e);
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
