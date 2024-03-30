using System.Collections.Generic;
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
            DbItemTemplate vaultItem = GetDummyVaultItem(player);
            AccountVault vault = new(player, 0, vaultItem);
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            if (source is not GamePlayer player)
                return false;

            if (text.Equals("first", System.StringComparison.OrdinalIgnoreCase))
            {
                AccountVault vault = new(player, 0, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            }

            if (text.Equals("second", System.StringComparison.OrdinalIgnoreCase))
            {
                AccountVault vault = new(player, 1, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            }

            return true;
        }

        private static DbItemTemplate GetDummyVaultItem(GamePlayer player)
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _vaultOwner;
        private int _vaultNumber = 0;

        /// <summary>
        /// An account vault that masquerades as a house vault to the game client
        /// </summary>
        /// <param name="player">Player who owns the vault</param>
        /// <param name="vaultOwner">ID of vault owner (can be anything unique, if it's the account name then all toons on account can access the items)</param>
        /// <param name="vaultNumber">Valid vault IDs are 0-3</param>
        /// <param name="dummyTemplate">An ItemTemplate to satisfy the base class's constructor</param>
        public AccountVault(GamePlayer player, int vaultNumber, DbItemTemplate dummyTemplate) : base(dummyTemplate, vaultNumber)
        {
            _vaultOwner = GetOwner(player);
            _vaultNumber = vaultNumber;

            DbHouse dbHouse = new()
            {
                AllowAdd = false,
                GuildHouse = false,
                HouseNumber = player.ObjectID,
                Name = "Account Vault",
                OwnerID = _vaultOwner,
                RegionID = player.CurrentRegionID
            };

            CurrentHouse = new House(dbHouse);
        }

        public override bool Interact(GamePlayer player)
        {
            if (!CanView(player))
            {
                player.Out.SendMessage("You don't have permission to view this vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            player.ActiveInventoryObject?.RemoveObserver(player);

            lock (LockObject)
            {
                _observers.TryAdd(player.Name, player);
            }

            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        /// <summary>
        /// Whether or not this player can view the contents of this vault.
        /// </summary>
        public override bool CanView(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        public override bool CanAddItems(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault.
        /// </summary>
        public override bool CanRemoveItems(GamePlayer player)
        {
            return GetOwner(player) == _vaultOwner;
        }

        public override string GetOwner(GamePlayer player)
        {
            return $"{player.Client.Account.Name}_{player.Realm}";
        }

        /// <summary>
        /// List of items in the vault.
        /// </summary>
        public override IList<DbInventoryItem> DBItems(GamePlayer player = null)
        {
            return GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(GetOwner(player)).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDbSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDbSlot))));
        }

        public override int FirstDbSlot => _vaultNumber switch
        {
            0 => 2500,
            1 => 2600,
            _ => 0,
        };

        public override int LastDbSlot => _vaultNumber switch
        {
            0 => 2599,
            1 => 2699,
            _ => 0,
        };
    }
}
