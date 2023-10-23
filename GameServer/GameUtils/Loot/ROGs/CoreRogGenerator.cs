using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.GameUtils;

/// <summary>
/// CoreROGGenerator
/// At the moment this generator only adds ROGs to the loot
/// </summary>
public class CoreRogGenerator : LootGeneratorBase {

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

            EPlayerClass classForLoot = (EPlayerClass)player.PlayerClass.ID;
            // allow the leader to decide the loot realm
            if (player.Group != null)
            {
                player = player.Group.Leader;
            }

            // chance to get a RoG Item
            int chance = BASE_ROG_CHANCE + ((killedcon < 0 ? killedcon + 1 : killedcon) * 3);

            //chance = 100;
            
            BattleGroupUtil bg = player.TempProperties.GetProperty<BattleGroupUtil>(BattleGroupUtil.BATTLEGROUP_PROPERTY, null);

            if (bg != null)
            {
                var maxDropCap = bg.PlayerCount / 50;
                if (maxDropCap < 1) maxDropCap = 1;
                if (mob is GameEpicNpc)
                    maxDropCap *= 2;
                chance = 2;

                int numDrops = 0;
                foreach (GamePlayer bgMember in bg.Members.Keys)
                {
                    if(bgMember.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
                        continue;
                    
                    if (Util.Chance(chance) && numDrops < maxDropCap)
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
                    
                    if (Util.Chance(chance) && numDrops < MaxDropCap)
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
                    var item = CoreRogMgr.GenerateBeadOfRegeneration();
                    loot.AddRandom(2, item, 1);
                }
                //classForLoot = GetRandomClassFromGroup(player.Group);
                //chance += 4 * player.Group.GetPlayersInTheGroup().Count; //4% extra drop chance per group member
            }
            else
            {
                int tmpChance = player.OutOfClassROGPercent > 0 ? player.OutOfClassROGPercent : 0;
                if (player.Level == 50 && Util.Chance(tmpChance))
                {
                    classForLoot = GetRandomClassFromRealm(player.Realm);
                }

                chance += 10; //solo drop bonus
                
                DbItemTemplate item = null;

                if (mob.Level < 5)
                {
                    chance += 75;
                }
                else if (mob.Level < 10)
                    chance += (100 - mob.Level * 10);

                if (Util.Chance(chance))
                {
                    GeneratedUniqueItem tmp = CoreRogMgr.GenerateMonsterLootROG(player.Realm, classForLoot, (byte)(mob.Level + 1), player.CurrentZone?.IsOF ?? false);
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
                    item = CoreRogMgr.GenerateBeadOfRegeneration();
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


    private DbItemTemplate GenerateItemTemplate(GamePlayer player, EPlayerClass classForLoot, byte lootLevel, int killedcon)
    {
        DbItemTemplate item = null;
            
            
        GeneratedUniqueItem tmp = CoreRogMgr.GenerateMonsterLootROG(player.Realm, classForLoot, lootLevel, player.CurrentZone?.IsOF ?? false);
        tmp.GenerateItemQuality(killedcon);
        //tmp.CapUtility(mob.Level + 1);
        item = tmp;
        item.MaxCount = 1;

        return item;
    }

    private EPlayerClass GetRandomClassFromGroup(GroupUtil group)
    {
        List<EPlayerClass> validClasses = new List<EPlayerClass>();

        foreach (GamePlayer player in group.GetMembersInTheGroup())
        {
            validClasses.Add((EPlayerClass)player.PlayerClass.ID);
        }
        EPlayerClass ranClass = validClasses[Util.Random(validClasses.Count - 1)];

        return ranClass;
    }
    
    private EPlayerClass GetRandomClassFromBattlegroup(BattleGroupUtil battlegroup)
    {
        List<EPlayerClass> validClasses = new List<EPlayerClass>();

        foreach (GamePlayer player in battlegroup.Members.Keys)
        {
            validClasses.Add((EPlayerClass)player.PlayerClass.ID);
        }
        EPlayerClass ranClass = validClasses[Util.Random(validClasses.Count - 1)];

        return ranClass;
    }

    private EPlayerClass GetRandomClassFromRealm(ERealm realm)
    {
        List<EPlayerClass> classesForRealm = new List<EPlayerClass>();
        switch (realm)
        {
            case ERealm.Albion:
                classesForRealm.Add(EPlayerClass.Armsman);
                classesForRealm.Add(EPlayerClass.Cabalist);
                classesForRealm.Add(EPlayerClass.Cleric);
                classesForRealm.Add(EPlayerClass.Friar);
                classesForRealm.Add(EPlayerClass.Infiltrator);
                classesForRealm.Add(EPlayerClass.Mercenary);
                classesForRealm.Add(EPlayerClass.Necromancer);
                classesForRealm.Add(EPlayerClass.Paladin);
                classesForRealm.Add(EPlayerClass.Reaver);
                classesForRealm.Add(EPlayerClass.Scout);
                classesForRealm.Add(EPlayerClass.Sorcerer);
                classesForRealm.Add(EPlayerClass.Theurgist);
                classesForRealm.Add(EPlayerClass.Wizard);
                break;
            case ERealm.Midgard:
                classesForRealm.Add(EPlayerClass.Berserker);
                classesForRealm.Add(EPlayerClass.Bonedancer);
                classesForRealm.Add(EPlayerClass.Healer);
                classesForRealm.Add(EPlayerClass.Hunter);
                classesForRealm.Add(EPlayerClass.Runemaster);
                classesForRealm.Add(EPlayerClass.Savage);
                classesForRealm.Add(EPlayerClass.Shadowblade);
                classesForRealm.Add(EPlayerClass.Skald);
                classesForRealm.Add(EPlayerClass.Spiritmaster);
                classesForRealm.Add(EPlayerClass.Thane);
                classesForRealm.Add(EPlayerClass.Warrior);
                break;
            case ERealm.Hibernia:
                classesForRealm.Add(EPlayerClass.Animist);
                classesForRealm.Add(EPlayerClass.Bard);
                classesForRealm.Add(EPlayerClass.Blademaster);
                classesForRealm.Add(EPlayerClass.Champion);
                classesForRealm.Add(EPlayerClass.Druid);
                classesForRealm.Add(EPlayerClass.Eldritch);
                classesForRealm.Add(EPlayerClass.Enchanter);
                classesForRealm.Add(EPlayerClass.Hero);
                classesForRealm.Add(EPlayerClass.Mentalist);
                classesForRealm.Add(EPlayerClass.Nightshade);
                classesForRealm.Add(EPlayerClass.Ranger);
                classesForRealm.Add(EPlayerClass.Valewalker);
                classesForRealm.Add(EPlayerClass.Warden);
                break;
        }

        return classesForRealm[Util.Random(classesForRealm.Count - 1)];
    }
}