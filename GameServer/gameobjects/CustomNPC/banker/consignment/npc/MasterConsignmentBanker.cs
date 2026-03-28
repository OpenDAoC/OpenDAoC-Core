using System;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class MasterConsignmentBanker : ConsignmentBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = "Master Consignment Banker";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            string msg =
                $"Why hello {player.Name}. I am the master consignment banker. " +
                $"I can help you recover possessions from repossessed houses. " +
                $"Which consignment merchant would you like to access?\n\n" +
                $"[Personal Consignment]\n" +
                $"[Guild Consignment]\n";

            player.Out.SendMessage(msg, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            if (source is not GamePlayer player)
                return false;

            if (text.Equals("personal consignment", StringComparison.OrdinalIgnoreCase))
            {
                OpenConsignment(player, VaultType.Personal);
                return true;
            }

            if (text.Equals("guild consignment", StringComparison.OrdinalIgnoreCase))
            {
                OpenConsignment(player, VaultType.Guild);
                return true;
            }

            return false;
        }

        private void OpenConsignment(GamePlayer player, VaultType type)
        {
            if (!TryGetConsignmentMerchant(player, type, out GameConsignmentMerchant consignmentMerchant))
            {
                string msg =
                    $"I cannot access this consignment merchant at this time. " +
                    $"Either you lack permission, or you have an active house and should use your real consignment merchant.";
                player.Out.SendMessage(msg, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return;
            }

            consignmentMerchant.Interact(player);
        }
    }
}
