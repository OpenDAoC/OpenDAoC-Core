using System;
using DOL.Database;
using DOL.Language;

namespace DOL.GS
{
	public class Alchemy : AdvancedCraftingSkill
	{
		public Alchemy()
		{
			Icon = 0x04;
			Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, 
                "Crafting.Name.Alchemy");
			eSkill = eCraftingSkill.Alchemy;
		}

        protected override String Profession
        {
            get 
            { 
                return "CraftersProfession.Alchemist"; 
            }
        }

		#region Classic Crafting Overrides
		public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
		{
			if (Util.Chance( CalculateChanceToGainPoint(player, recipe.Level)))
			{
				player.GainCraftingSkill(eCraftingSkill.Alchemy, 1);

				if (player.GetCraftingSkillValue(eCraftingSkill.HerbalCrafting) < subSkillCap)
					player.GainCraftingSkill(eCraftingSkill.HerbalCrafting, 1);

				player.Out.SendUpdateCraftingSkills();
			}
		}

		#endregion
		
		#region Requirement check

		/// <summary>
		/// This function is called when player accept the combine
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		public override bool IsAllowedToCombine(GamePlayer player, DbInventoryItem item)
		{
			if (!base.IsAllowedToCombine(player, item)) 
                return false;
			
			if (((DbInventoryItem)player.TradeWindow.TradeItems[0]).Object_Type != 
                (int)eObjectType.AlchemyTincture)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "Alchemy.IsAllowedToCombine.AlchemyTinctures"), PacketHandler.eChatType.CT_System, 
                    PacketHandler.eChatLoc.CL_SystemWindow);
				
                return false;
			}

			if (player.TradeWindow.ItemsCount > 1)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Alchemy.IsAllowedToCombine.OneTincture"), PacketHandler.eChatType.CT_System, 
                    PacketHandler.eChatLoc.CL_SystemWindow);

				return false;
			}

			if (item.ProcSpellID != 0 || item.SpellID != 0)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "Alchemy.IsAllowedToCombine.AlreadyImbued", item.Name), 
                    PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);

				return false;
			}

			return true;
		}

		#endregion

		#region Apply magical effect

		/// <summary>
		/// Apply all needed magical bonus to the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		protected override void ApplyMagicalEffect(GamePlayer player, DbInventoryItem item)
		{
			DbInventoryItem tincture = player.TradeWindow.TradeItems[0] as DbInventoryItem;

			// One item each side of the trade window.
			if (item == null || tincture == null)
				return ;

			if (tincture.ProcSpellID != 0)
			{
				item.ProcSpellID = tincture.ProcSpellID;

				if (tincture.ProcChance != 0)
					item.ProcChance = tincture.ProcChance;
			}
			else
			{
				item.MaxCharges = GetItemMaxCharges(item);
				item.Charges = item.MaxCharges;
				item.SpellID = tincture.SpellID;

				// Send an item update so that the tooltip shows the charge.
				GamePlayer playerToSendUpdate = player.TradeWindow.Partner ?? player;
				playerToSendUpdate.Out.SendInventoryItemsUpdate([item]);
			}

			item.LevelRequirement = tincture.LevelRequirement;

			player.Inventory.RemoveCountFromStack(tincture, 1);
			InventoryLogging.LogInventoryAction(player, "(craft)", eInventoryActionType.Craft, tincture.Template);

			if (item.Template is DbItemUnique uniqueTemplate)
			{
				GameInventoryObjectExtensions.SaveItem(item);
				GameServer.Database.SaveObject(uniqueTemplate);
			}
			else
			{
				ChatUtil.SendErrorMessage(player, "Alchemy crafting error: Item was not an ItemUnique, crafting changes not saved to DB!");

				if (log.IsErrorEnabled)
					log.ErrorFormat("Alchemy crafting error: Item {item.Id_nb} was not an ItemUnique for player {player.Name}, crafting changes not saved to DB!");
			}
		}

		#endregion

		/// <summary>
		/// Get the maximum charge the item will have
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int GetItemMaxCharges(DbInventoryItem item)
		{
			if(item.Quality < 94)
			{
				return 2;
			}
			if(item.Quality >= 100)
			{
				return 10;
			}
			return item.Quality - 92;
		}
	}
}
