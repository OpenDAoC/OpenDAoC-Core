/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.Database;
using DOL.Language;
using System;
using System.Collections.Generic;

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
		public override bool IsAllowedToCombine(GamePlayer player, DbInventoryItems item)
		{
			if (!base.IsAllowedToCombine(player, item)) 
                return false;
			
			if (((DbInventoryItems)player.TradeWindow.TradeItems[0]).Object_Type != 
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
		protected override void ApplyMagicalEffect(GamePlayer player, DbInventoryItems item)
		{
			DbInventoryItems tincture = (DbInventoryItems)player.TradeWindow.TradeItems[0];

            // One item each side of the trade window.

			if (item == null || tincture == null) 
                return ;
			
			if(tincture.ProcSpellID != 0)
			{
				item.ProcSpellID = tincture.ProcSpellID;
				if(tincture.ProcChance != 0)
					item.ProcChance = tincture.ProcChance;
			}
			else
			{
				item.MaxCharges = GetItemMaxCharges(item);
				item.Charges = item.MaxCharges;
				item.SpellID = tincture.SpellID;
			}

			item.LevelRequirement = tincture.LevelRequirement;
			//item.Level = tincture.LevelRequirement;

			player.Inventory.RemoveCountFromStack(tincture, 1);
			InventoryLogging.LogInventoryAction(player, "(craft)", eInventoryActionType.Craft, tincture.Template);

			if (item.Template is DbItemUniques)
			{
				GameServer.Database.SaveObject(item);
				GameServer.Database.SaveObject(item.Template as DbItemUniques);
			}
			else
			{
				ChatUtil.SendErrorMessage(player, "Alchemy crafting error: Item was not an ItemUnique, crafting changes not saved to DB!");
				log.ErrorFormat("Alchemy crafting error: Item {0} was not an ItemUnique for player {1}, crafting changes not saved to DB!", item.Id_nb, player.Name);
			}
		}

		#endregion

		/// <summary>
		/// Get the maximum charge the item will have
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int GetItemMaxCharges(DbInventoryItems item)
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
