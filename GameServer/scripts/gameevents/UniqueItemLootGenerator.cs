/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */


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
        [CmdAttribute(
            "&genuniques",
            ePrivLevel.GM,
            "/genuniques ([TOA] || [L51] || [self] || [deck] || [suit] || [objecttype]) [itemtype] : generate 8 unique items")]
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

                    if (args.Length > 1 && Convert.ToString(args[1]).ToUpper() == "DECK")
                    {
	                    PlayerDeck deck = new PlayerDeck();
                    }
                    else if (args.Length > 1 && Convert.ToString(args[1]).ToUpper() == "SUIT")
                    {
                        GamePlayer player = client.Player;
                        List<eInventorySlot> bodySlots = new List<eInventorySlot>();
                        bodySlots.Add(eInventorySlot.ArmsArmor);
                        bodySlots.Add(eInventorySlot.FeetArmor);
                        bodySlots.Add(eInventorySlot.HandsArmor);
                        bodySlots.Add(eInventorySlot.HeadArmor);
                        bodySlots.Add(eInventorySlot.LegsArmor);
                        bodySlots.Add(eInventorySlot.TorsoArmor);

                        eRealm realm = player.Realm;
                        eCharacterClass charclass = (eCharacterClass) player.CharacterClass.ID;
                        eObjectType armorType = GetArmorType(realm, charclass, (byte) (player.Level));

                        foreach (eInventorySlot islot in bodySlots)
                        {
                            GeneratedUniqueItem item = null;
                            item = new GeneratedUniqueItem(realm, charclass, (byte) (81), armorType, islot);
                            item.AllowAdd = true;
                            item.IsTradable = false;
                            item.Price = 1;
                            //item.CapUtility(81);
                            GameServer.Database.AddObject(item);
                            InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
                            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                            //player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }

                        List<eObjectType> weapons = GenerateWeaponsForClass(charclass, player);
                        
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
                                        (eCharacterClass) client.Player.CharacterClass.ID, 51);
                                    item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 60));
                                }
                                else if (Convert.ToString(args[1]).ToUpper() == "L51")
                                {
                                    item = new GeneratedUniqueItem(client.Player.Realm,
                                        (eCharacterClass) client.Player.CharacterClass.ID, 51);
                                    item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 50));
                                }
                                else if (Convert.ToString(args[1]).ToUpper() == "SELF")
                                {
                                    item = new GeneratedUniqueItem(client.Player.Realm,
                                        (eCharacterClass) client.Player.CharacterClass.ID, client.Player.Level);
                                    //item.CapUtility(client.Player.Level);
                                    //item.GenerateItemQuality(GameObject.GetConLevel(client.Player.Level, 50));
                                }
                                else
                                {
                                    if (args.Length > 2)
                                        item = new GeneratedUniqueItem(client.Player.Realm,
                                            (eCharacterClass) client.Player.CharacterClass.ID, client.Player.Level,
                                            (eObjectType) Convert.ToInt32(args[1]),
                                            (eInventorySlot) Convert.ToInt32(args[2]));
                                    else
                                        item = new GeneratedUniqueItem(client.Player.Realm,
                                            (eCharacterClass) client.Player.CharacterClass.ID, client.Player.Level,
                                            (eObjectType) Convert.ToInt32(args[1]));
                                }
                            }

                            item.AllowAdd = true;
                            GameServer.Database.AddObject(item);
                            InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
                            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                            client.Player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System,
                                eChatLoc.CL_SystemWindow);
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

        [CmdAttribute(
            "&clearinventory",
            ePrivLevel.GM,
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

                    client.Out.SendMessage("Inventory cleared!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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

                eCharacterClass classForLoot = (eCharacterClass) player.CharacterClass.ID;
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

        private eCharacterClass GetRandomClassFromGroup(Group group)
        {
            List<eCharacterClass> validClasses = new List<eCharacterClass>();
            foreach (GamePlayer player in group.GetMembersInTheGroup())
            {
                validClasses.Add((eCharacterClass) player.CharacterClass.ID);
            }

            return validClasses[Util.Random(validClasses.Count - 1)];
        }

        public static bool IsMobInTOA(GameNPC mob)
        {
            //if (mob.CurrentRegion.Expansion == (int)eClientExpansion.TrialsOfAtlantis)
            //	return true;

            return false;
        }

        private static eObjectType GetArmorType(eRealm realm, eCharacterClass charClass, byte level)
        {
            switch (realm)
            {
                case eRealm.Albion:
                    return GeneratedUniqueItem.GetAlbionArmorType(charClass, level);
                case eRealm.Hibernia:
                    return GeneratedUniqueItem.GetHiberniaArmorType(charClass, level);
                case eRealm.Midgard:
                    return GeneratedUniqueItem.GetMidgardArmorType(charClass, level);
            }

            return eObjectType.Cloth;
        }
        
         private static List<eObjectType> GenerateWeaponsForClass(eCharacterClass charClass, GameLiving player) {
			List<eObjectType> weapons = new List<eObjectType>();

            switch (charClass) {
				case eCharacterClass.Friar:
				case eCharacterClass.Cabalist:
				case eCharacterClass.Sorcerer:
				case eCharacterClass.Theurgist:
				case eCharacterClass.Wizard:
				case eCharacterClass.Necromancer:
				case eCharacterClass.Animist:
				case eCharacterClass.Eldritch:
				case eCharacterClass.Enchanter:
				case eCharacterClass.Mentalist:
				case eCharacterClass.Runemaster:
				case eCharacterClass.Spiritmaster:
				case eCharacterClass.Bonedancer:
					GenerateWeapon(player, charClass, eObjectType.Staff, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Valewalker:
					GenerateWeapon(player, charClass, eObjectType.Scythe, eInventorySlot.TwoHandWeapon); ;
					break;

				case eCharacterClass.Reaver:
					GenerateWeapon(player, charClass, eObjectType.Flexible, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					break;

				case eCharacterClass.Savage:
					GenerateWeapon(player, charClass, eObjectType.HandToHand, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.HandToHand, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Berserker:
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.LeftAxe, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Shadowblade:
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.LeftAxe, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Warrior:
				case eCharacterClass.Thane:
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Skald:
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Axe, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Hunter:
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Sword, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CompositeBow, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, eObjectType.Spear, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Healer:
				case eCharacterClass.Shaman:
					GenerateWeapon(player, charClass, eObjectType.Staff, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Hammer, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Bard:
					GenerateWeapon(player, charClass, eObjectType.Instrument, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					break;

				case eCharacterClass.Warden:
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;

				case eCharacterClass.Druid:
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Staff, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Blademaster:
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Fired, eInventorySlot.DistanceWeapon);
					
					break;

				case eCharacterClass.Hero:
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.LargeWeapons, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CelticSpear, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;

				case eCharacterClass.Champion:
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blunt, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.LargeWeapons, eInventorySlot.TwoHandWeapon);
					break;

				case eCharacterClass.Ranger:
					GenerateWeapon(player, charClass, eObjectType.RecurvedBow, eInventorySlot.DistanceWeapon);
					goto case eCharacterClass.Nightshade;

				case eCharacterClass.Nightshade:
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Blades, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Piercing, eInventorySlot.LeftHandWeapon);
					break;

				case eCharacterClass.Scout:
					GenerateWeapon(player, charClass, eObjectType.Longbow, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case eCharacterClass.Minstrel:
					GenerateWeapon(player, charClass, eObjectType.Instrument, eInventorySlot.DistanceWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case eCharacterClass.Infiltrator:
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Crossbow, eInventorySlot.DistanceWeapon);
					break;

				case eCharacterClass.Cleric:
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					break;

				case eCharacterClass.Armsman:
					GenerateWeapon(player, charClass, eObjectType.PolearmWeapon, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Crossbow, eInventorySlot.DistanceWeapon);
					goto case eCharacterClass.Paladin;

				case eCharacterClass.Paladin: //hey one guy might get these :')
					GenerateWeapon(player, charClass, eObjectType.TwoHandedWeapon, eInventorySlot.TwoHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Shield, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					break;

				case eCharacterClass.Mercenary:
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.RightHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.SlashingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.CrushingWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.ThrustWeapon, eInventorySlot.LeftHandWeapon);
					GenerateWeapon(player, charClass, eObjectType.Fired, eInventorySlot.DistanceWeapon);
					break;


				default:
					weapons.Add(eObjectType.GenericWeapon);
					break;
					
            }

			return weapons;
		}
         
         private static void GenerateWeapon(GameLiving player, eCharacterClass charClass, eObjectType type, eInventorySlot invSlot)
        {
			//need to figure out shield size
			eColor color = eColor.White;
			eRealm realm = player.Realm;
			switch (realm)
			{
				case eRealm.Hibernia:
					color = eColor.Green_4;
					break;
				case eRealm.Albion:
					color = eColor.Red_4;
					break;
				case eRealm.Midgard:
					color = eColor.Blue_4;
					break;
			}
			if(type == eObjectType.Shield)
            {
				int shieldSize = GetShieldSizeFromClass(charClass);
                for (int i = 0; i < shieldSize; i++)
                {
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, (eDamageType)i+1);
					item.AllowAdd = true;
					item.IsTradable = false;
					item.Price = 1;
					//item.CapUtility(81);
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
				}
				
			}
			else if (type == eObjectType.Flexible)
            {
				//slash flex
				GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, eDamageType.Slash);
				dmgTypeItem.AllowAdd = true;
				dmgTypeItem.IsTradable = false;
				dmgTypeItem.Price = 1;
				//dmgTypeItem.CapUtility(81);
				GameServer.Database.AddObject(dmgTypeItem);
				InventoryItem tempItem = GameInventoryItem.Create<ItemUnique>(dmgTypeItem);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem);

				//crush flex
				GeneratedUniqueItem dmgTypeItem2 = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, eDamageType.Crush);
				dmgTypeItem2.AllowAdd = true;
				dmgTypeItem2.IsTradable = false;
				dmgTypeItem2.Price = 1;
				//dmgTypeItem2.CapUtility(81);
				GameServer.Database.AddObject(dmgTypeItem2);
				InventoryItem tempItem2 = GameInventoryItem.Create<ItemUnique>(dmgTypeItem2);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem2);
			}
			else if(type == eObjectType.TwoHandedWeapon || type == eObjectType.PolearmWeapon || type == eObjectType.LargeWeapons)
            {
				int endDmgType = 4; //default for all 3, slash/crush/thrust
				if(type == eObjectType.LargeWeapons || realm == eRealm.Midgard)
                {
					endDmgType = 3; //only slash/crush
                }

				//one for each damage type
                for (int i = 1; i < endDmgType; i++)
                {
					GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(81), type, invSlot, (eDamageType) i);
					dmgTypeItem.AllowAdd = true;
					dmgTypeItem.IsTradable = false;
					dmgTypeItem.Price = 1;
					//dmgTypeItem.CapUtility(81);
					GameServer.Database.AddObject(dmgTypeItem);
					InventoryItem tempItem = GameInventoryItem.Create<ItemUnique>(dmgTypeItem);
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
				InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
			}	
		}

        private static int GetShieldSizeFromClass(eCharacterClass charClass)
        {
			//shield size is based off of damage type
			//1 = small shield
			//2 = medium
			//3 = large
            switch (charClass)
            {
				case eCharacterClass.Berserker:
				case eCharacterClass.Skald:
				case eCharacterClass.Savage:
				case eCharacterClass.Healer:
				case eCharacterClass.Shaman:
				case eCharacterClass.Shadowblade:
				case eCharacterClass.Bard:
				case eCharacterClass.Druid:
				case eCharacterClass.Nightshade:
				case eCharacterClass.Ranger:
				case eCharacterClass.Infiltrator:
				case eCharacterClass.Minstrel:
				case eCharacterClass.Scout:
					return 1;

				case eCharacterClass.Thane:
				case eCharacterClass.Warden:
				case eCharacterClass.Blademaster:
				case eCharacterClass.Champion:
				case eCharacterClass.Mercenary:
				case eCharacterClass.Cleric:
					return 2;

				case eCharacterClass.Warrior:
				case eCharacterClass.Hero:
				case eCharacterClass.Armsman:
				case eCharacterClass.Paladin:
				case eCharacterClass.Reaver:
					return 3;
				default: return 1;
            }
        }
    }
}