namespace Core.GS.Players;

/// <summary>
/// Special "empty" player title, always first in the list.
/// </summary>
public class ClearTitle : APlayerTitle
{
	/// <summary>
	/// The title description, shown in "Titles" window.
	/// </summary>
	/// <param name="player">The title owner.</param>
	/// <returns>The title description.</returns>
	public override string GetDescription(GamePlayer player)
	{
		return "Clear Title";
	}

	/// <summary>
	/// The title value, shown over player's head.
	/// </summary>
	/// <param name="source">The player looking.</param>
	/// <param name="player">The title owner.</param>
	/// <returns>The title value.</returns>
	public override string GetValue(GamePlayer source, GamePlayer player)
	{
		return string.Empty;
	}

	/// <summary>
	/// Verify whether the player is suitable for this title.
	/// </summary>
	/// <param name="player">The player to check.</param>
	/// <returns>true if the player is suitable for this title.</returns>
	public override bool IsSuitable(GamePlayer player)
	{
		return true;
	}
}