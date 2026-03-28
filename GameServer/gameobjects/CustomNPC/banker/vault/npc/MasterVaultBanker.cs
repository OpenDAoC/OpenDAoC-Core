using System;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class MasterVaultBanker : VaultBanker
    {
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            GuildName = "Master Vault Banker";
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            string msg =
                $"Why hello {player.Name}. I am the master vault banker. " +
                $"I can help you recover possessions from repossessed houses. " +
                $"Which vault would you like to access?\n";

            foreach (string vaultName in Enum.GetNames<VaultType>())
            {
                msg += "\n";

                for (int i = 1; i <= House.MAX_VAULT_COUNT; i++)
                    msg += $"[{vaultName} Vault {i}]\n";
            }

            player.Out.SendMessage(msg, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            if (source is not GamePlayer player)
                return false;

            if (text.StartsWith("personal vault ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(text.AsSpan(15), out int vaultIndex) && vaultIndex >= 1 && vaultIndex <= House.MAX_VAULT_COUNT)
                    OpenVault(player, VaultType.Personal, vaultIndex - 1);

                return true;
            }

            if (text.StartsWith("guild vault ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(text.AsSpan(12), out int vaultIndex) && vaultIndex >= 1 && vaultIndex <= House.MAX_VAULT_COUNT)
                    OpenVault(player, VaultType.Guild, vaultIndex - 1);

                return true;
            }

            return false;
        }

        private static void OpenVault(GamePlayer player, VaultType type, int index)
        {
            if (!TryGetHouseVault(player, type, index, out GameHouseVault houseVault))
            {
                string msg =
                    $"I cannot access this vault at this time. " +
                    $"Either you lack permission, or you have an active house and should use your real vaults.";
                player.Out.SendMessage(msg, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return;
            }

            houseVault.Interact(player);
        }
    }
}
