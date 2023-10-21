using System;
using Core.Base.Enums;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.Players.Realms;
using Core.GS.Players.Titles;
using Core.GS.Server;

namespace Core.GS.GameUtils;

public static class CoreRogMgr 
{
    private static DbItemTemplate beadTemplate = null;
    
    private static string _currencyID = ServerProperty.ALT_CURRENCY_ID;

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
            ERealm realm = player.Realm;
            EPlayerClass charclass = (EPlayerClass)player.PlayerClass.ID;

            GeneratedUniqueItem item = null;
            item = new GeneratedUniqueItem(realm, charclass, itemLevel);
            item.AllowAdd = true;
            item.IsTradable = true;

            if (UseEventColors)
            {
                EColor color = EColor.White;

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

                item.Color = (int)color;
            }
            
            GameServer.Database.AddObject(item);
            DbInventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
            invitem.IsROG = true;
            player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, invitem);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", invitem.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
        }
    }

    public static void GenerateMinimumUtilityROG(GameLiving living, byte minimumUtility)
    {
        if (living != null && living is GamePlayer)
        {
            GamePlayer player = living as GamePlayer;
            ERealm realm = player.Realm;
            EPlayerClass charclass = (EPlayerClass)player.PlayerClass.ID;

            GeneratedUniqueItem item = null;
            item = new GeneratedUniqueItem(realm, charclass, (byte)(living.Level+1), minimumUtility);
            item.AllowAdd = true;
            item.IsTradable = true;

            GameServer.Database.AddObject(item);
            DbInventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
            invitem.IsROG = true;
            player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, invitem);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", invitem.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
        }
    }


    public static void GenerateJewel(GameLiving living, byte itemLevel, int minimumUtility = 0)
    {
        if (living != null && living is GamePlayer)
        {
            GamePlayer player = living as GamePlayer;
            ERealm realm = player.Realm;
            EPlayerClass charclass = (EPlayerClass) player.PlayerClass.ID;

            GeneratedUniqueItem item = null;
            
            if(minimumUtility > 0)
                item = new GeneratedUniqueItem(realm, charclass, itemLevel, EObjectType.Magical, minimumUtility);
            else
                item = new GeneratedUniqueItem(realm, charclass, itemLevel, EObjectType.Magical);
            
            item.AllowAdd = true;
            item.IsTradable = true;

            GameServer.Database.AddObject(item);
            DbInventoryItem invitem = GameInventoryItem.Create<DbItemUnique>(item);
            invitem.IsROG = true;
            player.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, invitem);
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet",
                    invitem.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
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
            

            double numCurrentLoyalDays = RealmLoyaltyMgr.GetPlayerRealmLoyalty(player) != null ? RealmLoyaltyMgr.GetPlayerRealmLoyalty(player).Days : 0;

            if(numCurrentLoyalDays >= 30)
            {
                numCurrentLoyalDays = 30;
            }

            var loyaltyBonus = ((amount * .2) * (numCurrentLoyalDays / 30));
            
            double relicBonus = (amount * (0.025 * RelicMgr.GetRelicCount(player.Realm)));

            var totBPs = amount + Convert.ToInt32(loyaltyBonus) + Convert.ToInt32(relicBonus);
            
            player.GainBountyPoints(totBPs, false);
            
            if (loyaltyBonus > 0)
                player.Out.SendMessage($"You gained an additional {Convert.ToInt32(loyaltyBonus)} BPs due to your realm loyalty!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
            if (relicBonus > 0)
                player.Out.SendMessage($"You gained an additional {Convert.ToInt32(relicBonus)} BPs due to your realm's relic ownership!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

        }
        
    }
    public static void GenerateOrbAmount(GameLiving living, int amount)
    {
        if (amount == 0) return; 

        if (living != null && living is GamePlayer)
        {
            var player = living as GamePlayer;
            
            var orbs = GameServer.Database.FindObjectByKey<DbItemTemplate>(_currencyID);
            
            if (orbs == null)
            {
                player.Out.SendMessage("Error: Currency ID not found!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            DbInventoryItem item = GameInventoryItem.Create(orbs);

            double numCurrentLoyalDays = RealmLoyaltyMgr.GetPlayerRealmLoyalty(player) != null ? RealmLoyaltyMgr.GetPlayerRealmLoyalty(player).Days : 0;

            if(numCurrentLoyalDays >= 30)
            {
                numCurrentLoyalDays = 30;
            }

            var loyaltyBonus = ((amount * .2) * (numCurrentLoyalDays / 30));
            
            double relicOrbBonus = (amount * (0.025 * RelicMgr.GetRelicCount(player.Realm)));

            var totOrbs = amount + Convert.ToInt32(loyaltyBonus) + Convert.ToInt32(relicOrbBonus);

            item.OwnerID = player.InternalID;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
            
            if (loyaltyBonus > 0)
                player.Out.SendMessage($"You gained an additional {Convert.ToInt32(loyaltyBonus)} orb(s) due to your realm loyalty!", EChatType.CT_Loot, EChatLoc.CL_SystemWindow);
            if (relicOrbBonus > 0)
                player.Out.SendMessage($"You gained an additional {Convert.ToInt32(relicOrbBonus)} orb(s) due to your realm's relic ownership!", EChatType.CT_Loot, EChatLoc.CL_SystemWindow);

            if (!player.Inventory.AddCountToStack(item,totOrbs))
            {
                if(!player.Inventory.AddTemplate(item, totOrbs, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
                {
                    item.Count = totOrbs;
                    player.CreateItemOnTheGround(item);
                    player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                }

            }
            
            player.Achieve(AchievementUtil.AchievementName.Orbs_Earned, totOrbs);
        }
    }
    
    public static void GenerateBattlegroundToken(GameLiving living, int amount)
    {
        if (living != null && living is GamePlayer)
        {
            var player = living as GamePlayer;

            DbItemTemplate token = null;
            if(living.Level > 19 && living.Level < 25) //level bracket 20-24
                token = GameServer.Database.FindObjectByKey<DbItemTemplate>("L20RewardToken");
            if(living.Level > 33 && living.Level < 40) //level bracket 34-39
                token = GameServer.Database.FindObjectByKey<DbItemTemplate>("L35RewardToken");

            if (token == null) return;

            DbInventoryItem item = GameInventoryItem.Create(token);
            item.OwnerID = player.InternalID;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);

            if (!player.Inventory.AddCountToStack(item,amount))
            {
                if(!player.Inventory.AddTemplate(item, amount, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
                {
                    item.Count = amount;
                    player.CreateItemOnTheGround(item);
                    player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                }

            }
        }
    }
    
    public static void GenerateBeetleCarapace(GameLiving living, int amount = 1)
    {
        if (living != null && living is GamePlayer)
        {
            var player = living as GamePlayer;

            var itemTP = GameServer.Database.FindObjectByKey<DbItemTemplate>("beetle_carapace");

            DbInventoryItem item = GameInventoryItem.Create(itemTP);
            
            item.OwnerID = player.InternalID;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGetAmount", amount ,item.Name), EChatType.CT_Loot, EChatLoc.CL_SystemWindow);

            if (!player.Inventory.AddCountToStack(item,amount))
            {
                if(!player.Inventory.AddTemplate(item, amount, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
                {
                    item.Count = amount;
                    player.CreateItemOnTheGround(item);
                    player.Out.SendMessage($"Your inventory is full, your {item.Name}s have been placed on the ground.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                }

            }
            player.Achieve(AchievementUtil.AchievementName.Carapace_Farmed, amount);
        }
    }

    public static GeneratedUniqueItem GenerateMonsterLootROG(ERealm realm, EPlayerClass charClass, byte level, bool isFrontierKill)
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

    public static DbItemUnique GenerateBeadOfRegeneration()
    {
        if(beadTemplate == null)
            beadTemplate = GameServer.Database.FindObjectByKey<DbItemTemplate>("Bead_Of_Regeneration");
        
        DbItemUnique item = new DbItemUnique(beadTemplate);
        
        return item;
    }

}