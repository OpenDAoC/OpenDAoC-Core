using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;
using log4net;

namespace Core.GS.GameUtils;

public static class InventoryLogging
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public static readonly Dictionary<EInventoryActionType, string> ActionXformat =
		new Dictionary<EInventoryActionType, string>
            {
                {EInventoryActionType.Trade, "[TRADE] {0} > {1}: {2}"},
                {EInventoryActionType.Loot, "[LOOT] {0} > {1}: {2}"},
                {EInventoryActionType.Quest, "[QUEST] {0} > {1}: {2}"},
                {EInventoryActionType.Merchant, "[MERCHANT] {0} > {1}: {2}"},
                {EInventoryActionType.Craft, "[CRAFT] {0} > {1}: {2}"},
                {EInventoryActionType.Other, "[OTHER] {0} > {1}: {2}"}
            };

	public static Func<GameObject, string> GetGameObjectString = obj =>
		obj == null ? "(null)" : ("(" + obj.Name + ";" + obj.GetType() + ";" + obj.X + ";" + obj.Y + ";" + obj.Z + ";" + obj.CurrentRegionID + ")");

	public static Func<DbItemTemplate, int, string> GetItemString = (item, count) =>
		item == null ? "(null)" : ("(" + count + ";" + item.Name + ";" + item.Id_nb + ")");

	public static Func<long, string> GetMoneyString = amount =>
		"(MONEY;" + amount + ")";

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(GameObject source, GameObject destination, EInventoryActionType type, DbItemTemplate item, int count = 1)
	{
		LogInventoryAction(GetGameObjectString(source), GetGameObjectString(destination), type, item, count);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(string source, GameObject destination, EInventoryActionType type, DbItemTemplate item, int count = 1)
	{
		LogInventoryAction(source, GetGameObjectString(destination), type, item, count);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(GameObject source, string destination, EInventoryActionType type, DbItemTemplate item, int count = 1)
	{
		LogInventoryAction(GetGameObjectString(source), destination, type, item, count);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(string source, string destination, EInventoryActionType type, DbItemTemplate item, int count = 1)
	{
		// Check if you can log this action
		if (!_IsLoggingEnabled(type))
			return;

		string format;
		if (!ActionXformat.TryGetValue(type, out format))
			return; // Error, this format does not exists ?!

		try
		{
			GameServer.Instance.LogInventoryAction(string.Format(format, source ?? "(null)", destination ?? "(null)", GetItemString(item, count)));
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("Log inventory error", e);
		}
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(GameObject source, GameObject destination, EInventoryActionType type, long money)
	{
		LogInventoryAction(GetGameObjectString(source), GetGameObjectString(destination), type, money);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(string source, GameObject destination, EInventoryActionType type, long money)
	{
		LogInventoryAction(source, GetGameObjectString(destination), type, money);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(GameObject source, string destination, EInventoryActionType type, long money)
	{
		LogInventoryAction(GetGameObjectString(source), destination, type, money);
	}

	/// <summary>
	/// Log an action of player's inventory (loot, buy, trade, etc...)
	/// </summary>
	/// <param name="source">Source of the item</param>
	/// <param name="destination">Destination of the item</param>
	/// <param name="type">Type of action (trade, loot, quest, ...)</param>
	/// <param name="item">The item or money account traded</param>
	public static void LogInventoryAction(string source, string destination, EInventoryActionType type, long money)
	{
		// Check if you can log this action
		if (!_IsLoggingEnabled(type))
			return;

		string format;
		if (!ActionXformat.TryGetValue(type, out format))
			return; // Error, this format does not exists ?!

		try
		{
			GameServer.Instance.LogInventoryAction(string.Format(format, source ?? "(null)", destination ?? "(null)", GetMoneyString(money)));
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("Log inventory error", e);
		}
	}

	private static bool _IsLoggingEnabled(EInventoryActionType type)
	{
		if (!ServerProperty.LOG_INVENTORY)
			return false;

		switch (type)
		{
			case EInventoryActionType.Trade: return ServerProperty.LOG_INVENTORY_TRADE;
			case EInventoryActionType.Loot: return ServerProperty.LOG_INVENTORY_LOOT;
			case EInventoryActionType.Craft: return ServerProperty.LOG_INVENTORY_CRAFT;
			case EInventoryActionType.Merchant: return ServerProperty.LOG_INVENTORY_MERCHANT;
			case EInventoryActionType.Quest: return ServerProperty.LOG_INVENTORY_QUEST;
			case EInventoryActionType.Other: return ServerProperty.LOG_INVENTORY_OTHER;
		}
		return false;
	}
}