using System;
using System.Collections.Generic;
using DOL.Database;
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
            player.Out.SendInventoryItemsUpdate(player.ActiveInventoryObject.GetClientInventory(player), eInventoryWindowType.HouseVault);
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
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            }

            if (text.Equals("second", StringComparison.OrdinalIgnoreCase))
            {
                AccountVault vault = new(player, 1, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
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
        private int _vaultIndex;

        public AccountVault(GamePlayer player, int vaultIndex, DbItemTemplate dummyTemplate) : base(dummyTemplate, vaultIndex)
        {
            if (vaultIndex is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(vaultIndex), $"{nameof(vaultIndex)} must be either 0 or 1.");

            _vaultOwner = GetOwner(player);
            _vaultIndex = vaultIndex;

            DbHouse dbHouse = new()
            {
                AllowAdd = false,
                GuildHouse = false,
                HouseNumber = player.ObjectID,
                Name = "Account Vault",
                OwnerID = _vaultOwner,
                RegionID = player.CurrentRegionID
            };

            CurrentHouse = new(dbHouse);
        }

        public override bool CanView(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        public override bool CanRemoveItems(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        public override string GetOwner(GamePlayer player)
        {
            return $"{player.Client.Account.Name}_{player.Realm}";
        }

        public override IEnumerable<DbInventoryItem> GetDbItems(GamePlayer player)
        {
            return GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(GetOwner(player)).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDbSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDbSlot))));
        }

        public override int FirstDbSlot => (int) eInventorySlot.AccountVault_First + VaultSize * _vaultIndex;

        public override int LastDbSlot => FirstDbSlot + (VaultSize -1);
    }
}
