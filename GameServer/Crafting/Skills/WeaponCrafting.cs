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

using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	public class WeaponCrafting : AbstractProfession
	{
		public WeaponCrafting()
		{
			Icon = 0x01;
			Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, 
				"Crafting.Name.Weaponcraft");
			eSkill = eCraftingSkill.WeaponCrafting;
		}

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Weaponcrafter";
            }
        }

		protected override bool CheckForTools(GamePlayer player, Recipe recipe)
		{
			foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
			{
				if (item.Name.ToLower() == "forge" || item.Model == 478) // Forge
					return true;
			}

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			if (player.Client.Account.PrivLevel > 1)
				return true;

			return false;
		}

		/// <summary>
		/// Calculate the minumum needed secondary crafting skill level to make the item
		/// </summary>
		public override int GetSecondaryCraftingSkillMinimumLevel(Recipe recipe)
		{
			switch (recipe.Product.Object_Type)
			{
				case (int)eObjectType.CrushingWeapon:
				case (int)eObjectType.SlashingWeapon:
				case (int)eObjectType.ThrustWeapon:
				case (int)eObjectType.TwoHandedWeapon:
				case (int)eObjectType.PolearmWeapon:
				case (int)eObjectType.Flexible:
				case (int)eObjectType.Sword:
				case (int)eObjectType.Hammer:
				case (int)eObjectType.Axe:
				case (int)eObjectType.Spear:
				case (int)eObjectType.HandToHand:
				case (int)eObjectType.Blades:
				case (int)eObjectType.Blunt:
				case (int)eObjectType.Piercing:
				case (int)eObjectType.LargeWeapons:
				case (int)eObjectType.CelticSpear:
				case (int)eObjectType.Scythe:
					return recipe.Level - 60;
			}

			return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
		}

		public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
		{
			if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
			{
				player.GainCraftingSkill(eCraftingSkill.WeaponCrafting, 1);
				base.GainCraftingSkillPoints(player, recipe);
				player.Out.SendUpdateCraftingSkills();
			}
		}
	}
}
