using System;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using System.Reflection;
using DOL.Database;
using log4net.Core;
using System.Collections.Generic;

namespace DOL.GS.Scripts
{
    public class BattlegroundEventLoot : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override bool AddToWorld()
        {
            Model = 2026;
            Name = "Free Loot";
            GuildName = "Atlas Quartermaster";
            Level = 50;
            Size = 60;
            Flags |= GameNPC.eFlags.PEACE;
            return base.AddToWorld();
        }
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			string realmName = player.Realm.ToString();
			if (realmName.Equals("_FirstPlayerRealm")) {
				realmName = "Albion";
			} else if (realmName.Equals("_LastPlayerRealm")){
				realmName = "Hibernia";
            }
			TurnTo(player.X, player.Y);
			player.Out.SendMessage("Hello " + player.Name + "! We're happy to see you here, supporting your realm.\n" +
				"For your efforts, " + realmName + " has procured a [full suit] of equipment. " +
				"Additionally, I can provide you with some [weapons]. \n\n" +
				"Go forth, and do battle!", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
			return true;
		}
		public override bool WhisperReceive(GameLiving source, string str) {
			if (!base.WhisperReceive(source, str)) return false;
			if (!(source is GamePlayer)) return false;
			GamePlayer player = (GamePlayer)source;
			TurnTo(player.X, player.Y);
			eRealm realm = player.Realm;
			eCharacterClass charclass = (eCharacterClass)player.CharacterClass.ID;
			eObjectType armorType = GetArmorType(realm, charclass, (byte)(player.Level-4));
			eColor color = eColor.White;

			switch (realm) {
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
			if (str.Equals("full suit")) {
				List<eInventorySlot> bodySlots = new List<eInventorySlot>();
				bodySlots.Add(eInventorySlot.ArmsArmor);
				bodySlots.Add(eInventorySlot.FeetArmor);
				bodySlots.Add(eInventorySlot.HandsArmor);
				bodySlots.Add(eInventorySlot.HeadArmor);
				bodySlots.Add(eInventorySlot.LegsArmor);
				bodySlots.Add(eInventorySlot.TorsoArmor);

				foreach (eInventorySlot islot in bodySlots) {
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charclass, player.Level, armorType, islot);
					item.AllowAdd = true;
					item.Color = (int)color;
					item.IsTradable = false;
					item.IsDropable = false;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					invitem.SellPrice = 0;
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
					//player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			} 
			else if (str.Equals("weapons")) {
				List<eObjectType> weapons = GetWeaponsByClass(charclass);
				
				foreach (eObjectType wepType in weapons) {
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charclass, player.Level, wepType);
					item.AllowAdd = true;
					item.Color = (int)color;
					item.IsTradable = false;
					item.IsDropable = false;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					invitem.SellPrice = 0;
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
					//player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				
				//guarantee an offhand weapon
				if(charclass == eCharacterClass.Infiltrator || 
					charclass == eCharacterClass.Nightshade ||
					charclass == eCharacterClass.Ranger || 
					charclass == eCharacterClass.Blademaster ||
					charclass == eCharacterClass.Mercenary ||
					charclass == eCharacterClass.Shadowblade ||
					charclass == eCharacterClass.Berserker) {
					GeneratedUniqueItem item = null;
					eObjectType wepType = eObjectType.GenericWeapon;
					if(realm == eRealm.Hibernia) {
						wepType = (eObjectType) Util.Random(19, 20);
                    } else if (realm == eRealm.Albion) {
						wepType = (eObjectType)Util.Random(3, 4);
                    } else {
						wepType = eObjectType.LeftAxe;
                    }
					item = new GeneratedUniqueItem(realm, charclass, player.Level, wepType, eInventorySlot.LeftHandWeapon);
					item.AllowAdd = true;
					item.Color = (int)color;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
					//player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				/*
				if (charclass == eCharacterClass.Savage) {
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charclass, player.Level, eObjectType.HandToHand, eInventorySlot.RightHandWeapon);
					item.AllowAdd = true;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
					player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}*/
				
			}
			
			//GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot);
			
			return true;
		}

        private eObjectType GetArmorType(eRealm realm, eCharacterClass charClass, byte level) {
            switch (realm) {
				case eRealm.Albion:
					return GeneratedUniqueItem.GetAlbionArmorType(charClass, level);
				case eRealm.Hibernia:
					return GeneratedUniqueItem.GetHiberniaArmorType(charClass, level);
				case eRealm.Midgard:
					return GeneratedUniqueItem.GetMidgardArmorType(charClass, level);
			}
			return eObjectType.Cloth;
        }

        private void SendReply(GamePlayer target, string msg)
			{
				target.Client.Out.SendMessage(
					msg,
					eChatType.CT_Say,eChatLoc.CL_PopupWindow);
			}
		[ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("\t BG Loot NPC initialized: true");
        }
		
		private List<eObjectType> GetWeaponsByClass(eCharacterClass charClass) {
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
					weapons.Add(eObjectType.Staff);
					break;

				case eCharacterClass.Valewalker:
					weapons.Add(eObjectType.Scythe);
					break;

				case eCharacterClass.Reaver:
					weapons.Add(eObjectType.Flexible);
					weapons.Add(eObjectType.Shield);
					break;

				case eCharacterClass.Savage:
					weapons.Add(eObjectType.HandToHand);
					weapons.Add(eObjectType.TwoHandedWeapon);
					break;

				case eCharacterClass.Berserker:
					weapons.Add(eObjectType.Hammer);
					goto case eCharacterClass.Shadowblade;

				case eCharacterClass.Shadowblade:
					weapons.Add(eObjectType.Axe);
					weapons.Add(eObjectType.Sword);
					weapons.Add(eObjectType.LeftAxe);
					break;

				case eCharacterClass.Warrior:
				case eCharacterClass.Thane:
					weapons.Add(eObjectType.Shield);
					goto case eCharacterClass.Skald;

				case eCharacterClass.Skald:
					weapons.Add(eObjectType.Axe);
					weapons.Add(eObjectType.Sword);
					weapons.Add(eObjectType.Hammer);
					weapons.Add(eObjectType.TwoHandedWeapon);
					weapons.Add(eObjectType.TwoHandedWeapon);
					weapons.Add(eObjectType.TwoHandedWeapon);
					break;

				case eCharacterClass.Hunter:
					weapons.Add(eObjectType.Axe);
					weapons.Add(eObjectType.Sword);
					weapons.Add(eObjectType.Hammer);
					weapons.Add(eObjectType.Spear);
					weapons.Add(eObjectType.CompositeBow);
					break;

				case eCharacterClass.Healer:
				case eCharacterClass.Shaman:
					weapons.Add(eObjectType.Hammer);
					weapons.Add(eObjectType.Shield);
					weapons.Add(eObjectType.Staff);
					break;

				case eCharacterClass.Bard:
					weapons.Add(eObjectType.Instrument);
					weapons.Add(eObjectType.Blunt);
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Shield);
					break;

				case eCharacterClass.Warden:
					weapons.Add(eObjectType.Fired);
					weapons.Add(eObjectType.Shield);
					weapons.Add(eObjectType.Blunt);
					weapons.Add(eObjectType.Blades);
					break;

				case eCharacterClass.Druid:
					weapons.Add(eObjectType.Shield);
					weapons.Add(eObjectType.Staff);
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Blunt);
					break;

				case eCharacterClass.Blademaster:
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Blunt);
					weapons.Add(eObjectType.Piercing);
					weapons.Add(eObjectType.Fired);
					break;

				case eCharacterClass.Hero:
					weapons.Add(eObjectType.Shield);
					weapons.Add(eObjectType.Blunt);
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Piercing);
					weapons.Add(eObjectType.TwoHandedWeapon);
					weapons.Add(eObjectType.CelticSpear);
					break;

				case eCharacterClass.Champion:
					weapons.Add(eObjectType.Shield);
					weapons.Add(eObjectType.Blunt);
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Piercing);
					weapons.Add(eObjectType.TwoHandedWeapon);
					break;

				case eCharacterClass.Ranger:
					weapons.Add(eObjectType.RecurvedBow);
					goto case eCharacterClass.Nightshade;

				case eCharacterClass.Nightshade:
					weapons.Add(eObjectType.Blades);
					weapons.Add(eObjectType.Piercing);
					break;

				case eCharacterClass.Scout:
					weapons.Add(eObjectType.Longbow);
					weapons.Add(eObjectType.Shield);
					goto case eCharacterClass.Infiltrator;

				case eCharacterClass.Minstrel:
					weapons.Add(eObjectType.Instrument);
					goto case eCharacterClass.Infiltrator;

				case eCharacterClass.Infiltrator:
					weapons.Add(eObjectType.SlashingWeapon);
					weapons.Add(eObjectType.ThrustWeapon);
					break;

				case eCharacterClass.Cleric:
					weapons.Add(eObjectType.CrushingWeapon);
					weapons.Add(eObjectType.Shield);
					break;

				case eCharacterClass.Armsman:
					weapons.Add(eObjectType.PolearmWeapon);
					weapons.Add(eObjectType.PolearmWeapon);
					goto case eCharacterClass.Paladin;

				case eCharacterClass.Paladin:
					weapons.Add(eObjectType.TwoHandedWeapon);
					weapons.Add(eObjectType.TwoHandedWeapon);
					weapons.Add(eObjectType.Shield);
					goto case eCharacterClass.Mercenary;

				case eCharacterClass.Mercenary:
					weapons.Add(eObjectType.CrushingWeapon);
					weapons.Add(eObjectType.SlashingWeapon);
					weapons.Add(eObjectType.ThrustWeapon);
					break;


				default:
					weapons.Add(eObjectType.GenericWeapon);
					break;
					
            }

			return weapons;
		}
    }
}