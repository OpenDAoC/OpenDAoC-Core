using System.Collections.Generic;
using Core.Database;
using Core.GS;
using Core.GS.Housing;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

public class AccountVault : GameHouseVault
{
    public const int SIZE = 100;
    public const int Last_Used_FIRST_SLOT = 1600;
    public const int FIRST_SLOT = 2500;
    private GamePlayer m_player;
    private GameNpc m_vaultNPC;
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
    public AccountVault(GamePlayer player, GameNpc vaultNPC, string vaultOwner, int vaultNumber, DbItemTemplate dummyTemplate)
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
            player.Out.SendMessage("You don't have permission to view this vault!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
        player.Out.SendInventoryItemsUpdate(GetClientInventory(player), EInventoryWindowType.HouseVault);
        return true;
    }

    public override Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player)
    {

        var items = new Dictionary<int, DbInventoryItem>();
        int slotOffset = -FirstDBSlot + (int)(EInventorySlot.HousingInventory_First);
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
        if (fromSlot >= (ushort)EInventorySlot.HousingInventory_First &&
            fromSlot <= (ushort)EInventorySlot.HousingInventory_Last)
        {
            canHandle = true;
        }
        else if (toSlot >= (ushort)EInventorySlot.HousingInventory_First &&
            toSlot <= (ushort)EInventorySlot.HousingInventory_Last)
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

        bool fromAccountVault = (fromSlot >= (ushort)EInventorySlot.HousingInventory_First && fromSlot <= (ushort)EInventorySlot.HousingInventory_Last);
        bool toAccountVault = (toSlot >= (ushort)EInventorySlot.HousingInventory_First && toSlot <= (ushort)EInventorySlot.HousingInventory_Last);

        if (fromAccountVault == false && toAccountVault == false)
        {
            return false;
        }

        //Prevent exploit shift+clicking quiver exploit
        if (fromAccountVault)
        {
            if (fromSlot < (ushort)EInventorySlot.HousingInventory_First || fromSlot > (ushort) EInventorySlot.HousingInventory_Last) return false;
        }

        GameVault gameVault = player.ActiveInventoryObject as GameVault;
        if (gameVault == null)
        {
            player.Out.SendMessage("You are not actively viewing a vault!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            player.Out.SendInventoryItemsUpdate(null);
            return false;
        }

        if (toAccountVault)
        {
            DbInventoryItem item = player.Inventory.GetItem((EInventorySlot)toSlot);
            if (item != null)
            {
                if (gameVault.CanRemoveItems(player) == false)
                {
                    player.Out.SendMessage("You don't have permission to remove items!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return false;
                }
            }
            if (gameVault.CanAddItems(player) == false)
            {
                player.Out.SendMessage("You don't have permission to add items!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return false;
            }
        }

        if (fromAccountVault && gameVault.CanRemoveItems(player) == false)
        {
            player.Out.SendMessage("You don't have permission to remove items!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return false;
        }

        DbInventoryItem itemInFromSlot = player.Inventory.GetItem((EInventorySlot)fromSlot);
        DbInventoryItem itemInToSlot = player.Inventory.GetItem((EInventorySlot)toSlot);

        // Check for a swap to get around not allowing non-tradables in a housing vault - Tolakram
        if (fromAccountVault && itemInToSlot != null && itemInToSlot.IsTradable == false)
        {
            player.Out.SendMessage("You cannot swap with an untradable item!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            //log.DebugFormat("GameVault: {0} attempted to swap untradable item {2} with {1}", player.Name, itemInFromSlot.Name, itemInToSlot.Name);
            player.Out.SendInventoryItemsUpdate(null);
            return false;
        }

        // Allow people to get untradables out of their house vaults (old bug) but 
        // block placing untradables into housing vaults from any source - Tolakram
        if (toAccountVault && itemInFromSlot != null && itemInFromSlot.IsTradable == false)
        {
            if (itemInFromSlot.Id_nb != Properties.ALT_CURRENCY_ID)
            {
                player.Out.SendMessage("You can not put this item into an Account Vault!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
                    NotifyObservers(player, this.MoveItemInsideObject(player, (EInventorySlot)fromSlot, (EInventorySlot)toSlot));
                }
                else
                {
                    NotifyObservers(player, this.MoveItemFromObject(player, (EInventorySlot)fromSlot, (EInventorySlot)toSlot));
                }
            }
            else if (toAccountVault)
            {
                NotifyObservers(player, this.MoveItemToObject(player, (EInventorySlot)fromSlot, (EInventorySlot)toSlot));
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