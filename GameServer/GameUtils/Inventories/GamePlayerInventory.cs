using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
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
			lock (m_items)
			{
				try
				{
					m_items.Clear();

					// We only want to cache items in the players personal inventory and personal vault.
					// If we cache ALL items them all vault code must make sure to update cache, which is not ideal
					// in addition, a player with a housing vault may still have an item in cache that may have been
					// removed by another player with the appropriate house permission.  - Tolakram
					var filterBySlot = DB.Column("SlotPosition").IsLessOrEqualTo((int)EInventorySlot.LastVault).Or(DB.Column("SlotPosition").IsGreaterOrEqualTo(500).And(DB.Column("SlotPosition").IsLessThan(600)));
					var items = CoreDb<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(inventoryID).And(filterBySlot));

					foreach (DbInventoryItem item in items)
					{
						try
						{
							var itemSlot = (EInventorySlot)item.SlotPosition;

							if (item.CanUseEvery > 0)
							{
								item.SetCooldown();
							}

							if (GetValidInventorySlot((EInventorySlot)item.SlotPosition) == EInventorySlot.Invalid)
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
								    && item.Object_Type != (int)EObjectType.CompositeBow
								    && item.Object_Type != (int)EObjectType.Crossbow
								    && item.Object_Type != (int)EObjectType.Longbow
								    && item.Object_Type != (int)EObjectType.Fired
								    && item.Object_Type != (int)EObjectType.RecurvedBow)
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
					foreach (EInventorySlot slot in EQUIP_SLOTS)
					{
						// skip weapons. only active weapons should fire equip event, done in player.SwitchWeapon
						if (slot >= EInventorySlot.RightHandWeapon && slot <= EInventorySlot.DistanceWeapon)
							continue;

						DbInventoryItem item;

						if (m_items.TryGetValue(slot, out item))
						{
							m_player.Notify(PlayerInventoryEvent.ItemEquipped, this, new ItemEquippedArgs(item, slot));
						}
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
			lock (m_items) // Mannen 10:56 PM 10/30/2006 - Fixing every lock(this)
			{
				try
				{
					foreach (var item in m_items)
					{
						try
						{
							DbInventoryItem currentItem = item.Value;

							if (currentItem == null)
								continue;

							bool canPersist = true;
							GameInventoryItem gameItem = currentItem as GameInventoryItem;
							if (gameItem != null)
							{
								canPersist = gameItem.CanPersist;
							}

							if (canPersist == false)
								continue;

							if (GetValidInventorySlot((EInventorySlot) currentItem.SlotPosition) == EInventorySlot.Invalid)
							{
								if (Log.IsErrorEnabled)
									Log.Error("item's slot position is invalid. item slot=" + currentItem.SlotPosition + " id=" +
									          currentItem.ObjectId);

								continue;
							}

							if (currentItem.OwnerID != m_player.InternalID)
							{
								string itemOwner = currentItem.OwnerID ?? "(null)";

								if (Log.IsErrorEnabled)
									Log.Error("item owner id (" + itemOwner + ") not equals player ID (" + m_player.InternalID + "); item ID=" +
									          currentItem.ObjectId);

								continue;
							}

							if (currentItem.Dirty)
							{
								var realSlot = (int) item.Key;

								if (currentItem.SlotPosition != realSlot)
								{
									if (Log.IsErrorEnabled)
										Log.Error("Item slot and real slot position are different. Item slot=" + currentItem.SlotPosition +
										          " real slot=" + realSlot + " item ID=" + currentItem.ObjectId);
									currentItem.SlotPosition = realSlot; // just to be sure
								}

								// Check database to make sure player still owns this item before saving

								DbInventoryItem checkItem = GameServer.Database.FindObjectByKey<DbInventoryItem>(currentItem.ObjectId);

								if (checkItem == null || checkItem.OwnerID != m_player.InternalID)
								{
									if (checkItem != null)
									{
										Log.ErrorFormat("Item '{0}' : '{1}' does not have same owner id on save inventory.  Game Owner = '{2}' : '{3}', DB Owner = '{4}'", currentItem.Name, currentItem.ObjectId, m_player.Name, m_player.InternalID, checkItem.OwnerID);
									}
									else
									{
										Log.ErrorFormat("Item '{0}' : '{1}' not found in DBInventory for player '{2}'", currentItem.Name, currentItem.Id_nb, m_player.Name);
									}

									continue;
								}

								GameServer.Database.SaveObject(currentItem);
							}
						}
						catch (Exception e)
						{
							if (Log.IsErrorEnabled)
								Log.Error("Error saving inventory item: player=" + m_player.Name, e);
						}
					}

					return true;
				}
				catch (Exception e)
				{
					if (Log.IsErrorEnabled)
						Log.Error("Saving player inventory (" + m_player.Name + ")", e);

					return false;
				}
			}
		}

		#endregion Constructor/Declaration/LoadDatabase/SaveDatabase

		#region Add/Remove

		/// <summary>
		/// Adds an item to the inventory and DB
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool AddItem(EInventorySlot slot, DbInventoryItem item)
		{
			return AddItem(slot, item, true);
		}

		public override bool AddTradeItem(EInventorySlot slot, DbInventoryItem item)
		{
			return AddItem(slot, item, false);
		}

		protected bool AddItem(EInventorySlot slot, DbInventoryItem item, bool addObject)
		{
			int savePosition = item.SlotPosition;
			string saveOwnerID = item.OwnerID;

			if (!base.AddItem(slot, item))
				return false;

			item.OwnerID = m_player.InternalID;

			bool canPersist = true;
			GameInventoryItem gameItem = item as GameInventoryItem;
			if (gameItem != null)
			{
				canPersist = gameItem.CanPersist;
			}

			if (canPersist)
			{
				if (addObject)
				{
					if (GameServer.Database.AddObject(item) == false)
					{
						m_player.Out.SendMessage("Error adding item to the database, item may be lost!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						Log.ErrorFormat("Error adding item {0}:{1} for player {2} into the database during AddItem!", item.Id_nb, item.Name, m_player.Name);
						m_items.Remove(slot);
						item.SlotPosition = savePosition;
						item.OwnerID = saveOwnerID;
						return false;
					}
				}
				else
				{
					if (GameServer.Database.SaveObject(item) == false)
					{
						m_player.Out.SendMessage("Error saving item to the database, this item may be lost!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						Log.ErrorFormat("Error saving item {0}:{1} for player {2} into the database during AddItem!", item.Id_nb, item.Name, m_player.Name);
						m_items.Remove(slot);
						item.SlotPosition = savePosition;
						item.OwnerID = saveOwnerID;
						return false;
					}
				}
			}

			if (IsEquippedSlot((EInventorySlot)item.SlotPosition))
				m_player.Notify(PlayerInventoryEvent.ItemEquipped, this, new ItemEquippedArgs(item, EInventorySlot.Invalid));

			if (item is IGameInventoryItem)
			{
				(item as IGameInventoryItem).OnReceive(m_player);
			}

			return true;
		}

		public override bool RemoveItem(DbInventoryItem item)
		{
			return RemoveItem(item, true);
		}

		public override bool RemoveTradeItem(DbInventoryItem item)
		{
			return RemoveItem(item, false);
		}

		/// <summary>
		/// Removes an item from the inventory and DB
		/// </summary>
		/// <param name="item">the item to remove</param>
		/// <returns>true if successfull</returns>
		protected bool RemoveItem(DbInventoryItem item, bool deleteObject)
		{
			if (item == null)
				return false;

			if (item.OwnerID != m_player.InternalID)
			{
				if (Log.IsErrorEnabled)
					Log.Error(m_player.Name + ": PlayerInventory -> tried to remove item with wrong owner (" + (item.OwnerID ?? "null") +
					          ")\n\n" + Environment.StackTrace);
				return false;
			}

			int savePosition = item.SlotPosition;
			string saveOwnerID = item.OwnerID;


			var oldSlot = (EInventorySlot) item.SlotPosition;

			if (!base.RemoveItem(item))
				return false;

			bool canPersist = true;
			GameInventoryItem gameItem = item as GameInventoryItem;
			if (gameItem != null)
			{
				canPersist = gameItem.CanPersist;
			}

			if (canPersist)
			{
				if (deleteObject)
				{
					if (GameServer.Database.DeleteObject(item) == false)
					{
						m_player.Out.SendMessage("Error deleting item from the database, operation aborted!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						Log.ErrorFormat("Error deleting item {0}:{1} for player {2} from the database during RemoveItem!", item.Id_nb, item.Name, m_player.Name);
						m_items.Add(oldSlot, item);
						item.SlotPosition = savePosition;
						item.OwnerID = saveOwnerID;
						return false;
					}
				}
				else
				{
					if (GameServer.Database.SaveObject(item) == false)
					{
						m_player.Out.SendMessage("Error saving item to the database, operation aborted!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						Log.ErrorFormat("Error saving item {0}:{1} for player {2} to the database during RemoveItem!", item.Id_nb, item.Name, m_player.Name);
						m_items.Add(oldSlot, item);
						item.SlotPosition = savePosition;
						item.OwnerID = saveOwnerID;
						return false;
					}
				}
			}

			ITradeWindow window = m_player.TradeWindow;
			if (window != null)
				window.RemoveItemToTrade(item);

			if (oldSlot >= EInventorySlot.RightHandWeapon && oldSlot <= EInventorySlot.DistanceWeapon)
			{
				// if active weapon was destroyed
				if (m_player.ActiveWeapon == null)
				{
					m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
				}
				else
				{
					m_player.Notify(PlayerInventoryEvent.ItemUnequipped, this, new ItemUnequippedArgs(item, oldSlot));
				}
			}
			else if (oldSlot >= EInventorySlot.FirstQuiver && oldSlot <= EInventorySlot.FourthQuiver)
			{
				m_player.SwitchQuiver(EActiveQuiverSlot.None, true);
			}
			else if (IsEquippedSlot(oldSlot))
			{
				m_player.Notify(PlayerInventoryEvent.ItemUnequipped, this, new ItemUnequippedArgs(item, oldSlot));
			}

			if (item is IGameInventoryItem)
			{
				(item as IGameInventoryItem).OnLose(m_player);
			}

			return true;
		}

		/// <summary>
		/// Adds count of items to the inventory item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="count"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Removes one item from the inventory item
		/// </summary>
		/// <param name="item">the item to remove</param>
		/// <param name="count">the count of items to be removed from the stack</param>
		/// <returns>true one item removed</returns>
		public override bool RemoveCountFromStack(DbInventoryItem item, int count)
		{
			if (item != null && item.OwnerID != m_player.InternalID)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Item owner not equals inventory owner.\n\n" + Environment.StackTrace);

				return false;
			}

			return base.RemoveCountFromStack(item, count);
		}

		#endregion Add/Remove

		#region Get Inventory Informations

		/// <summary>
		/// Check if the slot is valid in this inventory
		/// </summary>
		/// <param name="slot">SlotPosition to check</param>
		/// <returns>the slot if it's valid or eInventorySlot.Invalid if not</returns>
		protected override EInventorySlot GetValidInventorySlot(EInventorySlot slot)
		{
			switch (slot)
			{
				case EInventorySlot.LastEmptyQuiver:
					slot = FindLastEmptySlot(EInventorySlot.FirstQuiver, EInventorySlot.FourthQuiver);
					break;
				case EInventorySlot.FirstEmptyQuiver:
					slot = FindFirstEmptySlot(EInventorySlot.FirstQuiver, EInventorySlot.FourthQuiver);
					break;
				case EInventorySlot.LastEmptyVault:
					slot = FindLastEmptySlot(EInventorySlot.FirstVault, EInventorySlot.LastVault);
					break;
				case EInventorySlot.FirstEmptyVault:
					slot = FindFirstEmptySlot(EInventorySlot.FirstVault, EInventorySlot.LastVault);
					break;
				case EInventorySlot.LastEmptyBackpack:
					slot = FindLastEmptySlot(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
					break;
				case EInventorySlot.FirstEmptyBackpack:
					slot = FindFirstEmptySlot(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
					break;
					// INVENTAIRE DES CHEVAUX
				case EInventorySlot.LastEmptyBagHorse:
					slot = FindLastEmptySlot(EInventorySlot.FirstBagHorse, EInventorySlot.LastBagHorse);
					break;
				case EInventorySlot.FirstEmptyBagHorse:
					slot = FindFirstEmptySlot(EInventorySlot.FirstBagHorse, EInventorySlot.LastBagHorse);
					break;
			}

			if ((slot >= EInventorySlot.FirstBackpack && slot <= EInventorySlot.LastBackpack)
			    //				|| ( slot >= eInventorySlot.Mithril && slot <= eInventorySlot.Copper ) // can't place items in money slots, is it?
			    || (slot >= EInventorySlot.HorseArmor && slot <= EInventorySlot.Horse)
			    || (slot >= EInventorySlot.FirstVault && slot <= EInventorySlot.LastVault)
			    || (slot >= EInventorySlot.HouseVault_First && slot <= EInventorySlot.HouseVault_Last)
			    || (slot >= EInventorySlot.Consignment_First && slot <= EInventorySlot.Consignment_Last)
			    || (slot == EInventorySlot.PlayerPaperDoll)
			    || (slot == EInventorySlot.Mythical)
			    // INVENTAIRE DES CHEVAUX
			    || (slot >= EInventorySlot.FirstBagHorse && slot <= EInventorySlot.LastBagHorse))
				return slot;


			return base.GetValidInventorySlot(slot);
		}

		#endregion Get Inventory Informations

		#region Move Item

		/// <summary>
		/// Moves an item from one slot to another
		/// </summary>
		/// <param name="fromSlot">First SlotPosition</param>
		/// <param name="toSlot">Second SlotPosition</param>
		/// <param name="itemCount">How many items to move</param>
		/// <returns>true if items switched successfully</returns>
		public override bool MoveItem(EInventorySlot fromSlot, EInventorySlot toSlot, int itemCount)
		{
			if (!m_player.IsAlive)
			{
				m_player.Out.SendMessage("You can't change your inventory when dead!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				m_player.Out.SendInventorySlotsUpdate(null);

				return false;
			}

			bool valid = true;
			DbInventoryItem fromItem, toItem;
			EInventorySlot[] updatedSlots;

			lock (m_items) // Mannen 10:56 PM 10/30/2006 - Fixing every lock(this)
			{
				fromSlot = GetValidInventorySlot(fromSlot);
				toSlot = GetValidInventorySlot(toSlot);

				if (fromSlot == EInventorySlot.Invalid || toSlot == EInventorySlot.Invalid)
				{
					ChatUtil.SendDebugMessage(m_player, string.Format("Invalid slot from: {0}, to: {1}!", fromSlot, toSlot));
					m_player.Out.SendInventorySlotsUpdate(null);
					return false;
				}

				// just change active weapon if placed in same slot
				if (fromSlot == toSlot)
				{
					switch (toSlot)
					{
						case EInventorySlot.RightHandWeapon:
						case EInventorySlot.LeftHandWeapon:
							m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
							return false;
						case EInventorySlot.TwoHandWeapon:
							m_player.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
							return false;
						case EInventorySlot.DistanceWeapon:
							m_player.SwitchWeapon(EActiveWeaponSlot.Distance);
							return false;
					}
				}

				m_items.TryGetValue(fromSlot, out fromItem);
				m_items.TryGetValue(toSlot, out toItem);

				updatedSlots = new EInventorySlot[2];
				updatedSlots[0] = fromSlot;
				updatedSlots[1] = toSlot;

				if (fromItem == toItem || fromItem == null)
					valid = false;
				
				/*************** Horse Inventory **************/
				if (((toSlot >= EInventorySlot.FirstBagHorse && toSlot <= EInventorySlot.LastBagHorse) ||
				     (fromSlot >= EInventorySlot.FirstBagHorse && fromSlot <= EInventorySlot.LastBagHorse)))
				{
					if (fromSlot == EInventorySlot.Horse)
					{
						// don't let player move active horse to a horse bag, which will disable all bags!
						m_player.Out.SendMessage("You can't move your active horse into a saddlebag!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						valid = false;
					}

					if (valid && m_player.Client.Account.PrivLevel == 1)
					{
						if (m_player.CanUseHorseInventorySlot((int)fromSlot) == false || m_player.CanUseHorseInventorySlot((int)toSlot) == false)
						{
							valid = false;
						}
					}
				}
				/***********************************************/
				
				if (valid && toItem != null && fromItem.Object_Type == (int) EObjectType.Poison &&
				    GlobalConstants.IsWeapon(toItem.Object_Type))
				{
					m_player.ApplyPoison(fromItem, toItem);
					m_player.Out.SendInventorySlotsUpdate(null);

					return false;
				}

				// TODO test & remove the following
				// graveen = fix for allowedclasses is empty or null
				if (fromItem != null && string.IsNullOrEmpty(fromItem.AllowedClasses))
				{
					fromItem.AllowedClasses = "";
				}

				if (toItem != null && string.IsNullOrEmpty(toItem.AllowedClasses))
				{
					toItem.AllowedClasses = "";
				}

                bool noactiveslot = false;
                //Andraste - Vico / fixing a bugexploit : when player switch from his char slot to an inventory slot, allowedclasses were not checked
                if (valid && !string.IsNullOrEmpty(fromItem.AllowedClasses))
                {

                    if (toSlot >= EInventorySlot.MaxEquipable)
                        noactiveslot = true;

                    if (!(toSlot >= EInventorySlot.FirstBackpack && toSlot <= EInventorySlot.LastBackpack) && !noactiveslot)
                    // but we allow the player to switch the item inside his inventory (check only char slots)
                    {
                        valid = false;
                        foreach (string allowed in Util.SplitCSV(fromItem.AllowedClasses, true))
                        {
                            if (m_player.CharacterClass.ID.ToString() == allowed || m_player.Client.Account.PrivLevel > 1)
                            {
                                valid = true;
                                break;
                            }

                        }

                        if (!valid)
                        {
                            m_player.Out.SendMessage("Your class cannot use this item!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                }

                if (valid && toItem != null && !string.IsNullOrEmpty(toItem.AllowedClasses))
                {

                    if (toSlot >= EInventorySlot.MaxEquipable)
                        noactiveslot = true;

                    if (!(fromSlot >= EInventorySlot.FirstBackpack && fromSlot <= EInventorySlot.LastBackpack) && !noactiveslot)
                    // but we allow the player to switch the item inside his inventory (check only char slots)
                    {
                        valid = false;
                        foreach (string allowed in Util.SplitCSV(toItem.AllowedClasses, true))
                        {
                            if (m_player.CharacterClass.ID.ToString() == allowed || m_player.Client.Account.PrivLevel > 1)
                            {
                                valid = true;
                                break;
                            }
                        }

                        if (!valid)
                        {
                            m_player.Out.SendMessage("Your class cannot use this item!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        }
                    }
                }

				if (valid)
				{
					switch (toSlot)
					{
							//Andraste - Vico : Mythical
						case EInventorySlot.Mythical:
							if (fromItem.Item_Type != (int) EInventorySlot.Mythical)
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}

							if (valid && fromItem.Type_Damage > m_player.ChampionLevel)
							{
								valid = false;
								m_player.Out.SendMessage(
									"You can't use " + fromItem.GetName(0, true) + " , you should increase your champion level.",
									EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
							//horse slots
						case EInventorySlot.HorseBarding:
							if (fromItem.Item_Type != (int) EInventorySlot.HorseBarding)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + fromItem.GetName(0, true) + " in your active barding slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.HorseArmor:
							if (fromItem.Item_Type != (int) EInventorySlot.HorseArmor)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + fromItem.GetName(0, true) + " in your active horse armor slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.Horse:
							if (fromItem.Item_Type != (int) EInventorySlot.Horse)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + fromItem.GetName(0, true) + " in your active mount slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
							//weapon slots
						case EInventorySlot.RightHandWeapon:
							if (fromItem.Object_Type == (int) EObjectType.Shield //shield can't be used in right hand slot
							    ||
							    (fromItem.Item_Type != (int) EInventorySlot.RightHandWeapon
							     //right hand weapons can be used in right hand slot
							     && fromItem.Item_Type != (int) EInventorySlot.LeftHandWeapon))
								//left hand weapons can be used in right hand slot
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(fromItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.TwoHandWeapon:
							if (fromItem.Object_Type == (int) EObjectType.Shield //shield can't be used in 2h slot
							    || (fromItem.Item_Type != (int) EInventorySlot.RightHandWeapon //right hand weapons can be used in 2h slot
							        && fromItem.Item_Type != (int) EInventorySlot.LeftHandWeapon //left hand weapons can be used in 2h slot
							        && fromItem.Item_Type != (int) EInventorySlot.TwoHandWeapon //2h weapons can be used in 2h slot
							        && fromItem.Object_Type != (int) EObjectType.Instrument)) //instruments can be used in 2h slot
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(fromItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftHandWeapon:
							if (fromItem.Item_Type != (int) toSlot ||
							    (fromItem.Object_Type != (int) EObjectType.Shield && !m_player.attackComponent.CanUseLefthandedWeapon))
								//shield can be used only in left hand slot
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(fromItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.DistanceWeapon:
							//m_player.Out.SendDebugMessage("From: {0} to {1} ItemType={2}",fromSlot,toSlot,fromItem.Item_Type);
							if (fromItem.Item_Type != (int) toSlot && fromItem.Object_Type != (int) EObjectType.Instrument)
								//instruments can be used in ranged slot
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(fromItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;

							//armor slots
						case EInventorySlot.HeadArmor:
						case EInventorySlot.HandsArmor:
						case EInventorySlot.FeetArmor:
						case EInventorySlot.TorsoArmor:
						case EInventorySlot.LegsArmor:
						case EInventorySlot.ArmsArmor:
							if (fromItem.Item_Type != (int) toSlot)
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(fromItem.Template ))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in wearing this armor type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;

						case EInventorySlot.Jewellery:
						case EInventorySlot.Cloak:
						case EInventorySlot.Neck:
						case EInventorySlot.Waist:
							if (fromItem.Item_Type != (int) toSlot)
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftBracer:
						case EInventorySlot.RightBracer:
							if (fromItem.Item_Type != Slot.RIGHTWRIST && fromItem.Item_Type != Slot.LEFTWRIST)
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftRing:
						case EInventorySlot.RightRing:
							if (fromItem.Item_Type != Slot.LEFTRING && fromItem.Item_Type != Slot.RIGHTRING)
							{
								valid = false;
								m_player.Out.SendMessage(fromItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.FirstQuiver:
						case EInventorySlot.SecondQuiver:
						case EInventorySlot.ThirdQuiver:
						case EInventorySlot.FourthQuiver:
							if (fromItem.Object_Type != (int) EObjectType.Arrow && fromItem.Object_Type != (int) EObjectType.Bolt)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put your " + fromItem.Name + " in your quiver!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
					}
					//"The Lute of the Initiate must be readied in the 2-handed slot!"
				}

				if (valid && (fromItem.Realm > 0 && (int) m_player.Realm != fromItem.Realm) &&
				    (toSlot >= EInventorySlot.HorseArmor && toSlot <= EInventorySlot.HorseBarding))
				{
					if (m_player.Client.Account.PrivLevel == 1)
					{
						valid = false;
					}

					m_player.Out.SendMessage("You cannot put an item from this realm!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}

				if (valid && toItem != null)
				{
					switch (fromSlot)
					{
							//Andraste
						case EInventorySlot.Mythical:
							if (toItem.Item_Type != (int) EInventorySlot.Mythical)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}

							if (valid && toItem.Type_Damage > m_player.ChampionLevel)
							{
								valid = false;
								m_player.Out.SendMessage(
									"You can't use " + toItem.GetName(0, true) + " , you should increase your champion level.", EChatType.CT_System,
									EChatLoc.CL_SystemWindow);
							}
							break;
							//horse slots
						case EInventorySlot.HorseBarding:
							if (toItem.Item_Type != (int) EInventorySlot.HorseBarding)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + toItem.GetName(0, true) + " in your active barding slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.HorseArmor:
							if (toItem.Item_Type != (int) EInventorySlot.HorseArmor)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + toItem.GetName(0, true) + " in your active horse armor slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.Horse:
							if (toItem.Item_Type != (int) EInventorySlot.Horse)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put " + toItem.GetName(0, true) + " in your active mount slot!",
								                         EChatType.CT_System, EChatLoc.CL_SystemWindow);
							}
							break;
							//weapon slots
						case EInventorySlot.RightHandWeapon:
							if (toItem.Object_Type == (int) EObjectType.Shield //shield can't be used in right hand slot
							    ||
							    (toItem.Item_Type != (int) EInventorySlot.RightHandWeapon //right hand weapons can be used in right hand slot
							     && toItem.Item_Type != (int) EInventorySlot.LeftHandWeapon))
								//left hand weapons can be used in right hand slot
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(toItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.TwoHandWeapon:
							if (toItem.Object_Type == (int) EObjectType.Shield //shield can't be used in 2h slot
							    || (toItem.Item_Type != (int) EInventorySlot.RightHandWeapon //right hand weapons can be used in 2h slot
							        && toItem.Item_Type != (int) EInventorySlot.LeftHandWeapon //left hand weapons can be used in 2h slot
							        && toItem.Item_Type != (int) EInventorySlot.TwoHandWeapon //2h weapons can be used in 2h slot
							        && toItem.Object_Type != (int) EObjectType.Instrument)) //instruments can be used in 2h slot
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(toItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftHandWeapon:
							if (toItem.Item_Type != (int) fromSlot ||
							    (toItem.Object_Type != (int) EObjectType.Shield && !m_player.attackComponent.CanUseLefthandedWeapon))
								//shield can be used only in left hand slot
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(toItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.DistanceWeapon:
							if (toItem.Item_Type != (int) fromSlot && toItem.Object_Type != (int) EObjectType.Instrument)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(toItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in using this weapon type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;

							//armor slots
						case EInventorySlot.HeadArmor:
						case EInventorySlot.HandsArmor:
						case EInventorySlot.FeetArmor:
						case EInventorySlot.TorsoArmor:
						case EInventorySlot.LegsArmor:
						case EInventorySlot.ArmsArmor:
							if (toItem.Item_Type != (int) fromSlot)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							else if (!m_player.HasAbilityToUseItem(toItem.Template))
							{
								valid = false;
								m_player.Out.SendMessage("You have no skill in wearing this armor type!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;

						case EInventorySlot.Jewellery:
						case EInventorySlot.Cloak:
						case EInventorySlot.Neck:
						case EInventorySlot.Waist:
							if (toItem.Item_Type != (int) fromSlot)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftBracer:
						case EInventorySlot.RightBracer:
							if (toItem.Item_Type != Slot.RIGHTWRIST && toItem.Item_Type != Slot.LEFTWRIST)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.LeftRing:
						case EInventorySlot.RightRing:
							if (toItem.Item_Type != Slot.LEFTRING && toItem.Item_Type != Slot.RIGHTRING)
							{
								valid = false;
								m_player.Out.SendMessage(toItem.GetName(0, true) + " can't go there!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
						case EInventorySlot.FirstQuiver:
						case EInventorySlot.SecondQuiver:
						case EInventorySlot.ThirdQuiver:
						case EInventorySlot.FourthQuiver:
							if (toItem.Object_Type != (int) EObjectType.Arrow && toItem.Object_Type != (int) EObjectType.Bolt)
							{
								valid = false;
								m_player.Out.SendMessage("You can't put your " + toItem.Name + " in your quiver!", EChatType.CT_System,
								                         EChatLoc.CL_SystemWindow);
							}
							break;
					}
				}

				if (valid)
				{
					base.MoveItem(fromSlot, toSlot, itemCount);
				}
			}

			if (valid)
			{
				foreach (EInventorySlot updatedSlot in updatedSlots)
				{
					if ((updatedSlot >= EInventorySlot.RightHandWeapon && updatedSlot <= EInventorySlot.DistanceWeapon)
					    || (updatedSlot >= EInventorySlot.FirstQuiver && updatedSlot <= EInventorySlot.FourthQuiver))
					{
						m_player.attackComponent.StopAttack();
						break;
					}
				}

				ITradeWindow window = m_player.TradeWindow;
				if (window != null)
				{
					if (toItem != null)
						window.RemoveItemToTrade(toItem);
					window.RemoveItemToTrade(fromItem);
				}


				// activate weapon slot if moved to it
				switch (toSlot)
				{
					case EInventorySlot.RightHandWeapon:
						m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
						break;
					case EInventorySlot.TwoHandWeapon:
						m_player.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						break;
					case EInventorySlot.DistanceWeapon:
						m_player.SwitchWeapon(EActiveWeaponSlot.Distance);
						break;
					case EInventorySlot.LeftHandWeapon:
						if (m_player.ActiveWeaponSlot != EActiveWeaponSlot.Distance)
							m_player.SwitchWeapon(m_player.ActiveWeaponSlot);
						else m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
						break;
					case EInventorySlot.FirstQuiver:
						m_player.SwitchQuiver(EActiveQuiverSlot.First, true);
						break;
					case EInventorySlot.SecondQuiver:
						m_player.SwitchQuiver(EActiveQuiverSlot.Second, true);
						break;
					case EInventorySlot.ThirdQuiver:
						m_player.SwitchQuiver(EActiveQuiverSlot.Third, true);
						break;
					case EInventorySlot.FourthQuiver:
						m_player.SwitchQuiver(EActiveQuiverSlot.Fourth, true);
						break;


					default:
						// change active weapon if moved from active slot
						if (fromSlot == EInventorySlot.RightHandWeapon &&
						    m_player.ActiveWeaponSlot == EActiveWeaponSlot.Standard)
						{
							m_player.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						}
						else if (fromSlot == EInventorySlot.TwoHandWeapon &&
						         m_player.ActiveWeaponSlot == EActiveWeaponSlot.TwoHanded)
						{
							m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
						}
						else if (fromSlot == EInventorySlot.DistanceWeapon &&
						         m_player.ActiveWeaponSlot == EActiveWeaponSlot.Distance)
						{
							m_player.SwitchWeapon(EActiveWeaponSlot.Standard);
						}
						else if (fromSlot == EInventorySlot.LeftHandWeapon &&
						         (m_player.ActiveWeaponSlot == EActiveWeaponSlot.TwoHanded ||
						          m_player.ActiveWeaponSlot == EActiveWeaponSlot.Standard))
						{
							m_player.SwitchWeapon(m_player.ActiveWeaponSlot);
						}

						if (fromSlot >= EInventorySlot.FirstQuiver && fromSlot <= EInventorySlot.FourthQuiver)
						{
							m_player.SwitchQuiver(EActiveQuiverSlot.None, true);
						}

						break;
				}
			}

			m_player.Out.SendInventorySlotsUpdate(null);

			return valid;
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
			    fromItem.SlotPosition < (int) EInventorySlot.FirstBackpack ||
			    fromItem.SlotPosition > (int) EInventorySlot.LastBackpack)
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
		protected override bool StackItems(EInventorySlot fromSlot, EInventorySlot toSlot, int itemCount)
		{
			DbInventoryItem fromItem;
			DbInventoryItem toItem;

			m_items.TryGetValue(fromSlot, out fromItem);
			m_items.TryGetValue(toSlot, out toItem);

			if ((toSlot > EInventorySlot.HorseArmor && toSlot < EInventorySlot.FirstQuiver)
			    || (toSlot > EInventorySlot.FourthQuiver && toSlot < EInventorySlot.FirstBackpack))
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
				GameServer.Database.AddObject(newItem);

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
		protected override bool ExchangeItems(EInventorySlot fromSlot, EInventorySlot toSlot)
		{
			DbInventoryItem fromItem;
			DbInventoryItem toItem;

			m_items.TryGetValue(fromSlot, out fromItem);
			m_items.TryGetValue(toSlot, out toItem);

			bool fromSlotEquipped = IsEquippedSlot(fromSlot);
			bool toSlotEquipped = IsEquippedSlot(toSlot);

			if (base.ExchangeItems(fromSlot, toSlot) == false)
			{

			}

			if (fromItem != null && fromItem.Id_nb != DbInventoryItem.BLANK_ITEM)
			{
				if (GameServer.Database.SaveObject(fromItem) == false)
				{
				}
			}
			if (toItem != null && toItem != fromItem && toItem.Id_nb != DbInventoryItem.BLANK_ITEM)
			{
				if (GameServer.Database.SaveObject(toItem) == false)
				{
				}
			}

			// notify handlers if items changing state
			if (fromSlotEquipped != toSlotEquipped)
			{
				if (toItem != null)
				{
					if (toSlotEquipped) // item was equipped
					{
						m_player.Notify(PlayerInventoryEvent.ItemUnequipped, this, new ItemUnequippedArgs(toItem, toSlot));
					}
					else
					{
						m_player.Notify(PlayerInventoryEvent.ItemEquipped, this, new ItemEquippedArgs(toItem, toSlot));
					}
				}

				if (fromItem != null)
				{
					if (fromSlotEquipped) // item was equipped
					{
						m_player.Notify(PlayerInventoryEvent.ItemUnequipped, this, new ItemUnequippedArgs(fromItem, fromSlot));
					}
					else
					{
						m_player.Notify(PlayerInventoryEvent.ItemEquipped, this, new ItemEquippedArgs(fromItem, fromSlot));
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
		public virtual bool IsEquippedSlot(EInventorySlot slot)
		{
			// skip weapons. only active weapons should fire equip event, done in player.SwitchWeapon
			if (slot > EInventorySlot.DistanceWeapon || slot < EInventorySlot.RightHandWeapon)
			{
				foreach (EInventorySlot staticSlot in EQUIP_SLOTS)
				{
					if (slot == staticSlot)
						return true;
				}

				return false;
			}

			switch (slot)
			{
				case EInventorySlot.RightHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x00;

				case EInventorySlot.LeftHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0xF0) == 0x10;

				case EInventorySlot.TwoHandWeapon:
					return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x02;

				case EInventorySlot.DistanceWeapon:
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

				lock (m_items) // Mannen 10:56 PM 10/30/2006 - Fixing every lock(this)
				{
					items = new List<DbInventoryItem>(m_items.Values);
				}
				
				foreach (var item in items)
				{
					if ((EInventorySlot) item.SlotPosition < EInventorySlot.FirstBackpack || (EInventorySlot)item.SlotPosition > EInventorySlot.LastBackpack)
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
                InventoryLogging.LogInventoryAction(m_player, "(dye)", EInventoryActionType.Other, dye.Template);
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
				foreach (EInventorySlot updatedSlot in m_changedSlots)
				{
					// update appearance if one of changed slots is visible
					if (!appearanceUpdated)
					{
						foreach (EInventorySlot visibleSlot in VISIBLE_SLOTS)
						{
							if (updatedSlot != visibleSlot)
								continue;
							
							appearanceUpdated = true;
							break;
						}
					}

					// update stats if equipped item has changed
					if (!statsUpdated && updatedSlot <= EInventorySlot.RightRing &&
					    updatedSlot >= EInventorySlot.RightHandWeapon)
					{
						statsUpdated = true;
					}

					// update encumberance if changed slot was in inventory or equipped
					if (!encumberanceUpdated &&
					    //					(updatedSlot >=(int)eInventorySlot.FirstVault && updatedSlot<=(int)eInventorySlot.LastVault) ||
					    (updatedSlot >= EInventorySlot.RightHandWeapon && updatedSlot <= EInventorySlot.RightRing) ||
					    (updatedSlot >= EInventorySlot.FirstBackpack && updatedSlot <= EInventorySlot.LastBackpack))
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
