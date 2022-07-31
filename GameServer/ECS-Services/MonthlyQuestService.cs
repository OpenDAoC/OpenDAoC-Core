using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;

namespace DOL.GS;

public class MonthlyQuestService
{
    private static DateTime lastMonthlyRollover;

    private const string ServiceName = "MonthlyQuestService";
    private const string MonthlyIntervalKey = "MONTHLY";

    static MonthlyQuestService()
    {
        EntityManager.AddService(typeof(MonthlyQuestService));
        
        var loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

        foreach (var interval in loadQuestsProp)
        {
            if (interval.RolloverInterval.Equals(lastMonthlyRollover))
                lastMonthlyRollover = interval.LastRollover;
        }
        
        if(lastMonthlyRollover == null)
            lastMonthlyRollover = DateTime.UnixEpoch;
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);
        //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

        //this is where the weekly check will go once testing is finished
        if (lastMonthlyRollover.Date.Month < DateTime.Now.Date.Month || lastMonthlyRollover.Year < DateTime.Now.Year)
        {
            lastMonthlyRollover = DateTime.Now;
            
            //update db
            var loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(MonthlyIntervalKey));
            //update the one we've got, otherwise...
            if (loadQuestsProp != null)
            {
                loadQuestsProp.LastRollover = DateTime.Now;
                GameServer.Database.SaveObject(loadQuestsProp);
            }
            else
            {
                //make a new one
                TaskRefreshIntervals newTime = new TaskRefreshIntervals();
                newTime.LastRollover = DateTime.Now;
                newTime.RolloverInterval = MonthlyIntervalKey;
                GameServer.Database.AddObject(newTime);
            }
            
            foreach (var player in EntityManager.GetAllPlayers())
            {
                List<AbstractQuest> questsToRemove = new List<AbstractQuest>();
                foreach (var quest in player.QuestListFinished)
                {
                    if (quest is Quests.MonthlyQuest)
                    {
                        quest.AbortQuest();
                        questsToRemove.Add(quest);    
                    }
                }

                foreach (var quest in questsToRemove)
                {
                    player.QuestList.Remove(quest);
                    player.QuestListFinished.Remove(quest);
                }
            }
            
            var existingMonthlyQuests = GameServer.Database.SelectObjects<DBQuest>(DB.Column("Name").IsLike("%MonthlyQuest%"));

            foreach (var existingMonthlyQuest in existingMonthlyQuests)
            {
                if(existingMonthlyQuest.Step <= -1)
                    GameServer.Database.DeleteObject(existingMonthlyQuest);
            }
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}