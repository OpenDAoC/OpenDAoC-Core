using System;

namespace DOL.GS {
    public class GameEpicNPC : GameNPC {
        public GameEpicNPC() : base()
        {
            ScalingFactor = 60;
        }
        public override void Die(GameObject killer)
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;
            
            var amount = Util.Random(Level / 10, Level * 2 / 10);
            var baseChance = 80;
            var realmLoyalty = 0;
            
            
            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    int numCurrentLoyalDays = groupPlayer.TempProperties.getProperty<int>("current_loyalty_days");
                    if (numCurrentLoyalDays >= 1)
                    {
                        realmLoyalty = (int)Math.Round(20 / (numCurrentLoyalDays / 30.0) );
                    }
                    if(Util.Chance(baseChance+realmLoyalty))
                    {
                        AtlasROGManager.GenerateOrbAmount(groupPlayer,amount);
                    }
                }
            }
            else
            {
                int numCurrentLoyalDays = LoyaltyManager.GetPlayerRealmLoyalty(playerKiller) != null ? LoyaltyManager.GetPlayerRealmLoyalty(playerKiller).Days : 0;
                if (numCurrentLoyalDays >= 1)
                {
                    realmLoyalty = (int)Math.Round(20 / (numCurrentLoyalDays / 30.0) );
                }
                if(Util.Chance(baseChance+realmLoyalty))
                {
                    AtlasROGManager.GenerateOrbAmount(playerKiller,amount);
                }
            }
            base.Die(killer);
        }
    }
}
