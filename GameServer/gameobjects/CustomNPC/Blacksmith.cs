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

using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.Language;

namespace DOL.GS
{
    [NPCGuildScript("Smith")]
    public class Blacksmith : GameNPC
    {
        private const string REPAIR_ITEM_WEAK = "repair item";

        /// <summary>
        /// Can accept any item
        /// </summary>
        public override bool CanTradeAnyItem
        {
            get { return true; }
        }


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
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.YouTarget",
                GetName(0, false, player.Client.Account.Language, this)));
            // Message: You examine {0}. {1} is {2} and is a smith.
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.YouExamine",
                GetName(0, false, player.Client.Account.Language, this),
                GetPronoun(0, true, player.Client.Account.Language), 
                GetAggroLevelString(player, false)));
            // Message: [Give {0} an item to be repaired]
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.GiveObject",
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
            // If the interaction is prevented by the base class on GamePlayer.cs, then return 'false'
            if (!base.Interact(player))
                return false;

            // NPC faces the player that just interacted with them for #### milliseconds
            TurnTo(player, 5000);

            // Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
            var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
            // If the NPC has no ambient trigger message assigned, then return this message
            if (triggers == null || triggers.Length == 0)
                // Message: {0} says, "I can repair weapons or armor for you. Just hand me the item you want repaired and I'll see what I can do, for a small fee."
                ChatUtil.SendDialogMessage(player, "GameNPC.Blacksmith.Say", GetName(0, true));
            return true;
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
                    quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));
                }
            }

            if (item.Count != 1)
            {
                // Message: {0} doesn't want stacked items.
                ChatUtil.SendErrorMessage(player, "GameNPC.Blacksmith.StackedObjets", GetName(0, true));

                return false;
            }

            switch (item.Object_Type)
            {
                case (int) eObjectType.GenericItem:
                case (int) eObjectType.Magical:
                case (int) eObjectType.Instrument:
                case (int) eObjectType.Poison:
                    // Message: {0} says, "I can't repair that."
                    ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

                    return false;
            }

            if (item.Condition < item.MaxCondition)
            {
                if (item.Durability <= 0)
                {
                    // Message: {0} says, "I can't repair that."
                    ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

                    return false;
                }
                else
                {
                    player.TempProperties.setProperty(REPAIR_ITEM_WEAK, new WeakRef(item));
                    // Pop-up dialog prompting repair with ACCEPT/DECLINE options
                    // Message: It will cost {0} to repair {1}. Do you accept?
                    player.Client.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.RepairCostAccept", 
                        Money.GetString(item.RepairCost), item.GetName(0, false)), new CustomDialogResponse(BlacksmithDialogResponse));
                }
            }
            else
            {
                // Message: Your {0} is already in perfect condition.
                ChatUtil.SendErrorMessage(player, "GameNPC.Blacksmith.NoNeedRepair", item.Name);
            }


            return false;
        }
        #endregion Receive Responses

        #region Repair Responses
        /// <summary>
        /// Responses that occur when an NPC attempts to repair an item given to them.
        /// </summary>
        /// <param name="player">The entity that originally gave the item to the NPC.</param>
        /// /// <param name="response">The player's response when prompted to initiate the repair (ACCEPT/DECLINE).</param>
        protected void BlacksmithDialogResponse(GamePlayer player, byte response)
        {
            WeakReference itemWeak =
                (WeakReference) player.TempProperties.getProperty<object>(
                    REPAIR_ITEM_WEAK,
                    new WeakRef(null)
                );
            player.TempProperties.removeProperty(REPAIR_ITEM_WEAK);
            InventoryItem item = (InventoryItem) itemWeak.Target;

            if (response != 0x01)
            {
                // Message: You decline to have your {0} repaired.
                ChatUtil.SendSystemMessage(player, "GameNPC.Blacksmith.AbortRepair", item.Name);

                return;
            }


            if (item == null || item.SlotPosition == (int) eInventorySlot.Ground
                             || item.OwnerID == null || item.OwnerID != player.InternalID)
            {
                // Message: {0} says, "I can't repair that."
                ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

                return;
            }

            int ToRecoverCond = item.MaxCondition - item.Condition;

            if (!player.RemoveMoney(item.RepairCost))
            {
                InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.RepairCost);
                // Message: {0} says, "It costs {1} to repair {2}. You don't have that much."
                ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.NotEnoughMoney", 
                    GetName(0, true), 
                    Money.GetString(item.RepairCost), 
                    item.GetName(0, false));

                return;
            }

            // Message: You pay {0} {1}.
            ChatUtil.SendSystemMessage(player, "GameNPC.Blacksmith.YouPay", GetName(0, false), Money.GetString(item.RepairCost));

            // Items with IsNotLosingDur are not....losing DUR.
            if (ToRecoverCond + 1 >= item.Durability)
            {
                item.Condition = item.Condition + item.Durability;
                item.Durability = 0;
                // Message: {0} says, "Uhh, {1} is rather old. I won't be able to repair it again, so be careful!"
                ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.ObjectRatherOld", GetName(0, true), item.GetName(0, false));
            }
            else
            {
                item.Condition = item.MaxCondition;
                if (!item.IsNotLosingDur) item.Durability -= (ToRecoverCond + 1);
            }


            player.Out.SendInventoryItemsUpdate(new InventoryItem[] {item});
            // Message: {0} says, "There, {1} is ready for combat."
            ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.ItsDone", GetName(0, true), item.GetName(0, false));

            return;
        }
        #endregion  Repair Responses
    }
}