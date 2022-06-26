using System;
using System.Text.RegularExpressions;

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
            
            var playerKiller = killer as GamePlayer;
            
            var amount = Util.Random(Level / 10, Level * 2 / 10);
            var baseChance = 80;
            var realmLoyalty = 0;
            
            var achievementMob = Regex.Replace(Name, @"\s+", "");
            
            var killerBG = (BattleGroup)playerKiller?.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);
            
            if (killerBG != null && (killerBG.Members.Contains(playerKiller) || (bool)killerBG.Members[playerKiller]!))
            {
                foreach (GamePlayer bgPlayer in killerBG.GetPlayersInTheBattleGroup())
                {
                    if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                    {
                        var numCurrentLoyalDays = bgPlayer.TempProperties.getProperty<int>("current_loyalty_days");
                        if (numCurrentLoyalDays >= 1)
                        {
                            realmLoyalty = (int)Math.Round(20 / (numCurrentLoyalDays / 30.0) );
                        }
                        if(Util.Chance(baseChance+realmLoyalty))
                        {
                            AtlasROGManager.GenerateOrbAmount(bgPlayer,amount);
                        }
                        bgPlayer.Achieve($"{achievementMob}-Credit");
                    }
                }
            }
            else if (playerKiller?.Group != null)
            {
                foreach (var groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    if (groupPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                    {
                        var numCurrentLoyalDays = groupPlayer.TempProperties.getProperty<int>("current_loyalty_days");
                        if (numCurrentLoyalDays >= 1)
                        {
                            realmLoyalty = (int)Math.Round(20 / (numCurrentLoyalDays / 30.0) );
                        }
                        if(Util.Chance(baseChance+realmLoyalty))
                        {
                            AtlasROGManager.GenerateOrbAmount(groupPlayer,amount);
                        }
                        groupPlayer.Achieve($"{achievementMob}-Credit");
            
                    }
                }
            }
            else
            {
                var numCurrentLoyalDays = LoyaltyManager.GetPlayerRealmLoyalty(playerKiller) != null ? LoyaltyManager.GetPlayerRealmLoyalty(playerKiller).Days : 0;
                if (numCurrentLoyalDays >= 1)
                {
                    realmLoyalty = (int)Math.Round(20 / (numCurrentLoyalDays / 30.0) );
                }
                if(Util.Chance(baseChance+realmLoyalty))
                {
                    AtlasROGManager.GenerateOrbAmount(playerKiller,amount);
                }
                playerKiller.Achieve($"{achievementMob}-Credit");
            
            }
            
            base.Die(killer);
        }
    }
}
