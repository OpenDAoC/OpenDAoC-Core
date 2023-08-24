using System;
using System.Collections.Generic;
using DOL.Database;
using ECS.Debug;

namespace DOL.GS
{
    public class MonthlyQuestService
    {
        private const string SERVICE_NAME = "MonthlyQuestService";
        private const string MONTHLY_INTERVAL_KEY = "MONTHLY";
        private static DateTime lastMonthlyRollover;

        static MonthlyQuestService()
        {
            IList<TaskRefreshIntervals> loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

            foreach (TaskRefreshIntervals interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(MONTHLY_INTERVAL_KEY))
                    lastMonthlyRollover = interval.LastRollover;
            }
        }

        public static void Tick()
        {
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

            // This is where the weekly check will go once testing is finished.
            if (lastMonthlyRollover.Date.Month < DateTime.Now.Date.Month || lastMonthlyRollover.Year < DateTime.Now.Year)
            {
                lastMonthlyRollover = DateTime.Now;
                TaskRefreshIntervals loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(MONTHLY_INTERVAL_KEY));

                // Update the one we've got, or make a new one.
                if (loadQuestsProp != null)
                {
                    loadQuestsProp.LastRollover = DateTime.Now;
                    GameServer.Database.SaveObject(loadQuestsProp);
                }
                else
                {
                    TaskRefreshIntervals newTime = new();
                    newTime.LastRollover = DateTime.Now;
                    newTime.RolloverInterval = MONTHLY_INTERVAL_KEY;
                    GameServer.Database.AddObject(newTime);
                }

                List<GameClient> clients = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out int lastValidIndex);

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GameClient client = clients[i];

                    if (client?.EntityManagerId.IsSet != true)
                        return;

                    client.Player?.RemoveFinishedQuests(x => x is Quests.MonthlyQuest);
                }

                IList<DBQuest> existingMonthlyQuests = GameServer.Database.SelectObjects<DBQuest>(DB.Column("Name").IsLike("%MonthlyQuest%"));

                foreach (DBQuest existingMonthlyQuest in existingMonthlyQuests)
                {
                    if (existingMonthlyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingMonthlyQuest);
                }
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
