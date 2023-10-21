namespace Core.GS.PlayerTitles
{
	/// <summary>
	/// Denotes a class as a player title.
	/// </summary>
	public interface IPlayerTitle
	{
		/// <summary>
		/// The title description, shown in "Titles" window.
		/// </summary>
		/// <param name="player">The title owner.</param>
		/// <returns>The title description.</returns>
		string GetDescription(GamePlayer player);
		/// <summary>
		/// The title value, shown over player's head.
		/// </summary>
		/// <param name="source">The player looking.</param>
		/// <param name="player">The title owner.</param>
		/// <returns>The title value.</returns>
		string GetValue(GamePlayer source, GamePlayer player);
		/// <summary>
		/// Checks whether this title can be changed by the player.
		/// </summary>
		/// <param name="player">The title owner.</param>
		/// <returns>True if player can not change the title.</returns>
		bool IsForced(GamePlayer player);
		/// <summary>
		/// Verify whether the player is suitable for this title.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <returns>true if the player is suitable for this title.</returns>
		bool IsSuitable(GamePlayer player);
		/// <summary>
		/// Callback for when player gains this title.
		/// </summary>
		/// <param name="player">The player that gained a title.</param>
		void OnTitleGained(GamePlayer player);
		/// <summary>
		/// Callback for when player loose this title.
		/// </summary>
		/// <param name="player">The player that lost a title.</param>
		void OnTitleLost(GamePlayer player);
	}
}