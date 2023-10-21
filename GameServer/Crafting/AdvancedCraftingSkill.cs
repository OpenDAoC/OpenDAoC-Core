using Core.Database.Tables;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Crafting
{
	/// <summary>
	/// AdvancedCraftingSkill is the skill for alchemy and spellcrafting whitch add all combine system
	/// </summary>
	public abstract class AdvancedCraftingSkill : AProfession
    {
		#region Classic craft function
		protected override bool CheckForTools(GamePlayer player, RecipeMgr recipe)
		{
			foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
			{
				if (item.Name.ToLower() == "alchemy table" || item.Model == 820) // Alchemy Table
				{
					return true;
				}
			}

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindAlchemyTable"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

			if (player.Client.Account.PrivLevel > 1)
				return true;

			return false;
		}

		#endregion

		#region Advanced craft function

		#region First call function
		
		/// <summary>
		/// Called when player accept to combine items
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual bool CombineItems(GamePlayer player)
		{
			if(player.TradeWindow.PartnerTradeItems == null || player.TradeWindow.PartnerItemsCount != 1)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.CombineItems.OnlyCombine"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			DbInventoryItem itemToCombine = (DbInventoryItem)player.TradeWindow.PartnerTradeItems[0];
			if(!IsAllowedToCombine(player, itemToCombine)) return false;

			ApplyMagicalEffect(player, itemToCombine);

			return true;
		}

		#endregion

		#region Requirement check

		/// <summary>
        /// Check if the player can enchant the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToCombine(GamePlayer player, DbInventoryItem item)
		{
			if(item == null) return false;
			
			if(player.TradeWindow.ItemsCount <= 0)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.Imbue", item.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;	
			}

			if(!item.IsCrafted)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.CraftedItems"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}


            DbInventoryItem itemToCombine = (DbInventoryItem)player.TradeWindow.TradeItems[0];

            if (itemToCombine.Object_Type == (int)EObjectType.AlchemyTincture)
            {
                if (item.Object_Type != (int)EObjectType.Instrument) // Only check for non instruments
                {
                    switch (itemToCombine.Type_Damage)
                    {
                        case 0: //Type damage 0 = armors
                            if (!GlobalConstants.IsArmor(item.Object_Type))
                            {
                                if (item.Object_Type == (int)EObjectType.Shield) // think shield can do armor and weapon ? not verified.
                                    return true;

                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.NoGoodCombine"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return false;
                            }
                            break;
                        case 1: //Type damage 1 = weapons
                            if (!GlobalConstants.IsWeapon(item.Object_Type))
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.NoGoodCombine"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return false;
                            }
                            break;
                        default:
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.ProblemCombine"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return false;
                    }
                }
                else // Instrument
                {
                    if (itemToCombine.Type_Damage != 0) //think instrument can do only armorproc ? not verified.
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.NoGoodCombine"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return false;
                    }
                }
            }

            if (!GlobalConstants.IsArmor(item.Object_Type) && !GlobalConstants.IsWeapon(item.Object_Type) && item.Object_Type != (int)EObjectType.Instrument)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AdvancedCraftingSkill.IsAllowedToCombine.NoEnchanted"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;	
			}

			return true;
		}
		
		#endregion
		
		#region Apply magical effect

		/// <summary>
        /// Apply the magical bonus to the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		protected abstract void ApplyMagicalEffect(GamePlayer player, DbInventoryItem item);
		
		#endregion

		#endregion

	}
}
