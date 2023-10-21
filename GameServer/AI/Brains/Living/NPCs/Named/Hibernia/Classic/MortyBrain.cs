using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class MortyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public MortyBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 0;
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
        if (hour >= 8 && hour < 12)
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