using DOL.AI.Brain;
using System;
using System.Collections.Generic;

namespace DOL.GS {

    /// <summary>
    /// ROGMobGenerator
    /// At the moment this generator only adds ROGs to the loot
    /// </summary>
    public class ROGMobGenerator : LootGeneratorBase {

        //base chance in %
        public static ushort BASE_ROG_CHANCE = 25;


        /// <summary>
        /// Generate loot for given mob
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="killer"></param>
        /// <returns></returns>
        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);

            try
            {
                GamePlayer player = killer as GamePlayer;
                if (killer is GameNPC && ((GameNPC)killer).Brain is IControlledBrain)
                {
                    player = ((ControlledNpcBrain)((GameNPC)killer).Brain).GetPlayerOwner();
                }

                if (player == null)
                {
                    return loot;
                }

                int killedcon = (int)player.GetConLevel(mob); 

                //grey con dont drop loot
                if (killedcon <= -3)
                {
                    return loot;
                }

                eCharacterClass classForLoot = (eCharacterClass)player.CharacterClass.ID;
                // allow the leader to decide the loot realm
                if (player.Group != null)
                {
                    player = player.Group.Leader;
                }

                // chance to get a RoG Item
                int chance = BASE_ROG_CHANCE + ((killedcon + 1) * 3);

                int lvl = mob.Level + 1;
                if (lvl < 1)
                {
                    lvl = 1;
                }

                //players below level 50 will always get loot for their class, 
                //or a valid class for one of their groupmates
                if (player.Group != null)
                {
                    classForLoot = GetRandomClassFromGroup(player.Group);
                    chance += 4 * player.Group.GetPlayersInTheGroup().Count; //4% extra drop chance per group member
                }
                else
                {
                    int tmpChance = player.OutOfClassROGPercent > 0 ? player.OutOfClassROGPercent : 0;
                    if (player.Level == 50 && Util.Chance(tmpChance))
                    {
                        classForLoot = GetRandomClassFromRealm(player.Realm);
                    }
                }

                //chance = 100;

                Database.ItemTemplate item = null;
                
                
                GeneratedUniqueItem tmp = AtlasROGManager.GenerateMonsterLootROG(player.Realm, classForLoot, (byte)(mob.Level + 1));
                tmp.GenerateItemQuality(killedcon);
                //tmp.CapUtility(mob.Level + 1);
                item = tmp;
                item.MaxCount = 1;
                if (mob.Level < 5)
                {
                    loot.AddRandom(100, item, 1);
                }
                else if (mob.Level < 10)
                    loot.AddRandom(chance + (100 - mob.Level * 10), item, 1);
                //50% bonus drop rate at lvl 5, down to normal chance at level 10
                else
                    loot.AddRandom(chance, item, 1);

                if(player.Level < 50 || mob.Level < 50)
                {
                    item = AtlasROGManager.GenerateBeadOfRegeneration();
                    loot.AddRandom(2, item, 1);
                }

            }
            catch
            {
                return loot;
            }

            return loot;
        }

        private eCharacterClass GetRandomClassFromGroup(Group group)
        {
            List<eCharacterClass> validClasses = new List<eCharacterClass>();

            foreach (GamePlayer player in group.GetMembersInTheGroup())
            {
                validClasses.Add((eCharacterClass)player.CharacterClass.ID);
            }
            eCharacterClass ranClass = validClasses[Util.Random(validClasses.Count - 1)];

            return ranClass;

        }

        private eCharacterClass GetRandomClassFromRealm(eRealm realm)
        {
            List<eCharacterClass> classesForRealm = new List<eCharacterClass>();
            switch (realm)
            {
                case eRealm.Albion:
                    classesForRealm.Add(eCharacterClass.Armsman);
                    classesForRealm.Add(eCharacterClass.Cabalist);
                    classesForRealm.Add(eCharacterClass.Cleric);
                    classesForRealm.Add(eCharacterClass.Friar);
                    classesForRealm.Add(eCharacterClass.Infiltrator);
                    classesForRealm.Add(eCharacterClass.Mercenary);
                    classesForRealm.Add(eCharacterClass.Necromancer);
                    classesForRealm.Add(eCharacterClass.Paladin);
                    classesForRealm.Add(eCharacterClass.Reaver);
                    classesForRealm.Add(eCharacterClass.Scout);
                    classesForRealm.Add(eCharacterClass.Sorcerer);
                    classesForRealm.Add(eCharacterClass.Theurgist);
                    classesForRealm.Add(eCharacterClass.Wizard);
                    break;
                case eRealm.Midgard:
                    classesForRealm.Add(eCharacterClass.Berserker);
                    classesForRealm.Add(eCharacterClass.Bonedancer);
                    classesForRealm.Add(eCharacterClass.Healer);
                    classesForRealm.Add(eCharacterClass.Hunter);
                    classesForRealm.Add(eCharacterClass.Runemaster);
                    classesForRealm.Add(eCharacterClass.Savage);
                    classesForRealm.Add(eCharacterClass.Shadowblade);
                    classesForRealm.Add(eCharacterClass.Skald);
                    classesForRealm.Add(eCharacterClass.Spiritmaster);
                    classesForRealm.Add(eCharacterClass.Thane);
                    classesForRealm.Add(eCharacterClass.Warrior);
                    break;
                case eRealm.Hibernia:
                    classesForRealm.Add(eCharacterClass.Animist);
                    classesForRealm.Add(eCharacterClass.Bard);
                    classesForRealm.Add(eCharacterClass.Blademaster);
                    classesForRealm.Add(eCharacterClass.Champion);
                    classesForRealm.Add(eCharacterClass.Druid);
                    classesForRealm.Add(eCharacterClass.Eldritch);
                    classesForRealm.Add(eCharacterClass.Enchanter);
                    classesForRealm.Add(eCharacterClass.Hero);
                    classesForRealm.Add(eCharacterClass.Mentalist);
                    classesForRealm.Add(eCharacterClass.Nightshade);
                    classesForRealm.Add(eCharacterClass.Ranger);
                    classesForRealm.Add(eCharacterClass.Valewalker);
                    classesForRealm.Add(eCharacterClass.Warden);
                    break;
            }

            return classesForRealm[Util.Random(classesForRealm.Count - 1)];
        }
    }
}