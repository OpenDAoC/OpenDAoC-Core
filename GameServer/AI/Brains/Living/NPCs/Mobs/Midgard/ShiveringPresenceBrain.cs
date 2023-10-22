using Core.GS.World;

namespace Core.GS.AI.Brains;

public class ShiveringPresenceBrain : StandardMobBrain
{
    public override int ThinkInterval
    {
        get { return 3000; }
    }

    public override void Think()
    {
        base.Think();
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            player.Out.SendSpellEffectAnimation(Body, Body, 152, 0, false, 1);
    }
}