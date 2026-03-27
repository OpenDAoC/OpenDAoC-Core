using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class ConsignmentState : ECSGameTimerWrapperBase
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int EXPIRES_AFTER = 30000;

        private readonly string _ownerId;
        private readonly Dictionary<string, GamePlayer> _observers = new();
        private readonly Lock _lock = new();
        private long _money;
        private bool _moneyLoaded;
        private bool _isDisposed;

        public ConsignmentState(string ownerId) : base(null)
        {
            _ownerId = ownerId;
        }

        public void AddObserver(GamePlayer player)
        {
            lock (_lock)
            {
                _observers.TryAdd(player.Name, player);
            }
        }

        public void RemoveObserver(GamePlayer player)
        {
            lock (_lock)
            {
                _observers.Remove(player.Name);
            }
        }

        public void WithdrawMoney(GamePlayer player, GameConsignmentMerchant consignmentMerchant)
        {
            lock (_lock)
            {
                long totalMoney = GetTotalMoney();

                if (totalMoney <= 0)
                    return;

                GameClient client = player.Client;

                if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
                {
                    player.Out.SendMessage($"You withdraw {totalMoney} BountyPoints from your Merchant.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
                    player.BountyPoints += totalMoney;
                    player.Out.SendUpdatePoints();
                }
                else
                {
                    ChatUtil.SendMerchantMessage(client, "GameMerchant.OnPlayerWithdraw", Money.GetString(totalMoney));
                    client.Player.AddMoney(totalMoney);
                    InventoryLogging.LogInventoryAction(consignmentMerchant, client.Player, eInventoryActionType.Merchant, totalMoney);
                }

                SetTotalMoney(0);

                if (ServerProperties.Properties.MARKET_ENABLE_LOG && log.IsDebugEnabled)
                    log.DebugFormat($"CM: [{client.Player.Name}:{client.Account.Name}] withdraws {totalMoney} from CM on lot {consignmentMerchant.HouseNumber}.");

                client.Out.SendConsignmentMerchantMoney(0);
            }
        }

        private void EnsureMoneyLoaded()
        {
            if (_moneyLoaded)
                return;

            DbHouseConsignmentMerchant houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(_ownerId));

            if (houseCM != null)
                _money = houseCM.Money;

            _moneyLoaded = true;
        }

        public long GetTotalMoney()
        {
            ConsignmentState newState = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    EnsureMoneyLoaded();
                    return _money;
                }

                newState = ConsignmentStateManager.GetState(_ownerId);
            }

            return newState?.GetTotalMoney() ?? 0;
        }

        public void AddMoney(long amount)
        {
            ConsignmentState newState = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    EnsureMoneyLoaded();
                    _money += amount;

                    DbHouseConsignmentMerchant houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(_ownerId));

                    if (houseCM != null)
                    {
                        houseCM.Money = _money;
                        GameServer.Database.SaveObject(houseCM);
                    }

                    return;
                }

                newState = ConsignmentStateManager.GetState(_ownerId);
            }

            newState?.AddMoney(amount);
        }

        public void SetTotalMoney(long amount)
        {
            ConsignmentState newState = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    EnsureMoneyLoaded();
                    _money = amount;

                    DbHouseConsignmentMerchant houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(_ownerId));

                    if (houseCM != null)
                    {
                        houseCM.Money = _money;
                        GameServer.Database.SaveObject(houseCM);
                    }

                    return;
                }

                newState = ConsignmentStateManager.GetState(_ownerId);
            }

            newState?.SetTotalMoney(amount);
        }

        public bool ProcessMoveItem(GamePlayer player, GameConsignmentMerchant merchant, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, ushort count)
        {
            ConsignmentState newState = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    Start(EXPIRES_AFTER);

                    if (fromClientSlot == toClientSlot)
                        return false;

                    if (GameInventoryObjectExtensions.IsHousingInventorySlot(fromClientSlot))
                    {
                        if (GameInventoryObjectExtensions.IsHousingInventorySlot(toClientSlot))
                        {
                            // Merchant -> Merchant.
                            if (merchant.HasPermissionToMove(player))
                            {
                                var updatedItems = GameInventoryObjectExtensions.MoveItemInternal(merchant, player, fromClientSlot, toClientSlot, count);

                                if (updatedItems.Count > 0)
                                    GameInventoryObjectExtensions.NotifyObservers(player, _observers, updatedItems);
                            }
                            else
                                return false;
                        }
                        else
                        {
                            // Merchant -> Player.
                            player.Inventory.Lock.Enter();

                            try
                            {
                                DbInventoryItem toItem = player.Inventory.GetItem(toClientSlot);

                                if (toItem != null)
                                {
                                    player.Client.Out.SendMessage("You can only move an item to an empty slot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return false;
                                }

                                if (!merchant.HasPermissionToMove(player))
                                {
                                    // The move must be an attempt to buy.
                                    merchant.OnPlayerBuy(player, fromClientSlot, toClientSlot);
                                }
                                else if (player.TargetObject is not null and not MarketExplorer)
                                {
                                    var updatedItems = GameInventoryObjectExtensions.MoveItemInternal(merchant, player, fromClientSlot, toClientSlot, count);

                                    if (updatedItems.Count > 0)
                                        GameInventoryObjectExtensions.NotifyObservers(player, _observers, updatedItems);
                                }
                                else
                                {
                                    player.Client.Out.SendMessage("You can't buy items from yourself!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return false;
                                }
                            }
                            finally
                            {
                                player.Inventory.Lock.Exit();
                            }
                        }
                    }
                    else if (GameInventoryObjectExtensions.IsHousingInventorySlot(toClientSlot))
                    {
                        // Player -> Merchant.
                        if (merchant.HasPermissionToMove(player))
                        {
                            if (merchant.TryGetItem((int) toClientSlot, out DbInventoryItem _))
                            {
                                player.Client.Out.SendMessage("You can only move an item to an empty slot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return false;
                            }

                            var updatedItems = GameInventoryObjectExtensions.MoveItemInternal(merchant, player, fromClientSlot, toClientSlot, count);

                            if (updatedItems.Count > 0)
                                GameInventoryObjectExtensions.NotifyObservers(player, _observers, updatedItems);
                        }
                        else
                            return false;
                    }

                    return true;
                }

                newState = ConsignmentStateManager.GetState(_ownerId);
            }

            return newState != null && newState.ProcessMoveItem(player, merchant, fromClientSlot, toClientSlot, count);
        }

        public void ProcessBuyItem(GamePlayer player, GameConsignmentMerchant merchant, bool usingMarketExplorer)
        {
            eInventorySlot fromClientSlot = player.TempProperties.GetProperty(GameConsignmentMerchant.CONSIGNMENT_BUY_ITEM, eInventorySlot.Invalid);
            player.TempProperties.RemoveProperty(GameConsignmentMerchant.CONSIGNMENT_BUY_ITEM);

            ConsignmentState newState = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    Start(EXPIRES_AFTER);

                    if (fromClientSlot is eInventorySlot.Invalid || !merchant.TryGetItem((int)fromClientSlot, out DbInventoryItem item))
                    {
                        ChatUtil.SendErrorMessage(player, "I can't find the item you want to purchase!");

                        if (log.IsErrorEnabled)
                            log.Error($"{player.Name}:{player.Client.Account} tried to purchase an item from slot {(int) fromClientSlot} for CM on lot {merchant.HouseNumber} and the item does not exist.");

                        return;
                    }

                    int sellPrice = item.SellPrice;
                    int purchasePrice = sellPrice;

                    if (usingMarketExplorer && ServerProperties.Properties.MARKET_FEE_PERCENT > 0)
                        purchasePrice += purchasePrice * ServerProperties.Properties.MARKET_FEE_PERCENT / 100;

                    player.Inventory.Lock.Enter();

                    try
                    {
                        if (purchasePrice <= 0)
                        {
                            ChatUtil.SendErrorMessage(player, "This item can't be purchased!");
                            return;
                        }

                        if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
                        {
                            if (player.BountyPoints < purchasePrice)
                            {
                                ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.YouNeedBP", purchasePrice);
                                return;
                            }
                        }
                        else if (player.GetCurrentMoney() < purchasePrice)
                        {
                            ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.YouNeed", Money.GetString(purchasePrice));
                            return;
                        }

                        eInventorySlot toClientSlot = player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

                        if (toClientSlot is eInventorySlot.Invalid)
                        {
                            ChatUtil.SendSystemMessage(player, "GameMerchant.OnPlayerBuy.NotInventorySpace", null);
                            return;
                        }

                        if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
                        {
                            ChatUtil.SendMerchantMessage(player, "GameMerchant.OnPlayerBuy.BoughtBP", item.GetName(1, false), purchasePrice);
                            player.BountyPoints -= purchasePrice;
                            player.Out.SendUpdatePoints();
                        }
                        else if (player.RemoveMoney(purchasePrice))
                        {
                            InventoryLogging.LogInventoryAction(player, merchant, eInventoryActionType.Merchant, purchasePrice);
                            ChatUtil.SendMerchantMessage(player, "GameMerchant.OnPlayerBuy.Bought", item.GetName(1, false), Money.GetString(purchasePrice));
                        }
                        else
                            return;

                        AddMoney(sellPrice);

                        if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                            log.Debug($"CM: {player.Name}:{player.Client.Account.Name} purchased '{item.Name}' for {purchasePrice} from consignment merchant on lot {merchant.HouseNumber}.");

                        var updatedItems = GameInventoryObjectExtensions.MoveItemInternal(merchant, player, fromClientSlot, toClientSlot, (ushort) item.Count);

                        if (updatedItems.Count > 0)
                            GameInventoryObjectExtensions.NotifyObservers(player, _observers, updatedItems);
                    }
                    finally
                    {
                        player.Inventory.Lock.Exit();
                    }

                    return;
                }

                newState = ConsignmentStateManager.GetState(_ownerId);
            }

            newState?.ProcessBuyItem(player, merchant, usingMarketExplorer);
        }

        protected override int OnTick(ECSGameTimer timer)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return 0;

                // Clean up observers that didn't remove themselves (quit without changing target).
                foreach (var pair in _observers)
                {
                    if (pair.Value.ObjectState is not GameObject.eObjectState.Active)
                        _observers.Remove(pair.Key);
                }

                if (_observers.Count > 0)
                    return EXPIRES_AFTER;

                _isDisposed = true;
                ConsignmentStateManager.RemoveState(_ownerId, this);
            }

            return 0;
        }
    }
}
