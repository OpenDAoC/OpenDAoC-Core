using System;
using System.Collections.Generic;
using DOL.Database;
using ECS.Debug;

namespace DOL.GS
{
    public class DailyQuestService
    {
        private const string SERVICE_NAME = "DailyQuestService";
        private const string DAILY_INTERVAL_KEY = "DAILY";
        private static DateTime lastDailyRollover;

        static DailyQuestService()
        {
            IList<TaskRefreshIntervals> loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

            foreach (TaskRefreshIntervals interval in loadQuestsProp)
            {
                if (interval.RolloverInterval.Equals(DAILY_INTERVAL_KEY))
                    lastDailyRollover = interval.LastRollover;
            }
        }

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

            if (lastDailyRollover.Date.DayOfYear < DateTime.Now.Date.DayOfYear || lastDailyRollover.Year < DateTime.Now.Year)
            {
                lastDailyRollover = DateTime.Now;
                TaskRefreshIntervals loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(DAILY_INTERVAL_KEY));

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
                    newTime.RolloverInterval = DAILY_INTERVAL_KEY;
                    GameServer.Database.AddObject(newTime);
                }

                List<GameClient> clients = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out int lastValidIndex);

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GameClient client = clients[i];

                    if (client?.EntityManagerId.IsSet != true)
                        return;

                    client.Player?.RemoveFinishedQuests(x => x is Quests.DailyQuest);
                }

                IList<DBQuest> existingDailyQuests = GameServer.Database.SelectObjects<DBQuest>(DB.Column("Name").IsLike("%DailyQuest%"));

                foreach (DBQuest existingDailyQuest in existingDailyQuests)
                {
                    if (existingDailyQuest.Step <= -1)
                        GameServer.Database.DeleteObject(existingDailyQuest);
                }

                //Console.WriteLine($"Daily refresh");
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
