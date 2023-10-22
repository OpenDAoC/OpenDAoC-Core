namespace Core.GS;

public class BountyPointHighMob : GameNpc
{
    public override void Die(GameObject killer)
    {
        GamePlayer player = killer as GamePlayer;
        if (player is GamePlayer && IsWorthReward)

            player.GainBountyPoints((this.Level * 5));

        DropLoot(killer);

        base.Die(killer);

        if ((Faction != null) && (killer is GamePlayer))
        {
            GamePlayer player3 = killer as GamePlayer;
            Faction.KillMember(player3);
        }

        StartRespawn();
    }
}