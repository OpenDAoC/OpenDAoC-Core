using System;
using System.Reflection;
using System.Threading;
using Core.Base;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.PerformanceStatistics;
using log4net;

namespace Core.GS.GameEvents
{
    public class StatSave
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly int INITIAL_DELAY = 60000;

        private static volatile Timer _timer;
        private static long _lastBytesIn;
        private static long _lastBytesOut;
        private static long _lastMeasureTick = DateTime.Now.Ticks;
        private static IPerformanceStatistic _programCpuUsagePercent;
        private static object _lock  = new();

        [GameServerStartedEvent]
        public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
        {
            if (ServerProperties.Properties.STATSAVE_INTERVAL <= 0)
                return;

            lock (_lock)
            {
                _timer = new(new TimerCallback(SaveStats), null, INITIAL_DELAY, Timeout.Infinite);
                _programCpuUsagePercent = new CurrentProcessCpuUsagePercentStatistic();
            }
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
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

        public static void SaveStats(object state)
        {
            try
            {
                long ticks = DateTime.Now.Ticks;
                long time = ticks - _lastMeasureTick;
                _lastMeasureTick = ticks;
                time /= 10000000L;

                if (time < 1)
                {
                    log.Warn("Time has not changed since last call of SaveStats");
                    time = 1;
                }

                double serverCpuUsage = _programCpuUsagePercent.GetNextValue();

                DbServerStat newStat = new()
                {
                    CPU = (float) (serverCpuUsage >= 0 ? serverCpuUsage : 0),
                    Clients = ClientService.ClientCount,
                    Upload = (int) ((Statistics.BytesOut - _lastBytesOut) / time / 1024),
                    Download = (int) ((Statistics.BytesIn - _lastBytesIn) / time / 1024),
                    Memory = GC.GetTotalMemory(false) / 1024,
                    AlbionPlayers = ClientService.GetPlayersOfRealm(ERealm.Albion).Count,
                    MidgardPlayers = ClientService.GetPlayersOfRealm(ERealm.Midgard).Count,
                    HiberniaPlayers = ClientService.GetPlayersOfRealm(ERealm.Hibernia).Count
                };

                _lastBytesIn = Statistics.BytesIn;
                _lastBytesOut = Statistics.BytesOut;

                GameServer.Database.AddObject(newStat);
                GameServer.Database.SaveObject(newStat);
            }
            catch (Exception e)
            {
                log.Error("Updating server stats", e);
            }
            finally
            {
                lock (_lock)
                {
                    _timer?.Change(60 * 1000 * ServerProperties.Properties.STATSAVE_INTERVAL, Timeout.Infinite);
                }
            }
        }
    }
}
