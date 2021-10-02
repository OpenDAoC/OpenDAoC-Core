//edited by loki for current SVN 2018


using DOL.Database;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class ItemModel : GameNPC
    {
        public string TempProperty = "ItemModel";
        private int Chance;
        private Random rnd = new Random();

        public override bool AddToWorld()
        {
            base.AddToWorld();
            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (base.Interact(player))
            {
                TurnTo(player, 500);
                InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
                if (item == null)
                {
                    SendReply(player, "Hello there! " +
                    "I can change the looks of your weapons and armour for a fee of 500bp" +
                    " Just hand me the item, and whisper me the model you would like (/whisper id).\n\n" +
                    "Visit http://www.dolserver.net/?nav=idlist");
                }
                else
                {
                    ReceiveItem(player, item);
                }
                return true;
            }
            return false;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer player = (GamePlayer)source;
            TurnTo(player.X, player.Y);

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);

            if (item == null)
            {
                SendReply(player, "I need an item to work on!");
                return false;
            }
            int model = item.Model;
            int tmpmodel = int.Parse(str);
            if (tmpmodel != 0) model = tmpmodel;
            SetModel(player, model);
            SendReply(player, "I have changed your item's model, you can now use it.");
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer player = source as GamePlayer;
            if (player == null || item == null) return false;
            bool output = false;
            #region messages
            if (player.BountyPoints < 300)
            {
                SendReply(player, "You need 300 bounty points to use my service!");
                return false;
            }           
            if ((player.TempProperties.getProperty<InventoryItem>(TempProperty)) != null)
            {
                item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
                SendReply(player, "You already gave me an item! What was it again?");
                output = true;
            }
            SendReply(player, "Ok, now whisper me the model ID.");
            player.TempProperties.setProperty(TempProperty, item);
            return true;
            #endregion messages
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }

        public void SetModel(GamePlayer player, int number)
        {
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
            player.TempProperties.removeProperty(TempProperty);

            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return;

            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Model = number;
            GameServer.Database.AddObject(unique);
            InventoryItem newInventoryItem = GameInventoryItem.Create(unique as ItemTemplate);
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            player.RemoveBountyPoints(300);
            player.SaveIntoDatabase();
        }
    }
}
