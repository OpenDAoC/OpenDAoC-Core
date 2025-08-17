using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Events;
using DOL.GS.PerformanceStatistics;

namespace DOL.GS
{
    public class StatPrint
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static volatile Timer _timer;

        private static long _lastMeasureTick = DateTime.Now.Ticks;
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
            try
            {
                if (!log.IsInfoEnabled)
                    return;

                ThreadPriority oldPriority = Thread.CurrentThread.Priority;
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                long newTick = DateTime.Now.Ticks;
                long time = newTick - _lastMeasureTick;
                _lastMeasureTick = newTick;
                time /= 10000000L;

                if (time < 1)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Time has not changed since last call of PrintStats");

                    time = 1;
                }

                // Memory usage.
                long memUsedMb = GC.GetTotalMemory(false) / 1024 / 1024;
                long memCommittedMb = GC.GetGCMemoryInfo().TotalCommittedBytes / 1024 / 1024;

                // Collection counts.
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
                ThreadPool.GetAvailableThreads(out int poolCurrent, out int iocpCurrent);
                ThreadPool.GetMinThreads(out int poolMin, out int iocpMin);
                ThreadPool.GetMaxThreads(out int poolMax, out int iocpMax);

                // DoL event handlers.
                int globalHandlers = GameEventMgr.NumGlobalHandlers;
                int objectHandlers = GameEventMgr.NumObjectHandlers;

                // Game loop average TPS.
                List<(int, double)> averageTps = GameLoop.GetAverageTps();

                StringBuilder stats = new StringBuilder(256)
                    .Append($"-stats-  Mem={memUsedMb}MB/{memCommittedMb}MB")
                    .Append($"  GC={gen0Delta}/{gen1Delta}/{gen2Delta}")
                    .Append($"  Clients={ClientService.Instance.ClientCount}")
                    .AppendFormat($"  Pool={poolCurrent}/{poolMax}({poolMin})")
                    .AppendFormat($"  IOCP={iocpCurrent}/{iocpMax}({iocpMin})")
                    .AppendFormat($"  GH/OH={globalHandlers}/{objectHandlers}")
                    .Append($"  TPS=");

                for (int i = averageTps.Count - 1; i >= 0; i--)
                {
                    string percent = $"{averageTps[i].Item2 / (10.0 / GameLoop.TickDuration):0.0}%";
                    int length = percent.Length;
                    stats.Append(percent);

                    if (i > 0)
                    {
                        stats.Append("".PadRight(Math.Max(0, 6 - length)));
                        stats.Append('|');
                    }
                }

                AppendStatistic(stats, "CPU(sys)", _systemCpuUsagePercent, "%");
                AppendStatistic(stats, "CPU(proc)", _programCpuUsagePercent, "%"); // This is pretty slow, at least on Windows. (over 6ms)
                AppendStatistic(stats, "pg/s", _pageFaultsPerSecond);
                AppendStatistic(stats, "dsk/s", _diskTransfersPerSecond);

                if (log.IsInfoEnabled)
                    log.Info(stats.ToString());

                Thread.CurrentThread.Priority = oldPriority;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
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
                if (log.IsErrorEnabled)
                    log.Error(ex);

                return DummyPerformanceStatistic.Instance;
            }
        }
    }
}
