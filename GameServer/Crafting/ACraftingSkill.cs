using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;
using log4net;

namespace Core.GS.Crafting
{
	/// <summary>
	/// AbstractCraftingSkill is the base class for all crafting skill
	/// </summary>
	public abstract class ACraftingSkill
	{
		protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private bool finishedCraft = false; 
		#region Declaration
		/// <summary>
		/// the maximum possible range within a player has to be to a forge , lathe ect to craft an item
		/// </summary>
		public const int CRAFT_DISTANCE = 512;

		/// <summary>
		/// The icone number used by this craft.
		/// </summary>
		private byte m_icon;

		/// <summary>
		/// The name show for this craft.
		/// </summary>
		private string m_name;

		/// <summary>
		/// The crafting skill id of this craft
		/// </summary>
		private ECraftingSkill m_eskill;

		/// <summary>
		/// The player currently crafting
		/// </summary>
		public const string PLAYER_CRAFTER = "PLAYER_CRAFTER";

		/// <summary>
		/// The recipe being crafted
		/// </summary>
		public const string RECIPE_BEING_CRAFTED = "RECIPE_BEING_CRAFTED";

		/// <summary>
		/// The list of raw materials for the recipe beign crafted
		/// </summary>
		public const string RECIPE_RAW_MATERIAL_LIST = "RECIPE_RAW_MATERIAL_LIST";

		public const int subSkillCap = 1300;

		public virtual string CRAFTER_TITLE_PREFIX
		{
			get
			{
				return "";
			}
		}
		public ECraftingSkill eSkill
		{
			get
			{
				return m_eskill;
			}
			set
			{
				m_eskill = value;
			}
		}
		public byte Icon
		{
			get
			{
				return m_icon;
			}
			set
			{
				m_icon = value;
			}
		}
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}
		#endregion

		#region First call function and callback
		public virtual void CraftItem(GamePlayer player, RecipeMgr recipe)
		{
			if (!CanPlayerStartToCraftItem(player, recipe))
			{
				return;
			}

			if (player.IsCrafting)
			{
				StopCraftingCurrentItem(player, recipe.Product);
				return;
			}

			int craftingTime = GetCraftingTime(player, recipe);

			var chanceToMakeItem = CalculateChanceToMakeItem(player, recipe.Level);
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.BeginWork", recipe.Product.Name, chanceToMakeItem.ToString()), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
			player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CurrentlyMaking", recipe.Product.Name), craftingTime);

			player.Stealth(false);

			//StartCraftingTimerAndSetCallBackMethod(player, recipe, craftingTime);
			player.craftComponent.StartCraft(recipe, this, craftingTime);
		}

		protected virtual void StartCraftingTimerAndSetCallBackMethod(GamePlayer player, RecipeMgr recipe, int craftingTime)
        {
			//player.CraftTimer = new ECSGameTimer(player);
			//player.CraftTimer.Callback = new ECSGameTimer.ECSTimerCallback(MakeItem);
			//player.CraftTimer.Properties.setProperty(PLAYER_CRAFTER, player);
			//player.CraftTimer.Properties.setProperty(RECIPE_BEING_CRAFTED, recipe);
			//player.CraftTimer.Start(craftingTime * 1000);
		}

		protected virtual void StopCraftingCurrentItem(GamePlayer player, DbItemTemplate itemToCraft)
		{
			//player.CraftTimer.Stop();
			player.craftComponent.StopCraft();
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.StopWork", itemToCraft.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		protected virtual bool CanPlayerStartToCraftItem(GamePlayer player, RecipeMgr recipe)
		{
			
			player.TempProperties.SetProperty("RecipeToCraft", recipe);
			
			if (!GameServer.ServerRules.IsAllowedToCraft(player, recipe.Product))
			{
				return false;
			}

			if (!CheckForTools(player, recipe))
			{
				return false;
			}

			if (!CheckSecondCraftingSkillRequirement(player, recipe))
			{
				return false;
			}

			if (!CheckRawMaterials(player, recipe))
			{
				return false;
			}

			if (player.IsMoving || player.IsStrafing)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.MoveAndInterrupt"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.InCombat)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CantCraftInCombat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			
			if (player.IsCasting)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CantCraftCasting"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			

			return true;
		}

		#endregion

		#region Requirement check

		protected virtual bool CheckForTools(GamePlayer player, RecipeMgr recipe)
		{
			return true;
		}

		public virtual bool CheckSecondCraftingSkillRequirement(GamePlayer player, RecipeMgr recipe)
        {
			int minimumLevel = GetSecondaryCraftingSkillMinimumLevel(recipe);

			if (minimumLevel <= 0)
				return true; // no requirement needed

			foreach (var ingredient in recipe.Ingredients)
			{
				DbItemTemplate material = ingredient.Material;

				switch (material.Model)
				{
					case 522:   //"cloth square"
					case 537:   //"heavy thread"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.ClothWorking) < minimumLevel)
							{
								player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckSecondCraftingSkillRequirement.NoClothworkingSkill", minimumLevel, material.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return false;
							}
							break;
						}

					case 521:   //"leather square"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.LeatherCrafting) < minimumLevel)
							{
								player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckSecondCraftingSkillRequirement.NoLeathercraftingSkill", minimumLevel, material.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return false;
							}
							break;
						}

					case 519:   //"metal bars"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.MetalWorking) < minimumLevel)
							{
								player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckSecondCraftingSkillRequirement.NoMetalworkingSkill", minimumLevel, material.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return false;
							}
							break;
						}

					case 520:   //"wooden boards"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.WoodWorking) < minimumLevel)
							{
								player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckSecondCraftingSkillRequirement.NoWoodworkingSkill", minimumLevel, material.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return false;
							}
							break;
						}
				}
			}
			return true;
		}

		public virtual bool CheckRawMaterials(GamePlayer player, RecipeMgr recipe)
        {
			ArrayList missingMaterials = null;

			long totalPrice = 0;
			lock (player.Inventory)
			{
				foreach (var ingredient in recipe.Ingredients)
				{
					DbItemTemplate material = ingredient.Material;

					totalPrice += material.Price * ingredient.Count;

					bool result = false;
					int count = ingredient.Count;

					foreach (DbInventoryItem item in player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
					{
						if (item != null && item.Name == material.Name)
						{
							if (item.Count >= count)
							{
								result = true;
								break;
							}
							else
							{
								count -= item.Count;
							}
						}
					}

					if (result == false)
					{
						if (missingMaterials == null)
						{
							missingMaterials = new ArrayList(5);
						}

						missingMaterials.Add("(" + count + ") " + material.Name);
					}
				}

				if (missingMaterials != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckRawMaterial.NoIngredients", recipe.Product.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CheckRawMaterial.YouAreMissing", recipe.Product.Name), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					foreach (string materialName in missingMaterials)
					{
						player.Out.SendMessage(materialName, EChatType.CT_Important, EChatLoc.CL_ChatWindow);
					}

					if (player.Client.Account.PrivLevel == (uint)EPrivLevel.Player) return false;
				}

				return true;
			}
		}

		#endregion

		#region Gain points
		public virtual void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
		{
			foreach (var ingredient in recipe.Ingredients)
			{
				DbItemTemplate template = ingredient.Material;

				switch (template.Model)
				{
					case 522:   //"cloth square"
					case 537:   //"heavy thread"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.ClothWorking) < subSkillCap)
							{
								player.GainCraftingSkill(ECraftingSkill.ClothWorking, 1);
							}
							break;
						}

					case 521:   //"leather square"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.LeatherCrafting) < subSkillCap)
							{
								player.GainCraftingSkill(ECraftingSkill.LeatherCrafting, 1);
							}
							break;
						}

					case 519:   //"metal bars"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.MetalWorking) < subSkillCap)
							{
								player.GainCraftingSkill(ECraftingSkill.MetalWorking, 1);
							}
							break;
						}

					case 520:   //"wooden boards"
						{
							if (player.GetCraftingSkillValue(ECraftingSkill.WoodWorking) < subSkillCap)
							{
								player.GainCraftingSkill(ECraftingSkill.WoodWorking, 1);
							}
							break;
						}
				}
				
			}

			player.Out.SendUpdateCraftingSkills();
		}
		#endregion

		#region Use materials and created crafted item
		public virtual bool RemoveUsedMaterials(GamePlayer player, RecipeMgr recipe)
		{
			Dictionary<int, int?> dataSlots = new Dictionary<int, int?>(10);

			lock (player.Inventory)
			{
				foreach (var ingredient in recipe.Ingredients)
				{
					DbItemTemplate template = ingredient.Material;

					bool result = false;
					int count = ingredient.Count;

					foreach (DbInventoryItem item in player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
					{
						if (item != null && item.Name == template.Name)
						{
							if (item.Count >= count)
							{
								if (item.Count == count)
								{
									dataSlots.Add(item.SlotPosition, null);
								}
								else
								{
									dataSlots.Add(item.SlotPosition, count);
								}
								result = true;
								break;
							}
							else
							{
								dataSlots.Add(item.SlotPosition, null);
								count -= item.Count;
							}
						}
					}
					if (result == false)
					{
						return false;
					}
				}
			}

			player.Inventory.BeginChanges();
			Dictionary<int, int?>.Enumerator enumerator = dataSlots.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<int, int?> de = enumerator.Current;
				DbInventoryItem item = player.Inventory.GetItem((EInventorySlot)de.Key);
				if (item != null)
				{
					if (!de.Value.HasValue)
					{
						player.Inventory.RemoveItem(item);
					}
					else
					{
						player.Inventory.RemoveCountFromStack(item, de.Value.Value);
					}
					InventoryLogging.LogInventoryAction(player, "(craft)", EInventoryActionType.Craft, item.Template, de.Value.HasValue ? de.Value.Value : item.Count);
				}
			}
			player.Inventory.CommitChanges();

			return true;//all raw material removed and item created
		}

		public virtual void BuildCraftedItem(GamePlayer player, RecipeMgr recipe)
		{
			var product = recipe.Product;

			Dictionary<int, int> changedSlots = new Dictionary<int, int>(5); // key : > 0 inventory ; < 0 ground || value: < 0 = new item count; > 0 = add to old

			lock (player.Inventory)
			{
				int count = product.PackSize < 1 ? 1 : product.PackSize;
				foreach (DbInventoryItem item in player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
				{
					if (item == null)
						continue;

					if (item.Id_nb.Equals(product.Id_nb) == false)
						continue;

					if (item.Count >= product.MaxCount)
						continue;

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

				if (count > 0) // Add new object
				{
					EInventorySlot firstEmptySlot = player.Inventory.FindFirstEmptySlot(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
					if (firstEmptySlot == EInventorySlot.Invalid)
					{
						changedSlots.Add(-1, -count); // Create the item on the ground
					}
					else
					{
						changedSlots.Add((int)firstEmptySlot, -count); // Create the item in the free slot
					}
					count = 0;
				}
			}
			
			DbInventoryItem newItem = null;

			player.Inventory.BeginChanges();

			Dictionary<int, int>.Enumerator enumerator = changedSlots.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<int, int> slot = enumerator.Current;
				int countToAdd = slot.Value;
				if (countToAdd > 0)	// Add to exiting item
				{
					newItem = player.Inventory.GetItem((EInventorySlot)slot.Key);
					if (newItem != null && player.Inventory.AddCountToStack(newItem, countToAdd))
					{
						InventoryLogging.LogInventoryAction("(craft)", player, EInventoryActionType.Other, newItem.Template, countToAdd);
						// count incremented, continue with next change
						continue;
					}
				}

				if (recipe.IsForUniqueProduct == false)
				{
					newItem = GameInventoryItem.Create(product);
				}
				else
				{
					DbItemUnique unique = new DbItemUnique(product);
					GameServer.Database.AddObject(unique);
					newItem = GameInventoryItem.Create(unique);
					newItem.Quality = GetQuality(player, recipe.Level);
				}

				newItem.IsCrafted = true;
				newItem.Creator = player.Name;
				newItem.Count = -countToAdd;

				if (slot.Key > 0)	// Create new item in the backpack
				{
					player.Inventory.AddItem((EInventorySlot)slot.Key, newItem);
					InventoryLogging.LogInventoryAction("(craft)", player, EInventoryActionType.Craft, newItem.Template, newItem.Count);
				}
				else					// Create new item on the ground
				{
					player.CreateItemOnTheGround(newItem);
					player.Out.SendDialogBox(EDialogCode.SimpleWarning, 0, 0, 0, 0, EDialogType.Ok, true, LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.BuildCraftedItem.BackpackFull", product.Name));
				}
			}

			player.Inventory.CommitChanges();

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.BuildCraftedItem.Successfully", product.Name, newItem.Quality), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);

			if (recipe.IsForUniqueProduct && newItem.Quality == 100)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.BuildCraftedItem.Masterpiece"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				player.Out.SendPlaySound(ESoundType.Craft, 0x04);
			}
			else
			{
				player.Out.SendPlaySound(ESoundType.Craft, 0x03);
			}
			
		}

		#endregion

		#region Calcul functions
		public virtual int CalculateChanceToMakeItem(GamePlayer player, int craftingLevel)
        {
			int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), craftingLevel);
			if (con < -3)
				con = -3;
			if (con > 3)
				con = 3;

			switch (con)
			{
				// Chance to MAKE ! (100 - chance to fail)
				case -3:
					return 100;
				case -2:
					return 100;
				case -1:
					return 100;
				case 0:
					return 100 - 8;
				case 1:
					return 100 - 16;
				case 2:
					return 100 - 32;
				case 3:
					return 0;
				default:
					return 0;
			}
		}

		/// <summary>
		/// Calculate chance to gain point
		/// </summary>
		public virtual int CalculateChanceToGainPoint(GamePlayer player, int recipeLevel)
		{
			int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipeLevel);
			if (con < -3)
				con = -3;
			if (con > 3)
				con = 3;
			int chance;

			switch (con)
			{
				case -3:
					return 0;
				case -2:
					chance = 15;
					break;
				case -1:
					chance = 30;
					break;
				case 0:
					chance = 45;
					break;
				case 1:
					chance = 55;
					break;
				case 2:
					chance = 65;
					break;
				case 3:
					return 0;
				default:
					return 0;
			}

			// In capital cities bonuses to crafting apply (patch 1.86)
			if (player.CurrentRegion.IsCapitalCity)
			{
				chance += player.CraftingSkillBonus;

				if (chance < 0)
					chance = 0;
				if (chance > 100)
					chance = 100;
			}

			return chance;
		}
		public virtual int GetCraftingTime(GamePlayer player, RecipeMgr recipe)
		{
			double baseMultiplier = (recipe.Level / 100) + 1;
			if (baseMultiplier < 1) baseMultiplier = 1;

			ushort materialsCount = 0;
			foreach (var ingredient in recipe.Ingredients)
			{
				var countMod = ingredient.Count;
				if (countMod > 100) countMod /= 10;
				materialsCount += (ushort)countMod;
			}

			var divisorMod = 4;
			var loyalDays = RealmLoyaltyMgr.GetPlayerRealmLoyalty(player).Days;
			if ( loyalDays > 30) divisorMod++;
			if ( loyalDays > 20) divisorMod++;
			if ( loyalDays > 10) divisorMod++;
			int craftingTime = (int)(baseMultiplier * materialsCount / divisorMod);

			// Player does check for capital city bonus as well
			craftingTime = (int)(craftingTime / player.CraftingSpeed);

			craftingTime = (int)(craftingTime * (1 - (.05 * RelicMgr.GetRelicCount(player.Realm))));

			//keep bonuses reduction in crafting time
			if (Keeps.KeepBonusMgr.RealmHasBonus(EKeepBonusType.Craft_Timers_5, (ERealm)player.Realm))
				craftingTime = (int)(craftingTime / 1.05);
			else if (Keeps.KeepBonusMgr.RealmHasBonus(EKeepBonusType.Craft_Timers_3, (ERealm)player.Realm))
				craftingTime = (int)(craftingTime / 1.03);

			int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipe.Level);
			double mod = 1.0;
			switch (con)
			{
				case -3:
					mod = 0.4;
					break;
				case -2:
					mod = 0.6;
					break;
				case -1:
					mod = 0.8;
					break;
				case 0:
					mod = 1.0;
					break;
				case 1:
					mod = 1.0;
					break;
				case 2:
					mod = 1.0;
					break;
				case 3:
					mod = 1.0;
					break;
			}

			craftingTime = (int)(craftingTime * mod);

			var maxCraftingTime = ServerProperty.MAX_CRAFT_TIME;

			if (craftingTime < 1)
				craftingTime = 1;
			else if (maxCraftingTime > 0 && craftingTime > maxCraftingTime)
				craftingTime = maxCraftingTime;
			
			if (player.Client.Account.PrivLevel > 1)
			{
				player.Out.SendMessage($"[DEBUG] Crafting time: {craftingTime}s", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			return craftingTime;
		}

		public virtual int GetSecondaryCraftingSkillMinimumLevel(RecipeMgr recipe)
		{
			return 0;
		}

		/// <summary>
		/// Calculate crafted item quality
		/// </summary>
		private int GetQuality(GamePlayer player, int recipeLevel)
		{
			// 2% chance to get masterpiece, 1:6 chance to get 94-99%, if legendary or if grey con
			// otherwise moving the most load towards 94%, the higher the item con to the crafter skill
			//1.87 patch raises min quality to 96%

			// legendary
			if (player.GetCraftingSkillValue(m_eskill) >= 1000)
			{
				if (Util.Chance(2))
				{
					return 100;	// 2% chance for master piece
				}
				return 96 + Util.Random(3);
			}

			int delta = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipeLevel);
			if (delta < -2)
			{
				if (Util.Chance(2))
					return 100; // grey items get 2% chance to be master piece
				return 96 + Util.Random(3); // handle grey items like legendary
			}

			// this is a type of roulette selection, imagine a roulette wheel where all chances get different sized
			// fields where the ball can drop, the bigger the field, the bigger the chance
			// field size is modulated by item con and can be also 0

			// possible chance allocation scheme for yellow item
			// 99:
			// 98:
			// 97: o
			// 96: oo
			// where one 'o' marks 100 size, this example results in 10% chance for yellow items to be 97% quality

			delta = delta * 100;

			int[] chancePart = new int[4]; // index ranges from 96%(0) to 99%(5)
			int sum = 0;
			for (int i = 0; i < 4; i++)
			{
				chancePart[i] = Math.Max((4 - i) * 100 - delta, 0);	// 0 minimum
				sum += chancePart[i];
			}

			// selection
			int rand = Util.Random(sum);
			for (int i = 3; i >= 0; i--)
			{
				if (rand < chancePart[i])
					return 96 + i;
				rand -= chancePart[i];
			}

			// if something still not clear contact Thrydon/Blue

			return 96;
		}


		/// <summary>
		/// get item con color compared to crafters skill
		/// </summary>
		/// <param name="crafterSkill"></param>
		/// <param name="itemCraftingLevel"></param>
		/// <returns>-3 grey, -2 green, -1 blue, 0 yellow, 1 orange, 2 red, 3 purple</returns>
		public int GetItemCon(int crafterSkill, int itemCraftingLevel)
		{
			int diff = itemCraftingLevel - crafterSkill;
			if (diff <= -50)
				return -3; // grey
			else if (diff <= -31)
				return -2; // green
			else if (diff <= -11)
				return -1; // blue
			else if (diff <= 0)
				return 0; // yellow
			else if (diff <= 19)
				return 1; // orange
			else if (diff <= 49)
				return 2; // red
			else
				return 3; // impossible
		}
		#endregion
	}
}
