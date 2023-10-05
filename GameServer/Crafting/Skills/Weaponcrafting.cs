using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS;

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

		player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
		player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

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