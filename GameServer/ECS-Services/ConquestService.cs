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
        
        long fullCycle = ServerProperties.Properties.CONQUEST_CYCLE_TIMER * 60000; //multiply by 60000 to accomodate for minute input
        long tallyCycle = ServerProperties.Properties.CONQUEST_TALLY_INTERVAL * 1000; //multiply by 000 to accomodate for second input

        var ActiveObjective = ConquestManager.ActiveObjective;

        if (ActiveObjective == null)
        {
            ConquestManager.ConquestTimeout();
            Diagnostics.StopPerfCounter(ServiceName);
            return;
        }
        
        if(ConquestManager.LastConquestStartTime + fullCycle < GameLoop.GameLoopTime)
            ConquestManager.BeginNextConquest();

        if (ActiveObjective.LastRolloverTick + tallyCycle < GameLoop.GameLoopTime)
        {
            ConquestManager.ActiveObjective.DoPeriodicReward();
        }
        /*       
        if (ConquestManager.LastConquestStartTime + fullCycle < GameLoop.GameLoopTime)
        {
            ConquestManager.StartConquest();
        }

        if (ConquestManager.ConquestIsActive)
        {
            if (ConquestManager.LastConquestStartTime + maxConquestTime <
                GameLoop.GameLoopTime)
            {
                ConquestManager.ConquestTimeout();
            }
            else
            {
                
                if (ConquestManager.ActiveObjective != null && ConquestManager.ActiveObjective.LastRolloverTick + tallyCycle < GameLoop.GameLoopTime)
                {
                    ConquestManager.ActiveObjective.DoRollover();
                }
                       
                
            }
                
        }*/
        
        /*
        if(ConquestManager.LastConquestStartTime + 7200000 < GameLoop.GameLoopTime) //multiply by 60k ms to accomodate minute input
        {
            ConquestManager.StartConquest();
        }else if(300000 - ((GameLoop.GameLoopTime - ConquestManager.LastConquestStartTime) % 300000) <= GameLoop.TickRate) //every 5 minutes
        {
            foreach (var activeObjective in ConquestManager.GetActiveObjectives)
            {
                activeObjective.DoRollover();
            }
        }
        */
        lastCheckTick = GameLoop.GameLoopTime;
        //Console.WriteLine($"conquest heartbeat {GameLoop.GameLoopTime} countdown {GameLoop.GameLoopTime - (ConquestManager.LastTaskRolloverTick + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 10000)}");
        
        Diagnostics.StopPerfCounter(ServiceName);
    }
}