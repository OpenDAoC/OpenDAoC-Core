using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class MonthlyQuestService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const string MONTHLY_INTERVAL_KEY = "MONTHLY";
        private DateTime _lastMonthlyRollover;

        public static new MonthlyQuestService Instance { get; }

        static MonthlyQuestService()
        {
            Instance = new();
        }

        private MonthlyQuestService()
        {
            IList<DbTaskRefreshInterval> loadQuestsProp = GameServer.Database.SelectAllObjects<DbTaskRefreshInterval>();

            foreach (DbTaskRefreshInterval interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(MONTHLY_INTERVAL_KEY))
                    _lastMonthlyRollover = interval.LastRollover;
            }
        }

        public override void Tick()
        {
            // This is where the weekly check will go once testing is finished.
            if (_lastMonthlyRollover.Date.Month < DateTime.Now.Date.Month || _lastMonthlyRollover.Year < DateTime.Now.Year)
            {
                DbTaskRefreshInterval loadQuestsProp = GameServer.Database.SelectObject<DbTaskRefreshInterval>(DB.Column("RolloverInterval").IsEqualTo(MONTHLY_INTERVAL_KEY));

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
                    newTime.RolloverInterval = MONTHLY_INTERVAL_KEY;
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

                _lastMonthlyRollover = DateTime.Now;

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GameClient client = clients[i];
                    client.Player?.RemoveFinishedQuests(x => x is Quests.MonthlyQuest);
                }

                IList<DbQuest> existingMonthlyQuests = GameServer.Database.SelectObjects<DbQuest>(DB.Column("Name").IsLike("%MonthlyQuest%"));

                foreach (DbQuest existingMonthlyQuest in existingMonthlyQuests)
                {
                    if (existingMonthlyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingMonthlyQuest);
                }
            }
        }
    }
}
