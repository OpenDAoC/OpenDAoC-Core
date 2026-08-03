namespace DOL.GS.Scripts
{
    /// <summary>
    /// Represents an in-game GameHealer NPC
    /// </summary>
    public class BPMobHigh : GameNPC
    {
        public override void ProcessDeath(GameObject killer)
        {
            GamePlayer player = killer as GamePlayer;

            if (player is GamePlayer && RewardStatus is RewardEligibility.Eligible)
                player.GainBountyPoints((this.Level * 5));

            base.ProcessDeath(killer);
        }
    }
}
