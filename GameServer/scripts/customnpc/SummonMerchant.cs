using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class SummonedMerchant : GameAtlasMerchant
    {
        
        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:Model = 10;break;
                case eRealm.Hibernia: Model = 307;break;
                case eRealm.Midgard:Model = 158;break;
                case eRealm.None: Model = 10;break;
            }
            GuildName = "Temp Worker";
            Realm = eRealm.None;
            return base.AddToWorld();
        }
        
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (player.InCombat)
            {
                player.Out.SendMessage("Merchant says \"stop your combat if you want me speak with me!\"", eChatType.CT_Say,
                    eChatLoc.CL_ChatWindow);
                return false;
            }

            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE)
            {
                player.Out.SendMessage("You are too far away " + GetName(0, false) + ".", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return false;
            }

            TurnTo(player, 3000);

            return true;
        }
        
    }
}
