using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Crafting;

public class Herbcraft : ACraftingSkill
{
	public Herbcraft()
	{
		Icon = 0x0A;
		Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "Crafting.Name.Herbcrafting");
		eSkill = ECraftingSkill.HerbalCrafting;
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
            if (player.GetCraftingSkillValue(ECraftingSkill.HerbalCrafting) < subSkillCap)
            {
                player.GainCraftingSkill(ECraftingSkill.HerbalCrafting, 1);
            }
			player.Out.SendUpdateCraftingSkills();
		}
	}
}