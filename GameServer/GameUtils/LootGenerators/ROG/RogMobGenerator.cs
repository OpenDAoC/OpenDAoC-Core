﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS {

    /// <summary>
    /// ROGMobGenerator
    /// At the moment this generator only adds ROGs to the loot
    /// </summary>
    public class RogMobGenerator : LootGeneratorBase {

        //base chance in %
        public static ushort BASE_ROG_CHANCE = 14;


        /// <summary>
        /// Generate loot for given mob
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="killer"></param>
        /// <returns></returns>
        public override LootList GenerateLoot(GameNpc mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);

            try
            {
                GamePlayer player = killer as GamePlayer;
                if (killer is GameNpc && ((GameNpc)killer).Brain is IControlledBrain)
                {
                    player = ((ControlledNpcBrain)((GameNpc)killer).Brain).GetPlayerOwner();
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

                ECharacterClass classForLoot = (ECharacterClass)player.CharacterClass.ID;
                // allow the leader to decide the loot realm
                if (player.Group != null)
                {
                    player = player.Group.Leader;
                }

                // chance to get a RoG Item
                int chance = BASE_ROG_CHANCE + ((killedcon < 0 ? killedcon + 1 : killedcon) * 3);

                //chance = 100;
                
                BattleGroup bg = (BattleGroup)player.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                if (bg != null)
                {
                    var maxDropCap = bg.PlayerCount / 50;
                    if (maxDropCap < 1) maxDropCap = 1;
                    if (mob is GameEpicNpc || mob is GameDragon)
                        maxDropCap *= 2;
                    chance = 2;

                    int numDrops = 0;
                    foreach (GamePlayer bgMember in bg.Members.Keys)
                    {
                        if(bgMember.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
                            continue;
                        
                        if (UtilCollection.Chance(chance) && numDrops < maxDropCap)
                        {
                            classForLoot = GetRandomClassFromBattlegroup(bg);
                            var item = GenerateItemTemplate(player, classForLoot, (byte)(mob.Level + 1), killedcon);
                            loot.AddFixed(item, 1);
                            numDrops++;
                        }
                    }
                }
                //players below level 50 will always get loot for their class, 
                //or a valid class for one of their groupmates
                else if (player.Group != null)
                {
                    var MaxDropCap = Math.Round((decimal) (player.Group.MemberCount)/3);
                    if (MaxDropCap < 1) MaxDropCap = 1;
                    if (MaxDropCap > 3) MaxDropCap = 3;
                    if (mob.Level > 65) MaxDropCap++; //increase drop cap beyond lvl 60
                    int guaranteedDrop = mob.Level > 67 ? 1 : 0; //guarantee a drop for very high level mobs
                    
                    if (mob.Level > 27)
                        chance -= 3;

                    if (mob.Level > 40)
                        chance -= 3;
                    
                    if (mob.Level < 5)
                    {
                        chance += 75;
                    }
                    else if (mob.Level < 10)
                        chance += (100 - mob.Level * 10);

                    int numDrops = 0;
                    //roll for an item for each player in the group
                    foreach (var groupPlayer in player.Group.GetNearbyPlayersInTheGroup(player))
                    {
                        if(groupPlayer.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
                            continue;
                        
                        if (UtilCollection.Chance(chance) && numDrops < MaxDropCap)
                        {
                            classForLoot = GetRandomClassFromGroup(player.Group);
                            var item = GenerateItemTemplate(player, classForLoot, (byte)(mob.Level + 1), killedcon);
                            loot.AddFixed(item, 1);
                            numDrops++;
                        }
                    }

                    //if we're under the cap, add in the guaranteed drop
                    if (numDrops < MaxDropCap && guaranteedDrop > 0)
                    {
                        classForLoot = GetRandomClassFromGroup(player.Group);
                        var item = GenerateItemTemplate(player, classForLoot, (byte)(mob.Level + 1), killedcon);
                        loot.AddFixed(item, 1);
                    }

                    if(player.Level < 50 || mob.Level < 50)
                    {
                        var item = RogMgr.GenerateBeadOfRegeneration();
                        loot.AddRandom(2, item, 1);
                    }
                    //classForLoot = GetRandomClassFromGroup(player.Group);
                    //chance += 4 * player.Group.GetPlayersInTheGroup().Count; //4% extra drop chance per group member
                }
                else
                {
                    int tmpChance = player.OutOfClassROGPercent > 0 ? player.OutOfClassROGPercent : 0;
                    if (player.Level == 50 && UtilCollection.Chance(tmpChance))
                    {
                        classForLoot = GetRandomClassFromRealm(player.Realm);
                    }

                    chance += 10; //solo drop bonus
                    
                    DbItemTemplates item = null;

                    if (mob.Level < 5)
                    {
                        chance += 75;
                    }
                    else if (mob.Level < 10)
                        chance += (100 - mob.Level * 10);

                    if (UtilCollection.Chance(chance))
                    {
                        GeneratedUniqueItem tmp = RogMgr.GenerateMonsterLootROG(player.Realm, classForLoot, (byte)(mob.Level + 1), player.CurrentZone?.IsOF ?? false);
                        item = tmp;
                        item.MaxCount = 1;
                        loot.AddFixed(item, 1);
                    }
                    //tmp.GenerateItemQuality(killedcon);
                    //tmp.CapUtility(mob.Level + 1);
                    
                    /*
                    if (mob.Level < 5)
                    {
                        chance += 50;
                        loot.AddRandom(chance, item, 1);
                    }
                    else if (mob.Level < 10)
                        loot.AddRandom(chance + (100 - mob.Level * 10), item, 1);
                    //25% bonus drop rate at lvl 5, down to normal chance at level 10
                    else
                        loot.AddRandom(chance, item, 1);
                        */

                    if(player.Level < 50 || mob.Level < 50)
                    {
                        item = RogMgr.GenerateBeadOfRegeneration();
                        loot.AddRandom(2, item, 1);
                    }
                }

                //chance = 100;

               

            }
            catch
            {
                return loot;
            }

            return loot;
        }


        private DbItemTemplates GenerateItemTemplate(GamePlayer player, ECharacterClass classForLoot, byte lootLevel, int killedcon)
        {
            DbItemTemplates item = null;
                
                
            GeneratedUniqueItem tmp = RogMgr.GenerateMonsterLootROG(player.Realm, classForLoot, lootLevel, player.CurrentZone?.IsOF ?? false);
            tmp.GenerateItemQuality(killedcon);
            //tmp.CapUtility(mob.Level + 1);
            item = tmp;
            item.MaxCount = 1;

            return item;
        }

        private ECharacterClass GetRandomClassFromGroup(Group group)
        {
            List<ECharacterClass> validClasses = new List<ECharacterClass>();

            foreach (GamePlayer player in group.GetMembersInTheGroup())
            {
                validClasses.Add((ECharacterClass)player.CharacterClass.ID);
            }
            ECharacterClass ranClass = validClasses[UtilCollection.Random(validClasses.Count - 1)];

            return ranClass;
        }
        
        private ECharacterClass GetRandomClassFromBattlegroup(BattleGroup battlegroup)
        {
            List<ECharacterClass> validClasses = new List<ECharacterClass>();

            foreach (GamePlayer player in battlegroup.Members.Keys)
            {
                validClasses.Add((ECharacterClass)player.CharacterClass.ID);
            }
            ECharacterClass ranClass = validClasses[UtilCollection.Random(validClasses.Count - 1)];

            return ranClass;
        }

        private ECharacterClass GetRandomClassFromRealm(ERealm realm)
        {
            List<ECharacterClass> classesForRealm = new List<ECharacterClass>();
            switch (realm)
            {
                case ERealm.Albion:
                    classesForRealm.Add(ECharacterClass.Armsman);
                    classesForRealm.Add(ECharacterClass.Cabalist);
                    classesForRealm.Add(ECharacterClass.Cleric);
                    classesForRealm.Add(ECharacterClass.Friar);
                    classesForRealm.Add(ECharacterClass.Infiltrator);
                    classesForRealm.Add(ECharacterClass.Mercenary);
                    classesForRealm.Add(ECharacterClass.Necromancer);
                    classesForRealm.Add(ECharacterClass.Paladin);
                    classesForRealm.Add(ECharacterClass.Reaver);
                    classesForRealm.Add(ECharacterClass.Scout);
                    classesForRealm.Add(ECharacterClass.Sorcerer);
                    classesForRealm.Add(ECharacterClass.Theurgist);
                    classesForRealm.Add(ECharacterClass.Wizard);
                    break;
                case ERealm.Midgard:
                    classesForRealm.Add(ECharacterClass.Berserker);
                    classesForRealm.Add(ECharacterClass.Bonedancer);
                    classesForRealm.Add(ECharacterClass.Healer);
                    classesForRealm.Add(ECharacterClass.Hunter);
                    classesForRealm.Add(ECharacterClass.Runemaster);
                    classesForRealm.Add(ECharacterClass.Savage);
                    classesForRealm.Add(ECharacterClass.Shadowblade);
                    classesForRealm.Add(ECharacterClass.Skald);
                    classesForRealm.Add(ECharacterClass.Spiritmaster);
                    classesForRealm.Add(ECharacterClass.Thane);
                    classesForRealm.Add(ECharacterClass.Warrior);
                    break;
                case ERealm.Hibernia:
                    classesForRealm.Add(ECharacterClass.Animist);
                    classesForRealm.Add(ECharacterClass.Bard);
                    classesForRealm.Add(ECharacterClass.Blademaster);
                    classesForRealm.Add(ECharacterClass.Champion);
                    classesForRealm.Add(ECharacterClass.Druid);
                    classesForRealm.Add(ECharacterClass.Eldritch);
                    classesForRealm.Add(ECharacterClass.Enchanter);
                    classesForRealm.Add(ECharacterClass.Hero);
                    classesForRealm.Add(ECharacterClass.Mentalist);
                    classesForRealm.Add(ECharacterClass.Nightshade);
                    classesForRealm.Add(ECharacterClass.Ranger);
                    classesForRealm.Add(ECharacterClass.Valewalker);
                    classesForRealm.Add(ECharacterClass.Warden);
                    break;
            }

            return classesForRealm[UtilCollection.Random(classesForRealm.Count - 1)];
        }
    }
}