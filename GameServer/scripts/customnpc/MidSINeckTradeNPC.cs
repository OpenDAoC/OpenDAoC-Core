using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class MidSINeckTradeNPC : GameNPC
    {

        public MidSINeckTradeNPC() : base() { }
        public override bool AddToWorld()
        {
            Model = 143;
            Name = "Noah";
            GuildName = "The Patient";
            Level = 50;
            Size = 30;
            Flags |= eFlags.PEACE;
            base.AddToWorld();
            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            
            TurnTo(player, 500);

            var message =
                $"Hello {player.Name}, I can trade your OLD Beaded Resisting Stones with the corrected NEW version, if you'd like.\n\n" +
                "When you're ready, hand me your old Beaded Resisting Stones and I'll give you the new version.\n\n";
            
            message += "OLD VERSION STATS:\n";
            message += "+10% resist Body/Thrust/Crush/Spirit\n\n";
            message += "NEW VERSION STATS:\n";
            message += "+10% resist Body/Thrust/Crush/Cold\n\n";
            
            message += "Choose wisely, as you cannot undo this trade.";
            
            SayTo(player, message);
            
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source is not GamePlayer player) return false;

            if (item.Id_nb != "beaded_resisting_stone")
            {
                SayTo(player, "This is not the correct item.");
                return false;
            }
            
            var message =
                $"Thank you {player.Name}, here is your new Beaded Resisting Stones Necklace.";
            
            SayTo(player, message);
            player.Inventory.RemoveItem(item);
            
            var newNeckTp = GameServer.Database.FindObjectByKey<ItemTemplate>("Beaded Resisting Stones");
            var newNeck = GameInventoryItem.Create(newNeckTp);

            if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newNeck)) return true;
            player.CreateItemOnTheGround(newNeck);
            player.Out.SendMessage($"Your inventory is full, your {newNeck.Name}s have been placed on the ground.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            return true;
            
        }
    }
}