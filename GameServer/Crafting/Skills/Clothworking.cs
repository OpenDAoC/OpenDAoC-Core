using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Crafting;

public class Clothworking : ACraftingSkill
{
	public Clothworking()
	{
		Icon = 0x08;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Clothworking");
		eSkill = ECraftingSkill.ClothWorking;
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.ClothWorking) < subSkillCap)
            {
                player.GainCraftingSkill(ECraftingSkill.ClothWorking, 1);
                player.Out.SendUpdateCraftingSkills();
            }
		}
	}
}