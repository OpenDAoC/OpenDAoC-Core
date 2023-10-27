using Core.GS.Players;

namespace Core.GS.Enums;

public enum EMerchantWindowSlot : int
{
    FirstEmptyInPage = -2,
    Invalid = -1,

    FirstInPage = 0,
    LastInPage = MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS - 1,
}