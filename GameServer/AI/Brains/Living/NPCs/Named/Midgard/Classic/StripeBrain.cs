using DOL.GS;

namespace DOL.AI.Brain;

public class StripeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public StripeBrain() : base()
    {
        AggroLevel = 50;
        AggroRange = 300;
        ThinkInterval = 1000;
    }
    ushort oldModel;
    ENpcFlags oldFlags;
    bool changed;
    public override void Think()
    {
        if (StripeAdd.StripeAddCount >= 20)
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
        base.Think();
    }
}

public class StripeAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public StripeAddBrain() : base()
    {
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}