using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.SalvageCalc;
using DOL.Language;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// The class holding all salvage functions
	/// </summary>
	public class Salvage
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#region Declaration

		/// <summary>
		/// The SalvageYield entry for the item being salvaged
		/// </summary>
		protected const string SALVAGE_YIELD = "SALVAGE_YIELD";

		/// <summary>
		/// The item being salvaged
		/// </summary>
		protected const string SALVAGED_ITEM = "SALVAGED_ITEM";
		
		protected const string SALVAGE_QUEUE = "SALVAGE_QUEUE";

		#endregion

		#region First call function and callback

		/// <summary>
		/// Begin salvaging an inventory item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="player"></param>
		/// <returns></returns>
		public static int BeginWork(GamePlayer player, DbInventoryItem item)
		{
            DbSalvageYield salvageYield = null;

			if (!IsAllowedToBeginWork(player, item))
			{
				return 0;
			}

			// int salvageLevel = CraftingMgr.GetItemCraftLevel(item) / 100;
			// if(salvageLevel > 9) salvageLevel = 9; // max 9

			var whereClause = WhereClause.Empty;

			// if (item.SalvageYieldID == 0)
			// {
			// 	whereClause = DB.Column("ObjectType").IsEqualTo(item.Object_Type).And(DB.Column("SalvageLevel").IsEqualTo(salvageLevel));
			// }
			// else
			// {
			// 	whereClause = DB.Column("ID").IsEqualTo(item.SalvageYieldID);
			// }
			//
			// if (ServerProperties.Properties.USE_SALVAGE_PER_REALM)
			// {
			// 	whereClause = whereClause.And(DB.Column("Realm").IsEqualTo((int)eRealm.None).Or(DB.Column("Realm").IsEqualTo(item.Realm)));
			// }
			if (item.SalvageYieldID > 0)
			{
				// salvageYield = new SalvageYield();
				whereClause = DB.Column("ID").IsEqualTo(item.SalvageYieldID);
				
				salvageYield = DOLDB<DbSalvageYield>.SelectObject(whereClause);
				DbItemTemplate material = null;
   
				if (salvageYield != null && string.IsNullOrEmpty(salvageYield.MaterialId_nb) == false)
				{
					material = GameServer.Database.FindObjectByKey<DbItemTemplate>(salvageYield.MaterialId_nb);
   
					if (material == null)
					{
						player.Out.SendMessage("Can't find material (" + material.Id_nb + ") needed to salvage this item!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						log.ErrorFormat("Salvage Error for ID: {0}:  Material not found: {1}", salvageYield.ID, material.Id_nb);
					}
				}
   
				if (material == null)
				{
					if (salvageYield == null && item.SalvageYieldID > 0)
					{
						player.Out.SendMessage("This items salvage recipe (" + item.SalvageYieldID + ") not implemented yet.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						log.ErrorFormat("SalvageYield ID {0} not found for item: {1}", item.SalvageYieldID, item.Name);
					}
					else if (salvageYield == null)
					{
						player.Out.SendMessage("Salvage recipe not found for this item.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						log.ErrorFormat("Salvage Lookup Error: ObjectType: {0}, Item: {1}", item.Object_Type, item.Name);
					}
					return 0;
				}
				// if (salvageYield == null)
				// {
				// 	player.Out.SendMessage("Can't find database entry (" + item.SalvageYieldID + ") for salvage ID, bypassing database value!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				// 	log.ErrorFormat("Salvage Error for salvageYield ID: {0}:  Entry not found, bypassing database entry", item.SalvageYieldID);
				// 	item.SalvageYieldID = 0;
				// }
				if (string.IsNullOrEmpty(salvageYield.MaterialId_nb))
				{
					player.Out.SendMessage("MaterialId_nb is null for (" + item.Name + ") salvageYield ID!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					log.ErrorFormat("Salvage Error for item: {0}:  MaterialId_nb is null", salvageYield.ID);
					return 0;
				}
				material = GameServer.Database.FindObjectByKey<DbItemTemplate>(salvageYield.MaterialId_nb);
				if (material == null)
				{
					player.Out.SendMessage("Can't find material (" + salvageYield.MaterialId_nb + ") needed to salvage this item!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					log.ErrorFormat("Salvage Error for ID: {0}:  Material not found", salvageYield.ID);
					return 0;
				}
				if (player.Client.Account.PrivLevel != 1)
				{
					player.Out.SendDebugMessage("DATABASE: SALVAGEYIELD ID " + salvageYield.ID);
				}
			}
			else
			{
				var sCalc = new SalvageCalculator();
				var ReturnSalvage = sCalc.GetSalvage(player, item);
				salvageYield = new DbSalvageYield();
				salvageYield.Count = ReturnSalvage.Count;
				salvageYield.MaterialId_nb = (string) ReturnSalvage.ID;
			}

			if (salvageYield.MaterialId_nb == string.Empty)
			{
				player.Out.SendMessage("No material set for this item", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return 0;
			}
			
			//Calculate a penalty based on players secondary crafting skill level
			salvageYield.Count = salvageYield.Count < 1 ? 0 : GetYieldPenalty(player, item, salvageYield.Count);

			if (player.IsMoving || player.IsStrafing)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.InterruptSalvage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return 0;
			}

			player.Stealth(false);
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.BeginSalvage", item.Template.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			if (salvageYield.Count < 1)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Template.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return 0;
			}

			player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.Salvaging", item.Name), salvageYield.Count);
			player.CraftTimer = new ECSGameTimer(player)
			{
				Callback = new ECSGameTimer.ECSTimerCallback(Proceed)
			};
			player.CraftTimer.Properties.SetProperty(AbstractCraftingSkill.PLAYER_CRAFTER, player);
			player.CraftTimer.Properties.SetProperty(SALVAGED_ITEM, item);
			player.CraftTimer.Properties.SetProperty(SALVAGE_YIELD, salvageYield);

			player.CraftTimer.Start(salvageYield.Count * 1000);
			return 1;
		}
		
		 public static int GetYieldPenalty(GamePlayer player, DbInventoryItem item, int SalvageCount)
        {
            int Multiplier = 0;
            int ReturnCount = SalvageCount;

            string iType = string.Empty;

            // if (item.IsCrafted)
            // {
            //     Multiplier = ServerProperties.Properties.SALVAGE_CRAFT_ITEM_MULTIPLIER;
            //     iType = "SALVAGE_CRAFT_ITEM_MULTIPLIER= %";
            // }
            // else
            // {
            //     Multiplier = ServerProperties.Properties.SALVAGE_ITEM_MULTIPLIER;
            //     iType = "SALVAGE_ITEM_MULTIPLIER= %";
            // }

            //The percentage of material to return if player does not meet the requirments
            Multiplier = Multiplier < 1 ? 1 : Multiplier;//Not less then 0
            Multiplier = Multiplier > 99 ? 100 : Multiplier;//Not more then 100

            //Magic items cannot be salvaged so give them cloth value
            item.Object_Type = item.Object_Type == 41 ? 32 : item.Object_Type;
            
            int Percent = (int) player.GetCraftingSkillValue(CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item)) * 100 / CraftingMgr.GetItemCraftLevel(item);
            Percent = Percent > 99 ? 100 : Percent;

            if (Percent < Multiplier) //Multiplier will never be below 0%
            {
                ReturnCount = (int)(ReturnCount * Multiplier) / 100 < 1 ? 1 : (ReturnCount * Multiplier / 100);
                if (player.Client.Account.PrivLevel != 1)
                {
                    player.Out.SendDebugMessage("SkillBelow = true " + iType + Multiplier + " PlayerSkill= %" + Percent + " Returning " + ReturnCount + " of " + SalvageCount);
                }
                return ReturnCount;
            }

            ReturnCount = (int)(ReturnCount * Percent) / 100 < 1 ? 1 : (ReturnCount * Percent) / 100;
            if (player.Client.Account.PrivLevel != 1)
            {
                player.Out.SendDebugMessage("SkillBelow = false " + iType + Multiplier + " PlayerSkill= %" + Percent + " Returning " + ReturnCount + " of " + SalvageCount);
            }
            return ReturnCount;
        }
		
		public static int BeginWorkList(GamePlayer player, IList<DbInventoryItem> itemList)
		{
			player.TempProperties.SetProperty(SALVAGE_QUEUE,itemList);
			player.CraftTimer?.Stop();
			player.Out.SendCloseTimerWindow();
			if (itemList == null || itemList.Count == 0) return 0;
			return BeginWork(player, itemList[0]);
		}

		/// <summary>
		/// Begin salvaging a siege weapon
		/// </summary>
		/// <param name="player"></param>
		/// <param name="siegeWeapon"></param>
		/// <returns></returns>
		public static int BeginWork(GamePlayer player, GameSiegeWeapon siegeWeapon)
		{
			if (siegeWeapon == null)
				return 0;
			// Galenas
			siegeWeapon.ReleaseControl();
			siegeWeapon.RemoveFromWorld();
			bool error = false;
			var recipe = DOLDB<DbCraftedItem>.SelectObject(DB.Column("Id_nb").IsEqualTo(siegeWeapon.ItemId));

			if (recipe == null)
            {
				player.Out.SendMessage("Error retrieving salvage data!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Error("Salvage Siege Error: DBCraftedItem is null for" + siegeWeapon.ItemId);
				return 1;
            }

			var rawMaterials = DOLDB<DbCraftedXItem>.SelectObjects(DB.Column("CraftedItemId_nb").IsEqualTo(recipe.Id_nb));

			if (rawMaterials == null || rawMaterials.Count == 0)
            {
				player.Out.SendMessage("No raw materials provided for this siege weapon!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Error("Salvage Siege Error: No Raw Materials found for " + siegeWeapon.ItemId);
				return 1;
            }

            if (player.IsCrafting || player.IsSalvagingOrRepairing)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.EndCurrentAction"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return 0;
            }
			DbInventoryItem item;
			DbItemTemplate template;
			foreach (DbCraftedXItem material in rawMaterials)
			{
				template = GameServer.Database.FindObjectByKey<DbItemTemplate>(material.IngredientId_nb);

				if (template == null)
				{
					player.Out.SendMessage("Missing raw material " + material.IngredientId_nb + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					log.Error("Salvage Siege Error: Raw Material not found " + material.IngredientId_nb);
					return 1;
				}

				item = GameInventoryItem.Create(template);
				item.Count = material.Count;
				if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
				{
					error = true;
					break;
				}
				InventoryLogging.LogInventoryAction("(salvage)", player, eInventoryActionType.Craft, item.Template, item.Count);
			}

			if (error)
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoRoom"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			return 1;
		}

		/// <summary>
		/// Called when craft time is finished
		/// </summary>
		/// <param name="timer"></param>
		/// <returns></returns>
		protected static int Proceed(ECSGameTimer timer)
		{
			GamePlayer player = timer.Properties.GetProperty<GamePlayer>(AbstractCraftingSkill.PLAYER_CRAFTER);
			DbInventoryItem itemToSalvage = timer.Properties.GetProperty<DbInventoryItem>(SALVAGED_ITEM);
			DbSalvageYield yield = timer.Properties.GetProperty<DbSalvageYield>(SALVAGE_YIELD);
			IList<DbInventoryItem> itemList = player.TempProperties.GetProperty<IList<DbInventoryItem>>(SALVAGE_QUEUE);
			int materialCount = yield.Count;

			if (player == null || itemToSalvage == null || yield == null || materialCount == 0)
			{
				player.Out.SendMessage("Error retrieving salvage data for this item!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Error("Salvage: There was a problem getting back salvage info from the craft timer.");
				return 0;
			}

			DbItemTemplate rawMaterial = null;

			if (string.IsNullOrEmpty(yield.MaterialId_nb) == false)
			{
				rawMaterial = GameServer.Database.FindObjectByKey<DbItemTemplate>(yield.MaterialId_nb);
			}

			if (rawMaterial == null)
			{
				player.Out.SendMessage("Error finding the raw material needed to salvage this item!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Error("Salvage: Error finding raw material " + yield.MaterialId_nb);
				return 0;
			}

			player.CraftTimer?.Stop();
			player.Out.SendCloseTimerWindow();

			if (!player.Inventory.RemoveItem(itemToSalvage)) // clean the free of the item to salvage
			{
				player.Out.SendMessage("Error finding the item to salvage!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return 0;
			}

			InventoryLogging.LogInventoryAction(player, "(salvage)", eInventoryActionType.Craft, itemToSalvage.Template, itemToSalvage.Count);

			Dictionary<int, int> changedSlots = new Dictionary<int, int>(5); // value: < 0 = new item count; > 0 = add to old
			lock (player.Inventory.Lock)
			{
				int count = materialCount;
				foreach (DbInventoryItem item in player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					if (item == null) continue;
					if (item.Id_nb != rawMaterial.Id_nb) continue;
					if (item.Count >= item.MaxCount) continue;

					int countFree = item.MaxCount - item.Count;
					if (count > countFree)
					{
						changedSlots.Add(item.SlotPosition, countFree); // existing item should be changed
						count -= countFree;
					}
					else
					{
						changedSlots.Add(item.SlotPosition, count); // existing item should be changed
						count = 0;
						break;
					}
				}

				if(count > 0) // Add new object
				{
					eInventorySlot firstEmptySlot = player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
					changedSlots.Add((int)firstEmptySlot, -count); // Create the item in the free slot (always at least one)
				}
				
			}

			DbInventoryItem newItem;

			player.Inventory.BeginChanges();
			Dictionary<int, int>.Enumerator enumerator = changedSlots.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<int, int> de = enumerator.Current;
				int countToAdd = de.Value;
				if(countToAdd > 0)	// Add to exiting item
				{
					newItem = player.Inventory.GetItem((eInventorySlot)de.Key);
					player.Inventory.AddCountToStack(newItem, countToAdd);
					InventoryLogging.LogInventoryAction("(salvage)", player, eInventoryActionType.Craft, newItem.Template, countToAdd);
				}
				else
				{
					newItem = GameInventoryItem.Create(rawMaterial);
					newItem.Count = -countToAdd;
					player.Inventory.AddItem((eInventorySlot)de.Key, newItem);
					InventoryLogging.LogInventoryAction("(salvage)", player, eInventoryActionType.Craft, newItem.Template, newItem.Count);
				}
			}

			player.Inventory.CommitChanges();
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.Proceed.GetBackMaterial", materialCount, rawMaterial.Name, itemToSalvage.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			if (itemList == null) return 0;
			player.CraftTimer?.Stop();
			player.CraftTimer = null;
			if (itemList.Count > 0)
			{
				itemList.RemoveAt(0);
				BeginWorkList(player, itemList);
			}

			return 1;
		}
		
		#endregion
		
		#region Requirement check

		/// <summary>
		/// Check if the player can begin to salvage an item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public static bool IsAllowedToBeginWork(GamePlayer player, DbInventoryItem item, bool mute = false)
		{
			if (player.InCombat && !player.IsSitting)
			{
				if (!mute)
					player.Out.SendMessage("You can't salvage while in combat.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (item.IsNotLosingDur || item.IsIndestructible)
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ".  This item is indestructible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (item.Level < 1)
			{
				if (!mute)
					player.Out.SendMessage("This item cannot be salvaged.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			// using negative numbers to indicate item cannot be salvaged
			if (item.SalvageYieldID < 0)
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Salvage.BeginWork.NoSalvage", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}
			
			if(item.SlotPosition < (int)eInventorySlot.FirstBackpack || item.SlotPosition > (int)eInventorySlot.LastBackpack)
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.BackpackItems"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			eCraftingSkill skill = CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item);
			if(skill == eCraftingSkill.NoCrafting)
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ".  You do not have the required secondary skill"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.IsCrafting || player.IsSalvagingOrRepairing)
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.EndCurrentAction"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.GetCraftingSkillValue(skill) < (0.75 * CraftingMgr.GetItemCraftLevel(item)))
			{
				if (!mute)
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.NotEnoughSkill", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			return true;
		}
		
		public static bool IsAllowedToBeginWorkSilent(GamePlayer player, DbInventoryItem item)
		{
			if (player.InCombat)
			{
				player.Out.SendMessage("You can't salvage while in combat.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (item.IsNotLosingDur || item.IsIndestructible)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ".  This item is indestructible"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			// using negative numbers to indicate item cannot be salvaged
			if (item.SalvageYieldID < 0)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Salvage.BeginWork.NoSalvage", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}
			
			if(item.SlotPosition < (int)eInventorySlot.FirstBackpack || item.SlotPosition > (int)eInventorySlot.LastBackpack)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.BackpackItems"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			eCraftingSkill skill = CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item);
			if(skill == eCraftingSkill.NoCrafting)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ".  You do not have the required secondary skill"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.IsCrafting || player.IsSalvagingOrRepairing)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.EndCurrentAction"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.GetCraftingSkillValue(skill) < (0.75 * CraftingMgr.GetItemCraftLevel(item)))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.NotEnoughSkill", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}

			return true;
		}
		
		#endregion

		#region Calcul functions

        /// <summary>
        /// Calculate the count per Object_Type
        /// </summary>
        public static int GetCountForSalvage(DbInventoryItem item, DbItemTemplate rawMaterial)
        {
            long maxCount = 0;

			if (rawMaterial == null)
				return 0;

            #region Weapons

            switch ((eObjectType)item.Object_Type)
            {
                case eObjectType.RecurvedBow:
                case eObjectType.CompositeBow:
                case eObjectType.Longbow:
                case eObjectType.Crossbow:
                case eObjectType.Staff:
                case eObjectType.Fired:
                    maxCount += 36;
                    break;
                case eObjectType.Thrown:
                case eObjectType.CrushingWeapon:
                case eObjectType.SlashingWeapon:
                case eObjectType.ThrustWeapon:
                case eObjectType.Flexible:
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.Piercing:
                case eObjectType.Sword:
                case eObjectType.Hammer:
                case eObjectType.LeftAxe:
                case eObjectType.Axe:
                case eObjectType.HandToHand:
                    {
                        int dps = item.DPS_AF;
                        if (dps > 520)
                            maxCount += 10;
                        else
                            maxCount += 5;
                        break;
                    }
                case eObjectType.TwoHandedWeapon:
                case eObjectType.PolearmWeapon:
                case eObjectType.LargeWeapons:
                case eObjectType.CelticSpear:
                case eObjectType.Scythe:
                case eObjectType.Spear:
                    {
                        int dps = item.DPS_AF;
                        if (dps > 520)
                            maxCount += 15;
                        else
                            maxCount += 10;
                    }
                    break;
                case eObjectType.Shield:
                    switch (item.Type_Damage)
                    {
                        case 1:
                            maxCount += 5;
                            break;
                        case 2:
                            maxCount += 8;
                            break;
                        case 3:
                            maxCount += 12;
                            break;
                        default:
                            maxCount += 5;
                            break;
                    }
                    break;
                case eObjectType.Instrument:
                    switch (item.Type_Damage)
                    {
                        case 1:
                            maxCount += 5;
                            break;
                        case 2:
                            maxCount += 8;
                            break;
                        case 3:
                            maxCount += 12;
                            break;
                        default:
                            maxCount += 5;
                            break;

                    }
                    break;

                #endregion Weapons

            #region Armor

                case eObjectType.Cloth:
                case eObjectType.Leather:
                case eObjectType.Reinforced:
                case eObjectType.Studded:
                case eObjectType.Scale:
                case eObjectType.Chain:
                case eObjectType.Plate:
                    switch (item.Item_Type)
                    {
                        case Slot.HELM:
                            maxCount += 12;
                            break;
                        case Slot.TORSO:
                            maxCount += 17;
                            break;
                        case Slot.LEGS:
                            maxCount += 15;
                            break;

                        case Slot.ARMS:
                            maxCount += 10;
                            break;

                        case Slot.HANDS:
                            maxCount += 6;
                            break;
                        case Slot.FEET:
                            maxCount += 5;
                            break;
                        default:
                            maxCount += 5;
                            break;
                    }
                    break;
            }
        #endregion Armor

            #region Modifications

            if (maxCount < 1)
                maxCount = (int)(item.Price * 0.45 / rawMaterial.Price);

            int toadd = 0;

            if (item.Quality > 97 && !item.IsCrafted)
                for (int i = 97; i < item.Quality;)
                {
                    toadd += 3;
                    i++;
                }

            if (item.Price > 300000 && !item.IsCrafted)
            {
                long i = item.Price / 100000;
                toadd += (int)i;
            }

            if (toadd > 0)
                maxCount += toadd;

            #region SpecialFix MerchantList

            if (item.Bonus8 > 0)
                if (item.Bonus8Type == 0 || item.Bonus8Type.ToString() == string.Empty)
                    maxCount = item.Bonus8;

            #endregion SpecialFix MerchantList

            if (item.Condition != item.MaxCondition && item.Condition < item.MaxCondition)
            {
                long usureoverall = (maxCount * ((item.Condition / 5) / 1000)) / 100; // assume that all items have 50000 base con
                maxCount = usureoverall;
            }

            if (item.Description.Contains("Atlas ROG"))
	            maxCount = 2;

            if (maxCount < 1)
                maxCount = 1;
            else if (maxCount > 500)
                maxCount = 500;

            #endregion Modifications

            return (int)maxCount;
        }

		/// <summary>
		/// Return the material yield for this salvage.
		/// </summary>
		public static int GetMaterialYield(GamePlayer player, DbInventoryItem item, DbSalvageYield salvageYield, DbItemTemplate rawMaterial)
		{
            int maxCount;

			if (rawMaterial == null)
				return 0;

			if (ServerProperties.Properties.USE_NEW_SALVAGE)
			{
				maxCount = GetCountForSalvage(item, rawMaterial);
			}
			else
			{
				maxCount = (int)(item.Price * 0.45 / rawMaterial.Price); // crafted item return max 45% of the item value in material

				if (item.IsCrafted)
				{
					maxCount = (int)Math.Ceiling((double)maxCount / 2);
				}
				
				
			}

			int playerPercent = player.GetCraftingSkillValue(CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item)) * 100 / CraftingMgr.GetItemCraftLevel(item);

			if (playerPercent > 100)
			{
				playerPercent = 100;
			}
			else if (playerPercent < 75)
			{
				playerPercent = 75;
			}

			int minCount = (int)(((maxCount - 1) / 25f) * playerPercent) - ((3 * maxCount) - 4); //75% => min = 1; 100% => min = maxCount;

			salvageYield.Count = Util.Random(minCount, maxCount);
			return salvageYield.Count;
		}

		#endregion
	}
}
