using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Crafting;

public class Woodworking : ACraftingSkill
{
	public Woodworking()
	{
		Icon = 0x0E;
		Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "Crafting.Name.Woodworking");
		eSkill = ECraftingSkill.WoodWorking;
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.WoodWorking) < subSkillCap)
            {
                player.GainCraftingSkill(ECraftingSkill.WoodWorking, 1);
            }
			player.Out.SendUpdateCraftingSkills();
		}

	}
}