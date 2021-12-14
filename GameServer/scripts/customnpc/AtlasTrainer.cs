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
 *
 * Script edited by clait, KNutters
 * Atlas - www.atlasfreeshard.com
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// The mother class for all class trainers
    /// </summary>
    public class AtlasTrainer : GameTrainer
    {
        /// <summary>
        /// Constructs a new GameTrainer
        /// </summary>
        public AtlasTrainer()
        {
        }
        /// <summary>
        /// Constructs a new GameTrainer that will also train Champion levels
        /// </summary>
        public AtlasTrainer(eChampionTrainerType championTrainerType)
        {
            m_championTrainerType = championTrainerType;
        }

        #region GetExamineMessages
        /// <summary>
        /// Adds messages to ArrayList which are sent when object is targeted
        /// </summary>
        /// <param name="player">GamePlayer that is examining this object</param>
        /// <returns>list with string messages</returns>
        public override IList GetExamineMessages(GamePlayer player)
        {
            string TrainerClassName = "";
            switch (player.Client.Account.Language)
            {
                case "DE":
                    {
                        var translation = (DBLanguageNPC)LanguageMgr.GetTranslation(player.Client.Account.Language, this);

                        if (translation != null)
                        {
                            int index = -1;
                            if (translation.GuildName.Length > 0)
                                index = translation.GuildName.IndexOf("-Ausbilder");
                            if (index >= 0)
                                TrainerClassName = translation.GuildName.Substring(0, index);
                        }
                        else
                        {
                            TrainerClassName = GuildName;
                        }
                    }
                    break;
                default:
                    {
                        int index = -1;
                        if (GuildName.Length > 0)
                            index = GuildName.IndexOf(" Trainer");
                        if (index >= 0)
                            TrainerClassName = GuildName.Substring(0, index);
                    }
                    break;
            }

            IList list = new ArrayList();
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouTarget",
                                                GetName(0, false, player.Client.Account.Language, this)));
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouExamine",
                                                GetName(0, false, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false), TrainerClassName));
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.RightClick"));
            return list;
        }
        #endregion

        /// <summary>
        /// For Recieving Respec Stones.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null) return false;

            GamePlayer player = source as GamePlayer;
            if (player != null)
            {
                switch (item.Id_nb)
                {
                    //case "token_solo":
                    //    {
                    //        if (player.Level >= 50)
                    //        {
                    //            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "You are already level 50!"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    //            return false;
                    //        }

                    //        if (item.Count < player.Level)
                    //        {
                    //            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "You need to turn in " + player.Level + " at once."), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    //            return false;
                    //        }

                    //        var remaining = item.Count;
                    //        var used = 0;
                    //        long totalXpGained = 0;
                    //        var timesTurnedIn = 0;

                    //        while (remaining >= player.Level && player.Level < 50)
                    //        {
                    //            remaining -= player.Level;
                    //            used += player.Level;
                    //            var amount = GamePlayer.GetExperienceAmountForLevel(player.Level) * 1;

                    //            totalXpGained += amount;
                    //            timesTurnedIn++;
                    //            player.GainExperience(eXPSource.Other, amount, false);
                    //        }

                    //        if (used > 0)
                    //        {
                    //            player.Inventory.RemoveCountFromStack(item, used);
                    //            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "You have made " + timesTurnedIn + " turn ins and used a total of " + used + " tokens. You have gained a total of " + totalXpGained + " experience points.(token_single)"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    //            return true;
                    //        }
                    //        break;
                    //    }
                    //case "token_many":
                    //    {

                    //        if (player.Level >= 50)
                    //        {
                    //            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "You are already level 50!"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    //            return false;
                    //        }

                    //        var orbXPTotal = 0;
                    //        double orbXPMultiplier = 0;
                    //        var usedOrbs = 0;
                    //        var orbXP = 0;
                    //        var orbCount = item.Count;
                    //        int iteration = 0;

                    //        while (orbCount > 0)
                    //        {
                    //            orbXPMultiplier = GetOrbMultuplier((byte)(player.Level + iteration));

                    //            // xp each orb is worth
                    //            orbXP = (int)((GamePlayer.GetExperienceAmountForLevel((byte)(player.Level + iteration)) -
                    //                player.GetExperienceNeededForLevel((byte)(player.Level + iteration))) * orbXPMultiplier);

                    //            // xp player needs to reach next level
                    //            double neededXPForLevel = GamePlayer.GetExperienceAmountForLevel((byte)(player.Level + iteration)) - player.Experience + orbXPTotal;

                    //            // number of orbs needed to level up
                    //            int neededOrbsForLevel = (int)(neededXPForLevel / orbXP + 1);

                    //            if (orbCount > neededOrbsForLevel)
                    //            {
                    //                orbXPTotal += neededOrbsForLevel * orbXP;
                    //                usedOrbs += neededOrbsForLevel;
                    //                orbCount -= neededOrbsForLevel;
                    //            }
                    //            else
                    //            {
                    //                orbXPTotal += orbCount * orbXP;
                    //                usedOrbs += orbCount;
                    //                orbCount -= orbCount;
                    //            }
                    //            iteration++;
                    //        }

                    //        player.GainExperience(eXPSource.Other, orbXPTotal, false);

                    //        player.Inventory.RemoveCountFromStack(item, item.Count);

                    //        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "You have turned in " + usedOrbs + " tokens. You have gained a total of " + orbXPTotal + " experience points."), eChatType.CT_System, eChatLoc.CL_PopupWindow);

                    //        return true;
                    //    }


                    case "respec_single":
                        {
                            player.Inventory.RemoveCountFromStack(item, 1);
                            InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);
                            player.RespecAmountSingleSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecSingle"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            return true;
                        }
                    case "respec_full":
                        {
                            player.Inventory.RemoveCountFromStack(item, 1);
                            InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);
                            player.RespecAmountAllSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecFull", item.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            return true;
                        }
                    case "respec_realm":
                        {
                            player.Inventory.RemoveCountFromStack(item, 1);
                            InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);
                            player.RespecAmountRealmSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecRealm"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            return true;
                        }
                }
            }
            return base.ReceiveItem(source, item);
        }

        public double GetOrbMultuplier(byte level)
        {
            double orbXPMultiplier = 0;
            if (level <= 4)
                orbXPMultiplier = 0.2;
            else if (level <= 9)
                orbXPMultiplier = 0.1;
            else if (level <= 19)
                orbXPMultiplier = 0.05;
            else if (level <= 35)
                orbXPMultiplier = 0.025;
            else if (level <= 45)
                orbXPMultiplier = 0.0125;
            else if (level <= 49)
                orbXPMultiplier = 0.00625;

            return orbXPMultiplier;
        }

        /// <summary>
        /// Offer training to the player.
        /// </summary>
        /// <param name="player"></param>
        protected override void OfferTraining(GamePlayer player)
        {
            // Left this here if messaging is ever requested

            //SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.Train.WouldYouLikeTo"));
        }

        /// <summary>
        /// No trainer for disabled classes
        /// </summary>
        /// <returns></returns>
        public override bool AddToWorld()
        {
            // This may need to be checked for accuracy before pushing to the live server
            // Wasn't 100% about this due to differences in DB

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Albion Trainer";
                    Model = 39;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 1290);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 713);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 715);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 714);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 716);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 717);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 4105);
                    break;

                case eRealm.Midgard:
                    Name = "Midgard Trainer";
                    Model = 153;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 1291);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 698);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 700);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 699);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 701);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 702);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 4107);
                    break;

                case eRealm.Hibernia:
                    Name = "Hibernia Trainer";
                    Model = 302;
                    Size = 55;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 1292);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 739);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 741);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 740);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 742);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 743);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 4109);
                    break;
            }

            Inventory = template.CloseTemplate();
            Flags |= eFlags.PEACE;
            GuildName = "Master Trainer";
            Level = 75;

            return base.AddToWorld();
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
	    {
		return eQuestIndicator.Lore;
	    }
    }

}
