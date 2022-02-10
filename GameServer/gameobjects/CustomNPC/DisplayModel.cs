

using System;
using DOL.Database;

namespace DOL.GS {
    public class DisplayModel : GameNPC
    {
        private GamePlayer m_displayedPlayer;
        
        
        public DisplayModel(GamePlayer player, InventoryItem item)
        {
            m_displayedPlayer = player;
            //player model contains 5 bits of extra data that causes issues if used
            //for an NPC model. we do this to drop the first 5 bits and fill w/ 0s
            ushort tmpModel =  (ushort) (player.Model << 5);
            tmpModel = (ushort) (tmpModel >> 5);

            //Fill the object variables
            this.Level = player.Level;
            this.Realm = player.Realm;
            this.Model = tmpModel;
            //mob.Model = 8;
            this.Name = player.Name + "'s Reflection";

            this.CurrentSpeed = 0;
            this.MaxSpeedBase = 200;
            this.GuildName = "A Faded You";
            this.Size = 50;
            
            var template = new GameNpcInventoryTemplate();
            foreach (var invItem in player.Inventory.EquippedItems)
            {
                template.AddNPCEquipment((eInventorySlot)invItem.Item_Type, invItem.Model, invItem.Color, invItem.Effect, invItem.Extension);
            }

            if (item != null)
            {
                if(template.GetItem((eInventorySlot)item.Item_Type) != null)
                    template.RemoveNPCEquipment((eInventorySlot)item.Item_Type);
                template.AddNPCEquipment((eInventorySlot)item.Item_Type, item.Model, item.Color, item.Effect, item.Extension);
            }
            
            this.Inventory = template.CloseTemplate();
        }

        public override bool Interact(GamePlayer player)
        {
            player.Out.SendLivingEquipmentUpdate(this);
            return true;
        }

        public override bool AddToWorld()
        {
            ObjectState = eObjectState.Inactive;
            m_spawnTick = GameLoop.GameLoopTime + 60*1000;
            
            m_displayedPlayer.Out.SendNPCCreate(this);
            m_displayedPlayer.Out.SendLivingEquipmentUpdate(this);
            
            return true;
        }
        
        
    }
}