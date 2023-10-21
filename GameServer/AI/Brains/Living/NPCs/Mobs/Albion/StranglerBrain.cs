namespace Core.GS.AI.Brains;

public class StranglerBrain : StandardMobBrain
{
    public override int ThinkInterval
    {
        get { return 3000; }
    }

    public override void Think()
    {
        base.Think();
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            player.Out.SendSpellEffectAnimation(Body, Body, 5206, 0, false, 1);
    }
}