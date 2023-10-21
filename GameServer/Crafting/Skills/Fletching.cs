using System;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Crafting;

public class Fletching : AProfession
{
    protected override String Profession
    {
        get
        {
            return "CraftersProfession.Fletcher";
        }
    }

	public Fletching()
	{
		Icon = 0x0C;
		Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, 
            "Crafting.Name.Fletching");
		eSkill = ECraftingSkill.Fletching;
	}

	protected override bool CheckForTools(GamePlayer player, RecipeMgr recipe)
	{
		if (recipe.Product.Object_Type != (int)EObjectType.Arrow &&
			recipe.Product.Object_Type != (int)EObjectType.Bolt)
		{
			foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
			{
				if (item.Name.ToLower() == "lathe" || item.Model == 481) // Lathe
					return true;
			}

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindLathe"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

			if (player.Client.Account.PrivLevel > 1)
				return true;

			return false;
		}

		return true;
	}

	public override int GetSecondaryCraftingSkillMinimumLevel(RecipeMgr recipe)
	{
		switch (recipe.Product.Object_Type)
		{
			case (int)EObjectType.Fired:  //tested
			case (int)EObjectType.Longbow: //tested
			case (int)EObjectType.Crossbow: //tested
			case (int)EObjectType.Instrument: //tested
			case (int)EObjectType.RecurvedBow:
			case (int)EObjectType.CompositeBow:
				return recipe.Level - 20;

			case (int)EObjectType.Arrow: //tested
			case (int)EObjectType.Bolt: //tested
			case (int)EObjectType.Thrown:
				return recipe.Level - 15;

			case (int)EObjectType.Staff: //tested
				return recipe.Level - 35;
		}

		return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
	}

	public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
	{
		if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
		{
			player.GainCraftingSkill(ECraftingSkill.Fletching, 1);
			base.GainCraftingSkillPoints(player, recipe);
			player.Out.SendUpdateCraftingSkills();
		}
	}
}