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
//12/13/2004
//Written by Gavinius
//based on Nardin and Zjovaz previous script


using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Quests;

namespace DOL.GS
{
	[NPCGuildScript("Enchanter")]
	public class Enchanter : GameNPC
	{
		private const string ENCHANT_ITEM_WEAK = "enchanting item";
		private int[] BONUS_TABLE = new int[] {5, 5, 10, 15, 20, 25, 30, 30};

		#region Examine Messages
		/// <summary>
		/// Adds messages to array, which are all sent when the NPC is examined (i.e., clicked on) by a GamePlayer.
		/// </summary>
		/// <param name="player">The GamePlayer examining the NPC.</param>
		/// <returns>Returns the messages in the array and sends them all to the player.</returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
            IList list = new ArrayList();
            // Message: You target [{0}].
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.GetExamineMessages.YouTarget", 
				GetName(0, false, player.Client.Account.Language, this)));
            // Message: You examine {0}. {1} is {2} and is an enchanter.
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.GetExamineMessages.YouExamine",
				GetName(0, false, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
				GetAggroLevelString(player, false)));
            // Message: [Give {0} an item to be magically enhanced]
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.GetExamineMessages.GiveItem",
	            GetPronoun(0, false, player.Client.Account.Language)));
            return list;
		}
		#endregion Examine Messages

		#region Interact Message
		/// <summary>
		/// Behaviors that occur when a GamePlayer interacts with the NPC (e.g., facing player, send interact message, etc.).
		/// </summary>
		/// <param name="player">The GamePlayer interacting with the NPC.</param>
		/// <returns>If the interaction is prevented by the base class on GamePlayer.cs, then return 'false'.</returns>
		public override bool Interact(GamePlayer player)
		{
			// If the interaction is not prevented by the base class on GamePlayer.cs, then return 'true'
			if (base.Interact(player))
			{
				TurnTo(player, 5000);
				
				//string Material;
				// Specify a different material requirement based on realm
				//if (player.Realm == eRealm.Hibernia)
					// Message: quartz
                    //Material = LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameNPC.Enchanter.Interact.Quartz");
				//else
					// Message: steel
                    //Material = LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameNPC.Enchanter.Interact.Steel");

				// Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
				var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
				// If the NPC has no ambient trigger message assigned, then return this message
				if (triggers == null || triggers.Length == 0)
					// Message: {0} says, "For a small fee, I can infuse your weapons or armor with an enchantment that will improve their quality. Just hand me the item. It must be of a material I can work with."
					ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.Interact.Message", GetName(0, true));
                return true;
			}
			return false;
		}
		#endregion Interact Messages

		#region Receive Responses
		/// <summary>
		/// Behaviors that occur when an NPC is given an item (primarily to repair, but also for XP item turn-in).
		/// </summary>
		/// <param name="source">The entity giving the item to the NPC (e.g., 'GamePlayer player').</param>
		/// /// <param name="item">The specific item being given to the NPC.</param>
		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			GamePlayer player = source as GamePlayer;
			if (player == null || item == null)
				return false;

			if (this.DataQuestList.Count > 0)
			{
				foreach (DataQuest quest in DataQuestList)
				{
					quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(player, this, item));
				}
			}
			
			if (item.Level >= 10 && item.IsCrafted)
			{
				if (item.Object_Type != (int) eObjectType.Magical && item.Object_Type != (int) eObjectType.Bolt && item.Object_Type != (int) eObjectType.Poison)
				{
					if (item.Bonus == 0)
					{
						player.TempProperties.setProperty(ENCHANT_ITEM_WEAK, new WeakRef(item));
						// Message: It will cost {0} to enchant that. Do you accept?
                        player.Client.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client, "GameNPC.Enchanter.ReceiveItem.Cost", Money.GetString(CalculEnchantPrice(item))), new CustomDialogResponse(EnchanterDialogResponse));
                    }
					else
						// Message: {0} says, "That is already enchanted."
						ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.ReceiveItem.AlreadyEnchanted", GetName(0, true));
                        //SayTo(player, eChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client, "GameNPC.Enchanter.ReceiveItem.AlreadyEnchanted"));
                }
				else
					// Message: {0} says, "That item can't be enchanted."
					ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.ReceiveItem.CantBeEnchanted", GetName(0, true));
                    //SayTo(player, eChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client, "GameNPC.Enchanter.ReceiveItem.CantBeEnchanted"));
            }
			else
				// Message: {0} says, "I can't enchant that material."
				ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.ReceiveItem.CantEnchantMaterial", GetName(0, true));
                //SayTo(player, eChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client, "GameNPC.Enchanter.ReceiveItem.CantEnchantMaterial"));

			return false;
		}
		#endregion Receive Responses

		#region Enchant Responses
		/// <summary>
		/// Responses that occur when an NPC attempts to enchant an item given to them.
		/// </summary>
		/// <param name="player">The entity that originally gave the item to the NPC.</param>
		/// /// <param name="response">The player's response when prompted to initiate the enchant (ACCEPT/DECLINE).</param>
		protected void EnchanterDialogResponse(GamePlayer player, byte response)
		{
			WeakReference itemWeak =
				(WeakReference) player.TempProperties.getProperty<object>(
					ENCHANT_ITEM_WEAK,
					new WeakRef(null)
					);
			player.TempProperties.removeProperty(ENCHANT_ITEM_WEAK);

			InventoryItem item = (InventoryItem) itemWeak.Target;

			if (response != 0x01 || !this.IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
			{
				// Message: You decline to have your {0} repaired.
				ChatUtil.SendSystemMessage(player, "GameNPC.Enchanter.Response.Decline", item.Name);
				
				return;
			}

			if (item == null || item.SlotPosition == (int) eInventorySlot.Ground
			                 || item.OwnerID == null || item.OwnerID != player.InternalID)
			{
				// Message: {0} says, "I can't enchant that."
				ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.Response.CantEnchant", GetName(0, true));
                //player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.Response.CantEnchant"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
			}

			long Fee = CalculEnchantPrice(item);

			if (player.GetCurrentMoney() < Fee)
			{
				// Message: {0} says, "It costs {1} to enchant {2}. You don't have that much."
				ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.Response.NotEnoughMoney", 
					GetName(0, true), 
					Money.GetString(Fee), 
					item.GetName(0, false));
                //SayTo(player, eChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.Response.NotEnoughMoney", Money.GetString(Fee)));
                return;
			}
			if (item.Level < 50)
				item.Bonus = BONUS_TABLE[(item.Level/5) - 2];
			else
				item.Bonus = 35;

			// bright
            item.Name = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.ItemLevel.Bright") + " " + item.Name;
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { item });
            GameServer.Database.SaveObject(item);
            // Message: You give {0} {1}.
            ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.Response.YouGive",
	            GetName(0, true),
	            Money.GetString(Fee));
            //player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.Response.YouGive", 
                                    //GetName(0, false, player.Client.Account.Language, this), Money.GetString(Fee)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.RemoveMoney(Fee, null);
            InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, Fee);
            // Message: {0} says, "There, it is now {1}!"
            ChatUtil.SendSayMessage(player, "GameNPC.Enchanter.Response.NowEnchanted", 
	            GetName(0, true),
	            item.GetName(1, false));
            //SayTo(player, eChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Enchanter.Response.NowEnchanted", item.GetName(1, false)));
            return;
		}
		#endregion Repair Responses

		public long CalculEnchantPrice(InventoryItem item)
		{
			return (item.Price/5);
		}
	}
}