/*
 * Account Vault Keeper - Kakuri Mar 20 2009
 * A fake GameHouseVault that works as an account vault.
 * The methods and properties of GameHouseVault *must* be marked as virtual for this to work (which was not the case in DOL builds prior to 1584).
 *
 */

using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class AccountVaultKeeper : GameNPC
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            
            if (player.HCFlag)
            {
                SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
                return false;
            }

            // if (player.Level <= 1)
            // {
            //     SayTo(player,$"I'm sorry {player.Name}, come back if you are venerable to use my services.");
            //     return false;
            // }

            string message = $"Greetings {player.Name}, nice meeting you.\n";
            
            message += "I am happy to offer you my services.\n\n";

            message += "You can browse the [first] or [second] page of your Account Vault.";
            player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            DbItemTemplate vaultItem = GetDummyVaultItem(player);
            AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 0, vaultItem);
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

            if (text == "first")
            {
                AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 0, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            }
            else if (text == "second")
            {
                AccountVault vault = new AccountVault(player, this, player.Client.Account.Name + "_" + player.Realm.ToString(), 1, GetDummyVaultItem(player));
                player.ActiveInventoryObject = vault;
                player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            }

            return true;
        }

        private static DbItemTemplate GetDummyVaultItem(GamePlayer player)
        {
            DbItemTemplate vaultItem = new DbItemTemplate();
            vaultItem.Object_Type = (int)EObjectType.HouseVault;
            vaultItem.Name = "Vault";
            vaultItem.ObjectId = player.Client.Account.Name + "_" + player.Realm.ToString();
            switch (player.Realm)
            {
                case ERealm.Albion:
                    vaultItem.Id_nb = "housing_alb_vault";
                    vaultItem.Model = 1489;
                    break;
                case ERealm.Hibernia:
                    vaultItem.Id_nb = "housing_hib_vault";
                    vaultItem.Model = 1491;
                    break;
                case ERealm.Midgard:
                    vaultItem.Id_nb = "housing_mid_vault";
                    vaultItem.Model = 1493;
                    break;
            }

            return vaultItem;
        }
    }

    public class AccountVault : GameHouseVault
    {
        public const int SIZE = 100;
        public const int Last_Used_FIRST_SLOT = 1600;
        public const int FIRST_SLOT = 2500;
        private GamePlayer m_player;
        private GameNPC m_vaultNPC;
        private string m_vaultOwner;
        private int m_vaultNumber = 0;

        private readonly object _vaultLock = new object();

        /// <summary>
        /// An account vault that masquerades as a house vault to the game client
        /// </summary>
        /// <param name="player">Player who owns the vault</param>
        /// <param name="vaultNPC">NPC controlling the interaction between player and vault</param>
        /// <param name="vaultOwner">ID of vault owner (can be anything unique, if it's the account name then all toons on account can access the items)</param>
        /// <param name="vaultNumber">Valid vault IDs are 0-3</param>
        /// <param name="dummyTemplate">An ItemTemplate to satisfy the base class's constructor</param>
        public AccountVault(GamePlayer player, GameNPC vaultNPC, string vaultOwner, int vaultNumber, DbItemTemplate dummyTemplate)
            : base(dummyTemplate, vaultNumber)
        {
            m_player = player;
            m_vaultNPC = vaultNPC;
            m_vaultOwner = vaultOwner;
            m_vaultNumber = vaultNumber;

            DbHouse dbh = new DbHouse();
            dbh.AllowAdd = false;
            dbh.GuildHouse = false;
            dbh.HouseNumber = player.ObjectID;
            dbh.Name = "Account Vault";
            //dbh.Name = "Maison de " + player.Name;
            dbh.OwnerID = player.Client.Account.Name + "_" + player.Realm.ToString();
            dbh.RegionID = player.CurrentRegionID;
            CurrentHouse = new House(dbh);
        }

        public override bool Interact(GamePlayer player)
        {
            if (!CanView(player))
            {
                player.Out.SendMessage("You don't have permission to view this vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.ActiveInventoryObject != null)
            {
                player.ActiveInventoryObject.RemoveObserver(player);
            }

            lock (_vaultLock)
            {
                if (!_observers.ContainsKey(player.Name))
                {
                    _observers.Add(player.Name, player);
                }
            }

            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        public override Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player)
        {

            var items = new Dictionary<int, DbInventoryItem>();
            int slotOffset = -FirstDBSlot + (int)(eInventorySlot.HousingInventory_First);
            foreach (DbInventoryItem item in DBItems(player))
            {
                if (item != null)
                {
                    if (!items.ContainsKey(item.SlotPosition + slotOffset))
                    {
                        items.Add(item.SlotPosition + slotOffset, item);
                    }
                    else
                    {
                       //log.ErrorFormat("GAMEACCOUNTVAULT: Duplicate item {0}, owner {1}, position {2}", item.Name, item.OwnerID, (item.SlotPosition + slotOffset));
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Is this a move request for a housing vault?
        /// </summary>
        /// <param name="player"></param>
        /// <param name="fromSlot"></param>
        /// <param name="toSlot"></param>
        /// <returns></returns>
        public override bool CanHandleMove(GamePlayer player, ushort fromSlot, ushort toSlot)
        {
            if (player == null || player.ActiveInventoryObject != this)
                return false;

            bool canHandle = false;

            // House Vaults and GameConsignmentMerchant Merchants deliver the same slot numbers
            if (fromSlot >= (ushort)eInventorySlot.HousingInventory_First &&
                fromSlot <= (ushort)eInventorySlot.HousingInventory_Last)
            {
                canHandle = true;
            }
            else if (toSlot >= (ushort)eInventorySlot.HousingInventory_First &&
                toSlot <= (ushort)eInventorySlot.HousingInventory_Last)
            {
                canHandle = true;
            }

            return canHandle;
        }

        /// <summary>
        /// Move an item from, to or inside a house vault.  From IGameInventoryObject
        /// </summary>
        public override bool MoveItem(GamePlayer player, ushort fromSlot, ushort toSlot)
        {
            if (GetOwner(player) != m_vaultOwner)
                return false;
            if (fromSlot == toSlot)
            {
                return false;
            }

            bool fromAccountVault = (fromSlot >= (ushort)eInventorySlot.HousingInventory_First && fromSlot <= (ushort)eInventorySlot.HousingInventory_Last);
            bool toAccountVault = (toSlot >= (ushort)eInventorySlot.HousingInventory_First && toSlot <= (ushort)eInventorySlot.HousingInventory_Last);

            if (fromAccountVault == false && toAccountVault == false)
            {
                return false;
            }

            //Prevent exploit shift+clicking quiver exploit
            if (fromAccountVault)
            {
                if (fromSlot < (ushort)eInventorySlot.HousingInventory_First || fromSlot > (ushort) eInventorySlot.HousingInventory_Last) return false;
            }

            GameVault gameVault = player.ActiveInventoryObject as GameVault;
            if (gameVault == null)
            {
                player.Out.SendMessage("You are not actively viewing a vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            if (toAccountVault)
            {
                DbInventoryItem item = player.Inventory.GetItem((eInventorySlot)toSlot);
                if (item != null)
                {
                    if (gameVault.CanRemoveItems(player) == false)
                    {
                        player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                }
                if (gameVault.CanAddItems(player) == false)
                {
                    player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (fromAccountVault && gameVault.CanRemoveItems(player) == false)
            {
                player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            DbInventoryItem itemInFromSlot = player.Inventory.GetItem((eInventorySlot)fromSlot);
            DbInventoryItem itemInToSlot = player.Inventory.GetItem((eInventorySlot)toSlot);

            // Check for a swap to get around not allowing non-tradables in a housing vault - Tolakram
            if (fromAccountVault && itemInToSlot != null && itemInToSlot.IsTradable == false)
            {
                player.Out.SendMessage("You cannot swap with an untradable item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //log.DebugFormat("GameVault: {0} attempted to swap untradable item {2} with {1}", player.Name, itemInFromSlot.Name, itemInToSlot.Name);
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            // Allow people to get untradables out of their house vaults (old bug) but 
            // block placing untradables into housing vaults from any source - Tolakram
            if (toAccountVault && itemInFromSlot != null && itemInFromSlot.IsTradable == false)
            {
                if (itemInFromSlot.Id_nb != ServerProperties.Properties.ALT_CURRENCY_ID)
                {
                    player.Out.SendMessage("You can not put this item into an Account Vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendInventoryItemsUpdate(null);
                    return false;
                }
            }

            // let's move it

            lock (m_vaultSync)
            {
                if (fromAccountVault)
                {
                    if (toAccountVault)
                    {
                        NotifyObservers(player, this.MoveItemInsideObject(player, (eInventorySlot)fromSlot, (eInventorySlot)toSlot));
                    }
                    else
                    {
                        NotifyObservers(player, this.MoveItemFromObject(player, (eInventorySlot)fromSlot, (eInventorySlot)toSlot));
                    }
                }
                else if (toAccountVault)
                {
                    NotifyObservers(player, this.MoveItemToObject(player, (eInventorySlot)fromSlot, (eInventorySlot)toSlot));
                }
            }

            return true;
        }
        /// <summary>
        /// Whether or not this player can view the contents of this vault.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanView(GamePlayer player)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanAddItems(GamePlayer player)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanRemoveItems(GamePlayer player)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }


        public override string GetOwner(GamePlayer player)
        {
            return player.Client.Account.Name + "_" + player.Realm.ToString();
        }

        /// <summary>
        /// List of items in the vault.
        /// </summary>
        private new IList<DbInventoryItem> DBItems(GamePlayer player = null)
        {
            return GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(GetOwner(player)).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDBSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDBSlot))));
        }

        public override int FirstDBSlot
        {
            get 
            {
                switch (m_vaultNumber)
                {
                    case 0:
                        return (int)2500;
                    case 1:
                        return (int)2600;
                    default: return 0;
                }
            }
        }

        public override int LastDBSlot
        {
            get 
            { 
                            
                switch (m_vaultNumber)
                {
                    case 0:
                        return (int)2599;
                    case 1:
                        return (int)2699;
                    default: return 0;
                }
            }
        }
    }
}