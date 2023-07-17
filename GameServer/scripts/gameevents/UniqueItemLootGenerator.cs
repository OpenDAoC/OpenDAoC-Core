﻿

//
// This is the Storm Unique Object Generator. changed to instanciate GeneratedUniqueItem
//
// Original version by Etaew
// Modified by Tolakram to add live like names and item models
//
// Released to the public on July 12th, 2010
//
// Please enjoy this generator and submit any fixes to the DOL project to benefit everyone.
// - Tolakram
//
// Updating to instance object of GeneratedUniqueITem by Leodagan on Aug 2013.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Events;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.Scripts;
using DOL.GS.Utils;
using log4net;


namespace DOL.GS
{
    public class LootGeneratorUniqueItem : LootGeneratorBase
    {
        [Command(
            "&genuniques",
            EPrivLevel.GM,
            "/genuniques ([TOA] || [L51] || [self] || [suit] || [objecttype]) [itemtype] : generate 8 unique items")]
        public class LootGeneratorUniqueObjectCommandHandler : DOL.GS.Commands.AbstractCommandHandler,
            DOL.GS.Commands.ICommandHandler
        {
            public void OnCommand(GameClient client, string[] args)
            {
                try
                {
                    if (args.Length <= 1)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    if (args.Length > 1 && Convert.ToString(args[1]).ToUpper() == "SUIT")
                    {
                        GamePlayer player = client.Player;
                        List<eInventorySlot> bodySlots = new List<eInventorySlot>();
                        bodySlots.Add(eInventorySlot.ArmsArmor);
                        bodySlots.Add(eInventorySlot.FeetArmor);
                        bodySlots.Add(eInventorySlot.HandsArmor);
                        bodySlots.Add(eInventorySlot.HeadArmor);
                        bodySlots.Add(eInventorySlot.LegsArmor);
                        bodySlots.Add(eInventorySlot.TorsoArmor);

                        ERealm realm = player.Realm;
                        ECharacterClass charclass = (ECharacterClass) player.CharacterClass.ID;
                        EObjectType armorType = GetArmorType(realm, charclass, (byte) (player.Level));

                        foreach (eInventorySlot islot in bodySlots)
                        {
                            GeneratedUniqueItem item = null;
                            item = new GeneratedUniqueItem(realm, charclass, (byte) (81), armorType, islot);
                            item.AllowAdd = true;
                            item.IsTradable = false;
                            item.Price = 1;
                            //item.CapUtility(81);
                            GameServer.Database.AddObject(item);
                            InventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
                            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                            //player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }

                        List<EObjectType> weapons = GenerateWeaponsForClass(charclass, player);
                        
                    }
                    else
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            GeneratedUniqueItem item = null;

                            if (args.Length > 1)
                            {
                                if (Convert.ToString(args[1]).ToUpper() == "TOA")
                                {
                                    item = new GeneratedUniqueItem(true, client.Player.Realm,
                                        (ECharacterClass) client.Player.CharacterClass.ID, 51);
                                    item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 60));
                                }
                                else if (Convert.ToString(args[1]).ToUpper() == "L51")
                                {
                                    item = new GeneratedUniqueItem(client.Player.Realm,
                                        (ECharacterClass) client.Player.CharacterClass.ID, 51);
                                    item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 50));
                                }
                                else if (Convert.ToString(args[1]).ToUpper() == "SELF")
                                {
                                    item = new GeneratedUniqueItem(client.Player.Realm,
                                        (ECharacterClass) client.Player.CharacterClass.ID, client.Player.Level);
                                    //item.CapUtility(client.Player.Level);
                                    //item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 50));
                                }
                                else
                                {
                                    if (args.Length > 2)
                                        item = new GeneratedUniqueItem(client.Player.Realm,
                                            (ECharacterClass) client.Player.CharacterClass.ID, client.Player.Level,
                                            (EObjectType) Convert.ToInt32(args[1]),
                                            (eInventorySlot) Convert.ToInt32(args[2]));
                                    else
                                        item = new GeneratedUniqueItem(client.Player.Realm,
                                            (ECharacterClass) client.Player.CharacterClass.ID, client.Player.Level,
                                            (EObjectType) Convert.ToInt32(args[1]));
                                }
                            }

                            item.AllowAdd = true;
                            GameServer.Database.AddObject(item);
                            InventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
                            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                            client.Player.Out.SendMessage("Generated: " + item.Name, EChatType.CT_System,
                                EChatLoc.CL_SystemWindow);
                        }
                    }
                }
                catch (Exception e)
                {
	                Console.WriteLine(e);
	                Console.WriteLine(e.StackTrace);
                    DisplaySyntax(client);
                }
            }
        }

        [Command(
            "&clearinventory",
            EPrivLevel.GM,
            "/clearinventory YES - clears your entire inventory!")]
        public class ClearInventoryCommandHandler : DOL.GS.Commands.AbstractCommandHandler,
            DOL.GS.Commands.ICommandHandler
        {
            public void OnCommand(GameClient client, string[] args)
            {
                // must add at least one parameter just to be safe
                if (args.Length > 1 && args[1].ToString() == "YES")
                {
                    foreach (InventoryItem item in client.Player.Inventory.GetItemRange(eInventorySlot.FirstBackpack,
                                 eInventorySlot.LastBackpack))
                        client.Player.Inventory.RemoveItem(item);

                    client.Out.SendMessage("Inventory cleared!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                }
                else
                {
                    DisplaySyntax(client);
                }
            }
        }


        //base chance in %
        public static ushort BASE_ROG_CHANCE = 15;

        //Named loot chance (added to base chance)
        public static ushort NAMED_ROG_CHANCE = 10;

        //base TOA chance in % (0 to disable TOA in other region than TOA)
        public static ushort BASE_TOA_CHANCE = 0;

        //Named TOA loot chance (added to named rog chance)
        public static ushort NAMED_TOA_CHANCE = 3;

        /// <summary>
        /// Generate loot for given mob
        /// </summary>
        /// <param name="mob"></param>
        /// <returns></returns>
        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);


            try
            {
                GamePlayer player = killer as GamePlayer;
                if (killer is GameNPC && ((GameNPC) killer).Brain is IControlledBrain)
                    player = ((ControlledNpcBrain) ((GameNPC) killer).Brain).GetPlayerOwner();
                if (player == null)
                    return loot;

                ECharacterClass classForLoot = (ECharacterClass) player.CharacterClass.ID;
                // allow the leader to decide the loot realm
                if (player.Group != null)
                {
                    player = player.Group.Leader;
                    classForLoot = GetRandomClassFromGroup(player.Group);
                }


                double killedCon = player.GetConLevel(mob);

                //grey don't loot RoG
                if (killedCon <= -3)
                    return loot;

                // chance to get a RoG Item
                int chance = BASE_ROG_CHANCE + ((int) killedCon + 3) * 2;
                // toa item
                bool toachance = false;

                if (IsMobInTOA(mob) && mob.Name.ToLower() != mob.Name && mob.Level >= 50)
                {
                    // ToA named mobs have good chance to drop unique loot
                    chance += NAMED_ROG_CHANCE + NAMED_TOA_CHANCE;
                    toachance = true;
                }
                else if (IsMobInTOA(mob))
                {
                    toachance = true;
                }
                else if (mob.Name.ToLower() != mob.Name)
                {
                    chance += NAMED_ROG_CHANCE;
                }

                GeneratedUniqueItem item = new GeneratedUniqueItem(toachance, player.Realm, classForLoot,
                    (byte) Math.Min(mob.Level + 1, 51));

                //item.CapUtility(mob.Level+1);

                item.AllowAdd = true;
                item.GenerateItemQuality(killedCon);

                if (player.Realm != 0)
                {
                    loot.AddRandom(chance, item, 1);
                }
            }
            catch
            {
                return loot;
            }

            return loot;
        }

        private ECharacterClass GetRandomClassFromGroup(Group group)
        {
            List<ECharacterClass> validClasses = new List<ECharacterClass>();
            foreach (GamePlayer player in group.GetMembersInTheGroup())
            {
                validClasses.Add((ECharacterClass) player.CharacterClass.ID);
            }

            return validClasses[UtilCollection.Random(validClasses.Count - 1)];
        }

        public static bool IsMobInTOA(GameNPC mob)
        {
            //if (mob.CurrentRegion.Expansion == (int)eClientExpansion.TrialsOfAtlantis)
            //	return true;

            return false;
        }

        private static EObjectType GetArmorType(ERealm realm, ECharacterClass charClass, byte level)
        {
            switch (realm)
            {
                case ERealm.Albion:
                    return GeneratedUniqueItem.GetAlbionArmorType(charClass, level);
                case ERealm.Hibernia:
                    return GeneratedUniqueItem.GetHiberniaArmorType(charClass, level);
                case ERealm.Midgard:
                    return GeneratedUniqueItem.GetMidgardArmorType(charClass, level);
            }

            return EObjectType.Cloth;
        }
        
         private static List<EObjectType> GenerateWeaponsForClass(ECharacterClass charClass, GameLiving player) {
			List<EObjectType> weapons = new List<EObjectType>();

            switch (charClass) {
				case ECharacterClass.Friar:
				case ECharacterClass.Cabalist:
				case ECharacterClass.Sorcerer:
				case ECharacterClass.Theurgist:
				case ECharacterClass.Wizard:
				case ECharacterClass.Necromancer:
				case ECharacterClass.Animist:
				case ECharacterClass.Eldritch:
				case ECharacterClass.Enchanter:
				case ECharacterClass.Mentalist:
				case ECharacterClass.Runemaster:
				case ECharacterClass.Spiritmaster:
				case ECharacterClass.Bonedancer:
					GenerateWeapon(player, charClass, EObjectType.Staff, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Valewalker:
					GenerateWeapon(player, charClass, EObjectType.Scythe, eInventorySlot.TwoHandWeapon); ;
					break;

				case ECharacterClass.Reaver:
					GenerateWeapon(player, charClass, EObjectType.Flexible, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					break;

				case ECharacterClass.Savage:
					GenerateWeapon(player, charClass, EObjectType.HandToHand, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.HandToHand, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Berserker:
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.LeftAxe, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Shadowblade:
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.LeftAxe, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Warrior:
				case ECharacterClass.Thane:
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Skald:
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Hunter:
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Sword, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CompositeBow, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, EObjectType.Spear, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Healer:
				case ECharacterClass.Shaman:
					GenerateWeapon(player, charClass, EObjectType.Staff, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Bard:
					GenerateWeapon(player, charClass, EObjectType.Instrument, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					break;

				case ECharacterClass.Warden:
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;

				case ECharacterClass.Druid:
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Staff, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Blademaster:
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Fired, eInventorySlot.DistanceWeapon);
					
					break;

				case ECharacterClass.Hero:
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.LargeWeapons, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CelticSpear, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;

				case ECharacterClass.Champion:
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.LargeWeapons, eInventorySlot.TwoHandWeapon);
					break;

				case ECharacterClass.Ranger:
					GenerateWeapon(player, charClass, EObjectType.RecurvedBow, eInventorySlot.DistanceWeapon);
					goto case ECharacterClass.Nightshade;

				case ECharacterClass.Nightshade:
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Blades, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Piercing, eInventorySlot.LeftHandWeapon);
					break;

				case ECharacterClass.Scout:
					GenerateWeapon(player, charClass, EObjectType.Longbow, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case ECharacterClass.Minstrel:
					GenerateWeapon(player, charClass, EObjectType.Instrument, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case ECharacterClass.Infiltrator:
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Crossbow, eInventorySlot.DistanceWeapon);
					break;

				case ECharacterClass.Cleric:
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					break;

				case ECharacterClass.Armsman:
					GenerateWeapon(player, charClass, EObjectType.PolearmWeapon, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Crossbow, eInventorySlot.DistanceWeapon);
					goto case ECharacterClass.Paladin;

				case ECharacterClass.Paladin: //hey one guy might get these :')
					GenerateWeapon(player, charClass, EObjectType.TwoHandedWeapon, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case ECharacterClass.Mercenary:
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.SlashingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.CrushingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.ThrustWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, EObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;


				default:
					weapons.Add(EObjectType.GenericWeapon);
					break;
					
            }

			return weapons;
		}
         
         private static void GenerateWeapon(GameLiving player, ECharacterClass charClass, EObjectType type, eInventorySlot invSlot)
        {
			//need to figure out shield size
			EColor color = EColor.White;
			ERealm realm = player.Realm;
			switch (realm)
			{
				case ERealm.Hibernia:
					color = EColor.Green_4;
					break;
				case ERealm.Albion:
					color = EColor.Red_4;
					break;
				case ERealm.Midgard:
					color = EColor.Blue_4;
					break;
			}
			if(type == EObjectType.Shield)
            {
				int shieldSize = GetShieldSizeFromClass(charClass);
                for (int i = 0; i < shieldSize; i++)
                {
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, (EDamageType)i+1);
					item.AllowAdd = true;
					item.IsTradable = false;
					item.Price = 1;
					//item.CapUtility(81);
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
				}
				
			}
			else if (type == EObjectType.Flexible)
            {
				//slash flex
				GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, EDamageType.Slash);
				dmgTypeItem.AllowAdd = true;
				dmgTypeItem.IsTradable = false;
				dmgTypeItem.Price = 1;
				//dmgTypeItem.CapUtility(81);
				GameServer.Database.AddObject(dmgTypeItem);
				InventoryItem tempItem = GameInventoryItem.Create<DbItemUnique>(dmgTypeItem);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem);

				//crush flex
				GeneratedUniqueItem dmgTypeItem2 = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, EDamageType.Crush);
				dmgTypeItem2.AllowAdd = true;
				dmgTypeItem2.IsTradable = false;
				dmgTypeItem2.Price = 1;
				//dmgTypeItem2.CapUtility(81);
				GameServer.Database.AddObject(dmgTypeItem2);
				InventoryItem tempItem2 = GameInventoryItem.Create<DbItemUnique>(dmgTypeItem2);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem2);
			}
			else if(type == EObjectType.TwoHandedWeapon || type == EObjectType.PolearmWeapon || type == EObjectType.LargeWeapons)
            {
				int endDmgType = 4; //default for all 3, slash/crush/thrust
				if(type == EObjectType.LargeWeapons || realm == ERealm.Midgard)
                {
					endDmgType = 3; //only slash/crush
                }

				//one for each damage type
                for (int i = 1; i < endDmgType; i++)
                {
					GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, (EDamageType) i);
					dmgTypeItem.AllowAdd = true;
					dmgTypeItem.IsTradable = false;
					dmgTypeItem.Price = 1;
					//dmgTypeItem.CapUtility(81);
					GameServer.Database.AddObject(dmgTypeItem);
					InventoryItem tempItem = GameInventoryItem.Create<DbItemUnique>(dmgTypeItem);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem);
				}	
			} else
            {
				GeneratedUniqueItem item = null;
				item = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot);
				item.AllowAdd = true;
				item.IsTradable = false;
				item.Price = 1;
				//item.CapUtility(81);
				GameServer.Database.AddObject(item);
				InventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
			}	
		}

        private static int GetShieldSizeFromClass(ECharacterClass charClass)
        {
			//shield size is based off of damage type
			//1 = small shield
			//2 = medium
			//3 = large
            switch (charClass)
            {
				case ECharacterClass.Berserker:
				case ECharacterClass.Skald:
				case ECharacterClass.Savage:
				case ECharacterClass.Healer:
				case ECharacterClass.Shaman:
				case ECharacterClass.Shadowblade:
				case ECharacterClass.Bard:
				case ECharacterClass.Druid:
				case ECharacterClass.Nightshade:
				case ECharacterClass.Ranger:
				case ECharacterClass.Infiltrator:
				case ECharacterClass.Minstrel:
				case ECharacterClass.Scout:
					return 1;

				case ECharacterClass.Thane:
				case ECharacterClass.Warden:
				case ECharacterClass.Blademaster:
				case ECharacterClass.Champion:
				case ECharacterClass.Mercenary:
				case ECharacterClass.Cleric:
					return 2;

				case ECharacterClass.Warrior:
				case ECharacterClass.Hero:
				case ECharacterClass.Armsman:
				case ECharacterClass.Paladin:
				case ECharacterClass.Reaver:
					return 3;
				default: return 1;
            }
        }
    }
}