using DOL.Language;

namespace DOL.GS.PlayerTitles
{
	/// <summary>
	/// Craft Title Handler
	/// </summary>
	public class CraftTitle : SimplePlayerTitle
	{
		/// <summary>
		/// The title description, shown in "Titles" window.
		/// </summary>
		/// <param name="player">The title owner.</param>
		/// <returns>The title description.</returns>
		public override string GetDescription(GamePlayer player)
		{
			return GetValue(player, player);
		}
		
		/// <summary>
		/// The title value, shown over player's head.
		/// </summary>
		/// <param name="source">The player looking.</param>
		/// <param name="player">The title owner.</param>
		/// <returns>The title value.</returns>
		public override string GetValue(GamePlayer source, GamePlayer player)
		{
			if (player.CraftingPrimarySkill == eCraftingSkill.NoCrafting || !player.CraftingSkills.TryGetValue(player.CraftingPrimarySkill, out int craftingLevel))
				return string.Format(LanguageMgr.TryTranslateOrDefault(source, "!BasicCrafting!", "Crafting.Name.BasicCrafting"));

			AbstractCraftingSkill craftingSkill = CraftingMgr.getSkillbyEnum(player.CraftingPrimarySkill);
			return craftingSkill is AbstractProfession profession ? profession.GetTitle(source, craftingLevel) : craftingSkill.Name;
		}

		/// <summary>
		/// Verify whether the player is suitable for this title.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <returns>true if the player is suitable for this title.</returns>
		public override bool IsSuitable(GamePlayer player)
		{
			if (player.CraftingPrimarySkill != eCraftingSkill.NoCrafting)
			{
				return true;
			}
			return false;
		}
	}
}
