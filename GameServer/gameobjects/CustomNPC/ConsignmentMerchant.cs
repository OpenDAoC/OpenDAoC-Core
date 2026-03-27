using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class GameConsignmentMerchant : GameNPC, IGameInventoryObject
    {
        private static new readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int CONSIGNMENT_SIZE = 100;
        public const int CONSIGNMENT_OFFSET = 1350; // Clients send the same slots as a housing vault.
        public const string CONSIGNMENT_BUY_ITEM = "ConsignmentBuyItem";

        public virtual eInventorySlot FirstClientSlot => eInventorySlot.HousingInventory_First;
        public virtual eInventorySlot LastClientSlot => eInventorySlot.HousingInventory_Last;
        public virtual int FirstDbSlot => (int) eInventorySlot.Consignment_First;
        public virtual int LastDbSlot => (int) eInventorySlot.Consignment_Last;

        private static FrozenDictionary<string, GameLocation> _tokenDestinations = new Dictionary<string, GameLocation>()
        {
            // ALBION
            {"caerwent_entrance", new("", 2, 584832, 561279, 3576, 2144)},
            {"caerwent_market", new("", 2, 557035, 560048, 3624, 1641)},
            {"rilan_market", new("", 2, 559906, 491141, 3392, 1829)},
            {"brisworthy_market", new("", 2, 489474, 489323, 3600, 3633)},
            {"stoneleigh_market", new("", 2, 428964, 490962, 3624, 1806)},
            {"chiltern_market", new("", 2, 428128, 557606, 3624, 3888)},
            {"sherborne_market", new("", 2, 428840, 622221, 3248, 1813)},
            {"aylesbury_market", new("", 2, 492794, 621373, 3624, 1643)},
            {"old_sarum_market", new("", 2, 560030, 622022, 3624, 1819)},
            {"dalton_market", new("", 2, 489334, 559242, 3720, 1821)},

            // MIDGARD
            {"erikstaad_entrance", new("", 102, 526881, 561661, 3633, 80)},
            {"erikstaad_market", new("", 102, 554099, 565239, 3624, 504)},
            {"arothi_market", new("", 102, 558093, 485250, 3488, 1231)},
            {"kaupang_market", new("", 102, 625574, 483303, 3592, 2547)},
            {"stavgaard_market", new("", 102, 686901, 490396, 3744, 332)},
            {"carlingford_market", new("", 102, 625056, 557887, 3696, 1366)},
            {"holmestrand_market", new("", 102, 686903, 556050, 3712, 313)},
            {"nittedal_market", new("", 102, 689199, 616329, 3488, 1252)},
            {"frisia_market", new("", 102, 622620, 615491, 3704, 804)},
            {"wyndham_market", new("", 102, 555839, 621432, 3744, 314)},

            // HIBERNIA
            {"meath_entrance", new("", 202, 555246, 526470, 3008, 1055)},
            {"meath_market", new("", 202, 564448, 559995, 3008, 1024)},
            {"kilcullen_market", new("", 202, 618653, 561227, 3032, 3087)},
            {"aberillan_market", new("", 202, 615145, 619457, 3008, 3064)},
            {"torrylin_market", new("", 202, 566890, 620027, 3008, 1500)},
            {"tullamore_market", new("", 202, 560999, 692301, 3032, 1030)},
            {"broughshane_market", new("", 202, 618653, 692296, 3032, 3090)},
            {"moycullen_market", new("", 202, 495552, 686733, 2960, 1077)},
            {"saeranthal_market", new("", 202, 493148, 620361, 2952, 2471)},
            {"dunshire_market", new("", 202, 495494, 555646, 2960, 1057)}
        }.ToFrozenDictionary();

        public virtual long TotalMoney
        {
            get => GetState()?.GetTotalMoney() ?? 0;
            set => GetState()?.SetTotalMoney(value);
        }

        public override House CurrentHouse
        {
            get => HouseMgr.GetHouse(CurrentRegionID, HouseNumber);
            set { }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.ActiveInventoryObject?.RemoveObserver(player);
            player.ActiveInventoryObject = this;

            AddObserver(player);
            House house = CurrentHouse;

            if (house == null)
                return false;

            if (house.CanUseConsignmentMerchant(player, ConsignmentPermissions.Any))
            {
                player.Out.SendInventoryItemsUpdate(GetClientInventory(), eInventoryWindowType.ConsignmentOwner);
                long amount = TotalMoney;
                player.Out.SendConsignmentMerchantMoney(amount);

                if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
                    player.Out.SendMessage($"Your merchant currently holds {amount} Bounty Points.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            }
            else
                player.Out.SendInventoryItemsUpdate(GetClientInventory(), eInventoryWindowType.ConsignmentViewer);

            return true;
        }

        public override bool AddToWorld()
        {
            House house = CurrentHouse;
            DbHouseConsignmentMerchant houseCm = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));

            if (house == null)
            {
                if (log.IsErrorEnabled)
                    log.Error($"CM: Can't find house {HouseNumber}. Deleting CM.");

                DeleteFromDatabase();

                if (houseCm != null)
                {
                    houseCm.HouseNumber = 0;
                    GameServer.Database.SaveObject(houseCm);
                }

                return false;
            }

            SetInventoryTemplate();

            if (houseCm != null)
                TotalMoney = houseCm.Money;
            else
            {
                if (log.IsErrorEnabled)
                    log.Error($"CM: Can't find {nameof(DbHouseConsignmentMerchant)} for house {HouseNumber}. Deleting CM.");

                DeleteFromDatabase();
                return false;
            }

            base.AddToWorld();
            house.ConsignmentMerchant = this;
            SetEmblem();
            return true;
        }

        public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
        {
            if (source is not GamePlayer player)
                return false;

            if (!player.IsWithinRadius(this, 500))
            {
                player.Out.SendMessage($"You are to far away to give anything to {Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (item != null)
            {
                if (_tokenDestinations.TryGetValue(item.Id_nb, out GameLocation destination))
                {
                    player.MoveTo(destination);
                    player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template, item.Count);
                    player.SaveIntoDatabase();
                    return true;
                }
            }

            return base.ReceiveItem(source, item);
        }

        public virtual string GetOwner()
        {
            return CurrentHouse.OwnerID;
        }

        public virtual IEnumerable<DbInventoryItem> GetDbItems()
        {
            House house = CurrentHouse;

            if (house == null)
                return null;

            // Get all items owned by the owner of the house. Ignore the lot number.
            ItemQuery query = new() { Owner = house.OwnerID };
            return MarketCache.SearchItems(query);
        }

        private DbInventoryItem GetDbItem(int clientSlot)
        {
            int slotOffset = (int) FirstClientSlot - FirstDbSlot;

            // Should find a way to optimize this.
            foreach (DbInventoryItem item in GetDbItems())
            {
                int slot = item.SlotPosition + slotOffset;

                if (slot == clientSlot)
                    return item;
            }

            return null;
        }

        public virtual Dictionary<int, DbInventoryItem> GetClientInventory()
        {
            Dictionary<int, DbInventoryItem> inventory = [];
            int slotOffset = (int) FirstClientSlot - FirstDbSlot;

            foreach (DbInventoryItem item in GetDbItems())
            {
                int slot = item.SlotPosition + slotOffset;

                if (item != null && !inventory.ContainsKey(slot))
                    inventory.Add(slot, item);
            }

            return inventory;
        }

        public virtual bool TryGetItem(int clientSlot, out DbInventoryItem item)
        {
            item = GetDbItem(clientSlot);
            return item != null;
        }

        public virtual bool CanHandleMove(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot)
        {
            return player.ActiveInventoryObject == this && this.CanHandleRequest(fromClientSlot, toClientSlot);
        }

        public virtual bool MoveItem(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, ushort count)
        {
            if (fromClientSlot == toClientSlot)
                return false;

            if (CurrentHouse == null || !CanHandleMove(player, fromClientSlot, toClientSlot))
                return false;

            return GetState()?.ProcessMoveItem(player, this, fromClientSlot, toClientSlot, count) ?? false;
        }

        public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                log.Debug($"CM: {player.Name}:{player.Client.Account.Name} adding '{item.Name}' to consignment merchant on lot {HouseNumber}.");

            // Update owner lot and ID before adding to the cache, so that the item can be retrieved when its price will be set (from another packet).
            item.OwnerLot = HouseNumber;
            item.OwnerID = GetOwner();
            MarketCache.AddItem(item);
            return true;
        }

        public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                log.Debug($"CM: {player.Name}:{player.Client.Account.Name} removing '{item.Name}' from consignment merchant on lot {HouseNumber}.");

            // Remove from the cache before changing item data.
            MarketCache.RemoveItem(item);
            item.OwnerLot = 0;
            item.SellPrice = 0;
            return true;
        }

        public virtual bool OnMoveItem(GamePlayer player, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot)
        {
            return true;
        }

        public virtual void OnItemManipulationError(GamePlayer player) { }

        public virtual bool SetSellPrice(GamePlayer player, eInventorySlot slot, uint price)
        {
            if (player.ActiveInventoryObject is not GameConsignmentMerchant)
                return false;

            House house = CurrentHouse;

            if (house == null || !house.CanUseConsignmentMerchant(player, ConsignmentPermissions.AddRemove))
                return false;

            eInventorySlot clientSlot = slot + (int) FirstClientSlot;

            if (!TryGetItem((int) clientSlot, out DbInventoryItem item))
                return false;

            MarketCache.UpdateItem(item, static (item, price) => item.SellPrice = item.IsTradable ? (int) price : 0, price);

            if (item.IsTradable)
            {
                ChatUtil.SendDebugMessage(player, $"{item.Name} SellPrice={price} OwnerLot={item.OwnerLot} OwnerID={item.OwnerID}");
                player.Out.SendMessage("Price set!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
                player.Out.SendCustomDialog("This item is not tradable. You can store it here but cannot sell it.", null);

            if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                log.Debug($"CM: {player.Name}:{player.Client.Account.Name} set sell price of '{item.Name}' to {item.SellPrice} for consignment merchant on lot {HouseNumber}.");

            if (item.Dirty)
                GameServer.Database.SaveObject(item);

            return true;
        }

        public virtual bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData)
        {
            return false;
        }

        public virtual void AddObserver(GamePlayer player)
        {
            GetState()?.AddObserver(player);
        }

        public virtual void RemoveObserver(GamePlayer player)
        {
            GetState()?.RemoveObserver(player);
        }

        public void WithdrawMoney(GamePlayer player)
        {
            GetState()?.WithdrawMoney(player, this);
        }

        public bool HasPermissionToMove(GamePlayer player)
        {
            House house = CurrentHouse;
            return house != null && house.CanUseConsignmentMerchant(player, ConsignmentPermissions.AddRemove);
        }

        public void UpdateItems()
        {
            // This is needed in case the player had his house repossessed, and they bought a new one on a different lot.

            IEnumerable<DbInventoryItem> items = GetDbItems();

            foreach (DbInventoryItem item in items)
                MarketCache.UpdateItem(item, static (item, lot) => item.OwnerLot = lot, HouseNumber);

            GameServer.Database.SaveObject(items);

            if (log.IsDebugEnabled)
                log.Debug($"CM: Updated {nameof(DbInventoryItem.OwnerLot)} for {items.Count()} items on consignment merchant on lot {HouseNumber}.");
        }

        public void OnPlayerBuy(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, bool usingMarketExplorer = false)
        {
            if (!TryGetItem((int) fromClientSlot, out DbInventoryItem fromItem))
            {
                ChatUtil.SendErrorMessage(player, "I can't find the item you want to purchase!");

                if (log.IsErrorEnabled)
                    log.Error($"CM: {player.Name}:{player.Client.Account} can't find item to buy in slot {(int) fromClientSlot} on consignment merchant on lot {HouseNumber}.");

                return;
            }

            // If the player has a market explorer targeted, they will be charged a commission.
            if (player.TargetObject is MarketExplorer)
            {
                player.TempProperties.SetProperty(CONSIGNMENT_BUY_ITEM, fromClientSlot);

                if (ServerProperties.Properties.MARKET_FEE_PERCENT > 0)
                    player.Out.SendCustomDialog($"Buying directly from the market explorer costs an additional {ServerProperties.Properties.MARKET_FEE_PERCENT}% fee. Do you want to buy this item?", BuyMarketResponse);
                else
                    player.Out.SendCustomDialog($"Do you want to buy this item?", BuyResponse);
            }
            else if (player.TargetObject == this)
            {
                player.TempProperties.SetProperty(CONSIGNMENT_BUY_ITEM, fromClientSlot);
                player.Out.SendCustomDialog($"Do you want to buy this item?", BuyResponse);
            }
            else
                ChatUtil.SendErrorMessage(player, "I'm sorry, you need to be talking to a market explorer or consignment merchant in order to make a purchase.");
        }

        private void BuyResponse(GamePlayer player, byte response)
        {
            if (response != 0x01)
            {
                player.TempProperties.RemoveProperty(CONSIGNMENT_BUY_ITEM);
                return;
            }

            BuyItem(player);
        }

        private void BuyMarketResponse(GamePlayer player, byte response)
        {
            if (response != 0x01)
            {
                player.TempProperties.RemoveProperty(CONSIGNMENT_BUY_ITEM);
                return;
            }

            BuyItem(player, true);
        }

        private void BuyItem(GamePlayer player, bool usingMarketExplorer = false)
        {
            GetState()?.ProcessBuyItem(player, this, usingMarketExplorer);
        }

        private void SetInventoryTemplate()
        {
            GameNpcInventoryTemplate template = new();

            switch (Realm)
            {
                case eRealm.Albion:
                {
                    Model = 92;
                    template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 310, 81);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 1301);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 1312);

                    if (Util.Chance(50))
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005, 67);
                    else
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1313);

                    template.AddNPCEquipment(eInventorySlot.Cloak, 669, 65);
                    break;
                }
                case eRealm.Midgard:
                {
                    Model = 156;
                    template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 321, 81);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 1301);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 1303);

                    if (Util.Chance(50))
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1300);
                    else
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 993);

                    template.AddNPCEquipment(eInventorySlot.Cloak, 669, 51);
                    break;
                }
                case eRealm.Hibernia:
                {
                    Model = 335;
                    template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 457, 81);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 1333);

                    if (Util.Chance(50))
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1336);
                    else
                        template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1008);

                    template.AddNPCEquipment(eInventorySlot.Cloak, 669);
                    break;
                }
            }

            Inventory = template.CloseTemplate();
        }

        private void SetEmblem()
        {
            if (Inventory == null)
                return;

            House house = CurrentHouse;

            if (house == null)
                return;

            if (house.DatabaseItem.GuildHouse)
            {
                DbGuild guild = DOLDB<DbGuild>.SelectObject(DB.Column("GuildName").IsEqualTo(house.DatabaseItem.GuildName));
                int emblem = guild.Emblem;
                DbInventoryItem cloak = Inventory.GetItem(eInventorySlot.Cloak);

                if (cloak != null)
                {
                    cloak.Emblem = emblem;
                    BroadcastLivingEquipmentUpdate();
                }
            }
        }

        private ConsignmentState GetState()
        {
            return ConsignmentStateManager.GetState(this);
        }
    }
}
