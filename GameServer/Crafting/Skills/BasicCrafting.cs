using System;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Crafting;

public class BasicCrafting : AProfession
{
	public BasicCrafting()
	{
		Icon = 0x0F;
        Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "Crafting.Name.BasicCrafting");
        eSkill = ECraftingSkill.BasicCrafting;
	}

    protected override String Profession
    {
        get
        {
            return "CraftersProfession.BasicCrafter";
        }
    }

    public override string CRAFTER_TITLE_PREFIX
	{
		get
		{
			return "Crafter's";
        }
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
			player.GainCraftingSkill(ECraftingSkill.BasicCrafting, 1);
			player.Out.SendUpdateCraftingSkills();
		}
	}
}