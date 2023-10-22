using Core.GS.Crafting;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Players.Titles;

public class CraftTitle : APlayerTitle
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
		if (player.CraftingPrimarySkill == ECraftingSkill.NoCrafting || !player.CraftingSkills.ContainsKey(player.CraftingPrimarySkill))
			return string.Format(LanguageMgr.TryTranslateOrDefault(source, "!BasicCrafting!", "Crafting.Name.BasicCrafting"));
		
		var craftingSkill = CraftingMgr.getSkillbyEnum(player.CraftingPrimarySkill);
		var profession = craftingSkill as AProfession;
		
		if (profession == null)
			return craftingSkill.Name;
		
		return profession.GetTitle(source, player.CraftingSkills[player.CraftingPrimarySkill]);
	}
	
	/// <summary>
	/// Verify whether the player is suitable for this title.
	/// </summary>
	/// <param name="player">The player to check.</param>
	/// <returns>true if the player is suitable for this title.</returns>
	public override bool IsSuitable(GamePlayer player)
	{
		if (player.CraftingPrimarySkill != ECraftingSkill.NoCrafting)
		{
			return true;
		}
		return false;
	}
}