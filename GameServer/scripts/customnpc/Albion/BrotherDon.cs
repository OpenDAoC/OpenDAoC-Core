using DOL.Database;
using DOL.Events;
using System;

namespace DOL.GS.Scripts
{
    public class BrotherDon : GameNPC
    {
        private static ItemTemplate _wolfPeltCloak = null;
        protected const string wolfPeltCloak = "wolf_pelt_cloak";
        public BrotherDon()
        {
            _wolfPeltCloak = GameServer.Database.FindObjectByKey<ItemTemplate>(wolfPeltCloak);
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e == GamePlayerEvent.ReceiveItem)
            {
                ReceiveItemEventArgs gArgs = (ReceiveItemEventArgs)args;
                GamePlayer player = gArgs.Source as GamePlayer;
                if(player == null)
                {
                    return;
                }

                if (gArgs.Target.Name == this.Name && gArgs.Item.Id_nb == _wolfPeltCloak.Id_nb)
                {
                    InventoryItem item = player.Inventory.GetFirstItemByID(wolfPeltCloak, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                    if(item != null)
                    {
                        SayTo(player, "Thank you! Your service to the church will been noted!");
                        player.Inventory.RemoveItem(item);
                        SayTo(player, "Well done! You've helped the children get over the harsh winter.");
                        player.GainExperience(eXPSource.Quest, 200, true);
                        return;
                    }
                }
            }
            base.Notify(e, sender, args);
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (source == null)
            {
                return false;
            }
            GamePlayer player = source as GamePlayer;
            if (player == null)
            {
                return false;
            }
            switch (text)
            {
                case "orphanage":
                    SayTo(player, "Why yes, the little ones can get an awful chill during the long cold nights, so the orphanage could use a good [donation] of wolf cloaks. I would take any that you have.");
                    break;
                case "donation":
                    SayTo(player, "Do you want to donate your cloak?");
                    break;
            }
            return base.WhisperReceive(source, text);
        }

        public override bool Interact(GamePlayer player)
        {
            if (player == null)
            {
                return false;
            }
            if (player.Inventory.GetFirstItemByID(wolfPeltCloak, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
            {
                SayTo(player, "Hail! You don't perhaps have one of those fine wolf pelt cloaks? If you no longer have need of it, we could greatly use it at the [orphanage].");
            }            
            return base.Interact(player);
        }
    }
}
