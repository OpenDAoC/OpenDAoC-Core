using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// This class represents a full player inventory
	/// and contains functions that can be used to
	/// add and remove items from the player
	/// </summary>
	public class GamePlayerInventory : GameLivingInventory
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructor/Declaration/LoadDatabase/SaveDatabase

		/// <summary>
		/// Holds the player that owns
		/// this inventory
		/// </summary>
		protected readonly GamePlayer m_player;

		/// <summary>
		/// Constructs a new empty inventory for player
		/// </summary>
		/// <param name="player">GamePlayer to create the inventory for</param>
		public GamePlayerInventory(GamePlayer player)
		{
			m_player = player;
		}
		/// <summary>
		/// Loads the inventory from the DataBase
		/// </summary>
		/// <param name="inventoryID">The inventory ID</param>
		/// <returns>success</returns>
		public override bool LoadFromDatabase(string inventoryID)
		{
			lock (LockObject)
			{
				try
				{
					m_items.Clear();

					// We only want to cache items in the players personal inventory and personal vault.
					// If we cache ALL items them all vault code must make sure to update cache, which is not ideal
					// in addition, a player with a housing vault may still have an item in cache that may have been
					// removed by another player with the appropriate house permission.  - Tolakram
					var filterBySlot = DB.Column("SlotPosition").IsLessOrEqualTo((int)eInventorySlot.LastVault).Or(DB.Column("SlotPosition").IsGreaterOrEqualTo(500).And(DB.Column("SlotPosition").IsLessThan(600)));
					var items = DOLDB<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(inventoryID).And(filterBySlot));

					foreach (DbInventoryItem item in items)
					{
						try
						{
							var itemSlot = (eInventorySlot)item.SlotPosition;

							if (item.CanUseEvery > 0)
							{
								item.SetCooldown();
							}

							if (GetValidInventorySlot((eInventorySlot)item.SlotPosition) == eInventorySlot.Invalid)
							{
								if (Log.IsErrorEnabled)
									Log.Error("Tried to load an item in invalid slot, ignored. Item id=" + item.ObjectId);

								continue;
							}

							if (m_items.ContainsKey(itemSlot))
							{
								if (Log.IsErrorEnabled)
								{
									Log.ErrorFormat("Error loading {0}'s ({1}) inventory!\nDuplicate item {2} found in slot {3}; Skipping!",
									                m_player.Name, inventoryID, item.Name, itemSlot);
								}

								continue;
							}

							// Depending on whether or not the item is an artifact we will
							// create different types of inventory items. That way we can speed
							// up item type checks and implement item delve information in
							// a natural way, i.e. through inheritance.

							// Tolakram - Leaving this functionality as is for now.  InventoryArtifact now inherits from
							// GameInventoryItem and utilizes the new Delve system.  No need to set ClassType for all artifacts when
							// this code works fine as is.


							GameInventoryItem playerItem = GameInventoryItem.Create(item);

							if (playerItem.CheckValid(m_player))
							{
								m_items.Add(itemSlot, playerItem as DbInventoryItem);
							}
							else
							{
								Log.ErrorFormat("Item '{0}', ClassType '{1}' failed valid test for player '{2}'!", item.Name, item.ClassType, m_player.Name);
								GameInventoryItem invalidItem = new GameInventoryItem();
								invalidItem.Name = "Invalid Item";
								invalidItem.OwnerID = item.OwnerID;
								invalidItem.SlotPosition = item.SlotPosition;
								invalidItem.AllowAdd = false;
								m_items.Add(itemSlot, invalidItem);
							}

							if (Log.IsWarnEnabled)
							{
								// bows don't use damage type - no warning needed
								if (GlobalConstants.IsWeapon(item.Object_Type)
								    && item.Type_Damage == 0
								    && item.Object_Type != (int)eObjectType.CompositeBow
								    && item.Object_Type != (int)eObjectType.Crossbow
								    && item.Object_Type != (int)eObjectType.Longbow
								    && item.Object_Type != (int)eObjectType.Fired
								    && item.Object_Type != (int)eObjectType.RecurvedBow)
								{
									Log.Warn(m_player.Name + ": weapon with damage type 0 is loaded \"" + item.Name + "\" (" + item.ObjectId + ")");
								}
							}
						}
						catch (Exception ex)
						{
							Log.Error("Error loading player inventory (" + inventoryID + "), Inventory_ID: " +
							          item.ObjectId +
							          " (" + (item.ITemplate_Id == null ? "" : item.ITemplate_Id) +
							          ", " + (item.UTemplate_Id == null ? "" : item.UTemplate_Id) +
							          "), slot: " + item.SlotPosition, ex);
						}
					}

					// notify handlers that the item was just equipped
					foreach (eInventorySlot slot in EQUIP_SLOTS)
					{
						// skip weapons. only active weapons should fire equip event, done in player.SwitchWeapon
						if (slot is >= eInventorySlot.RightHandWeapon and <= eInventorySlot.DistanceWeapon)
							continue;

						if (m_items.TryGetValue(slot, out DbInventoryItem item))
							m_player.OnItemEquipped(item, slot);
					}

					return true;
				}
				catch (Exception e)
				{
					if (Log.IsErrorEnabled)
						Log.Error("Error loading player inventory (" + inventoryID + ").  Load aborted!", e);

					return false;
				}
			}
		}

		/// <summary>
		/// Saves all dirty items to database
		/// </summary>
		/// <param name="inventoryID">The inventory ID</param>
		/// <returns>success</returns>
		public override bool SaveIntoDatabase(string inventoryID)
		{
			lock (LockObject)
			{
				foreach (DbInventoryItem item in _itemsAwaitingDeletion)
				{
					try
					{
						DeleteItem(item);
					}
					catch (Exception e)
					{
						if (Log.IsErrorEnabled)
							Log.Error($"Error deleting item when saving player inventory. (ObjectId: {item.ObjectId}) (Player: {m_player})", e);
					}
				}

				_itemsAwaitingDeletion.Clear();

				foreach (var pair in m_items)
				{
					try
					{
						SaveItem(pair);
					}
					catch (Exception e)
					{
						if (Log.IsErrorEnabled)
							Log.Error($"Error saving item. (ObjectId: {pair.Value?.ObjectId}) (Player: {m_player})", e);
					}
				}

				return true;
			}

			void DeleteItem(DbInventoryItem item)
			{
				bool canPersist = true;

				if (item is GameInventoryItem gameItem)
					canPersist = gameItem.CanPersist;

				if (!canPersist)
					return;

				if (item.PendingDatabaseAction is PendingDatabaseAction.DELETE)
				{
					GameServer.Database.DeleteObject(item);
					item.PendingDatabaseAction = PendingDatabaseAction.SAVE;
				}
			}

			void SaveItem(KeyValuePair<eInventorySlot, DbInventoryItem> pair)
			{
				DbInventoryItem item = pair.Value;

				if (item == null)
					return;

				bool canPersist = true;

				if (item is GameInventoryItem gameItem)
					canPersist = gameItem.CanPersist;

				if (!canPersist)
					return;

				int slot = (int) pair.Key;

				if (item.SlotPosition != slot)
				{
					if (Log.IsErrorEnabled)
						Log.Error($"Item's slot doesn't match. Changing it to InventorySlot. (SlotPosition: {item.SlotPosition}) (InventorySlot: {slot}) (ObjectId: {item.ObjectId}");

					item.SlotPosition = slot; // Just to be sure.
				}

				if (GetValidInventorySlot((eInventorySlot) item.SlotPosition) == eInventorySlot.Invalid)
				{
					if (Log.IsErrorEnabled)
						Log.Error($"Item's slot position is invalid. (SlotPosition: {item.SlotPosition}) (ObjectId: {item.ObjectId})");

					return;
				}

				if (item.OwnerID != m_player.InternalID)
				{
					if (Log.IsErrorEnabled)
						Log.Error($"Item's owner ID doesn't equal inventory owner's ID. (ItemOwner: {item.OwnerID}) (InventoryOwner: {m_player.InternalID}) (ObjectId: {item.ObjectId}");

					return;
				}

				if (item.PendingDatabaseAction is PendingDatabaseAction.ADD)
				{
					GameServer.Database.AddObject(item);
					item.PendingDatabaseAction = PendingDatabaseAction.SAVE;
				}
				else if (item.PendingDatabaseAction is PendingDatabaseAction.SAVE)
					GameServer.Database.SaveObject(item);
			}
		}

		#endregion Constructor/Declaration/LoadDatabase/SaveDatabase

		#region Add/Remove

		public override bool AddItem(eInventorySlot slot, DbInventoryItem item)
		{
			return AddItem(slot, item, true);
		}

		public override bool AddItemWithoutDbAddition(eInventorySlot slot, DbInventoryItem item)
		{
			return AddItem(slot, item, false);
		}

		private bool AddItem(eInventorySlot slot, DbInventoryItem item, bool markForAddition)
		{
			if (!base.AddItem(slot, item))
				return false;

			item.OwnerID = m_player.InternalID;

			if (markForAddition)
			{
				bool canPersist = true;

				if (item is GameInventoryItem gameItem)
					canPersist = gameItem.CanPersist;

				if (canPersist)
					item.PendingDatabaseAction = PendingDatabaseAction.ADD;
			}

			if (IsEquippedSlot((eInventorySlot) item.SlotPosition))
				m_player.OnItemEquipped(item, eInventorySlot.Invalid);

			(item as IGameInventoryItem)?.OnReceive(m_player);
			return true;
		}

		public override bool RemoveItem(DbInventoryItem item)
		{
			return RemoveItem(item, true);
		}

		public override bool RemoveItemWithoutDbDeletion(DbInventoryItem item)
		{
			return RemoveItem(item, false);
		}

		private bool RemoveItem(DbInventoryItem item, bool markForDeletion)
		{
			if (item == null)
				return false;

			if (item.OwnerID != m_player.InternalID)
			{
				if (Log.IsErrorEnabled)
					Log.Error($"{m_player.Name} tried to remove item with wrong owner ({item.OwnerID})\n{Environment.StackTrace}");

				return false;
			}

			eInventorySlot oldSlot = (eInventorySlot) item.SlotPosition;

			if (!base.RemoveItem(item))
				return false;

			if (markForDeletion)
			{
				bool canPersist = true;

				if (item is GameInventoryItem gameItem)
					canPersist = gameItem.CanPersist;

				if (canPersist)
				{
					item.PendingDatabaseAction = PendingDatabaseAction.DELETE;
					_itemsAwaitingDeletion.Add(item);
				}
			}

			m_player.TradeWindow?.RemoveItemToTrade(item);

			if (IsEquippedSlot(oldSlot))
				m_player.OnItemUnequipped(item, oldSlot);
			else if (oldSlot is >= eInventorySlot.FirstQuiver and <= eInventorySlot.FourthQuiver)
				m_player.SwitchQuiver(eActiveQuiverSlot.None, true);

			(item as IGameInventoryItem)?.OnLose(m_player);
			return true;
		}

		public override bool AddCountToStack(DbInventoryItem item, int count)
		{
			if (item != null && item.OwnerID != m_player.InternalID)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Item owner not equals inventory owner.\n" + Environment.StackTrace);

				return false;
			}

			return base.AddCountToStack(item, count);
		}

		public override bool RemoveCountFromStack(DbInventoryItem item, int count)
		{
			if (item != null && item.OwnerID != m_player.InternalID)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Item owner not equals inventory owner.\n" + Environment.StackTrace);

				return false;
			}

			if (item.Count <= 0)
				item.PendingDatabaseAction = PendingDatabaseAction.DELETE;

			return base.RemoveCountFromStack(item, count);
		}

		#endregion Add/Remove

		#region Get Inventory Informations

		/// <summary>
		/// Check if the slot is valid in this inventory
		/// </summary>
		/// <param name="slot">SlotPosition to check</param>
		/// <returns>the slot if it's valid or eInventorySlot.Invalid if not</returns>
		protected override eInventorySlot GetValidInventorySlot(eInventorySlot slot)
		{
			switch (slot)
			{
				case eInventorySlot.LastEmptyQuiver:
					slot = FindLastEmptySlot(eInventorySlot.FirstQuiver, eInventorySlot.FourthQuiver);
					break;
				case eInventorySlot.FirstEmptyQuiver:
					slot = FindFirstEmptySlot(eInventorySlot.FirstQuiver, eInventorySlot.FourthQuiver);
					break;
				case eInventorySlot.LastEmptyVault:
					slot = FindLastEmptySlot(eInventorySlot.FirstVault, eInventorySlot.LastVault);
					break;
				case eInventorySlot.FirstEmptyVault:
					slot = FindFirstEmptySlot(eInventorySlot.FirstVault, eInventorySlot.LastVault);
					break;
				case eInventorySlot.LastEmptyBackpack:
					slot = FindLastEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
					break;
				case eInventorySlot.FirstEmptyBackpack:
					slot = FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
					break;
					// INVENTAIRE DES CHEVAUX
				case eInventorySlot.LastEmptyBagHorse:
					slot = FindLastEmptySlot(eInventorySlot.FirstBagHorse, eInventorySlot.LastBagHorse);
					break;
				case eInventorySlot.FirstEmptyBagHorse:
					slot = FindFirstEmptySlot(eInventorySlot.FirstBagHorse, eInventorySlot.LastBagHorse);
					break;
			}

			if ((slot >= eInventorySlot.FirstBackpack && slot <= eInventorySlot.LastBackpack)
			    //				|| ( slot >= eInventorySlot.Mithril && slot <= eInventorySlot.Copper ) // can't place items in money slots, is it?
			    || (slot >= eInventorySlot.HorseArmor && slot <= eInventorySlot.Horse)
			    || (slot >= eInventorySlot.FirstVault && slot <= eInventorySlot.LastVault)
			    || (slot >= eInventorySlot.HouseVault_First && slot <= eInventorySlot.HouseVault_Last)
			    || (slot >= eInventorySlot.Consignment_First && slot <= eInventorySlot.Consignment_Last)
			    || (slot == eInventorySlot.Mythical)
			    // INVENTAIRE DES CHEVAUX
			    || (slot >= eInventorySlot.FirstBagHorse && slot <= eInventorySlot.LastBagHorse))
				return slot;


			return base.GetValidInventorySlot(slot);
		}

		#endregion Get Inventory Informations

		#region Move Item

        public override bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
        {
            if (!CheckPlayerState())
                return false;

            DbInventoryItem fromItem;
            DbInventoryItem toItem;
            bool moved;

            lock (LockObject)
            {
                if (!GetValidInventorySlot(ref fromSlot) || !GetValidInventorySlot(ref toSlot))
                    return false;

                // Just change active weapon if placed in same slot.
                if (fromSlot == toSlot)
                {
                    switch (toSlot)
                    {
                        case eInventorySlot.RightHandWeapon:
                        case eInventorySlot.LeftHandWeapon:
                        {
                            m_player.SwitchWeapon(eActiveWeaponSlot.Standard);
                            return false;
                        }
                        case eInventorySlot.TwoHandWeapon:
                        {
                            m_player.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            return false;
                        }
                        case eInventorySlot.DistanceWeapon:
                        {
                            m_player.SwitchWeapon(eActiveWeaponSlot.Distance);
                            return false;
                        }
                    }
                }

                m_items.TryGetValue(fromSlot, out fromItem);
                m_items.TryGetValue(toSlot, out toItem);

                if (fromItem == toItem || fromItem == null)
                {
                    m_player.Out.SendInventorySlotsUpdate(null);
                    return false;
                }

                if (!CheckHorseInventoryRestrictions(fromSlot, toSlot))
                    return false;

                if (!CheckPoisonApplication(fromItem, toItem))
                    return false;

                if (!CheckItemsRestrictions(fromItem, toItem, fromSlot, toSlot))
                    return false;

                moved = base.MoveItem(fromSlot, toSlot, itemCount);
            }

            if (!moved)
                return false;

            OnItemMove(fromItem, toItem, fromSlot, toSlot);
            return true;
        }

        public override bool CheckItemsBeforeMovingFromOrToExternalInventory(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot externalSlot, eInventorySlot playerInventorySlot, int itemCount)
        {
            if (!CheckPlayerState())
                return false;

            lock (LockObject)
            {
                if (!GetValidInventorySlot(ref playerInventorySlot))
                    return false;

                if (!CheckHorseInventoryRestrictions(externalSlot, playerInventorySlot))
                    return false;

                if (!CheckPoisonApplication(fromItem, toItem))
                    return false;

                if (!CheckItemsRestrictions(fromItem, toItem, externalSlot, playerInventorySlot))
                    return false;
            }

            return true;
        }

        public override void OnItemMove(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot fromSlot, eInventorySlot toSlot)
        {
            CheckAttackStateChange(fromSlot, toSlot);
            CheckTradeWindow(fromItem, toItem);
            SwitchWeaponContextually(fromSlot, toSlot);

            void CheckAttackStateChange(eInventorySlot fromSlot, eInventorySlot toSlot)
            {
                if (fromSlot is (>= eInventorySlot.RightHandWeapon and <= eInventorySlot.DistanceWeapon) or (>= eInventorySlot.FirstQuiver and <= eInventorySlot.FourthQuiver) ||
                    toSlot is (>= eInventorySlot.RightHandWeapon and <= eInventorySlot.DistanceWeapon) or (>= eInventorySlot.FirstQuiver and <= eInventorySlot.FourthQuiver))
                {
                    m_player.attackComponent.StopAttack();
                }
            }

            void CheckTradeWindow(DbInventoryItem fromItem, DbInventoryItem toItem)
            {
                ITradeWindow window = m_player.TradeWindow;

                if (window != null)
                {
                    window.RemoveItemToTrade(toItem);
                    window.RemoveItemToTrade(fromItem);
                }
            }

            void SwitchWeaponContextually(eInventorySlot fromSlot, eInventorySlot toSlot)
            {
                switch (toSlot)
                {
                    case eInventorySlot.RightHandWeapon:
                    {
                        m_player.SwitchWeapon(eActiveWeaponSlot.Standard);
                        break;
                    }
                    case eInventorySlot.TwoHandWeapon:
                    {
                        m_player.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        break;
                    }
                    case eInventorySlot.DistanceWeapon:
                    {
                        m_player.SwitchWeapon(eActiveWeaponSlot.Distance);
                        break;
                    }
                    case eInventorySlot.LeftHandWeapon:
                    {
                        if (m_player.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                            m_player.SwitchWeapon(m_player.ActiveWeaponSlot);
                        else
                            m_player.SwitchWeapon(eActiveWeaponSlot.Standard);

                        break;
                    }
                    case eInventorySlot.FirstQuiver:
                    {
                        m_player.SwitchQuiver(eActiveQuiverSlot.First, true);
                        break;
                    }
                    case eInventorySlot.SecondQuiver:
                    {
                        m_player.SwitchQuiver(eActiveQuiverSlot.Second, true);
                        break;
                    }
                    case eInventorySlot.ThirdQuiver:
                    {
                        m_player.SwitchQuiver(eActiveQuiverSlot.Third, true);
                        break;
                    }
                    case eInventorySlot.FourthQuiver:
                    {
                        m_player.SwitchQuiver(eActiveQuiverSlot.Fourth, true);
                        break;
                    }
                    default:
                    {
                        switch (fromSlot)
                        {
                            case eInventorySlot.RightHandWeapon:
                            {
                                if (m_player.ActiveWeaponSlot is eActiveWeaponSlot.Standard)
                                    m_player.SwitchWeapon(eActiveWeaponSlot.TwoHanded);

                                break;
                            }
                            case eInventorySlot.TwoHandWeapon:
                            {
                                if (m_player.ActiveWeaponSlot is eActiveWeaponSlot.TwoHanded)
                                    m_player.SwitchWeapon(eActiveWeaponSlot.Standard);

                                break;
                            }
                            case eInventorySlot.DistanceWeapon:
                            {
                                if (m_player.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                                    m_player.SwitchWeapon(eActiveWeaponSlot.Standard);

                                break;
                            }
                            case eInventorySlot.LeftHandWeapon:
                            {
                                if (m_player.ActiveWeaponSlot is eActiveWeaponSlot.TwoHanded or eActiveWeaponSlot.Standard)
                                    m_player.SwitchWeapon(m_player.ActiveWeaponSlot);

                                break;
                            }
                        }

                        if (fromSlot is >= eInventorySlot.FirstQuiver and <= eInventorySlot.FourthQuiver)
                            m_player.SwitchQuiver(eActiveQuiverSlot.None, true);

                        break;
                    }
                }
            }
        }

        private bool GetValidInventorySlot(ref eInventorySlot slot)
        {
            slot = GetValidInventorySlot(slot);

            if (slot != eInventorySlot.Invalid)
                return true;

            ChatUtil.SendDebugMessage(m_player, $"Invalid slot: {slot}.");
            m_player.Out.SendInventorySlotsUpdate(null);
            return false;
        }

        private bool CheckPlayerState()
        {
            if (m_player.IsAlive)
                return true;

            m_player.Out.SendMessage("You can't change your inventory when dead!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            m_player.Out.SendInventorySlotsUpdate(null);
            return false;
        }

        private bool CheckHorseInventoryRestrictions(eInventorySlot fromSlot, eInventorySlot toSlot)
        {
            if (toSlot is not >= eInventorySlot.FirstBagHorse or not <= eInventorySlot.LastBagHorse && fromSlot is not >= eInventorySlot.FirstBagHorse or not <= eInventorySlot.LastBagHorse)
                return true;

            // Don't let player move active horse to a horse bag, which will disable all bags!
            if (fromSlot == eInventorySlot.Horse)
                m_player.Out.SendMessage("You can't move your active horse into a saddlebag!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (m_player.Client.Account.PrivLevel != 1 || (m_player.CanUseHorseInventorySlot((int) fromSlot) && m_player.CanUseHorseInventorySlot((int) toSlot)))
                return true;

            m_player.Out.SendInventorySlotsUpdate(null);
            return false;
        }

        private bool CheckPoisonApplication(DbInventoryItem fromItem, DbInventoryItem toItem)
        {
            if (toItem == null || (eObjectType) fromItem.Object_Type != eObjectType.Poison || !GlobalConstants.IsWeapon(toItem.Object_Type))
                return true;

            m_player.ApplyPoison(fromItem, toItem);
            m_player.Out.SendInventorySlotsUpdate(null);
            return false;
        }

        private bool CheckItemsRestrictions(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot fromSlot, eInventorySlot toSlot)
        {
            if (CheckItemClassRestriction(fromItem, toSlot) &&
                CheckItemClassRestriction(toItem, fromSlot) &&
                CheckItemRealmRestriction(fromItem, toSlot) &&
                CheckItemSlotRestriction(fromItem, toSlot) &&
                CheckItemSlotRestriction(toItem, fromSlot))
            {
                return true;
            }

            m_player.Out.SendInventorySlotsUpdate(null);
            return false;
        }

        private bool CheckItemRealmRestriction(DbInventoryItem item, eInventorySlot slot)
        {
            if (item == null ||item.Realm <= 0 || (eRealm) item.Realm == m_player.Realm || slot is eInventorySlot.HorseArmor or eInventorySlot.HorseBarding || m_player.Client.Account.PrivLevel > 1)
                return true;

            m_player.Out.SendMessage("You cannot equip an item from another realm!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        private bool CheckItemClassRestriction(DbInventoryItem item, eInventorySlot slot)
        {
            if (item == null || slot is >= eInventorySlot.MaxEquipable or (>= eInventorySlot.FirstBackpack and <= eInventorySlot.LastBackpack) || string.IsNullOrEmpty(item.AllowedClasses))
                return true;

            foreach (string allowed in Util.SplitCSV(item.AllowedClasses, true))
            {
                if (m_player.CharacterClass.ID == int.Parse(allowed) || m_player.Client.Account.PrivLevel > 1)
                    return true;
            }

            m_player.Out.SendMessage("Your class cannot use this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        private bool CheckItemSlotRestriction(DbInventoryItem item, eInventorySlot slot)
        {
            if (item == null)
                return true;

            switch (slot)
            {
                case eInventorySlot.Mythical:
                {
                    if ((eInventorySlot) item.Item_Type is not eInventorySlot.Mythical)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    if (item.Type_Damage > m_player.ChampionLevel)
                    {
                        m_player.Out.SendMessage($"You can't use {item.GetName(0, true)}, you should increase your champion level.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.HorseBarding:
                {
                    if ((eInventorySlot) item.Item_Type is not eInventorySlot.HorseBarding)
                    {
                        m_player.Out.SendMessage($"You can't put {item.GetName(0, true)} in your active barding slot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.HorseArmor:
                {
                    if ((eInventorySlot) item.Item_Type is not eInventorySlot.HorseArmor)
                    {
                        m_player.Out.SendMessage($"You can't put {item.GetName(0, true)} in your active horse armor slot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.Horse:
                {
                    if ((eInventorySlot) item.Item_Type is not eInventorySlot.Horse)
                    {
                        m_player.Out.SendMessage($"You can't put {item.GetName(0, true)} in your active mount slot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.RightHandWeapon:
                {
                    if ((eObjectType) item.Object_Type is eObjectType.Shield ||
                        ((eInventorySlot) item.Item_Type is not eInventorySlot.RightHandWeapon and not eInventorySlot.LeftHandWeapon))
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else if (!m_player.HasAbilityToUseItem(item.Template))
                    {
                        m_player.Out.SendMessage("You have no skill in using this weapon type!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.TwoHandWeapon:
                {
                    if ((eObjectType) item.Object_Type is eObjectType.Shield ||
                        ((eInventorySlot) item.Item_Type is not eInventorySlot.RightHandWeapon and not eInventorySlot.LeftHandWeapon and not eInventorySlot.TwoHandWeapon && (eObjectType) item.Object_Type is not eObjectType.Instrument))
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else if (!m_player.HasAbilityToUseItem(item.Template))
                    {
                        m_player.Out.SendMessage("You have no skill in using this weapon type!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.LeftHandWeapon:
                {
                    if ((eInventorySlot) item.Item_Type != slot ||
                        ((eObjectType) item.Object_Type is not eObjectType.Shield && !m_player.attackComponent.CanUseLefthandedWeapon))
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else if (!m_player.HasAbilityToUseItem(item.Template))
                    {
                        m_player.Out.SendMessage("You have no skill in using this weapon type!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.DistanceWeapon:
                {
                    if ((eInventorySlot) item.Item_Type != slot && (eObjectType) item.Object_Type is not eObjectType.Instrument)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else if (!m_player.HasAbilityToUseItem(item.Template))
                    {
                        m_player.Out.SendMessage("You have no skill in using this weapon type!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.HeadArmor:
                case eInventorySlot.HandsArmor:
                case eInventorySlot.FeetArmor:
                case eInventorySlot.TorsoArmor:
                case eInventorySlot.LegsArmor:
                case eInventorySlot.ArmsArmor:
                {
                    if ((eInventorySlot) item.Item_Type != slot)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else if (!m_player.HasAbilityToUseItem(item.Template))
                    {
                        m_player.Out.SendMessage("You have no skill in wearing this armor type!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.Jewelry:
                case eInventorySlot.Cloak:
                case eInventorySlot.Neck:
                case eInventorySlot.Waist:
                {
                    if ((eInventorySlot) item.Item_Type != slot)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.LeftBracer:
                case eInventorySlot.RightBracer:
                {
                    if (item.Item_Type is not Slot.RIGHTWRIST and not Slot.LEFTWRIST)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.LeftRing:
                case eInventorySlot.RightRing:
                {
                    if (item.Item_Type is not Slot.LEFTRING and not Slot.RIGHTRING)
                    {
                        m_player.Out.SendMessage($"{item.GetName(0, true)} can't go there!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
                case eInventorySlot.FirstQuiver:
                case eInventorySlot.SecondQuiver:
                case eInventorySlot.ThirdQuiver:
                case eInventorySlot.FourthQuiver:
                {
                    if ((eObjectType) item.Object_Type is not eObjectType.Arrow and not eObjectType.Bolt)
                    {
                        m_player.Out.SendMessage($"You can't put your {item.Name} in your quiver!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }

                    break;
                }
            }

            return true;
        }

        #endregion Move Item

        #region Combine/Exchange/Stack Items

        /// <summary>
        /// Combine 2 items together if possible
        /// </summary>
        /// <param name="fromItem">First Item</param>
        /// <param name="toItem">Second Item</param>
        /// <returns>true if items combined successfully</returns>
        protected override bool CombineItems(DbInventoryItem fromItem, DbInventoryItem toItem)
		{
			if (toItem == null ||
			    fromItem.SlotPosition < (int) eInventorySlot.FirstBackpack ||
			    fromItem.SlotPosition > (int) eInventorySlot.LastBackpack)
				return false;

			if (fromItem is IGameInventoryItem)
			{
				if ((fromItem as IGameInventoryItem).Combine(m_player, toItem))
				{
					return true;
				}
			}

			//Is the fromItem a dye or dyepack?
			//TODO shouldn't be done with model check
			switch (fromItem.Model)
			{
				case 229:
				case 494:
				case 495:
				case 538:
					return DyeItem(fromItem, toItem);
			}

			return false;
		}

		/// <summary>
		/// Stack an item with another one
		/// </summary>
		/// <param name="fromSlot">First SlotPosition</param>
		/// <param name="toSlot">Second SlotPosition</param>
		/// <param name="itemCount">How many items to move</param>
		/// <returns>true if items stacked successfully</returns>
		protected override bool StackItems(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
		{
			DbInventoryItem fromItem;
			DbInventoryItem toItem;

			m_items.TryGetValue(fromSlot, out fromItem);
			m_items.TryGetValue(toSlot, out toItem);

			if ((toSlot > eInventorySlot.HorseArmor && toSlot < eInventorySlot.FirstQuiver)
			    || (toSlot > eInventorySlot.FourthQuiver && toSlot < eInventorySlot.FirstBackpack))
				return false;

			if (itemCount == 0)
			{
				itemCount = fromItem.Count > 0 ? fromItem.Count : 1;
			}

			if (toItem != null && toItem.IsStackable && toItem.Name.Equals(fromItem.Name))
			{
				if (fromItem.Count + toItem.Count > fromItem.MaxCount)
				{
					fromItem.Count -= (toItem.MaxCount - toItem.Count);
					toItem.Count = toItem.MaxCount;
				}
				else
				{
					toItem.Count += fromItem.Count;
					RemoveItem(fromItem);
				}

				return true;
			}

			if (toItem == null && fromItem.Count > itemCount)
			{
				var newItem = (DbInventoryItem) fromItem.Clone();
				m_items[toSlot] = newItem;
				newItem.Count = itemCount;
				newItem.SlotPosition = (int) toSlot;
				fromItem.Count -= itemCount;
				newItem.AllowAdd = fromItem.Template.AllowAdd;
				newItem.PendingDatabaseAction = PendingDatabaseAction.ADD;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Exchange one item position with another one
		/// </summary>
		/// <param name="fromSlot">First SlotPosition</param>
		/// <param name="toSlot">Second SlotPosition</param>
		/// <returns>true if items exchanged successfully</returns>
		protected override bool ExchangeItems(eInventorySlot fromSlot, eInventorySlot toSlot)
		{
			DbInventoryItem fromItem;
			DbInventoryItem toItem;

			m_items.TryGetValue(fromSlot, out fromItem);
			m_items.TryGetValue(toSlot, out toItem);

			bool fromSlotEquipped = IsEquippedSlot(fromSlot);
			bool toSlotEquipped = IsEquippedSlot(toSlot);

			base.ExchangeItems(fromSlot, toSlot);

			// notify handlers if items changing state
			if (fromSlotEquipped != toSlotEquipped)
			{
				if (toItem != null)
				{
					if (toSlotEquipped) // item was equipped
					{
						m_player.OnItemUnequipped(toItem, toSlot);
					}
					else
					{
						m_player.OnItemEquipped(toItem, toSlot);
					}
				}

				if (fromItem != null)
				{
					if (fromSlotEquipped) // item was equipped
					{
						m_player.OnItemUnequipped(fromItem, fromSlot);
					}
					else
					{
						m_player.OnItemEquipped(fromItem, fromSlot);
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if slot is equipped on player
		/// </summary>
		/// <param name="slot">The slot to check</param>
		/// <returns>true if slot is one of equipment slots and should add magical bonuses</returns>
		public virtual bool IsEquippedSlot(eInventorySlot slot)
		{
			// skip weapons. only active weapons should fire equip event, done in player.SwitchWeapon
			if (slot > eInventorySlot.DistanceWeapon || slot < eInventorySlot.RightHandWeapon)
			{
				foreach (eInventorySlot staticSlot in EQUIP_SLOTS)
				{
					if (slot == staticSlot)
						return true;
				}

				return false;
			}

			switch (slot)
			{
				case eInventorySlot.RightHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x00;

				case eInventorySlot.LeftHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0xF0) == 0x10;

				case eInventorySlot.TwoHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x02;

				case eInventorySlot.DistanceWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x03;
			}

			return false;
		}

		#endregion Combine/Exchange/Stack Items

		#region Encumberance
		
		public override int InventoryWeight
		{
			get
			{
				var weight = 0;
				IList<DbInventoryItem> items;

				lock (LockObject)
				{
					items = new List<DbInventoryItem>(m_items.Values);
				}
				
				foreach (var item in items)
				{
					if ((eInventorySlot) item.SlotPosition < eInventorySlot.FirstBackpack || (eInventorySlot)item.SlotPosition > eInventorySlot.LastBackpack)
						continue;
					var itemWeight = item.Weight;
					if (item.Description.Contains("Atlas ROG")) itemWeight = (int)(itemWeight * 0.75);
					weight += itemWeight;
				}
				
				return weight/10 + base.InventoryWeight;;
			}
		}

		#endregion

		#region Dyes

		protected virtual bool DyeItem(DbInventoryItem dye, DbInventoryItem objectToDye)
		{
			bool canApply = false;
			//TODO should not be tested via model

			int itemObjType = objectToDye.Object_Type;
			int itemItemType = objectToDye.Item_Type;

			switch (dye.Model)
			{
				case 229: //Dyes
					if (itemObjType == 32) //Cloth
					{
						canApply = true;
					}
					if (itemObjType == 41 && itemItemType == 26) // magical cloaks
					{
						canApply = true;
					}
					if (itemObjType == 41 && itemItemType == 8) // horse barding
					{
						canApply = true;
					}

					break;
				case 494: //Dye pack
					if (itemObjType == 33) //Leather
					{
						canApply = true;
					}
					break;
				case 495: //Dye pack
					if ((itemObjType == 42) // Shield
					    || (itemObjType == 34) // Studded
					    || (itemObjType == 35) // Chain
					    || (itemObjType == 36) // Plate
					    || (itemObjType == 37) // Reinforced
					    || (itemObjType == 38) // Scale
					    || (itemObjType == 45) // Instrument
					    || ((itemObjType == 41) && (itemItemType == 7))) // horse saddle
					{
						canApply = true;
					}
					break;
				case 538: //Dye pot
					if (itemObjType >= 1 && itemObjType <= 26)
					{
						canApply = true;
					}
					break;
			}

			if (canApply)
			{
				objectToDye.Color = dye.Color;
				objectToDye.Emblem = 0;
				RemoveCountFromStack(dye, 1);
                InventoryLogging.LogInventoryAction(m_player, "(dye)", eInventoryActionType.Other, dye.Template);
			}

			return canApply;
		}

		#endregion

		#region UpdateChangedSlots

		/// <summary>
		/// Updates changed slots, inventory is already locked.
		/// Inventory must be locked before invoking this method.
		/// </summary>
		protected override void UpdateChangedSlots()
		{
			
			lock (m_changedSlots)
			{
				var invSlots = m_changedSlots.ToList();
				var slotsToUpdate = new List<int>();
				foreach (var inv in invSlots)
				{
					slotsToUpdate.Add((int)inv);
				}
				m_player.Out.SendInventorySlotsUpdate(slotsToUpdate);
			}

			bool statsUpdated = false;
			bool appearanceUpdated = false;
			bool encumberanceUpdated = false;

			lock (InventorySlotLock)
			{
				foreach (eInventorySlot updatedSlot in m_changedSlots)
				{
					// update appearance if one of changed slots is visible
					if (!appearanceUpdated)
					{
						foreach (eInventorySlot visibleSlot in VISIBLE_SLOTS)
						{
							if (updatedSlot != visibleSlot)
								continue;
							
							appearanceUpdated = true;
							break;
						}
					}

					// update stats if equipped item has changed
					if (!statsUpdated && updatedSlot <= eInventorySlot.RightRing &&
					    updatedSlot >= eInventorySlot.RightHandWeapon)
					{
						statsUpdated = true;
					}

					// update encumberance if changed slot was in inventory or equipped
					if (!encumberanceUpdated &&
					    //					(updatedSlot >=(int)eInventorySlot.FirstVault && updatedSlot<=(int)eInventorySlot.LastVault) ||
					    (updatedSlot >= eInventorySlot.RightHandWeapon && updatedSlot <= eInventorySlot.RightRing) ||
					    (updatedSlot >= eInventorySlot.FirstBackpack && updatedSlot <= eInventorySlot.LastBackpack))
					{
						encumberanceUpdated = true;
					}
				}
			}
			
			if(appearanceUpdated)
				m_player.UpdateEquipmentAppearance();
				
			if(statsUpdated)
				m_player.Out.SendUpdateWeaponAndArmorStats();
				
			if(encumberanceUpdated)
				m_player.UpdateEncumberance();

			base.UpdateChangedSlots();
		}

		#endregion
	}
}
