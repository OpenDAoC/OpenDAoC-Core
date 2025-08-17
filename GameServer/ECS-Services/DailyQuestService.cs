using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class DailyQuestService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const string DAILY_INTERVAL_KEY = "DAILY";
        private DateTime _lastDailyRollover;

        public static new DailyQuestService Instance { get; }

        static DailyQuestService()
        {
            Instance = new();
        }

        private DailyQuestService()
        {
            IList<DbTaskRefreshInterval> loadQuestsProp = GameServer.Database.SelectAllObjects<DbTaskRefreshInterval>();

            foreach (DbTaskRefreshInterval interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(DAILY_INTERVAL_KEY))
                    _lastDailyRollover = interval.LastRollover;
            }
        }

        public override void Tick()
        {
            ProcessPostedActions();

            if (_lastDailyRollover.Date.DayOfYear < DateTime.Now.Date.DayOfYear || _lastDailyRollover.Year < DateTime.Now.Year)
            {
                DbTaskRefreshInterval loadQuestsProp = GameServer.Database.SelectObject<DbTaskRefreshInterval>(DB.Column("RolloverInterval").IsEqualTo(DAILY_INTERVAL_KEY));

                // Update the one we've got, or make a new one.
                if (loadQuestsProp != null)
                {
                    loadQuestsProp.LastRollover = DateTime.Now;
                    GameServer.Database.SaveObject(loadQuestsProp);
                }
                else
                {
                    DbTaskRefreshInterval newTime = new();
                    newTime.LastRollover = DateTime.Now;
                    newTime.RolloverInterval = DAILY_INTERVAL_KEY;
                    GameServer.Database.AddObject(newTime);
                }

                List<GameClient> clients;
                int lastValidIndex;

                try
                {
                    clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(ServiceObjectType.Client, out lastValidIndex);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                    return;
                }

                _lastDailyRollover = DateTime.Now;

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GameClient client = clients[i];
                    client.Player?.RemoveFinishedQuests(x => x is Quests.DailyQuest);
                }

                IList<DbQuest> existingDailyQuests = GameServer.Database.SelectObjects<DbQuest>(DB.Column("Name").IsLike("%DailyQuest%"));

                foreach (DbQuest existingDailyQuest in existingDailyQuests)
                {
                    if (existingDailyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingDailyQuest);
                }
            }
        }
    }
}
