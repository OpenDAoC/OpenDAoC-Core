using System.Collections;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS;

[NpcGuildScript("Name Registrar")]
public class NameRegistrar : GameNpc
{
	public override IList GetExamineMessages(GamePlayer player)
	{
		IList list = new ArrayList(2);
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "NameRegistrar.YouExamine", GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
        return list;
	}

	public override bool Interact(GamePlayer player)
	{
		if(base.Interact(player))
		{
			/* Get primary crafting skill (if any) */
			int CraftSkill = 0;
			if (player.CraftingPrimarySkill != ECraftingSkill.NoCrafting)
				CraftSkill = player.GetCraftingSkillValue(player.CraftingPrimarySkill);

			/* Check if level and/or crafting skill let you have a lastname */
			if (player.Level < LastNameCommand.LASTNAME_MIN_LEVEL && CraftSkill < LastNameCommand.LASTNAME_MIN_CRAFTSKILL)
				SayTo(player, EChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "NameRegistrar.ReturnToMe", LastNameCommand.LASTNAME_MIN_LEVEL));
			else
                SayTo(player, EChatLoc.CL_SystemWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "NameRegistrar.LastName"));
            return true;
		}
		return false;
	}

}