namespace Core.GS.AI.Brains;

public class CrusadersAntithesisBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public CrusadersAntithesisBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (Util.Chance(20))
        {
            SetFlags();
        }
        base.Think();
    }
    private protected void SetFlags()
    {
        if (HasAggro)
        {
            switch (Util.Random(8))
            {
                case 0:
                    if (Body.Model != 667)
                    {
                        Body.Model = 667;
                        Body.Flags = (ENpcFlags)12;//NONAME + NOTARGET
                        Body.BroadcastLivingEquipmentUpdate();
                    }
                    break;
                case 1:
                    if (Body.Model != 927)
                    {
                        Body.Model = 927;
                        Body.Flags = (ENpcFlags)1;
                        Body.BroadcastLivingEquipmentUpdate();
                    }
                    break;
            }
        }
        else
        {
            if (Body.Model != 927)
            {
                Body.Model = 927;
                Body.Flags = (ENpcFlags)1;
                Body.BroadcastLivingEquipmentUpdate();
            }
        }
    }
}