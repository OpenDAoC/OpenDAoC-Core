using System;
using System.Collections;
using System.Text.RegularExpressions;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class GameEpicNPC : GameNPC, IGameEpicNpc
    {
        public double DefaultArmorFactorScalingFactor => 0.8;
        public int ArmorFactorScalingFactorPetCap => 16;
        public double ArmorFactorScalingFactor { get; set; }

        public GameEpicNPC() : base()
        {
            ScalingFactor = 60;
            ArmorFactorScalingFactor = DefaultArmorFactorScalingFactor;
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.ConfusionImmunity)
                return true;

            if (IsAlive && keyName == GS.Abilities.NSImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public override short MaxSpeedBase => (short) (191 + Level * 2);

        public override int MaxHealth => 10000 + Level * 125;

        public override void Die(GameObject killer)
        {
            try
            {
                log.Debug($"{Name} killed by {killer.Name}");

                if (killer is GameSummonedPet pet)
                    killer = pet.Owner;

                GamePlayer playerKiller = killer as GamePlayer;
                int amount = Util.Random(Level / 10, Level * 2 / 10);
                int baseChance = 80;
                double carapaceChance = Properties.CARAPACE_DROPCHANCE;
                int realmLoyalty = 0;
                double numCurrentLoyalDays = LoyaltyManager.GetPlayerRealmLoyalty(playerKiller)?.Days ?? 0;

                if (numCurrentLoyalDays > 30)
                    numCurrentLoyalDays = 30;

                if (numCurrentLoyalDays >= 1)
                    realmLoyalty = (int)Math.Round(20 * (numCurrentLoyalDays / 30.0));

                string achievementMob = Regex.Replace(Name, @"\s+", "");
                BattleGroup killerBG = playerKiller?.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                if (killerBG != null)
                {
                    ICollection bgPlayers;

                    lock (killerBG.Members.Keys)
                    {
                         bgPlayers = killerBG.Members.Keys;
                    }

                    if (bgPlayers != null)
                    {
                        foreach (GamePlayer bgPlayer in bgPlayers)
                        {
                            if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                            {
                                if (bgPlayer.Level < 45)
                                    continue;

                                if (Util.Chance(baseChance + realmLoyalty))
                                    AtlasROGManager.GenerateReward(bgPlayer, amount);

                                if (Util.ChanceDouble(carapaceChance))
                                    AtlasROGManager.GenerateBeetleCarapace(bgPlayer);

                                bgPlayer.Achieve($"{achievementMob}-Credit");
                            }
                        }
                    }
                }
                else if (playerKiller?.Group != null)
                {
                    foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                    {
                        if (groupPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                        {
                            if (groupPlayer.Level < 45)
                                continue;

                            if (Util.Chance(baseChance + realmLoyalty))
                                AtlasROGManager.GenerateReward(groupPlayer, amount);

                            if (Util.ChanceDouble(carapaceChance))
                                AtlasROGManager.GenerateBeetleCarapace(groupPlayer);

                            groupPlayer.Achieve($"{achievementMob}-Credit");
                        }
                    }
                }
                else if (playerKiller != null)
                {
                    if (playerKiller.Level >= 45)
                    {
                        if (Util.Chance(baseChance + realmLoyalty))
                            AtlasROGManager.GenerateReward(playerKiller, amount);

                        if (Util.ChanceDouble(carapaceChance))
                            AtlasROGManager.GenerateBeetleCarapace(playerKiller);

                        playerKiller.Achieve($"{achievementMob}-Credit");
                    }
                }
            }
            finally
            {
                base.Die(killer);
            }
        }
    }
}
