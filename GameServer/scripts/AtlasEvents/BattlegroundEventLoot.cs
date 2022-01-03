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
		private int freeLootLevelOffset = 2;
		private int playerRewardOffset = 6;
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
				"For your efforts, " + realmName + " has procured a [full suit] of equipment and some [gems] to adorn them with. " +
				"Additionally, I can provide you with some [weapons] or some free [Atlas Orbs]. \n\n" +
                "This is the best gear we could provide on short notice. If you want something better, you'll have to take it from your enemies on the battlefield. " + 
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
			eObjectType armorType = GetArmorType(realm, charclass, (byte)(player.Level));
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
			if (str.Equals("full suit"))
			{
				const string customKey = "free_event_armor";
				var hasFreeArmor = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

				if (hasFreeArmor != null)
				{
					player.Out.SendMessage("Sorry " + player.Name + ", I don't have enough items left to give you another set.\n\n Go fight for your Realm to get more equipment!", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
					return false;
				}
				
				List<eInventorySlot> bodySlots = new List<eInventorySlot>();
					bodySlots.Add(eInventorySlot.ArmsArmor);
					bodySlots.Add(eInventorySlot.FeetArmor);
					bodySlots.Add(eInventorySlot.HandsArmor);
					bodySlots.Add(eInventorySlot.HeadArmor);
					bodySlots.Add(eInventorySlot.LegsArmor);
					bodySlots.Add(eInventorySlot.TorsoArmor);

					foreach (eInventorySlot islot in bodySlots) {
						GeneratedUniqueItem item = null;
						item = new GeneratedUniqueItem(realm, charclass, (byte)(player.Level + freeLootLevelOffset), armorType, islot);
						item.AllowAdd = true;
						item.Color = (int)color;
						item.IsTradable = false;
						item.Price = 1;
						GameServer.Database.AddObject(item);
						InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
						player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
						//player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}

				DOLCharactersXCustomParam charFreeEventEquip = new DOLCharactersXCustomParam();
					charFreeEventEquip.DOLCharactersObjectId = player.ObjectId;
					charFreeEventEquip.KeyName = customKey;
					charFreeEventEquip.Value = "1";
					GameServer.Database.AddObject(charFreeEventEquip);
			} 
			else if (str.Equals("weapons")) {
				
				const string customKey = "free_event_weapons";
				var hasFreeWeapons = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

				if (hasFreeWeapons != null)
				{
					player.Out.SendMessage("Sorry " + player.Name + ", I don't have enough weapons left to give you another set.\n\n Go fight for your Realm to get more equipment!", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
					return false;
				}
				
				GenerateWeaponsForClass(charclass, player);
				
				DOLCharactersXCustomParam charFreeEventEquip = new DOLCharactersXCustomParam();
				charFreeEventEquip.DOLCharactersObjectId = player.ObjectId;
				charFreeEventEquip.KeyName = customKey;
				charFreeEventEquip.Value = "1";
				GameServer.Database.AddObject(charFreeEventEquip);
			} else if (str.Equals("gems"))
            {
				const string customKey = "free_event_gems";
				var hasFreeArmor = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

				if (hasFreeArmor != null)
				{
					player.Out.SendMessage("Sorry " + player.Name + ", I don't have enough items left to give you another set.\n\n Go fight for your Realm to get more equipment!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					return false;
				}

				List<eInventorySlot> gemSlots = new List<eInventorySlot>();
				gemSlots.Add(eInventorySlot.Cloak);
				gemSlots.Add(eInventorySlot.Neck);
				gemSlots.Add(eInventorySlot.Waist);
				gemSlots.Add(eInventorySlot.Jewellery);
				gemSlots.Add(eInventorySlot.LeftRing);
				gemSlots.Add(eInventorySlot.RightRing);
				gemSlots.Add(eInventorySlot.LeftBracer);
				gemSlots.Add(eInventorySlot.RightBracer);

				foreach (eInventorySlot islot in gemSlots)
				{
					GeneratedUniqueItem item = null;
					item = new GeneratedUniqueItem(realm, charclass, (byte)(player.Level + freeLootLevelOffset), eObjectType.Magical, islot);
					item.AllowAdd = true;
					item.Color = (int)color;
					item.IsTradable = false;
					item.Price = 1;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
					//player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				DOLCharactersXCustomParam charFreeEventEquip = new DOLCharactersXCustomParam();
				charFreeEventEquip.DOLCharactersObjectId = player.ObjectId;
				charFreeEventEquip.KeyName = customKey;
				charFreeEventEquip.Value = "1";
				GameServer.Database.AddObject(charFreeEventEquip);
			}
			else if (str.Equals("Atlas Orbs"))
			{

				const string customKey = "free_orbs";
				var hasFreeOrbs = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

				if (hasFreeOrbs != null)
				{
					player.Out.SendMessage("Sorry " + player.Name + ", I don't have enough Atlas Orbs left to give you more.\n\n Go fight for your Realm to get some!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					return false;
				}

				DOLCharactersXCustomParam charFreeEventEquip = new DOLCharactersXCustomParam();
				charFreeEventEquip.DOLCharactersObjectId = player.ObjectId;
				charFreeEventEquip.KeyName = customKey;
				charFreeEventEquip.Value = "1";
				GameServer.Database.AddObject(charFreeEventEquip);

				ItemTemplate orbs = GameServer.Database.FindObjectByKey<ItemTemplate>("token_many");

				InventoryItem item = GameInventoryItem.Create(orbs);

				player.Inventory.AddTemplate(item, 10000, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

				//GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot);
			}
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

		private void GenerateWeapon(GameLiving player, eCharacterClass charClass, eObjectType type, eInventorySlot invSlot)
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
					item = new GeneratedUniqueItem(realm, charClass, (byte)(player.Level + freeLootLevelOffset), type, invSlot, (eDamageType)i+1);
					item.AllowAdd = true;
					item.Color = (int)color;
					item.IsTradable = false;
					item.Price = 1;
					GameServer.Database.AddObject(item);
					InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
				}
				
			}
			else if (type == eObjectType.Flexible)
            {
				//slash flex
				GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(player.Level + freeLootLevelOffset), type, invSlot, eDamageType.Slash);
				dmgTypeItem.AllowAdd = true;
				dmgTypeItem.Color = (int)color;
				dmgTypeItem.IsTradable = false;
				dmgTypeItem.Price = 1;
				GameServer.Database.AddObject(dmgTypeItem);
				InventoryItem tempItem = GameInventoryItem.Create<ItemUnique>(dmgTypeItem);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem);

				//crush flex
				GeneratedUniqueItem dmgTypeItem2 = new GeneratedUniqueItem(realm, charClass, (byte)(player.Level + freeLootLevelOffset), type, invSlot, eDamageType.Crush);
				dmgTypeItem2.AllowAdd = true;
				dmgTypeItem2.Color = (int)color;
				dmgTypeItem2.IsTradable = false;
				dmgTypeItem2.Price = 1;
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
					GeneratedUniqueItem dmgTypeItem = new GeneratedUniqueItem(realm, charClass, (byte)(player.Level + freeLootLevelOffset), type, invSlot, (eDamageType) i);
					dmgTypeItem.AllowAdd = true;
					dmgTypeItem.Color = (int)color;
					dmgTypeItem.IsTradable = false;
					dmgTypeItem.Price = 1;
					GameServer.Database.AddObject(dmgTypeItem);
					InventoryItem tempItem = GameInventoryItem.Create<ItemUnique>(dmgTypeItem);
					player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, tempItem);
				}	
			} else
            {
				GeneratedUniqueItem item = null;
				item = new GeneratedUniqueItem(realm, charClass, (byte)(player.Level + freeLootLevelOffset), type, invSlot);
				item.AllowAdd = true;
				item.Color = (int)color;
				item.IsTradable = false;
				item.Price = 1;
				GameServer.Database.AddObject(item);
				InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
			}	
		}

        private int GetShieldSizeFromClass(eCharacterClass charClass)
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

        private List<eObjectType> GenerateWeaponsForClass(eCharacterClass charClass, GameLiving player) {
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
    }
}