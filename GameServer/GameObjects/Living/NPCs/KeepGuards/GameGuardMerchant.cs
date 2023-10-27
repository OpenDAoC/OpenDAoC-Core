﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Quests;
using Core.GS.Server;

namespace Core.GS;

public class GameGuardMerchant : GameKeepGuard
{
	#region GetExamineMessages / Interact

	/// <summary>
	/// Adds messages to ArrayList which are sent when object is targeted
	/// </summary>
	/// <param name="player">GamePlayer that is examining this object</param>
	/// <returns>list with string messages</returns>
	public override IList GetExamineMessages(GamePlayer player)
	{
		IList list = base.GetExamineMessages(player);
		list.RemoveAt(list.Count - 1);
		list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
			"GameMerchant.GetExamineMessages.YouExamine",
			GetName(0, false, player.Client.Account.Language, this),
			GetPronoun(0, true, player.Client.Account.Language),
			GetAggroLevelString(player, false)));
		list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
			"GameMerchant.GetExamineMessages.RightClick"));
		return list;
	}

	/// <summary>
	/// Called when a player right clicks on the merchant
	/// </summary>
	/// <param name="player">Player that interacted with the merchant</param>
	/// <returns>True if succeeded</returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;
		if (player.Realm != Realm && player.Client.Account.PrivLevel == 1) return false;
		
		TurnTo(player, 10000);
		SendMerchantWindow(player);
		return true;
	}

	/// <summary>
	/// send the merchants item offer window to a player
	/// </summary>
	/// <param name="player"></param>
	public virtual void SendMerchantWindow(GamePlayer player)
	{
		ThreadPool.QueueUserWorkItem(new WaitCallback(SendMerchantWindowCallback), player);
	}

	/// <summary>
	/// Sends merchant window from threadpool thread
	/// </summary>
	/// <param name="state">The game player to send to</param>
	protected virtual void SendMerchantWindowCallback(object state)
	{
		((GamePlayer) state).Out.SendMerchantWindow(m_tradeItems, EMerchantWindowType.Normal);
	}

	#endregion

	#region Items List

	/// <summary>
	/// Items available for sale
	/// </summary>
	protected MerchantTradeItems m_tradeItems;

	/// <summary>
	/// Gets the items available from this merchant
	/// </summary>
	public MerchantTradeItems TradeItems
	{
		get { return m_tradeItems; }
		set { m_tradeItems = value; }
	}

	#endregion

	#region Buy / Sell / Apparaise

	/// <summary>
	/// Called when a player buys an item
	/// </summary>
	/// <param name="player">The player making the purchase</param>
	/// <param name="item_slot">slot of the item to be bought</param>
	/// <param name="number">Number to be bought</param>
	/// <returns>true if buying is allowed, false if buying should be prevented</returns>
	public virtual void OnPlayerBuy(GamePlayer player, int item_slot, int number)
	{
		//Get the template
		int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
		int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

		DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (EMerchantWindowSlot) slotnumber);
		if (template == null) return;

		//Calculate the amout of items
		int amountToBuy = number;
		if (template.PackSize > 0)
			amountToBuy *= template.PackSize;

		if (amountToBuy <= 0) return;

		//Calculate the value of items
		long totalValue = number * template.Price;

		lock (player.Inventory)
		{

			if (player.GetCurrentMoney() < totalValue)
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed",
						MoneyMgr.GetString(totalValue)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (!this.IsWithinRadius(player, ServerProperty.WORLD_PICKUP_DISTANCE)) // tested
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameMerchant.OnPlayerSell.TooFarAway", GetName(0, true)), EChatType.CT_Merchant,
					EChatLoc.CL_SystemWindow);
				return;
			}

			if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy,
				    EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameMerchant.OnPlayerBuy.NotInventorySpace"), EChatType.CT_System,
					EChatLoc.CL_SystemWindow);
				return;
			}

			InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Merchant, template, amountToBuy);
			//Generate the buy message
			string message;
			if (amountToBuy > 1)
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false),
					MoneyMgr.GetString(totalValue));
			else
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), MoneyMgr.GetString(totalValue));

			// Check if player has enough money and subtract the money
			if (!player.RemoveMoney(totalValue, message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow))
			{
				throw new Exception("Money amount changed while adding items.");
			}

			InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, totalValue);
		}
	}

	public virtual void OnPlayerBuy(GamePlayer player, int item_slot, int pagenumber, int number)
	{
		//Get the template
		int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

		DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (EMerchantWindowSlot) slotnumber);
		if (template == null) return;

		//Calculate the amout of items
		int amountToBuy = number;
		if (template.PackSize > 0)
			amountToBuy *= template.PackSize;

		if (amountToBuy <= 0) return;

		//Calculate the value of items
		long totalValue = number * template.Price;

		lock (player.Inventory)
		{

			if (player.GetCurrentMoney() < totalValue)
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed",
						MoneyMgr.GetString(totalValue)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (!this.IsWithinRadius(player, ServerProperty.WORLD_PICKUP_DISTANCE)) // tested
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameMerchant.OnPlayerSell.TooFarAway", GetName(0, true)), EChatType.CT_Merchant,
					EChatLoc.CL_SystemWindow);
				return;
			}

			if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy,
				    EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameMerchant.OnPlayerBuy.NotInventorySpace"), EChatType.CT_System,
					EChatLoc.CL_SystemWindow);
				return;
			}

			InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Merchant, template, amountToBuy);
			//Generate the buy message
			string message;
			if (amountToBuy > 1)
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false),
					MoneyMgr.GetString(totalValue));
			else
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), MoneyMgr.GetString(totalValue));

			// Check if player has enough money and subtract the money
			if (!player.RemoveMoney(totalValue, message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow))
			{
				throw new Exception("Money amount changed while adding items.");
			}

			InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, totalValue);
		}
	}


	/// <summary>
	/// Called when a player buys an item
	/// </summary>
	/// <param name="player">The player making the purchase</param>
	/// <param name="item_slot">slot of the item to be bought</param>
	/// <param name="number">Number to be bought</param>
	/// <param name="TradeItems"></param>
	/// <returns>true if buying is allowed, false if buying should be prevented</returns>
	public static void OnPlayerBuy(GamePlayer player, int item_slot, int number, MerchantTradeItems TradeItems)
	{
		//Get the template
		int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
		int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

		DbItemTemplate template = TradeItems.GetItem(pagenumber, (EMerchantWindowSlot) slotnumber);
		if (template == null) return;

		//Calculate the amout of items
		int amountToBuy = number;
		if (template.PackSize > 0)
			amountToBuy *= template.PackSize;

		if (amountToBuy <= 0) return;

		//Calculate the value of items
		long totalValue = number * template.Price;

		lock (player.Inventory)
		{

			if (player.GetCurrentMoney() < totalValue)
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed",
						MoneyMgr.GetString(totalValue)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy,
				    EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(
					LanguageMgr.GetTranslation(player.Client.Account.Language,
						"GameMerchant.OnPlayerBuy.NotInventorySpace"), EChatType.CT_System,
					EChatLoc.CL_SystemWindow);
				return;
			}

			InventoryLogging.LogInventoryAction("(TRADEITEMS;" + TradeItems.ItemsListID + ")", player,
				EInventoryActionType.Merchant, template, amountToBuy);
			//Generate the buy message
			string message;
			if (amountToBuy > 1)
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false),
					MoneyMgr.GetString(totalValue));
			else
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), MoneyMgr.GetString(totalValue));

			// Check if player has enough money and subtract the money
			if (!player.RemoveMoney(totalValue, message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow))
			{
				throw new Exception("Money amount changed while adding items.");
			}

			InventoryLogging.LogInventoryAction(player, "(TRADEITEMS;" + TradeItems.ItemsListID + ")",
				EInventoryActionType.Merchant, totalValue);
		}
	}

	/// <summary>
	/// Called when a player sells something
	/// </summary>
	/// <param name="player">Player making the sale</param>
	/// <param name="item">The InventoryItem to be sold</param>
	/// <returns>true if selling is allowed, false if it should be prevented</returns>
	public virtual void OnPlayerSell(GamePlayer player, DbInventoryItem item)
	{
		if (item == null || player == null) return;
		if (!item.IsDropable)
		{
			player.Out.SendMessage(
				LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerSell.CantBeSold"),
				EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
			return;
		}

		if (!this.IsWithinRadius(player, ServerProperty.WORLD_PICKUP_DISTANCE)) // tested
		{
			player.Out.SendMessage(
				LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerSell.TooFarAway",
					GetName(0, true)), EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
			return;
		}

		long itemValue = OnPlayerAppraise(player, item, true);

		if (itemValue == 0)
		{
			player.Out.SendMessage(
				LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerSell.IsntInterested", GetName(0, true), item.GetName(0, false)),
				EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
			return;
		}

		if (player.Inventory.RemoveItem(item))
		{
			string message = LanguageMgr.GetTranslation(player.Client.Account.Language,
				"GameMerchant.OnPlayerSell.GivesYou", GetName(0, true), MoneyMgr.GetString(itemValue),
				item.GetName(0, false));
			player.AddMoney(itemValue, message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
			InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template,
				item.Count);
			InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Merchant, itemValue);
			return;
		}
		else
			player.Out.SendMessage(
				LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerSell.CantBeSold"),
				EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Called to appraise the value of an item
	/// </summary>
	/// <param name="player">The player whose item needs appraising</param>
	/// <param name="item">The item to be appraised</param>
	/// <param name="silent"></param>
	/// <returns>The price this merchant will pay for the offered items</returns>
	public virtual long OnPlayerAppraise(GamePlayer player, DbInventoryItem item, bool silent)
	{
		if (item == null)
			return 0;

		int itemCount = Math.Max(1, item.Count);
		int packSize = Math.Max(1, item.PackSize);

		long val = item.Price * itemCount / packSize * ServerProperty.ITEM_SELL_RATIO / 100;

		if (item.Price == 1 && val == 0)
			val = item.Price * itemCount / packSize;

		if (!item.IsDropable)
		{
			val = 0;
		}

		if (!silent)
		{
			string message;
			if (val == 0)
			{
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerSell.IsntInterested", GetName(0, true), item.GetName(0, false));
			}
			else
			{
				message = LanguageMgr.GetTranslation(player.Client.Account.Language,
					"GameMerchant.OnPlayerAppraise.Offers", GetName(0, true), MoneyMgr.GetString(val),
					item.GetName(0, false));
			}

			player.Out.SendMessage(message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
		}

		return val;
	}

	#endregion

	#region NPCTemplate

	public override void LoadTemplate(INpcTemplate template)
	{
		base.LoadTemplate(template);

		if (template != null && string.IsNullOrEmpty(template.ItemsListTemplateID) == false)
		{
			TradeItems = new MerchantTradeItems(template.ItemsListTemplateID);
		}
	}

	#endregion NPCTemplate

	#region Database

	/// <summary>
	/// Loads a merchant from the DB
	/// </summary>
	/// <param name="merchantobject">The merchant DB object</param>
	public override void LoadFromDatabase(DataObject merchantobject)
	{
		base.LoadFromDatabase(merchantobject);
		if (!(merchantobject is DbMob)) return;
		DbMob merchant = (DbMob) merchantobject;
		if (merchant.ItemsListTemplateID != null && merchant.ItemsListTemplateID.Length > 0)
			m_tradeItems = new MerchantTradeItems(merchant.ItemsListTemplateID);
	}

	/// <summary>
	/// Saves a merchant into the DB
	/// </summary>
	public override void SaveIntoDatabase()
	{
		DbMob merchant = null;
		if (InternalID != null)
			merchant = GameServer.Database.FindObjectByKey<DbMob>(InternalID);
		if (merchant == null)
			merchant = new DbMob();

		merchant.Name = Name;
		merchant.Guild = GuildName;
		merchant.X = X;
		merchant.Y = Y;
		merchant.Z = Z;
		merchant.Heading = Heading;
		merchant.Speed = MaxSpeedBase;
		merchant.Region = CurrentRegionID;
		merchant.Realm = (byte) Realm;
		merchant.RoamingRange = RoamingRange;
		merchant.Model = Model;
		merchant.Size = Size;
		merchant.Level = Level;
		merchant.Gender = (byte) Gender;
		merchant.Flags = (uint) Flags;
		merchant.PathID = PathID;
		merchant.PackageID = PackageID;
		merchant.OwnerID = OwnerID;

		IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
		if (aggroBrain != null)
		{
			merchant.AggroLevel = aggroBrain.AggroLevel;
			merchant.AggroRange = aggroBrain.AggroRange;
		}

		merchant.ClassType = this.GetType().ToString();
		merchant.EquipmentTemplateID = EquipmentTemplateID;
		if (m_tradeItems == null)
		{
			merchant.ItemsListTemplateID = null;
		}
		else
		{
			merchant.ItemsListTemplateID = m_tradeItems.ItemsListID;
		}

		if (InternalID == null)
		{
			GameServer.Database.AddObject(merchant);
			InternalID = merchant.ObjectId;
		}
		else
		{
			GameServer.Database.SaveObject(merchant);
		}
	}

	/// <summary>
	/// Deletes a merchant from the DB
	/// </summary>
	public override void DeleteFromDatabase()
	{
		if (InternalID != null)
		{
			DbMob merchant = GameServer.Database.FindObjectByKey<DbMob>(InternalID);
			if (merchant != null)
				GameServer.Database.DeleteObject(merchant);
		}

		InternalID = null;
	}

	#endregion
}

/* 
* Author:   Avithan 
* Date:   22.12.2005 
* Bounty merchant 
*/
public abstract class GameItemCurrencyGuardMerchant : GameGuardMerchant
{
	public virtual string MoneyKey { get { return null; } }
	protected DbItemTemplate m_itemTemplate = null;
	protected WorldInventoryItem m_moneyItem = null;
	protected static readonly Dictionary<String, int> m_currencyValues = null;

	/// <summary>
	/// The item to use as currency
	/// </summary>
	public virtual WorldInventoryItem MoneyItem
	{
		get { return m_moneyItem; }
	}

	/// <summary>
	/// The name of the money item.  Defaults to Item Name
	/// </summary>
	public virtual string MoneyItemName
	{
		get
		{
			if (m_moneyItem != null)
				return m_moneyItem.Name;

			return "not found";
		}
	}

	/// <summary>
	/// Assign templates based on MoneyKey
	/// </summary>
	public GameItemCurrencyGuardMerchant() : base() 
	{
		if (MoneyKey != null)
		{
			m_itemTemplate = GameServer.Database.FindObjectByKey<DbItemTemplate>(MoneyKey);

			if (m_itemTemplate != null)
				m_moneyItem = WorldInventoryItem.CreateFromTemplate(m_itemTemplate);

			// Don't waste memory on an item template we won't use.
			if (ServerProperty.BP_EXCHANGE_ALLOW == false)
				m_itemTemplate = null;
		}
	}

	/// <summary>
	/// Populate the currency exchange table
	/// </summary>
	static GameItemCurrencyGuardMerchant()
    {
		if (ServerProperty.CURRENCY_EXCHANGE_ALLOW == true)
			foreach (string sCurrencyValue in ServerProperty.CURRENCY_EXCHANGE_VALUES.Split(';'))
			{
				string[] asVal = sCurrencyValue.Split('|');

				if (asVal.Length > 1 && int.TryParse(asVal[1], out int currencyValue) && currencyValue > 0)
				{
					// Don't create a dictionary until there is at least one valid value
					if (m_currencyValues == null)
						m_currencyValues = new Dictionary<string, int>(1);

					m_currencyValues[asVal[0]] = currencyValue;
				}
			} // foreach
	}

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		TurnTo(player, 10000);
		SendInteractMessage(player);
		return true;
	}

	protected virtual void SendInteractMessage(GamePlayer player)
	{
		string text = "";
		if (m_moneyItem == null || m_moneyItem.Item == null)
		{
			text = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.GetExamineMessages.Nothing");
			ChatUtil.SendDebugMessage(player, "MoneyItem is null!");
		}
		else
		{
			text = MoneyItemName + "s";
		}

		player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.GetExamineMessages.BuyItemsFor", this.Name, text), EChatType.CT_Say, EChatLoc.CL_ChatWindow);
	}

	protected override void SendMerchantWindowCallback(object state)
	{
		((GamePlayer)state).Out.SendMerchantWindow(m_tradeItems, EMerchantWindowType.Count);
	}

	public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
	{
		if (m_moneyItem == null || m_moneyItem.Item == null)
			return;
		//Get the template
		int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
		int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

		DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (EMerchantWindowSlot)slotnumber);
		if (template == null) return;

		//Calculate the amout of items
		int amountToBuy = number;
		if (template.PackSize > 0)
			amountToBuy *= template.PackSize;

		if (amountToBuy <= 0) return;

		//Calculate the value of items
		long totalValue = number * template.Price;

		lock (player.Inventory)
		{
			if (player.Inventory.CountItemTemplate(m_moneyItem.Item.Id_nb, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) < totalValue)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed2", totalValue, MoneyItemName), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.NotInventorySpace"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

				return;
			}
			InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Merchant, template, amountToBuy);
			//Generate the buy message
			string message;
			if (amountToBuy > 1)
				message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.BoughtPieces2", amountToBuy, template.GetName(1, false), totalValue, MoneyItemName);
			else
				message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.Bought2", template.GetName(1, false), totalValue, MoneyItemName);

			var items = player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
			int removed = 0;

			foreach (DbInventoryItem item in items)
			{
				if (item.Id_nb != m_moneyItem.Item.Id_nb)
					continue;
				int remFromStack = Math.Min(item.Count, (int)(totalValue - removed));
				player.Inventory.RemoveCountFromStack(item, remFromStack);
				InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template, remFromStack);
				removed += remFromStack;
				if (removed == totalValue)
					break;
			}

			player.Out.SendInventoryItemsUpdate(items);
			player.Out.SendMessage(message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
		}
	}

	/// <summary>
	/// Exchange special currency for merchant currency type
	/// </summary>
	/// <param name="source"></param>
	/// <param name="item"></param>
	/// <returns></returns>
	public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
	{
		
		GamePlayer t = source as GamePlayer;
		if (t == null || item == null)
			return false;
		
		if (DataQuestList.Count > 0)
		{
			foreach (DataQuest quest in DataQuestList)
			{
				quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(t, this, item));
				return true;
			}
		}

		return false;
	}
}
public class GameServerGuardMerchant : GameItemCurrencyGuardMerchant
{
	//Atlas Orbs itemtemplate = token_many
	public override string MoneyKey { get; } = ServerProperty.ALT_CURRENCY_ID; // remember to set this in server properties

	public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
	{
		if (m_moneyItem == null || m_moneyItem.Item == null)
			return;
		//Get the template
		int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
		int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

		DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (EMerchantWindowSlot)slotnumber);
		if (template == null) return;

		//Calculate the amout of items
		int amountToBuy = number;
		if (template.PackSize > 0)
			amountToBuy *= template.PackSize;

		if (amountToBuy <= 0) return;

		//Calculate the value of items
		long totalValue;

		if (ServerProperty.ORBS_FIRE_SALE)
		{
			totalValue = 0;
		}
		else
		{
			totalValue = number * template.Price;
		}

		var mobRequirement = KillCreditUtil.GetRequiredKillMob(template.Id_nb);

		if (mobRequirement != null && player.Client.Account.PrivLevel == 1)
		{
			var hasCredit = AchievementUtil.CheckPlayerCredit(mobRequirement, player, (int) player.Realm);

			if (!hasCredit)
			{
				player.Out.SendMessage($"You need to defeat {mobRequirement} at least once to purchase {template.Name}", EChatType.CT_Merchant,EChatLoc.CL_SystemWindow);
				return;
			}
		}
		
		lock (player.Inventory)
		{
			if (player.Inventory.CountItemTemplate(m_moneyItem.Item.Id_nb, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) < totalValue)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed2", totalValue, MoneyItemName), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.NotInventorySpace"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

				return;
			}
			InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Merchant, template, amountToBuy);
			//Generate the buy message
			string message;
			if (amountToBuy > 1)
				message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.BoughtPieces2", amountToBuy, template.GetName(1, false), totalValue, MoneyItemName);
			else
				message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.Bought2", template.GetName(1, false), totalValue, MoneyItemName);

			var items = player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
			int removed = 0;

			foreach (DbInventoryItem item in items)
			{
				if (item.Id_nb != m_moneyItem.Item.Id_nb)
					continue;
				int remFromStack = Math.Min(item.Count, (int)(totalValue - removed));
				player.Inventory.RemoveCountFromStack(item, remFromStack);
				InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template, remFromStack);
				removed += remFromStack;
				if (removed == totalValue)
					break;
			}

			player.Out.SendInventoryItemsUpdate(items);
			player.Out.SendMessage(message, EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
		}
	}
}