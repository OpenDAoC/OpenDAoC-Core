﻿using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS {
    public static class AtlasROGManager {

        private static DbItemTemplates beadTemplate = null;
        
        private static string _currencyID = ServerProperties.Properties.ALT_CURRENCY_ID;

        public static void GenerateROG(GameLiving living)
        {
            GenerateROG(living, false, (byte)(living.Level + 3));
        }

        public static void GenerateROG(GameLiving living, byte itemLevel)
        {
            GenerateROG(living, false, itemLevel);
        }

        public static void GenerateROG(GameLiving living, bool useEventColor)
        {
            GenerateROG(living, useEventColor, (byte)(living.Level + 3));
        }

        public static void GenerateROG(GameLiving living, bool UseEventColors, byte itemLevel)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;
                eRealm realm = player.Realm;
                eCharacterClass charclass = (eCharacterClass)player.CharacterClass.ID;

                GeneratedUniqueItem item = null;
                item = new GeneratedUniqueItem(realm, charclass, itemLevel);
                item.AllowAdd = true;
                item.IsTradable = true;

                if (UseEventColors)
                {
                    eColor color = eColor.White;

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

                    item.Color = (int)color;
                }
                
                GameServer.Database.AddObject(item);
                DbInventoryItems invitem = GameInventoryItem.Create<DbItemUniques>(item);
                invitem.IsROG = true;
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", invitem.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }

        public static void GenerateMinimumUtilityROG(GameLiving living, byte minimumUtility)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;
                eRealm realm = player.Realm;
                eCharacterClass charclass = (eCharacterClass)player.CharacterClass.ID;

                GeneratedUniqueItem item = null;
                item = new GeneratedUniqueItem(realm, charclass, (byte)(living.Level+1), minimumUtility);
                item.AllowAdd = true;
                item.IsTradable = true;

                GameServer.Database.AddObject(item);
                DbInventoryItems invitem = GameInventoryItem.Create<DbItemUniques>(item);
                invitem.IsROG = true;
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", invitem.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }


        public static void GenerateJewel(GameLiving living, byte itemLevel, int minimumUtility = 0)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;
                eRealm realm = player.Realm;
                eCharacterClass charclass = (eCharacterClass) player.CharacterClass.ID;

                GeneratedUniqueItem item = null;
                
                if(minimumUtility > 0)
                    item = new GeneratedUniqueItem(realm, charclass, itemLevel, eObjectType.Magical, minimumUtility);
                else
                    item = new GeneratedUniqueItem(realm, charclass, itemLevel, eObjectType.Magical);
                
                item.AllowAdd = true;
                item.IsTradable = true;

                GameServer.Database.AddObject(item);
                DbInventoryItems invitem = GameInventoryItem.Create<DbItemUniques>(item);
                invitem.IsROG = true;
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet",
                        invitem.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }

        public static void GenerateReward(GameLiving living, int amount)
        {
            if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
            {
                GenerateBPs(living, amount);
            }
            else
            {
                GenerateOrbAmount(living, amount);
            }
        }

        private static void GenerateBPs(GameLiving living, int amount)
        {
            if (amount == 0) return; 

            if (living != null && living is GamePlayer)
            {
                var player = living as GamePlayer;
                

                double numCurrentLoyalDays = LoyaltyManager.GetPlayerRealmLoyalty(player) != null ? LoyaltyManager.GetPlayerRealmLoyalty(player).Days : 0;

                if(numCurrentLoyalDays >= 30)
                {
                    numCurrentLoyalDays = 30;
                }

                var loyaltyBonus = ((amount * .2) * (numCurrentLoyalDays / 30));
                
                double relicBonus = (amount * (0.025 * RelicMgr.GetRelicCount(player.Realm)));

                var totBPs = amount + Convert.ToInt32(loyaltyBonus) + Convert.ToInt32(relicBonus);
                
                player.GainBountyPoints(totBPs, false);
                
                if (loyaltyBonus > 0)
                    player.Out.SendMessage($"You gained an additional {Convert.ToInt32(loyaltyBonus)} BPs due to your realm loyalty!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                if (relicBonus > 0)
                    player.Out.SendMessage($"You gained an additional {Convert.ToInt32(relicBonus)} BPs due to your realm's relic ownership!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            }
            
        }
        public static void GenerateOrbAmount(GameLiving living, int amount)
        {
            if (amount == 0) return; 

            if (living != null && living is GamePlayer)
            {
                var player = living as GamePlayer;
                
                var orbs = GameServer.Database.FindObjectByKey<DbItemTemplates>(_currencyID);
                
                if (orbs == null)
                {
                    player.Out.SendMessage("Error: Currency ID not found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                DbInventoryItems item = GameInventoryItem.Create(orbs);

                double numCurrentLoyalDays = LoyaltyManager.GetPlayerRealmLoyalty(player) != null ? LoyaltyManager.GetPlayerRealmLoyalty(player).Days : 0;

                if(numCurrentLoyalDays >= 30)
                {
                    numCurrentLoyalDays = 30;
                }

                var loyaltyBonus = ((amount * .2) * (numCurrentLoyalDays / 30));
                
                double relicOrbBonus = (amount * (0.025 * RelicMgr.GetRelicCount(player.Realm)));

                var totOrbs = amount + Convert.ToInt32(loyaltyBonus) + Convert.ToInt32(relicOrbBonus);

                item.OwnerID = player.InternalID;

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                
                if (loyaltyBonus > 0)
                    player.Out.SendMessage($"You gained an additional {Convert.ToInt32(loyaltyBonus)} orb(s) due to your realm loyalty!", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                if (relicOrbBonus > 0)
                    player.Out.SendMessage($"You gained an additional {Convert.ToInt32(relicOrbBonus)} orb(s) due to your realm's relic ownership!", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);

                if (!player.Inventory.AddCountToStack(item,totOrbs))
                {
                    if(!player.Inventory.AddTemplate(item, totOrbs, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                    {
                        item.Count = totOrbs;
                        player.CreateItemOnTheGround(item);
                        player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }

                }
                
                player.Achieve(AchievementUtils.AchievementNames.Orbs_Earned, totOrbs);
            }
        }
        
        public static void GenerateBattlegroundToken(GameLiving living, int amount)
        {
            if (living != null && living is GamePlayer)
            {
                var player = living as GamePlayer;

                DbItemTemplates token = null;
                if(living.Level > 19 && living.Level < 25) //level bracket 20-24
                    token = GameServer.Database.FindObjectByKey<DbItemTemplates>("L20RewardToken");
                if(living.Level > 33 && living.Level < 40) //level bracket 34-39
                    token = GameServer.Database.FindObjectByKey<DbItemTemplates>("L35RewardToken");

                if (token == null) return;

                DbInventoryItems item = GameInventoryItem.Create(token);
                item.OwnerID = player.InternalID;

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);

                if (!player.Inventory.AddCountToStack(item,amount))
                {
                    if(!player.Inventory.AddTemplate(item, amount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                    {
                        item.Count = amount;
                        player.CreateItemOnTheGround(item);
                        player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }

                }
            }
        }
        
        public static void GenerateBeetleCarapace(GameLiving living, int amount = 1)
        {
            if (living != null && living is GamePlayer)
            {
                var player = living as GamePlayer;

                var itemTP = GameServer.Database.FindObjectByKey<DbItemTemplates>("beetle_carapace");

                DbInventoryItems item = GameInventoryItem.Create(itemTP);
                
                item.OwnerID = player.InternalID;

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);

                if (!player.Inventory.AddCountToStack(item,amount))
                {
                    if(!player.Inventory.AddTemplate(item, amount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                    {
                        item.Count = amount;
                        player.CreateItemOnTheGround(item);
                        player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }

                }
                player.Achieve(AchievementUtils.AchievementNames.Carapace_Farmed, amount);
            }
        }

        public static GeneratedUniqueItem GenerateMonsterLootROG(eRealm realm, eCharacterClass charClass, byte level, bool isFrontierKill)
        {
            GeneratedUniqueItem item = null;
            
            if(isFrontierKill)
                item = new GeneratedUniqueItem(realm, charClass, level, level - Util.Random(-5,10));
            else
                item = new GeneratedUniqueItem(realm, charClass, level, level - Util.Random(15,20));
            
            item.AllowAdd = true;
            item.IsTradable = true;
            //item.CapUtility(level);
            return item;
            
        }

        public static DbItemUniques GenerateBeadOfRegeneration()
        {
            if(beadTemplate == null)
                beadTemplate = GameServer.Database.FindObjectByKey<DbItemTemplates>("Bead_Of_Regeneration");
            
            DbItemUniques item = new DbItemUniques(beadTemplate);
            
            return item;
        }

    }
}
