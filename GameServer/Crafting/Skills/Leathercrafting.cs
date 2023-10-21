using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Crafting;

public class Leathercrafting : ACraftingSkill
{

	public Leathercrafting()
	{
		Icon = 0x07;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Leathercrafting");
		eSkill = ECraftingSkill.LeatherCrafting;
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.LeatherCrafting) < subSkillCap)
            {
                player.GainCraftingSkill(ECraftingSkill.LeatherCrafting, 1);
            }
			player.Out.SendUpdateCraftingSkills();
		}
	}
}