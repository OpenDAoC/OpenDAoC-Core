using System;
using Core.GS;

namespace Core.AI.Brain;

public class AntipassBrain : StandardMobBrain
{
    public AntipassBrain()
        : base()
    {
        ThinkInterval = 50;
        AggroLevel = 100;
        AggroRange = 400;
    }

    public override void Think()
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
        {
            if (player.Client.Account.PrivLevel != 3)
            {
                double angle = 0.00153248422;
                player.MoveTo(player.CurrentRegionID, (int)(Body.X - ((AggroRange + 10) * Math.Sin(angle * Body.Heading))), (int)(Body.Y + ((AggroRange + 10) * Math.Cos(angle * Body.Heading))), Body.Z, player.Heading);
            }
        }
    }
}