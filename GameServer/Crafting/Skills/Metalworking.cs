using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Crafting;

public class Metalworking : ACraftingSkill
{
	public Metalworking()
	{
		Icon = 0x06;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, 
			"Crafting.Name.Metalworking");
		eSkill = ECraftingSkill.MetalWorking;
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

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.MetalWorking) < subSkillCap)
                player.GainCraftingSkill(ECraftingSkill.MetalWorking, 1);

			player.Out.SendUpdateCraftingSkills();
		}
	}
}