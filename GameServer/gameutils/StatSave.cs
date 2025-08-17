using System;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.PerformanceStatistics;

namespace DOL.GS.GameEvents
{
    public class StatSave
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly int INITIAL_DELAY = 60000;

        private static volatile Timer _timer;
        private static long _lastMeasureTick = DateTime.Now.Ticks;
        private static IPerformanceStatistic _programCpuUsagePercent;
        private static readonly Lock _lock  = new();

        [GameServerStartedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
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
                    Clients = ClientService.Instance.ClientCount,
                    Memory = GC.GetTotalMemory(false) / 1024,
                    AlbionPlayers = ClientService.Instance.GetPlayersOfRealm(eRealm.Albion).Count,
                    MidgardPlayers = ClientService.Instance.GetPlayersOfRealm(eRealm.Midgard).Count,
                    HiberniaPlayers = ClientService.Instance.GetPlayersOfRealm(eRealm.Hibernia).Count
                };

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
