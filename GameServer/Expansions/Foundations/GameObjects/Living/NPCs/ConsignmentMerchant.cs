﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.Server;
using log4net;

namespace Core.GS.Expansions.Foundations;

public class GameConsignmentMerchant : GameNpc, IGameInventoryObject
{
	private static new readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public const int CONSIGNMENT_SIZE = 100;
	public const int CONSIGNMENT_OFFSET = 1350; // client sends the same slots as a housing vault

	public const string CONSIGNMENT_BUY_ITEM = "ConsignmentBuyItem";

    protected readonly Dictionary<string, GamePlayer> _observers = new Dictionary<string, GamePlayer>();
    protected readonly object m_moneyLock = new object();
    protected readonly object m_vaultSync = new object();
    protected long m_totalMoney;

	public object LockObject()
	{
		return m_vaultSync;
	}

	/// <summary>
	/// First slot of the client window that shows this inventory
	/// </summary>
	public virtual int FirstClientSlot
	{
		get { return (int)EInventorySlot.HousingInventory_First; }
	}

	/// <summary>
	/// Last slot of the client window that shows this inventory
	/// </summary>
	public virtual int LastClientSlot
	{
		get { return (int)EInventorySlot.HousingInventory_Last; }
	}

    /// <summary>
    /// First slot in the DB.
    /// </summary>
	public virtual int FirstDBSlot
    {
        get { return (int)EInventorySlot.Consignment_First; }
    }

    /// <summary>
    /// Last slot in the DB.
    /// </summary>
	public virtual int LastDBSlot
    {
        get { return (int)EInventorySlot.Consignment_Last; }
    }

    #region Token return

    private static readonly Dictionary<string, GameLocation> _itemXdestination =
        new Dictionary<string, GameLocation>
            {
                // Item Id_nb, new Tuple<>(Region, X, Y, Z, Heading)
                // ALBION
                {"caerwent_entrance", new GameLocation("", 2, 584832, 561279, 3576, 2144)},
                {"caerwent_market", new GameLocation("", 2, 557035, 560048, 3624, 1641)},
                {"rilan_market", new GameLocation("", 2, 559906, 491141, 3392, 1829)},
                {"brisworthy_market", new GameLocation("", 2, 489474, 489323, 3600, 3633)},
                {"stoneleigh_market", new GameLocation("", 2, 428964, 490962, 3624, 1806)},
                {"chiltern_market", new GameLocation("", 2, 428128, 557606, 3624, 3888)},
                {"sherborne_market", new GameLocation("", 2, 428840, 622221, 3248, 1813)},
                {"aylesbury_market", new GameLocation("", 2, 492794, 621373, 3624, 1643)},
                {"old_sarum_market", new GameLocation("", 2, 560030, 622022, 3624, 1819)},
                {"dalton_market", new GameLocation("", 2, 489334, 559242, 3720, 1821)},

                // MIDGARD
                {"erikstaad_entrance", new GameLocation("", 102, 526881, 561661, 3633, 80)},
                {"erikstaad_market", new GameLocation("", 102, 554099, 565239, 3624, 504)},
                {"arothi_market", new GameLocation("", 102, 558093, 485250, 3488, 1231)},
                {"kaupang_market", new GameLocation("", 102, 625574, 483303, 3592, 2547)},
                {"stavgaard_market", new GameLocation("", 102, 686901, 490396, 3744, 332)},
                {"carlingford_market", new GameLocation("", 102, 625056, 557887, 3696, 1366)},
                {"holmestrand_market", new GameLocation("", 102, 686903, 556050, 3712, 313)},
                {"nittedal_market", new GameLocation("", 102, 689199, 616329, 3488, 1252)},
                {"frisia_market", new GameLocation("", 102, 622620, 615491, 3704, 804)},
                {"wyndham_market", new GameLocation("", 102, 555839, 621432, 3744, 314)},

                // HIBERNIA
                {"meath_entrance", new GameLocation("", 202, 555246, 526470, 3008, 1055)},
                {"meath_market", new GameLocation("", 202, 564448, 559995, 3008, 1024)},
                {"kilcullen_market", new GameLocation("", 202, 618653, 561227, 3032, 3087)},
                {"aberillan_market", new GameLocation("", 202, 615145, 619457, 3008, 3064)},
                {"torrylin_market", new GameLocation("", 202, 566890, 620027, 3008, 1500)},
                {"tullamore_market", new GameLocation("", 202, 560999, 692301, 3032, 1030)},
                {"broughshane_market", new GameLocation("", 202, 618653, 692296, 3032, 3090)},
                {"moycullen_market", new GameLocation("", 202, 495552, 686733, 2960, 1077)},
                {"saeranthal_market", new GameLocation("", 202, 493148, 620361, 2952, 2471)},
                {"dunshire_market", new GameLocation("", 202, 495494, 555646, 2960, 1057)},
            };

    public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
    {
        GamePlayer player = source as GamePlayer;

        if (player == null)
            return false;

        if (!player.IsWithinRadius(this, 500))
        {
            ((GamePlayer)source).Out.SendMessage("You are to far away to give anything to " + this.Name + ".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return false;
        }

        if (item != null)
        {
            GameLocation destination;
            if (_itemXdestination.TryGetValue(item.Id_nb, out destination))
            {
                player.MoveTo(destination);
                player.Inventory.RemoveItem(item);
                InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template, item.Count);
                player.SaveIntoDatabase();
                return true;
            }
        }
        return base.ReceiveItem(source, item);
    }

    #endregion Token return

	public virtual string GetOwner(GamePlayer player)
	{
		return CurrentHouse.OwnerID;
	}

	public override House CurrentHouse
	{
		get
		{
			return HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
		}
		set
		{
		}
	}

    /// <summary>
    /// Inventory of the Consignment Merchant, mapped to client slots
    /// </summary>
    public virtual Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player)
    {
		return this.GetClientItems(player);
    }

    /// <summary>
    /// List of items in the Consignment Merchants Inventory
    /// </summary>
	public virtual IList<DbInventoryItem> DBItems(GamePlayer player = null)
    {
        House house = HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
		if (house == null)
			return null;

		return MarketCache.Items.Where(item => item?.OwnerID == house?.OwnerID).ToList();
    }

    /// <summary>
    ///  Gets or sets the total amount of money held by this consignment merchant.
    /// </summary>
	public virtual long TotalMoney
    {
        get
        {
            lock (m_moneyLock)
            {
                return m_totalMoney;
            }
        }
        set
        {
            lock (m_moneyLock)
            {
                m_totalMoney = value;

                var merchant = CoreDb<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
                merchant.Money = m_totalMoney;
                GameServer.Database.SaveObject(merchant);
            }
        }
    }

    /// <summary>
    /// Checks if the Player is allowed to move an Item
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
	public virtual bool HasPermissionToMove(GamePlayer player)
    {
        House house = HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
        if (house == null)
            return false;
        
        if (house.HasOwnerPermissions(player) && !player.NoHelp)
            return true;

        return false;
    }

	/// <summary>
	/// Can this inventory object handle the a move item request?
	/// </summary>
	/// <param name="player"></param>
	/// <param name="fromClientSlot"></param>
	/// <param name="toClientSlot"></param>
	/// <returns></returns>
	public virtual bool CanHandleMove(GamePlayer player, ushort fromClientSlot, ushort toClientSlot)
	{
		if (player == null || player.ActiveInventoryObject != this)
			return false;

		return this.CanHandleRequest(player, fromClientSlot, toClientSlot);
	}

    /// <summary>
    /// Is this a move request for a consigment merchant?
    /// </summary>
    /// <param name="playerInventory"></param>
    /// <param name="fromClientSlot"></param>
    /// <param name="toClientSlot"></param>
    /// <returns></returns> 
    public virtual bool MoveItem(GamePlayer player, ushort fromClientSlot, ushort toClientSlot)
    {
        if (fromClientSlot == toClientSlot)
            return false;

        House house = HouseMgr.GetHouse(HouseNumber);
        if (house == null)
            return false;

        if (!CanHandleMove(player, fromClientSlot, toClientSlot))
			return false;

		// let's move it

        lock (m_vaultSync)
        {
            if (fromClientSlot == toClientSlot)
            {
                NotifyObservers(player, null);
            }
            else if (fromClientSlot >= FirstClientSlot && fromClientSlot <= LastClientSlot)
            {
				// Moving from the consignment merchant to ...

				if (toClientSlot >= FirstClientSlot && toClientSlot <= LastClientSlot)
				{
					// ... consignment merchant
					if (HasPermissionToMove(player))
					{
						NotifyObservers(player, this.MoveItemInsideObject(player, (EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot));
					}
					else
					{
						return false;
					}
				}
				else
				{
					// ... player

					DbInventoryItem toItem = player.Inventory.GetItem((EInventorySlot)toClientSlot);

					if (toItem != null)
					{
						player.Client.Out.SendMessage("You can only move an item to an empty slot!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return false;
					}

					if (HasPermissionToMove(player) == false)
					{
						// Move must be an attempt to buy
						OnPlayerBuy(player, (EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot);
					}
					else if (player.TargetObject == this)
					{
						// Allow a move only if the player with permission is standing in front of the CM.
						// This prevents moves if player has owner permission but is viewing from the Market Explorer
						NotifyObservers(player, this.MoveItemFromObject(player, (EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot));
					}
					else
					{
						player.Client.Out.SendMessage("You can't buy items from yourself!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return false;
					}
				}
            }
			else if (toClientSlot >= FirstClientSlot && toClientSlot <= LastClientSlot)
            {
				// moving an item from the client to the consignment merchant
				if (HasPermissionToMove(player))
				{
					DbInventoryItem toItem = player.Inventory.GetItem((EInventorySlot)toClientSlot);

					if (toItem != null)
					{
						// in most clients this is actually handled ON the client, but just in case...
						player.Client.Out.SendMessage("You can only move an item to an empty slot!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return false;
					}

					NotifyObservers(player, this.MoveItemToObject(player, (EInventorySlot)fromClientSlot, (EInventorySlot)toClientSlot));
				}
				else
				{
					return false;
				}
            }
        }

		return true;
    }

	/// <summary>
	/// Add an item to this object
	/// </summary>
	public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item)
	{
		if (ServerProperty.MARKET_ENABLE_LOG)
		{
			log.DebugFormat("CM: {0}:{1} adding '{2}' to consignment merchant on lot {3}.", player.Name, player.Client.Account.Name, item.Name, HouseNumber);
		}
		return MarketCache.AddItem(item);
	}

	/// <summary>
	/// Remove an item from this object
	/// </summary>
	public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item)
	{
		if (ServerProperty.MARKET_ENABLE_LOG)
		{
			log.DebugFormat("CM: {0}:{1} removing '{2}' from consignment merchant on lot {3}.", player.Name, player.Client.Account.Name, item.Name, HouseNumber);
		}
		item.OwnerLot = 0;
		item.SellPrice = 0;
		return MarketCache.RemoveItem(item);
	}

	/// <summary>
	/// What to do after an item is added.  For consignment merchants this is called after price is set
	/// </summary>
	/// <param name="player"></param>
	/// <param name="clientSlot"></param>
	/// <param name="price"></param>
	/// <returns></returns>
	public virtual bool SetSellPrice(GamePlayer player, ushort clientSlot, uint price)
	{
		GameConsignmentMerchant conMerchant = player.ActiveInventoryObject as GameConsignmentMerchant;
		if (conMerchant == null)
		{
			return false;
		}
		House house = HouseMgr.GetHouse(conMerchant.HouseNumber);
		if (house == null)
		{
			return false;
		}

		if (house.HasOwnerPermissions(player) == false)
		{
			return false;
		}

		DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(GameInventoryObjectExtensions.ITEM_BEING_ADDED, null);

		if (item != null)
		{
			if (item.IsTradable)
			{
				item.SellPrice = (int)price;
			}
			else
			{
				// Unique DOL feature
				item.SellPrice = 0;
				player.Out.SendCustomDialog("This item is not tradable. You can store it here but cannot sell it.", null);
			}

			item.OwnerLot = conMerchant.HouseNumber;
			item.OwnerID = conMerchant.GetOwner(player);
			GameServer.Database.SaveObject(item);
			ChatUtil.SendDebugMessage(player, item.Name + " SellPrice=" + price + ", OwnerLot=" + item.OwnerLot + ", OwnerID=" + item.OwnerID);
			player.Out.SendMessage("Price set!", EChatType.CT_System, EChatLoc.CL_SystemWindow);

			if (ServerProperty.MARKET_ENABLE_LOG)
			{
				log.DebugFormat("CM: {0}:{1} set sell price of '{2}' to {3} for consignment merchant on lot {4}.", player.Name, player.Client.Account.Name, item.Name, item.SellPrice, HouseNumber);
			}

			NotifyObservers(player, null);
		}


		return true;
	}

	/// <summary>
	/// Do we handle a search?
	/// </summary>
	public virtual bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData)
	{
		return false; // not applicable
	}

    /// <summary>
    /// The Player is buying an Item from the merchant
    /// </summary>
    /// <param name="player"></param>
    /// <param name="playerInventory"></param>
    /// <param name="fromClientSlot"></param>
    /// <param name="toClientSlot"></param>
	public virtual void OnPlayerBuy(GamePlayer player, EInventorySlot fromClientSlot, EInventorySlot toClientSlot, bool usingMarketExplorer = false)
    {
		IDictionary<int, DbInventoryItem> clientInventory = GetClientInventory(player);

		DbInventoryItem fromItem = null;

		if (clientInventory.ContainsKey((int)fromClientSlot))
		{
			fromItem = clientInventory[(int)fromClientSlot];
		}

		if (fromItem == null)
		{
			ChatUtil.SendErrorMessage(player, "I can't find the item you want to purchase!");
			log.ErrorFormat("CM: {0}:{1} can't find item to buy in slot {2} on consignment merchant on lot {3}.", player.Name, player.Client.Account, (int)fromClientSlot, HouseNumber);
			return;
		}

		string buyText = "Do you want to buy this Item?";

		// If the player has a marketExplorer activated they will be charged a commission
		if (player.TargetObject is MarketExplorer)
		{
			player.TempProperties.SetProperty(CONSIGNMENT_BUY_ITEM, fromClientSlot);
			if (ServerProperty.MARKET_FEE_PERCENT > 0)
			{
				player.Out.SendCustomDialog("Buying directly from the Market Explorer costs an additional " +  ServerProperty.MARKET_FEE_PERCENT + "% fee. Do you want to buy this Item?", new CustomDialogResponse(BuyMarketResponse));
			}
			else
			{
				player.Out.SendCustomDialog(buyText, new CustomDialogResponse(BuyResponse));
			}
		}
		else if (player.TargetObject == this)
		{
			player.TempProperties.SetProperty(CONSIGNMENT_BUY_ITEM, fromClientSlot);
			player.Out.SendCustomDialog(buyText, new CustomDialogResponse(BuyResponse));
		}
		else
		{
			ChatUtil.SendErrorMessage(player, "I'm sorry, you need to be talking to a market explorer or consignment merchant in order to make a purchase.");
			log.ErrorFormat("CM: {0}:{1} did not have a CM or ME targeted when attempting to purchase {2} on consignment merchant on lot {3}.", player.Name, player.Client.Account, fromItem.Name, HouseNumber);
		}
    }

	/// <summary>
	/// Response when buying directly from consignment
	/// </summary>
	/// <param name="player"></param>
	/// <param name="response"></param>
	protected virtual void BuyResponse(GamePlayer player, byte response)
	{
		if (response != 0x01)
		{
			player.TempProperties.RemoveProperty(CONSIGNMENT_BUY_ITEM);
			return;
		}

		BuyItem(player);
	}

	/// <summary>
	/// Response when buying from the MarketExplorer
	/// </summary>
	/// <param name="player"></param>
	/// <param name="response"></param>
	protected virtual void BuyMarketResponse(GamePlayer player, byte response)
	{
		if (response != 0x01)
		{
			player.TempProperties.RemoveProperty(CONSIGNMENT_BUY_ITEM);
			return;
		}

		BuyItem(player, true);
	}

	protected virtual void BuyItem(GamePlayer player, bool usingMarketExplorer = false)
	{
		EInventorySlot fromClientSlot = player.TempProperties.GetProperty<EInventorySlot>(CONSIGNMENT_BUY_ITEM, EInventorySlot.Invalid);
		player.TempProperties.RemoveProperty(CONSIGNMENT_BUY_ITEM);

		DbInventoryItem item = null;

		lock (LockObject())
		{

			if (fromClientSlot != EInventorySlot.Invalid)
			{
				IDictionary<int, DbInventoryItem> clientInventory = GetClientInventory(player);

				if (clientInventory.ContainsKey((int)fromClientSlot))
				{
					item = clientInventory[(int)fromClientSlot];
				}
			}

			if (item == null)
			{
				ChatUtil.SendErrorMessage(player, "I can't find the item you want to purchase!");
				log.ErrorFormat("{0}:{1} tried to purchase an item from slot {2} for consignment merchant on lot {3} and the item does not exist.", player.Name, player.Client.Account, (int)fromClientSlot, HouseNumber);

				return;
			}

			int sellPrice = item.SellPrice;
			int purchasePrice = sellPrice;

			if (usingMarketExplorer && ServerProperty.MARKET_FEE_PERCENT > 0)
			{
				purchasePrice += ((purchasePrice * ServerProperty.MARKET_FEE_PERCENT) / 100);
			}

			lock (player.Inventory)
			{
				if (purchasePrice <= 0)
				{
					ChatUtil.SendErrorMessage(player, "This item can't be purchased!");
					log.ErrorFormat("{0}:{1} tried to purchase {2} for consignment merchant on lot {3} and purchasePrice was {4}.", player.Name, player.Client.Account, item.Name, HouseNumber, purchasePrice);
					return;
				}

				if (ServerProperty.CONSIGNMENT_USE_BP)
				{
					if (player.BountyPoints < purchasePrice)
					{
						ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.YouNeedBP", purchasePrice);
						return;
					}
				}
				else
				{
					if (player.GetCurrentMoney() < purchasePrice)
					{
						ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.YouNeed", MoneyMgr.GetString(purchasePrice));
						return;
					}
				}

				EInventorySlot toClientSlot = player.Inventory.FindFirstEmptySlot(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);

				if (toClientSlot == EInventorySlot.Invalid)
				{
					ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.NotInventorySpace", null);
					return;
				}

				if (ServerProperty.CONSIGNMENT_USE_BP)
				{
					ChatUtil.SendMerchantMessage(player, "GameMerchant.OnPlayerBuy.BoughtBP", item.GetName(1, false), purchasePrice);
					player.BountyPoints -= purchasePrice;
					player.Out.SendUpdatePoints();
				}
				else
				{
					if (player.RemoveMoney(purchasePrice))
					{
						InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, purchasePrice);
						ChatUtil.SendMerchantMessage(player, "GameMerchant.OnPlayerBuy.Bought", item.GetName(1, false), MoneyMgr.GetString(purchasePrice));
					}
					else
					{
						return;
					}
				}

				TotalMoney += sellPrice;

				if (ServerProperty.MARKET_ENABLE_LOG)
				{
					log.DebugFormat("CM: {0}:{1} purchased '{2}' for {3} from consignment merchant on lot {4}.", player.Name, player.Client.Account.Name, item.Name, purchasePrice, HouseNumber);
				}

				NotifyObservers(player, this.MoveItemFromObject(player, fromClientSlot, toClientSlot));
			}
		}
	}

	/// <summary>
	/// Add an observer to this CM
	/// </summary>
	/// <param name="player"></param>
	public virtual void AddObserver(GamePlayer player)
	{
		if (_observers.ContainsKey(player.Name) == false)
		{
			_observers.Add(player.Name, player);
		}
	}

	/// <summary>
	/// Remove an observer of this CM
	/// </summary>
	/// <param name="player"></param>
	public virtual void RemoveObserver(GamePlayer player)
	{
		if (_observers.ContainsKey(player.Name))
		{
			_observers.Remove(player.Name);
		}
	}

    /// <summary>
    /// Send inventory updates to all players actively viewing this merchant;
    /// players that are too far away will be considered inactive.
    /// </summary>
    /// <param name="updateItems"></param>
	protected virtual void NotifyObservers(GamePlayer player, IDictionary<int, DbInventoryItem> updateItems)
    {
        var inactiveList = new List<string>();
		bool hasUpdatedPlayer = false;

        foreach (GamePlayer observer in _observers.Values)
        {
			if (observer.ActiveInventoryObject != this)
            {
				inactiveList.Add(observer.Name);
                continue;
            }

            if ((observer.TargetObject is MarketExplorer) == false && observer.IsWithinRadius(this, WorldMgr.INTERACT_DISTANCE) == false)
            {
				observer.ActiveInventoryObject = null;
				inactiveList.Add(observer.Name);
                continue;
            }

            observer.Client.Out.SendInventoryItemsUpdate(updateItems, EInventoryWindowType.Update);

			// The above code is suspect, it seems to work 80% of the time, so let's make sure we update the player doing the move - Tolakram
			if (hasUpdatedPlayer == false)
			{
				player.Client.Out.SendInventoryItemsUpdate(updateItems, EInventoryWindowType.Update);
			}
		}

        // Now remove all inactive observers.
        foreach (string observerName in inactiveList)
        {
            _observers.Remove(observerName);
        }
    }

    /// <summary>
    /// Player interacting with this Merchant.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public override bool Interact(GamePlayer player)
    {
        if (!base.Interact(player))
            return false;

		CheckInventory();

		if (player.ActiveInventoryObject != null)
		{
			player.ActiveInventoryObject.RemoveObserver(player);
			player.ActiveInventoryObject = null;
		}

		player.ActiveInventoryObject = this;

		AddObserver(player);

        House house = HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
        if (house == null)
            return false;

        if (house.CanUseConsignmentMerchant(player, EConsignmentPermissions.Any))
        {
			player.Out.SendInventoryItemsUpdate(GetClientInventory(player), EInventoryWindowType.ConsignmentOwner);

            long amount = m_totalMoney;
            player.Out.SendConsignmentMerchantMoney(amount);

			if (ServerProperty.CONSIGNMENT_USE_BP)
            {
                player.Out.SendMessage("Your merchant currently holds " + amount + " BountyPoints.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
            }
        }
        else
        {
			player.Out.SendInventoryItemsUpdate(GetClientInventory(player), EInventoryWindowType.ConsignmentViewer);
        }

        return true;
    }

    public override bool AddToWorld()
    {
		House house = HouseMgr.GetHouse(HouseNumber);

		if (house == null)
		{
			log.ErrorFormat("CM: Can't find house #{0}!", HouseNumber);
			return false;
		}

		SetInventoryTemplate();

		var houseCM = CoreDb<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
		if (houseCM != null)
		{
			TotalMoney = houseCM.Money;
		}
		else
		{
			log.ErrorFormat("CM: Can't find HouseConsignmentMerchant entry for CM on lot {0}!", HouseNumber);
			return false;
		}

        base.AddToWorld();

        house.ConsignmentMerchant = this;
        SetEmblem();

		CheckInventory();

        return true;
    }

	/// <summary>
	/// Check all items that belong to this ownerid and fix the OwnerLot if needed
	/// </summary>
	public virtual bool CheckInventory()
	{
		House house = HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
		if (house == null)
			return false;

		bool isFixed = false;

		var items = CoreDb<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(house.OwnerID).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDBSlot)).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDBSlot)).And(DB.Column("OwnerLot").IsEqualTo(0)));

		foreach (DbInventoryItem item in items)
		{
			item.OwnerLot = (ushort)HouseNumber;
			MarketCache.AddItem(item);
			if (ServerProperty.MARKET_ENABLE_LOG)
			{
				log.DebugFormat("CM: Fixed OwnerLot for item '{0}' on CM for lot {1}", item.Name, HouseNumber);
			}
			isFixed = true;
		}
		GameServer.Database.SaveObject(items);

		return isFixed;
	}

	public virtual void SetInventoryTemplate()
	{
		var template = new GameNpcInventoryTemplate();
		switch (Realm)
		{
			case ERealm.Albion:
				{
					Model = 92;
					template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 310, 81);
					template.AddNPCEquipment(EInventorySlot.FeetArmor, 1301);
					template.AddNPCEquipment(EInventorySlot.LegsArmor, 1312);

					if (Util.Chance(50))
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1005, 67);
					}
					else
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1313);
					}

					template.AddNPCEquipment(EInventorySlot.Cloak, 669, 65);
				}
				break;
			case ERealm.Midgard:
				{
					Model = 156;
					template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 321, 81);
					template.AddNPCEquipment(EInventorySlot.FeetArmor, 1301);
					template.AddNPCEquipment(EInventorySlot.LegsArmor, 1303);

					if (Util.Chance(50))
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1300);
					}
					else
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 993);
					}

					template.AddNPCEquipment(EInventorySlot.Cloak, 669, 51);
				}
				break;
			case ERealm.Hibernia:
				{
					Model = 335;
					template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 457, 81);
					template.AddNPCEquipment(EInventorySlot.FeetArmor, 1333);

					if (Util.Chance(50))
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1336);
					}
					else
					{
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1008);
					}

					template.AddNPCEquipment(EInventorySlot.Cloak, 669);
				}
				break;
		}

		Inventory = template.CloseTemplate();
	}

    /// <summary>
    /// Not Livelike but looks better - adds the owners guild emblem to the consignment merchants cloak
    /// </summary>
	public virtual void SetEmblem()
    {
        if (Inventory == null)
            return;

        House house = HouseMgr.GetHouse(HouseNumber);
        if (house == null)
            return;

        if (house.DatabaseItem.GuildHouse)
        {
            var guild = CoreDb<DbGuild>.SelectObject(DB.Column("GuildName").IsEqualTo(house.DatabaseItem.GuildName));
            int emblem = guild.Emblem;

            DbInventoryItem cloak = Inventory.GetItem(EInventorySlot.Cloak);
            if (cloak != null)
            {
                cloak.Emblem = emblem;
                BroadcastLivingEquipmentUpdate();
            }
        }
    }
}


/*
 * Just have to run this query, adds all needed token in the databse, also add the merchantlist
 * Albion = PortMerchantHousingAlb
 * Midgard = PortMerchantHousingMid
 * Hibernia = PortMerchantHousingHib
 * 
INSERT INTO `itemtemplate` (`ItemTemplate_ID`, `Id_nb`, `Name`, `Level`, `Durability`, `Condition`, `MaxDurability`, `MaxCondition`, `Quality`, `DPS_AF`, `SPD_ABS`, `Hand`, `Type_Damage`, `Object_Type`, `Item_Type`, `Color`, `Emblem`, `Effect`, `Weight`, `Model`, `Extension`, `Bonus`, `Bonus1`, `Bonus2`, `Bonus3`, `Bonus4`, `Bonus5`, `Bonus6`, `Bonus7`, `Bonus8`, `Bonus9`, `Bonus10`, `ExtraBonus`, `Bonus1Type`, `Bonus2Type`, `Bonus3Type`, `Bonus4Type`, `Bonus5Type`, `Bonus6Type`, `Bonus7Type`, `Bonus8Type`, `Bonus9Type`, `Bonus10Type`, `ExtraBonusType`, `IsPickable`, `IsDropable`, `CanDropAsLoot`, `IsTradable`, `Price`, `MaxCount`, `IsIndestructible`, `IsNotLosingDur`, `PackSize`, `Charges`, `MaxCharges`, `Charges1`, `MaxCharges1`, `SpellID`, `SpellID1`, `ProcSpellID`, `ProcSpellID1`, `PoisonSpellID`, `PoisonMaxCharges`, `PoisonCharges`, `Realm`, `AllowedClasses`, `CanUseEvery`, `Flags`, `BonusLevel`, `LevelRequirement`, `PackageID`, `Description`, `ClassType`, `ProcChance`) VALUES
('entrancehousingalb', 'entrancehousingalb', 'Return token to Caerwent entrance', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('entrancehousinghib', 'entrancehousinghib', 'Return token to Meath entrance', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('entrancehousingmid', 'entrancehousingmid', 'Return token to Housing entrance', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketaberillan', 'marketaberillan', 'Token return to Market of Aberillan', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketarothi', 'marketarothi', 'Jeton de retour Marché d''Arothi', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketaylesbury', 'marketaylesbury', 'Token return to Market of Aylesbury', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketbrisworthy', 'marketbrisworthy', 'Token return to Market of Brisworthy', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketbroughshane', 'marketbroughshane', 'Token return to Market of Broughshane', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketcaerwent', 'marketcaerwent', 'Token return to Market of Caerwent', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketcarlingford', 'marketcarlingford', 'Token return to Market of Carlingford', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketchiltern', 'marketchiltern', 'Token return to Market of Chiltern', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketdalton', 'marketdalton', 'Token return to Market of Dalton', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketdunshire', 'marketdunshire', 'Token return to Market of Dunshire', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketerikstaad', 'marketerikstaad', 'Jeton de retour Marché d''Erikstaad', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketfrisia', 'marketfrisia', 'Token return to Market of Frisia', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketholmestrand', 'marketholmestrand', 'Token return to Market of Holmestrand', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketkaupang', 'marketkaupang', 'Token return to Market of Kaupang', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketkilcullen', 'marketkilcullen', 'Token return to Market of Kilcullen', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketkillcullen', 'marketkillcullen', 'Token return to Market of Kilcullen', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketmeath', 'marketmeath', 'Token return to Market of Meath', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketmoycullen', 'marketmoycullen', 'Token return to Market of Moycullen', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketnittedal', 'marketnittedal', 'Token return to Market of Nittedal', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketoldsarum', 'marketoldsarum', 'Token return to Market of Old Sarum', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketrilan', 'marketrilan', 'Token return to Market of Rilan', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketsaeranthal', 'marketsaeranthal', 'Token return to Market of Saeranthal', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketsherborne', 'marketsherborne', 'Token return to Market of Sherborne', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('marketstavgaard', 'marketstavgaard', 'Token return to Market of Stavgaard', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0),
('marketstoneleigh', 'marketstoneleigh', 'Token return to Market of Stoneleigh', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, '0', 0, 0, 0, 0, '', '', '', 0),
('markettorrylin', 'markettorrylin', 'Token return to Market of Torrylin', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('markettullamore', 'markettullamore', 'Token return to Market of Tullamore', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, '0', 0, 0, 0, 0, '', '', '', 0),
('marketwyndham', 'marketwyndham', 'Token return to Market of Wyndham', 0, 50000, 50000, 50000, 50000, 85, 0, 0, 0, 0, 0, 35, 0, 0, 0, 10, 485, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 500, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, '0', 0, 0, 0, 0, '', '', '', 0);
    
INSERT INTO `merchantitem` (`MerchantItem_ID`, `ItemListID`, `ItemTemplateID`, `PageNumber`, `SlotPosition`, `PackageID`) VALUES
('204280b2-b6fb-4041-bf11-3c9b434c2e4c', 'PortMerchantHousingHib', 'marketdunshire', 0, 9, NULL),
('20f4f965-5848-46aa-a211-56a3d25d5efb', 'PortMerchantHousingAlb', 'marketchiltern', 0, 5, NULL),
('28eb35cf-1a0e-4dee-a1cd-f2cd72b1c0b1', 'PortMerchantHousingAlb', 'marketrilan', 0, 2, NULL),
('4084de34-896a-4ea8-96da-6f40dd8d88a2', 'PortMerchantHousingMid', 'marketerikstaad', 0, 1, NULL),
('44c63a42-f043-4fc4-8c39-1d1a28da27c9', 'PortMerchantHousingHib', 'marketmeath', 0, 1, NULL),
('4743ae0a-23ec-4cf2-8efe-6d34f722eb22', 'PortMerchantHousingHib', 'marketsaeranthal', 0, 8, NULL),
('5d475184-f7d3-41f1-abb5-d6f5340ebd19', 'PortMerchantHousingAlb', 'marketaylesbury', 0, 7, NULL),
('5de46ee8-4107-4406-9132-da8441d185fd', 'PortMerchantHousingMid', 'marketstavgaard', 0, 4, NULL),
('624b989d-ba5e-4989-88c9-3f72bd431c08', 'PortMerchantHousingMid', 'marketwyndham', 0, 9, NULL),
('657a966b-c558-4326-b1b7-9b381daf043e', 'PortMerchantHousingMid', 'entrancehousingmid', 0, 0, NULL),
('6ec755ad-eb04-4290-b698-e357d1aa4261', 'PortMerchantHousingAlb', 'marketsherborne', 0, 6, NULL),
('7558d4dc-27d6-422a-a591-d0d43666d93a', 'PortMerchantHousingMid', 'marketkaupang', 0, 3, NULL),
('75892dfe-cba0-4604-a21d-4fa4186df99f', 'PortMerchantHousingHib', 'entrancehousinghib', 0, 0, NULL),
('761d6772-07d9-4783-aff5-f1aa0c1c9335', 'PortMerchantHousingMid', 'marketarothi', 0, 2, NULL),
('773d466c-9de7-403f-a383-3069fdaac13a', 'PortMerchantHousingMid', 'marketfrisia', 0, 8, NULL),
('79d4ab1d-1e69-49ce-884c-c62f3a0a2e09', 'PortMerchantHousingHib', 'marketmoycullen', 0, 7, NULL),
('7e93386f-117e-4653-a414-135b78943f7c', 'PortMerchantHousingMid', 'marketcarlingford', 0, 5, NULL),
('83d90272-82d4-44a6-94af-1a83a1ba6f73', 'PortMerchantHousingAlb', 'marketstoneleigh', 0, 4, NULL),
('90e48c1a-23c9-4059-a139-fdf42b80106a', 'PortMerchantHousingMid', 'marketnittedal', 0, 7, NULL),
('a17688fc-038a-4d46-9f10-5633fff35304', 'PortMerchantHousingHib', 'markettorrylin', 0, 4, NULL),
('a6b83ba8-73a6-4caa-be0d-11621a4bcf6e', 'PortMerchantHousingHib', 'marketkilcullen', 0, 2, NULL),
('ac9179a4-22c2-4a14-bcfd-e7195943ff1d', 'PortMerchantHousingHib', 'marketbroughshane', 0, 6, NULL),
('bb3b60bb-c1fd-4506-b7c8-490ff13cf51d', 'PortMerchantHousingMid', 'marketholmestrand', 0, 6, NULL),
('cd66879c-ba5e-48b9-900c-fd3bce5c99bc', 'PortMerchantHousingAlb', 'marketoldsarum', 0, 8, NULL),
('d1f058f9-82b2-49c2-98cf-470965c41590', 'PortMerchantHousingAlb', 'entrancehousingalb', 0, 0, NULL),
('d3549e07-3e39-4f06-b7b7-6b789c7ba00a', 'PortMerchantHousingHib', 'marketaberillan', 0, 3, NULL),
('d999f092-ca79-4d89-9574-c2d8624bc99c', 'PortMerchantHousingAlb', 'marketbrisworthy', 0, 3, NULL),
('dac83cf5-903a-48be-baab-e7272923de4a', 'PortMerchantHousingAlb', 'marketdalton', 0, 9, NULL),
('ed615aa0-9246-4df2-a7b8-11f879bf0f46', 'PortMerchantHousingHib', 'markettullamore', 0, 5, NULL),
('efda9497-77a1-4f5d-ac23-ab28c7aa51ae', 'PortMerchantHousingAlb', 'marketcaerwent', 0, 1, NULL);
*/