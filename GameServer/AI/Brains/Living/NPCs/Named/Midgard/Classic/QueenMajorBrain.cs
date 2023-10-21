using Core.GS;

namespace Core.AI.Brain;

public class QueenMajorBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public QueenMajorBrain() : base()
    {
        AggroLevel = 80;
        AggroRange = 400;
        ThinkInterval = 1000;
    }
    ushort oldModel;
    ENpcFlags oldFlags;
    bool changed;
    public override void Think()
    {
        //uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
        //uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
        //log.Warn("Current time: " + hour + ":" + minute);
        if (QueenMajorAdd.QueenMajorAddCount >= 20)
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
            foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is QueenMajorAddBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
                        brain.AddToAggroList(target, 10);
                }
            }
            foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "QueenMajorBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}

public class QueenMajorAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public QueenMajorAddBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}