using DOL.Database;
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

        private static ItemTemplate beadTemplate = null;

        public static void GenerateROG(GameLiving living)
        {
            GenerateROG(living, false);
        }

        public static void GenerateROG(GameLiving living, bool UseEventColors)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;
                eRealm realm = player.Realm;
                eCharacterClass charclass = (eCharacterClass)player.CharacterClass.ID;

                GeneratedUniqueItem item = null;
                item = new GeneratedUniqueItem(realm, charclass, (byte)(player.Level + 3));
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
                InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", invitem.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }

        public static void GenerateOrbs(GameLiving living)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;

                ItemTemplate orbs = GameServer.Database.FindObjectByKey<ItemTemplate>("token_many");

                InventoryItem item = GameInventoryItem.Create(orbs);

                int maxcount = Util.Random(10, 20);
                player.Inventory.AddTemplate(item, maxcount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", item.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }
        
        public static void GenerateOrbAmount(GameLiving living, int amount)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;

                ItemTemplate orbs = GameServer.Database.FindObjectByKey<ItemTemplate>("token_many");

                InventoryItem item = GameInventoryItem.Create(orbs);
                
                player.Inventory.AddTemplate(item, amount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.YouGet", item.Name), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }
        }

        public static GeneratedUniqueItem GenerateMonsterLootROG(eRealm realm, eCharacterClass charClass, byte level)
        {
            GeneratedUniqueItem item = null;
            item = new GeneratedUniqueItem(realm, charClass, level);
            item.AllowAdd = true;
            item.IsTradable = true;
            item.CapUtility(level);
            return item;
            
        }

        public static ItemUnique GenerateBeadOfRegeneration()
        {
            if(beadTemplate == null)
                beadTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>("Bead_Of_Regeneration");
            
            ItemUnique item = new ItemUnique(beadTemplate);
            
            return item;
        }

    }
}
