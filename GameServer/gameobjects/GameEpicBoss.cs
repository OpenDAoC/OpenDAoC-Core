using System.Text.RegularExpressions;
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
            if (MaxHealth < 60000 && MaxHealth > 30000)// 1.5k orbs for finalll boss in dungeon, or huge one in classic/SI zone
                OrbsReward /= 2;

            if (MaxHealth < 40000)// 750 orbs for normal nameds
                OrbsReward /= 4;
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            var playerKiller = killer as GamePlayer;
            
            var achievementMob = Regex.Replace(Name, @"\s+", "");

            var killerBG = (BattleGroup)playerKiller?.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);
            
            if (killerBG != null && (killerBG.Members.Contains(playerKiller) || (bool)killerBG.Members[playerKiller]!))
            {
                foreach (var bgPlayer in killerBG.GetPlayersInTheBattleGroup())
                {
                    if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                    {
                        AtlasROGManager.GenerateOrbAmount(bgPlayer,OrbsReward);
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
                        AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
                        groupPlayer.Achieve($"{achievementMob}-Credit");
                    }
                }
            }
            else
            {
                AtlasROGManager.GenerateOrbAmount(playerKiller,OrbsReward);
                playerKiller.Achieve($"{achievementMob}-Credit");
            }

            base.Die(killer);
        }
    }
}
