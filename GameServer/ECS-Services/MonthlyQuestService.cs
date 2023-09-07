using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Quests;
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

                List<GamePlayer> players = EntityManager.UpdateAndGetAll<GamePlayer>(EntityManager.EntityType.Player, out int lastValidIndex);

                for (int i = 0; i < lastValidIndex + 1; i++)
                {
                    GamePlayer player = players[i];

                    if (!player.EntityManagerId.IsSet)
                        continue;

                    List<AbstractQuest> questsToRemove;

                    lock (player.QuestLock)
                    {
                        questsToRemove = player.QuestListFinished.Where(x => x is Quests.MonthlyQuest).ToList();

                        foreach (AbstractQuest quest in questsToRemove)
                        {
                            quest.AbortQuest();
                            player.QuestListFinished.Remove(quest);
                        }
                    }
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
