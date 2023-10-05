using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS;

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
		if (recipe.Product.Object_Type != (int)eObjectType.Arrow &&
			recipe.Product.Object_Type != (int)eObjectType.Bolt)
		{
			foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
			{
				if (item.Name.ToLower() == "lathe" || item.Model == 481) // Lathe
					return true;
			}

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindLathe"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

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
			case (int)eObjectType.Fired:  //tested
			case (int)eObjectType.Longbow: //tested
			case (int)eObjectType.Crossbow: //tested
			case (int)eObjectType.Instrument: //tested
			case (int)eObjectType.RecurvedBow:
			case (int)eObjectType.CompositeBow:
				return recipe.Level - 20;

			case (int)eObjectType.Arrow: //tested
			case (int)eObjectType.Bolt: //tested
			case (int)eObjectType.Thrown:
				return recipe.Level - 15;

			case (int)eObjectType.Staff: //tested
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