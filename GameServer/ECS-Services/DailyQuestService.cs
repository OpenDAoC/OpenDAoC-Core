using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;

namespace DOL.GS;

public class DailyQuestService
{
    private static DateTime lastDailyRollover;


    private const string ServiceName = "DailyQuestService";
    private const string DailyIntervalKey = "DAILY";


    static DailyQuestService()
    {
        EntityManager.AddService(typeof(AttackService));

        //var loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(DailyIntervalKey));
        var loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

        foreach (var interval in loadQuestsProp)
        {
            if (interval.RolloverInterval.Equals(DailyIntervalKey))
                lastDailyRollover = interval.LastRollover;
        }
        
        if(lastDailyRollover == null)
            lastDailyRollover = DateTime.UnixEpoch;
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);
        //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

        if (lastDailyRollover.Date.DayOfYear < DateTime.Now.Date.DayOfYear || lastDailyRollover.Year < DateTime.Now.Year)
        {
            lastDailyRollover = DateTime.Now;
            
            //update db
            var loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(DailyIntervalKey));
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
                newTime.RolloverInterval = DailyIntervalKey;
                GameServer.Database.AddObject(newTime);
            }

            foreach (var player in EntityManager.GetAllPlayers())
            {
                List<AbstractQuest> questsToRemove = new List<AbstractQuest>();
                foreach (var quest in player.QuestListFinished)
                {
                    if (quest is Quests.DailyQuest)
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
            //Console.WriteLine($"Daily refresh");
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}