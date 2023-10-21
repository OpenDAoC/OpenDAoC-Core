using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Crafting;

public class Gemcutting : ACraftingSkill
{
	public Gemcutting()
	{
		Icon = 0x09;
		Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "Crafting.Name.Gemcutting");
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