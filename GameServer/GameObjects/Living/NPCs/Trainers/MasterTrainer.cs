using System.Collections;
using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Scripts
{
    public class MasterTrainer : GameTrainer
    {
        /// <summary>
        /// Constructs a new GameTrainer
        /// </summary>
        public MasterTrainer()
        {
        }
        /// <summary>
        /// Constructs a new GameTrainer that will also train Champion levels
        /// </summary>
        public MasterTrainer(eChampionTrainerType championTrainerType)
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
                        var translation = (DbLanguageGameNpc)LanguageMgr.GetTranslation(player.Client.Account.Language, this);

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
        public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
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
                            InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
                            player.RespecAmountSingleSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecSingle"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
                            return true;
                        }
                    case "respec_full":
                        {
                            player.Inventory.RemoveCountFromStack(item, 1);
                            InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
                            player.RespecAmountAllSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecFull", item.Name), EChatType.CT_System, EChatLoc.CL_PopupWindow);
                            return true;
                        }
                    case "respec_realm":
                        {
                            player.Inventory.RemoveCountFromStack(item, 1);
                            InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
                            player.RespecAmountRealmSkill++;
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecRealm"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
                case ERealm.Albion:
                    Name = "Albion Trainer";
                    Model = 39;
                    template.AddNPCEquipment(EInventorySlot.HeadArmor, 1290);
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 713);
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 715);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 714);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 716);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 717);
                    template.AddNPCEquipment(EInventorySlot.Cloak, 4105);
                    break;

                case ERealm.Midgard:
                    Name = "Midgard Trainer";
                    Model = 153;
                    template.AddNPCEquipment(EInventorySlot.HeadArmor, 1291);
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 698);
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 700);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 699);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 701);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 702);
                    template.AddNPCEquipment(EInventorySlot.Cloak, 4107);
                    break;

                case ERealm.Hibernia:
                    Name = "Hibernia Trainer";
                    Model = 302;
                    Size = 55;
                    template.AddNPCEquipment(EInventorySlot.HeadArmor, 1292);
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 739);
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 741);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 740);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 742);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 743);
                    template.AddNPCEquipment(EInventorySlot.Cloak, 4109);
                    break;
            }

            Inventory = template.CloseTemplate();
            Flags |= ENpcFlags.PEACE;
            GuildName = "Master Trainer";
            Level = 75;

            return base.AddToWorld();
        }

        public override EQuestIndicator GetQuestIndicator(GamePlayer player)
	    {
		return EQuestIndicator.Lore;
	    }
    }

}
