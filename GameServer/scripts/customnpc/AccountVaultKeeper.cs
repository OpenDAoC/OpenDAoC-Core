using System;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class AccountVaultKeeper : GameNPC
    {
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.HCFlag)
            {
                SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
                return false;
            }

            string message = $"Greetings {player.Name}, nice meeting you.\n";
            message += "I am happy to offer you my services.\n\n";
            message += "You can browse the [first] or [second] page of your Account Vault.";
            player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            player.ActiveInventoryObject = player.AccountVault;
            player.Out.SendInventoryItemsUpdate(player.ActiveInventoryObject.GetClientInventory(), eInventoryWindowType.HouseVault);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            if (source is not GamePlayer player)
                return false;

            if (text.Equals("first", StringComparison.OrdinalIgnoreCase))
            {
                AccountVault vault = new(player, 0, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(), eInventoryWindowType.HouseVault);
            }

            if (text.Equals("second", StringComparison.OrdinalIgnoreCase))
            {
                AccountVault vault = new(player, 1, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(), eInventoryWindowType.HouseVault);
            }

            return true;
        }

        public static DbItemTemplate GetDummyVaultItem(GamePlayer player)
        {
            DbItemTemplate vaultItem = new()
            {
                Object_Type = (int) eObjectType.HouseVault,
                Name = "Vault",
                ObjectId = $"{player.Client.Account.Name}_{player.Realm}"
            };

            switch (player.Realm)
            {
                case eRealm.Albion:
                    vaultItem.Id_nb = "housing_alb_vault";
                    vaultItem.Model = 1489;
                    break;
                case eRealm.Hibernia:
                    vaultItem.Id_nb = "housing_hib_vault";
                    vaultItem.Model = 1491;
                    break;
                case eRealm.Midgard:
                    vaultItem.Id_nb = "housing_mid_vault";
                    vaultItem.Model = 1493;
                    break;
            }

            return vaultItem;
        }
    }

    public class AccountVault : GameHouseVault
    {
        private string _vaultOwner;

        public AccountVault(GamePlayer player, int vaultIndex, DbItemTemplate dummyTemplate) : base(dummyTemplate, vaultIndex)
        {
            if (vaultIndex is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(vaultIndex), $"{nameof(vaultIndex)} must be either 0 or 1.");

            _vaultOwner = BuildOwnerId(player);
            CurrentHouse = new NullHouse(_vaultOwner, false);
        }

        public override bool CanView(GamePlayer player)
        {
            return BuildOwnerId(player) == _vaultOwner;
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return BuildOwnerId(player) == _vaultOwner;
        }

        public override bool CanRemoveItems(GamePlayer player)
        {
            return BuildOwnerId(player) == _vaultOwner;
        }

        public override string GetOwner()
        {
            return _vaultOwner;
        }

        public override int FirstDbSlot => (int) eInventorySlot.AccountVault_First + VaultSize * Index;

        public override int LastDbSlot => FirstDbSlot + (VaultSize - 1);

        private static string BuildOwnerId(GamePlayer player)
        {
            return $"{player.Client.Account.Name}_{player.Realm}";
        }
    }
}
