using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	public class ArmorCraftingSkill : AbstractProfession
	{
		public ArmorCraftingSkill()
		{
			Icon = 0x02;
			Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, 
                "Crafting.Name.Armorcraft");
			eSkill = eCraftingSkill.ArmorCrafting;
		}

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Armorer";
            }
        }

		protected override bool CheckForTools(GamePlayer player, RecipeMgr recipe)
		{
			foreach (GameStaticItem item in player.GetItemsInRadius(CRAFT_DISTANCE))
			{
				if (item.Name.ToLower() == "forge" || item.Model == 478) // Forge
					return true;
			}

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.ServerProperties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

			if (player.Client.Account.PrivLevel > 1)
				return true;

			return false;
		}

		public override int GetSecondaryCraftingSkillMinimumLevel(RecipeMgr recipe)
		{
			switch(recipe.Product.Object_Type)
			{
				case (int)EObjectType.Studded:
				case (int)EObjectType.Chain:
				case (int)EObjectType.Plate:
				case (int)EObjectType.Reinforced:
				case (int)EObjectType.Scale:
					return recipe.Level - 60;
			}

			return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
		}

		public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
		{
			if (UtilCollection.Chance( CalculateChanceToGainPoint(player, recipe.Level)))
			{
				player.GainCraftingSkill(eCraftingSkill.ArmorCrafting, 1);
                base.GainCraftingSkillPoints(player, recipe);
				player.Out.SendUpdateCraftingSkills();
			}
		}
	}
}