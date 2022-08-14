using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;

namespace DOL.GS;

public class ConquestService
{
    private const string ServiceName = "Conquest Service";

    public static ConquestManager ConquestManager;

    private static long lastCheckTick;


    static ConquestService()
    {
        EntityManager.AddService(typeof(ConquestService));
        ConquestManager = new ConquestManager();
        ConquestManager.StartConquest();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        long fullCycle = ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 60000; //ServerProperties.Properties.MAX_CONQUEST_INTERVAL
        long tallyCycle = ServerProperties.Properties.CONQUEST_TALLY_INTERVAL * 1000; //multiply by 000 to accomodate for second input
        long subCycle = fullCycle / 3;

        var ActiveObjective = ConquestManager.ActiveObjective;

        if (ActiveObjective == null)
        {
            ConquestManager.ConquestTimeout();
            Diagnostics.StopPerfCounter(ServiceName);
            return;
        }
        
        if(ConquestManager.LastConquestStartTime + fullCycle < GameLoop.GameLoopTime)
            ConquestManager.BeginNextConquest(); //start a new conquest if we're due

        if (ActiveObjective.LastRolloverTick + tallyCycle < GameLoop.GameLoopTime)
            ConquestManager.ActiveObjective.DoPeriodicReward(); //award anyone who has participated so far

        if (ConquestManager.LastConquestWindowStart + subCycle < GameLoop.GameLoopTime)
            ConquestManager.ResetConquestWindow(); //clear users in case of 
        
        ConquestManager.ActiveObjective.CheckNearbyPlayers();
        
        lastCheckTick = GameLoop.GameLoopTime;
        //Console.WriteLine($"conquest heartbeat {GameLoop.GameLoopTime} countdown {GameLoop.GameLoopTime - (ConquestManager.LastTaskRolloverTick + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 10000)}");
        
        Diagnostics.StopPerfCounter(ServiceName);
    }
}