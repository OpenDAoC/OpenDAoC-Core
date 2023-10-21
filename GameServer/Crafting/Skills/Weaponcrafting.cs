using System;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Crafting;

public class Weaponcrafting : AProfession
{
	public Weaponcrafting()
	{
		Icon = 0x01;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, 
			"Crafting.Name.Weaponcraft");
		eSkill = ECraftingSkill.WeaponCrafting;
	}

    protected override String Profession
    {
        get
        {
            return "CraftersProfession.Weaponcrafter";
        }
    }

	protected override bool CheckForTools(GamePlayer player, RecipeMgr recipe)
	{
		foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
		{
			if (item.Name.ToLower() == "forge" || item.Model == 478) // Forge
				return true;
		}

		player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

		if (player.Client.Account.PrivLevel > 1)
			return true;

		return false;
	}

	/// <summary>
	/// Calculate the minumum needed secondary crafting skill level to make the item
	/// </summary>
	public override int GetSecondaryCraftingSkillMinimumLevel(RecipeMgr recipe)
	{
		switch (recipe.Product.Object_Type)
		{
			case (int)EObjectType.CrushingWeapon:
			case (int)EObjectType.SlashingWeapon:
			case (int)EObjectType.ThrustWeapon:
			case (int)EObjectType.TwoHandedWeapon:
			case (int)EObjectType.PolearmWeapon:
			case (int)EObjectType.Flexible:
			case (int)EObjectType.Sword:
			case (int)EObjectType.Hammer:
			case (int)EObjectType.Axe:
			case (int)EObjectType.Spear:
			case (int)EObjectType.HandToHand:
			case (int)EObjectType.Blades:
			case (int)EObjectType.Blunt:
			case (int)EObjectType.Piercing:
			case (int)EObjectType.LargeWeapons:
			case (int)EObjectType.CelticSpear:
			case (int)EObjectType.Scythe:
				return recipe.Level - 60;
		}

		return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
			player.GainCraftingSkill(ECraftingSkill.WeaponCrafting, 1);
			base.GainCraftingSkillPoints(player, recipe);
			player.Out.SendUpdateCraftingSkills();
		}
	}
}