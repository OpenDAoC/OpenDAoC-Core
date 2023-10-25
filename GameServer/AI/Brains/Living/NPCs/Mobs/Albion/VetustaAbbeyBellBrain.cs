using Core.GS.ECS;
using Core.GS.World;

namespace Core.GS.AI;

public class VetustaAbbeyBellBrain : APlayerVicinityBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public VetustaAbbeyBellBrain()
        : base()
    {
        ThinkInterval = 1000;
    }
    private int RingBell(EcsGameTimer timer)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
        {
            if (player != null)
                player.Out.SendSoundEffect(12, 0, 0, 0, 0, 0);
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetRingBell), 10000);//reset ring bell after 10s to avoid double ding dong ^^
        return 0;
    }
    private int ResetRingBell(EcsGameTimer timer)
    {
        runtimer = false;
        return 0;
    }
    bool runtimer = false;
    public override void Think()
    {
        uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
        uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
        //log.Warn("Current time: " + hour + ":" + minute);
        switch(hour,minute)
        {
            case (1, 0):
            case (2, 0):
            case (3, 0):
            case (4, 0):
            case (5, 0):
            case (6, 0):
            case (7, 0):
            case (8, 0):
            case (9, 0):
            case (10, 0):
            case (11, 0):
            case (12, 0):
            case (13, 0):
            case (14, 0):
            case (15, 0):
            case (16, 0):
            case (17, 0):
            case (18, 0):
            case (19, 0):
            case (20, 0):
            case (21, 0):
            case (22, 0):
            case (23, 0):
            case (24, 0):
            {
                if(!runtimer)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RingBell), 500);
                    runtimer = true;
                }
            }
                break;
        }
		
    }

    public override void KillFSM()
    {
		
    }
}