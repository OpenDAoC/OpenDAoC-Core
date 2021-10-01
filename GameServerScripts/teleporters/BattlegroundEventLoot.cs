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
			player.Out.SendMessage("Hello " + player.Name + "! We're happy to see you here, supporting your realm.\n\n" +
				"For your efforts, " + realmName + " has procured a [full suit] of equipment. \n\n" +
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
				GameServer.Database.AddObject(item);
				InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
				player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
				player.Out.SendMessage("Generated: " + item.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
    }
}