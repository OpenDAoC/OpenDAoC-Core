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
    private static DateTime lastWeeklyRollover;

    private const string ServiceName = "DailyQuestService";
    private const string DailyIntervalKey = "DAILY";
    private const string WeeklyIntervalKey = "WEEKLY";

    static DailyQuestService()
    {
        EntityManager.AddService(typeof(AttackService));

        //var loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(DailyIntervalKey));
        var loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

        foreach (var interval in loadQuestsProp)
        {
            if (interval.RolloverInterval.Equals(DailyIntervalKey))
                lastDailyRollover = interval.LastRollover;

            if (interval.RolloverInterval.Equals(WeeklyIntervalKey))
                lastWeeklyRollover = interval.LastRollover;
        }
        
        //if we didn't find anything, just set it to the big bang
        if(lastDailyRollover == null)
            lastDailyRollover = DateTime.UnixEpoch;
        
        if(lastWeeklyRollover == null)
            lastWeeklyRollover = DateTime.UnixEpoch;
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);
        //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

        //DateTime.Today.AddDays(1)
        //midnight today/tomorrow -^
        
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
                    Console.WriteLine($"Aborting {quest} for {player}");
                    quest.AbortQuest();
                    questsToRemove.Add(quest);
                }

                foreach (var quest in questsToRemove)
                {
                    player.QuestList.Remove(quest);
                    player.QuestListFinished.Remove(quest);
                }
            }
            //Console.WriteLine($"Daily refresh");
        } 
        //this is where the weekly check will go once testing is finished
        else if (lastWeeklyRollover.Date.DayOfYear+7 < DateTime.Now.Date.DayOfYear || lastWeeklyRollover.Year < DateTime.Now.Year)
        {
            lastWeeklyRollover = DateTime.Now;
            
            //update db
            var loadQuestsProp = GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval").IsEqualTo(WeeklyIntervalKey));
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
                newTime.RolloverInterval = WeeklyIntervalKey;
                GameServer.Database.AddObject(newTime);
            }
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}