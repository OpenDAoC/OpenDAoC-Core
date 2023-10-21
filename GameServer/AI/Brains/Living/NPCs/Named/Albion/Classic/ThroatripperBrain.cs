namespace Core.GS.AI.Brains;

public class ThroatripperBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ThroatripperBrain() : base()
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
        if (ThroatripperAdd.ThroatripperAddCount >= 10 && Body.CurrentRegion.IsNightTime)
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
                if (npc != null && npc.IsAlive && npc.Brain is ThroatripperAddBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}

public class ThroatripperAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ThroatripperAddBrain() : base()
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