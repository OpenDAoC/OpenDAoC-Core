using Core.GS.GameLoop;
using Core.GS.Server;

namespace Core.GS.ECS;

public class ConquestService
{
    private const string SERVICE_NAME = "ConquestService";

    public static ConquestMgr ConquestManager;
    private static long lastCheckTick;

    static ConquestService()
    {
        ConquestManager = new ConquestMgr();
        ConquestManager.StartConquest();
    }

    public static void Tick()
    {
        GameLoopMgr.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        long fullCycle = ServerProperty.MAX_CONQUEST_TASK_DURATION * 60000; //ServerProperties.Properties.MAX_CONQUEST_INTERVAL
        long tallyCycle = ServerProperty.CONQUEST_TALLY_INTERVAL * 1000; //multiply by 000 to accomodate for second input
        long subCycle = fullCycle / 6;

        var ActiveObjective = ConquestManager.ActiveObjective;

        if (ActiveObjective == null)
        {
            ConquestManager.ConquestTimeout();
            Diagnostics.StopPerfCounter(SERVICE_NAME);
            return;
        }

        if (ConquestManager.LastConquestStartTime + fullCycle < GameLoopMgr.GameLoopTime)
            ConquestManager.BeginNextConquest(); //start a new conquest if we're due

        if (ActiveObjective.LastRolloverTick + tallyCycle < GameLoopMgr.GameLoopTime)
            ConquestManager.ActiveObjective.DoPeriodicReward(); //award anyone who has participated so far

        if (ConquestManager.LastConquestWindowStart + subCycle < GameLoopMgr.GameLoopTime)
            ConquestManager.ResetConquestWindow(); //clear participants of conquest + predator

        ConquestManager.ActiveObjective.CheckNearbyPlayers();

        lastCheckTick = GameLoopMgr.GameLoopTime;
        //Console.WriteLine($"conquest heartbeat {GameLoop.GameLoopTime} countdown {GameLoop.GameLoopTime - (ConquestManager.LastTaskRolloverTick + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 10000)}");

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }

    public static bool IsOverHalfwayDone()
    {
        long fullCycle = ServerProperty.MAX_CONQUEST_TASK_DURATION * 60000; //ServerProperties.Properties.MAX_CONQUEST_INTERVAL
        return (ConquestManager.LastConquestStartTime + (fullCycle / 2)) < GameLoopMgr.GameLoopTime;
    }

    public static long GetTicksUntilContributionReset()
    {
        return ConquestManager.LastConquestWindowStart + (ServerProperty.MAX_CONQUEST_TASK_DURATION * 60000 / 6) - GameLoopMgr.GameLoopTime;
    }

    public static long GetTicksUntilNextAward()
    {
        return ConquestManager.ActiveObjective.LastRolloverTick + ServerProperty.CONQUEST_TALLY_INTERVAL * 1000 - GameLoopMgr.GameLoopTime;
    }
}