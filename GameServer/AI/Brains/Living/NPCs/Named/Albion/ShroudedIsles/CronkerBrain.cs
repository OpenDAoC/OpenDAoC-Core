using DOL.GS;

namespace DOL.AI.Brain;

public class CronkerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public CronkerBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1000;
    }
    ushort oldModel;
    ENpcFlags oldFlags;
    bool changed;
    public override void Think()
    {
        uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
        //uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
        //log.Warn("Current time: " + hour + ":" + minute);
        if (hour >= 8 && hour < 14)
        {
            if (changed)
            {
                Body.Flags = oldFlags;
                Body.Model = oldModel;
                changed = false;
            }
        }
        else
        {
            if (changed == false)
            {
                oldFlags = Body.Flags;
                Body.Flags ^= ENpcFlags.CANTTARGET;
                Body.Flags ^= ENpcFlags.DONTSHOWNAME;
                Body.Flags ^= ENpcFlags.PEACE;

                if (oldModel == 0)
                    oldModel = Body.Model;

                Body.Model = 1;
                changed = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "CronkerBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}