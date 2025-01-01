using System;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Events;
using DOL.GS.PerformanceStatistics;
using log4net;

namespace DOL.GS.GameEvents
{
    public class StatPrint
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static volatile Timer _timer;
        private static long _lastBytesIn;
        private static long _lastBytesOut;
        private static long _lastPacketsIn;
        private static long _lastPacketsOut;
        private static long _lastMeasureTick = DateTime.Now.Ticks;
        private static IPerformanceStatistic _systemCpuUsagePercent;
        private static IPerformanceStatistic _programCpuUsagePercent;
        private static IPerformanceStatistic _pageFaultsPerSecond;
        private static IPerformanceStatistic _diskTransfersPerSecond;
        private static readonly Lock _lock  = new();

        [GameServerStartedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            if (ServerProperties.Properties.STATPRINT_FREQUENCY <= 0)
                return;

            lock (_lock)
            {
                _timer = new(new TimerCallback(PrintStats), null, 10000, 0);
                _systemCpuUsagePercent = TryToCreateStatistic(() => new SystemCpuUsagePercent());
                _programCpuUsagePercent = TryToCreateStatistic(() => new CurrentProcessCpuUsagePercentStatistic());
                _pageFaultsPerSecond = TryToCreateStatistic(() => new PageFaultsPerSecondStatistic());
                _diskTransfersPerSecond = TryToCreateStatistic(() => new DiskTransfersPerSecondStatistic());
            }
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            lock (_lock)
            {
                if (_timer == null)
                    return;

                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }
        }

        public static void PrintStats(object state)
        {
            try
            {
                long newTick = DateTime.Now.Ticks;
                long time = newTick - _lastMeasureTick;
                _lastMeasureTick = newTick;
                time /= 10000000L;

                if (time < 1)
                {
                    log.Warn("Time has not changed since last call of PrintStats");
                    time = 1;
                }

                long inRate = (Statistics.BytesIn - _lastBytesIn) / time;
                long outRate = (Statistics.BytesOut - _lastBytesOut) / time;
                long inPckRate = (Statistics.PacketsIn - _lastPacketsIn) / time;
                long outPckRate = (Statistics.PacketsOut - _lastPacketsOut) / time;
                _lastBytesIn = Statistics.BytesIn;
                _lastBytesOut = Statistics.BytesOut;
                _lastPacketsIn = Statistics.PacketsIn;
                _lastPacketsOut = Statistics.PacketsOut;

                // Get thread pool info.
                ThreadPool.GetAvailableThreads(out int poolCurrent, out int iocpCurrent);
                ThreadPool.GetMinThreads(out int poolMin, out int iocpMin);
                ThreadPool.GetMaxThreads(out int poolMax, out int iocpMax);

                int globalHandlers = GameEventMgr.NumGlobalHandlers;
                int objectHandlers = GameEventMgr.NumObjectHandlers;

                if (log.IsInfoEnabled)
                {
                    StringBuilder stats = new StringBuilder(256)
                        .Append("-stats- Mem=").Append(GC.GetTotalMemory(false) / 1024 / 1024).Append("MB")
                        .Append("  Clients=").Append(ClientService.ClientCount)
                        //.Append("  Down=").Append(inRate / 1024).Append("kb/s (").Append(Statistics.BytesIn / 1024 / 1024).Append("MB)")
                        //.Append("  Up=").Append(outRate / 1024).Append("kb/s (").Append(Statistics.BytesOut / 1024 / 1024).Append("MB)")
                        //.Append("  In=").Append(inPckRate).Append("pck/s (").Append(Statistics.PacketsIn / 1000).Append("K)")
                        //.Append("  Out=").Append(outPckRate).Append("pck/s (").Append(Statistics.PacketsOut / 1000).Append("K)")
                        .AppendFormat("  Pool={0}/{1}({2})", poolCurrent, poolMax, poolMin)
                        .AppendFormat("  IOCP={0}/{1}({2})", iocpCurrent, iocpMax, iocpMin)
                        .AppendFormat("  GH/OH={0}/{1}", globalHandlers, objectHandlers);

                    AppendStatistic(stats, "CPU(sys)", _systemCpuUsagePercent, "%");
                    AppendStatistic(stats, "CPU(proc)", _programCpuUsagePercent, "%");
                    AppendStatistic(stats, "pg/s", _pageFaultsPerSecond);
                    AppendStatistic(stats, "dsk/s", _diskTransfersPerSecond);

                    log.Info(stats);
                }
            }
            catch (Exception e)
            {
                log.Error("stats Log callback", e);
            }
            finally
            {
                lock (_lock)
                {
                    _timer?.Change(ServerProperties.Properties.STATPRINT_FREQUENCY, 0);
                }
            }
        }

        private static void AppendStatistic(StringBuilder stringBuilder, string shortName, IPerformanceStatistic statistic, string unit = null)
        {
            if (statistic == null)
                return;

            unit ??= string.Empty;
            stringBuilder.Append($"  {shortName}={statistic.GetNextValue().ToString("0.0")}{unit}");
        }

        private static IPerformanceStatistic TryToCreateStatistic(Func<IPerformanceStatistic> createFunc)
        {
            try
            {
                return createFunc();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return null;
            }
        }
    }
}
