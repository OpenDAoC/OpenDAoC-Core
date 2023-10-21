using System.Collections;
using Core.Database.Tables;

namespace Core.GS.Players.Clients;

public interface ITradeWindow
{
	ArrayList TradeItems { get; set;}
	ArrayList PartnerTradeItems { get; }

	long TradeMoney { get; set; }
	long PartnerTradeMoney { get; }

	GamePlayer Owner { get; }
	GamePlayer Partner { get; }

	int ItemsCount { get; }
	int PartnerItemsCount { get; }

	bool Repairing { get; set; }
	bool Combine { get; set; }
	
	bool AddItemToTrade(DbInventoryItem itemForTrade);
	void RemoveItemToTrade(DbInventoryItem itemToRemove);
	void AddMoneyToTrade(long money);
	
	bool AcceptTrade();
	void TradeUpdate();

	object Sync { get; }

	void CloseTrade();
}