using System;
using System.Collections.Generic;
using DOL.Database;
using ECS.Debug;

namespace DOL.GS
{
    public class WeeklyQuestService
    {
        private const string SERVICE_NAME = "WeeklyQuestService";
        private const string WEEKLY_INTERVAL_KEY = "WEEKLY";
        private static DateTime lastWeeklyRollover;

        static WeeklyQuestService()
        {
            IList<DbTaskRefreshInterval> loadQuestsProp = GameServer.Database.SelectAllObjects<DbTaskRefreshInterval>();

            foreach (DbTaskRefreshInterval interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(WEEKLY_INTERVAL_KEY))
                    lastWeeklyRollover = interval.LastRollover;
            }
        }

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

            // This is where the weekly check will go once testing is finished.
            if (lastWeeklyRollover.Date.DayOfYear + 7 < DateTime.Now.Date.DayOfYear || lastWeeklyRollover.Year < DateTime.Now.Year)
            {
                lastWeeklyRollover = DateTime.Now;
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

                List<GameClient> clients = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out int lastValidIndex);

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GameClient client = clients[i];

                    if (client?.EntityManagerId.IsSet != true)
                        return;

                    client.Player?.RemoveFinishedQuests(x => x is Quests.WeeklyQuest);
                }

                IList<DbQuest> existingWeeklyQuests = GameServer.Database.SelectObjects<DbQuest>(DB.Column("Name").IsLike("%WeeklyQuest%"));

                foreach (DbQuest existingWeeklyQuest in existingWeeklyQuests)
                {
                    if (existingWeeklyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingWeeklyQuest);
                }
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
