using DOL.GS.ServerProperties;

namespace DOL.GS {
    public class GameEpicBoss : GameNPC {
        public GameEpicBoss() : base()
        {
            ScalingFactor = 80;
            OrbsReward = Properties.EPICBOSS_ORBS;
        }
        public override void Die(GameObject killer)
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;
            
            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
                }
            }
            else
            {
                AtlasROGManager.GenerateOrbAmount(playerKiller,OrbsReward);
            }
            
            base.Die(killer);
        }
    }
}
