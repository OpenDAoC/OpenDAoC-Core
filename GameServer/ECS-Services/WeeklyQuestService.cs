using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class WeeklyQuestService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const string WEEKLY_INTERVAL_KEY = "WEEKLY";
        private static DateTime _lastWeeklyRollover;

        public static WeeklyQuestService Instance { get; }

        static WeeklyQuestService()
        {
            Instance = new();
        }

        private WeeklyQuestService()
        {
            IList<DbTaskRefreshInterval> loadQuestsProp = GameServer.Database.SelectAllObjects<DbTaskRefreshInterval>();

            foreach (DbTaskRefreshInterval interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(WEEKLY_INTERVAL_KEY))
                    _lastWeeklyRollover = interval.LastRollover;
            }
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            // This is where the weekly check will go once testing is finished.
            if (_lastWeeklyRollover.Date.DayOfYear + 7 < DateTime.Now.Date.DayOfYear || _lastWeeklyRollover.Year < DateTime.Now.Year)
            {
                DbTaskRefreshInterval loadQuestsProp = GameServer.Database.SelectObject<DbTaskRefreshInterval>(DB.Column("RolloverInterval").IsEqualTo(WEEKLY_INTERVAL_KEY));

                // Update the one we've got, or make a new one.
                if (loadQuestsProp != null)
                {
                    loadQuestsProp.LastRollover = DateTime.Now;
                    GameServer.Database.SaveObject(loadQuestsProp);
                }
                else
                {
                    DbTaskRefreshInterval newTime = new DbTaskRefreshInterval();
                    newTime.LastRollover = DateTime.Now;
                    newTime.RolloverInterval = WEEKLY_INTERVAL_KEY;
                    GameServer.Database.AddObject(newTime);
                }

                ServiceObjectView<GameClient> view;

                try
                {
                    view = ServiceObjectStore.UpdateAndGetView<GameClient>(ServiceObjectType.Client);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(ServiceObjectStore.UpdateAndGetView)} failed. Skipping this tick.", e);

                    return;
                }

                _lastWeeklyRollover = DateTime.Now;

                for (int i = 0; i < view.TotalValidCount; i++)
                {
                    GameClient client = view.Items[i];
                    client.Player?.RemoveFinishedQuests(x => x is Quests.WeeklyQuest);
                }

                IList<DbQuest> existingWeeklyQuests = GameServer.Database.SelectObjects<DbQuest>(DB.Column("Name").IsLike("%WeeklyQuest%"));

                foreach (DbQuest existingWeeklyQuest in existingWeeklyQuests)
                {
                    if (existingWeeklyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingWeeklyQuest);
                }
            }
        }
    }
}
