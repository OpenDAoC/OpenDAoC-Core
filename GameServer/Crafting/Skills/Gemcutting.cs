using DOL.Language;

namespace DOL.GS;

public class Gemcutting : ACraftingSkill
{
	public Gemcutting()
	{
		Icon = 0x09;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Gemcutting");
		eSkill = ECraftingSkill.GemCutting;
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.GemCutting) < subSkillCap)
            {
                player.GainCraftingSkill(ECraftingSkill.GemCutting, 1);
                player.Out.SendUpdateCraftingSkills();
            }
		}
	}
}