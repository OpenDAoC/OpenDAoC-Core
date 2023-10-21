using Core.GS;

namespace Core.AI.Brain;

public class DoobenBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public DoobenBrain() : base()
    {
        ThinkInterval = 1500;
    }
    private bool NotInCombat = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            if (NotInCombat == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
                NotInCombat = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
            NotInCombat = false;

        base.Think();
    }

    protected int Show_Effect(EcsGameTimer timer)
    {
        if (Body.IsAlive && !HasAggro)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(Body, Body, 479, 0, false, 0x01);

            return 1600;
        }

        return 0;
    }
}