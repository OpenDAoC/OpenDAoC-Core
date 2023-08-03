using System;
using DOL.Language;

namespace DOL.GS
{
	public class BasicCraftingSkill : AbstractProfession
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public BasicCraftingSkill()
		{
			Icon = 0x0F;
            Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "Crafting.Name.BasicCrafting");
            eSkill = eCraftingSkill.BasicCrafting;
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
			if (UtilCollection.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
			{
				player.GainCraftingSkill(eCraftingSkill.BasicCrafting, 1);
				player.Out.SendUpdateCraftingSkills();
			}
		}
	}
}