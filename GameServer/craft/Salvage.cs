using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.SalvageCalc;
using DOL.Language;

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
		protected static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		#region Declaration

		protected const string SALVAGE_YIELD = "SALVAGE_YIELD";
		protected const string SALVAGED_ITEM = "SALVAGED_ITEM";
		protected const string SALVAGE_QUEUE = "SALVAGE_QUEUE";
		protected const string SALVAGED_SIEGE_WEAPON = "SALVAGED_SIEGE_WEAPON";
		protected const string SIEGE_SALVAGE_MATERIALS = "SIEGE_SALVAGE_MATERIALS";

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

			WhereClause whereClause = WhereClause.Empty;

			if (item.SalvageYieldID > 0)
			{
				whereClause = DB.Column("ID").IsEqualTo(item.SalvageYieldID);
				salvageYield = DOLDB<DbSalvageYield>.SelectObject(whereClause);
				DbItemTemplate material = null;

				if (salvageYield != null && string.IsNullOrEmpty(salvageYield.MaterialId_nb) == false)
				{
					material = GameServer.Database.FindObjectByKey<DbItemTemplate>(salvageYield.MaterialId_nb);

					if (material == null)
					{
						if (log.IsErrorEnabled)
							log.Error($"Salvage Error for ID: {salvageYield.ID}:  Material not found: {salvageYield.MaterialId_nb}");
					}
				}

				if (material == null)
				{
					if (salvageYield == null && item.SalvageYieldID > 0)
					{
						if (log.IsErrorEnabled)
							log.Error($"SalvageYield ID {item.SalvageYieldID} not found for item: {item.Name}");
					}
					else if (salvageYield == null)
					{
						if (log.IsErrorEnabled)
							log.Error($"Salvage Lookup Error: ObjectType: {item.Object_Type}, Item: {item.Name}");
					}

					return 0;
				}

				if (string.IsNullOrEmpty(salvageYield.MaterialId_nb))
				{
					if (log.IsErrorEnabled)
						log.Error($"Salvage Error for item: {salvageYield.ID}: MaterialId_nb is null");

					return 0;
				}

				material = GameServer.Database.FindObjectByKey<DbItemTemplate>(salvageYield.MaterialId_nb);

				if (material == null)
				{
					if (log.IsErrorEnabled)
						log.Error($"Salvage Error for ID: {salvageYield.ID}:  Material not found");

					return 0;
				}

				if (player.Client.Account.PrivLevel != 1)
				{
					player.Out.SendDebugMessage("DATABASE: SALVAGEYIELD ID " + salvageYield.ID);
				}
			}
			else
			{
				SalvageCalculator salvageCalculator = new();
				SalvageReturn salvageReturn = salvageCalculator.GetSalvage(player, item);

				salvageYield = new()
				{
					Count = salvageReturn.Count,
					MaterialId_nb = salvageReturn.ID
				};
			}

			if (string.IsNullOrEmpty(salvageYield.MaterialId_nb))
				return 0;

			// Calculate a penalty based on players secondary crafting skill level.
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
			player.CraftTimer = new(player, Proceed);
			player.CraftTimer.Properties.SetProperty(AbstractCraftingSkill.PLAYER_CRAFTER, player);
			player.CraftTimer.Properties.SetProperty(SALVAGED_ITEM, item);
			player.CraftTimer.Properties.SetProperty(SALVAGE_YIELD, salvageYield);
			player.CraftTimer.Start(salvageYield.Count * 1000);
			return 1;
		}

		public static int GetYieldPenalty(GamePlayer player, DbInventoryItem item, int salvageCount)
		{
			int percent = player.GetCraftingSkillValue(CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item)) * 100 / CraftingMgr.GetItemCraftLevel(item);

			if (percent > 99)
				percent = 100;

			int returnCount = Math.Max(1, salvageCount * percent / 100);

			if ((ePrivLevel) player.Client.Account.PrivLevel >= ePrivLevel.GM)
				player.Out.SendDebugMessage($"PlayerSkill={percent}% Returning {returnCount} of {salvageCount}");

			return returnCount;
		}

		public static int BeginWorkList(GamePlayer player, List<DbInventoryItem> itemList)
		{
			player.TempProperties.SetProperty(SALVAGE_QUEUE,itemList);
			player.CraftTimer?.Stop();
			player.Out.SendCloseTimerWindow();

			if (itemList == null || itemList.Count == 0)
				return 0;

			return BeginWork(player, itemList[0]);
		}

		public static void BeginWork(GamePlayer player, GameSiegeWeapon siegeWeapon)
		{
			if (siegeWeapon == null)
				return;

			GameSiegeWeapon currentSalvage = player.CraftTimer?.Properties.GetProperty<GameSiegeWeapon>(SALVAGED_SIEGE_WEAPON);

			if (currentSalvage != null)
			{
				StopSiegeSalvage(player, true);
				return;
			}

			if (player.GetCraftingSkillValue(eCraftingSkill.SiegeCrafting) == -1)
			{
				player.Out.SendMessage("You must be a siege weapon crafter to salvage one.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (player.Realm != siegeWeapon.Realm)
			{
				player.Out.SendMessage("You cannot salvage another realm's siege weapon!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (player.IsCrafting || player.IsSalvagingOrRepairing)
			{
				string message = LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.IsAllowedToBeginWork.EndCurrentAction");
				player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (string.IsNullOrEmpty(siegeWeapon.ItemId))
			{
				SendNotWorthMessage(player);
				return;
			}

			var recipe = DOLDB<DbCraftedItem>.SelectObject(DB.Column("Id_nb").IsEqualTo(siegeWeapon.ItemId));
			List<DbCraftedXItem> rawMaterials = null;

			if (recipe == null)
			{
				if (log.IsDebugEnabled)
					log.Debug($"{nameof(DbCraftedItem)} is null for '{siegeWeapon.ItemId}'");

				SendNotWorthMessage(player);
				return;
			}

			rawMaterials = DOLDB<DbCraftedXItem>.SelectObjects(DB.Column("CraftedItemId_nb").IsEqualTo(recipe.Id_nb));

			if (rawMaterials == null || rawMaterials.Count == 0)
			{
				if (log.IsDebugEnabled)
					log.Debug($"No raw materials found for '{siegeWeapon.ItemId}'");

				SendNotWorthMessage(player);
				return;
			}

			// Fixed 10 seconds duration for now.
			const int DURATION = 10;

			player.Out.SendMessage("You begin to salvage the siege weapon.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendTimerWindow("Salvaging Siege Weapon", DURATION);

			player.CraftTimer = new(player, ProceedSiege);
			player.CraftTimer.Properties.SetProperty(AbstractCraftingSkill.PLAYER_CRAFTER, player);
			player.CraftTimer.Properties.SetProperty(SALVAGED_SIEGE_WEAPON, siegeWeapon);
			player.CraftTimer.Properties.SetProperty(SIEGE_SALVAGE_MATERIALS, rawMaterials);
			player.CraftTimer.Start(DURATION * 1000);

			static void SendNotWorthMessage(GamePlayer player)
			{
				player.Out.SendMessage("This salvage would not yield any material.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}

		private static void StopSiegeSalvage(GamePlayer player, bool sendMessage)
		{
			if (player == null)
				return;

			player.CraftTimer?.Stop();
			player.CraftTimer = null;
			player.Out.SendCloseTimerWindow();

			if (sendMessage)
				player.Out.SendMessage("You stop salvaging the siege weapon.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		private static int Proceed(ECSGameTimer timer)
		{
			GamePlayer player = timer.Properties.GetProperty<GamePlayer>(AbstractCraftingSkill.PLAYER_CRAFTER);
			DbInventoryItem itemToSalvage = timer.Properties.GetProperty<DbInventoryItem>(SALVAGED_ITEM);
			DbSalvageYield yield = timer.Properties.GetProperty<DbSalvageYield>(SALVAGE_YIELD);

			if (player == null || itemToSalvage == null || yield == null)
				return 0;

			int materialCount = yield.Count;

			if (materialCount == 0)
				return 0;

			DbItemTemplate rawMaterial = null;

			if (string.IsNullOrEmpty(yield.MaterialId_nb) == false)
			{
				rawMaterial = GameServer.Database.FindObjectByKey<DbItemTemplate>(yield.MaterialId_nb);
			}

			if (rawMaterial == null)
			{
				if (log.IsErrorEnabled)
					log.Error($"Raw material not found: '{yield.MaterialId_nb}'");

				return 0;
			}

			player.CraftTimer?.Stop();
			player.Out.SendCloseTimerWindow();

			if (!player.Inventory.RemoveItem(itemToSalvage))
			{
				player.Out.SendMessage("Couldn't find the item to salvage.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return 0;
			}

			InventoryLogging.LogInventoryAction(player, "(salvage)", eInventoryActionType.Craft, itemToSalvage.Template, itemToSalvage.Count);

			int granted = GrantSalvageMaterial(player, rawMaterial, materialCount);

			if (granted > 0)
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.Proceed.GetBackMaterial", granted, rawMaterial.Name, itemToSalvage.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			List<DbInventoryItem> itemList = player.TempProperties.GetProperty<List<DbInventoryItem>>(SALVAGE_QUEUE);

			if (itemList == null)
				return 0;

			player.CraftTimer?.Stop();
			player.CraftTimer = null;

			if (itemList.Count > 0)
			{
				itemList.RemoveAt(0);
				BeginWorkList(player, itemList);
			}

			return 1;
		}

		protected static int ProceedSiege(ECSGameTimer timer)
		{
			GamePlayer player = timer.Properties.GetProperty<GamePlayer>(AbstractCraftingSkill.PLAYER_CRAFTER);
			GameSiegeWeapon siegeWeapon = timer.Properties.GetProperty<GameSiegeWeapon>(SALVAGED_SIEGE_WEAPON);
			List<DbCraftedXItem> rawMaterials = timer.Properties.GetProperty<List<DbCraftedXItem>>(SIEGE_SALVAGE_MATERIALS);

			StopSiegeSalvage(player, false);

			if (player == null || siegeWeapon == null)
				return 0;

			// Guard against the weapon having been destroyed/removed by other means while the salvage timer was running.
			if (siegeWeapon.ObjectState is not GameObject.eObjectState.Active)
			{
				player.Out.SendMessage("The siege weapon is no longer available to salvage.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return 0;
			}

			siegeWeapon.ReleaseControl();
			siegeWeapon.RemoveFromWorld();

			if (rawMaterials != null)
			{
				foreach (DbCraftedXItem craftedMaterial in rawMaterials)
				{
					if (craftedMaterial == null || craftedMaterial.Count < 1)
						continue;

					DbItemTemplate rawMaterial = GameServer.Database.FindObjectByKey<DbItemTemplate>(craftedMaterial.IngredientId_nb);

					if (rawMaterial == null)
					{
						if (log.IsErrorEnabled)
							log.Error($"Raw material '{craftedMaterial.IngredientId_nb}' not found for '{craftedMaterial.CraftedItemId_nb}'");

						continue;
					}

					int granted = GrantSalvageMaterial(player, rawMaterial, craftedMaterial.Count);

					if (granted > 0)
					{
						string message = LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.Proceed.GetBackMaterial", granted, rawMaterial.Name, siegeWeapon.Name);
						player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					}

					int remaining = craftedMaterial.Count - granted;

					if (remaining > 0)
					{
						GameInventoryItem item = GameInventoryItem.Create(rawMaterial);
						item.Count = remaining;
						_ = player.CreateItemOnTheGround(item);
						InventoryLogging.LogInventoryAction(siegeWeapon, "(ground)", eInventoryActionType.Other, rawMaterial, item.Count);
					}
				}
			}

			return 0;
		}

		protected static int GrantSalvageMaterial(GamePlayer player, DbItemTemplate rawMaterial, int materialCount)
		{
			if (player == null || rawMaterial == null || materialCount < 1)
				return 0;

			int count = materialCount;
			int granted = materialCount;

			lock (player.Inventory.Lock)
			{
				// Try to fill existing partial stacks.
				foreach (DbInventoryItem item in player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					if (item == null || item.Id_nb != rawMaterial.Id_nb || item.Count >= item.MaxCount)
						continue;

					int countFree = item.MaxCount - item.Count;
					int amountToAdd = Math.Min(count, countFree);

					if (player.Inventory.AddCountToStack(item, amountToAdd))
					{
						InventoryLogging.LogInventoryAction("(salvage)", player, eInventoryActionType.Craft, item.Template, amountToAdd);
						count -= amountToAdd;

						if (count <= 0)
							return granted;
					}
				}

				// If we still have materials left, place them in an empty slot.
				if (count > 0)
				{
					GameInventoryItem newItem = GameInventoryItem.Create(rawMaterial);
					newItem.Count = count;
				
					if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newItem))
						InventoryLogging.LogInventoryAction("(salvage)", player, eInventoryActionType.Craft, newItem.Template, count);
					else
						return materialCount - count;
				}
			}

			return granted;
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
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ". This item is indestructible."), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ". You do not have the required secondary skill."), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ". This item is indestructible."), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Salvage.BeginWork.NoSalvage", item.Name + ". You do not have the required secondary skill."), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
