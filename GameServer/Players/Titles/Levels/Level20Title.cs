using System;
using Core.GS.Events;
using Core.GS.Languages;

namespace Core.GS.Players;

public class Level20Title : EventPlayerTitle
{
	/// <summary>
	/// The title description, shown in "Titles" window.
	/// </summary>
	/// <param name="player">The title owner.</param>
	/// <returns>The title description.</returns>
	public override string GetDescription(GamePlayer player)
	{
		return LanguageMgr.TryTranslateOrDefault(player, "!Level 20+!", "Titles.Level.Level20Info");
	}

	/// <summary>
	/// The title value, shown over player's head.
	/// </summary>
	/// <param name="source">The player looking.</param>
	/// <param name="player">The title owner.</param>
	/// <returns>The title value.</returns>
	public override string GetValue(GamePlayer source, GamePlayer player)
	{
		return LanguageMgr.TryTranslateOrDefault(source, "!Level {0}!", "Titles.Level.Level20", player.Level);
	}
	
	/// <summary>
	/// The event to hook.
	/// </summary>
	public override CoreEvent Event
	{
		get { return GamePlayerEvent.LevelUp; }
	}
	
	/// <summary>
	/// Verify whether the player is suitable for this title.
	/// </summary>
	/// <param name="player">The player to check.</param>
	/// <returns>true if the player is suitable for this title.</returns>
	public override bool IsSuitable(GamePlayer player)
	{
		return player.Level >= 20;
	}
	
	/// <summary>
	/// The event callback.
	/// </summary>
	/// <param name="e">The event fired.</param>
	/// <param name="sender">The event sender.</param>
	/// <param name="arguments">The event arguments.</param>
	protected override void EventCallback(CoreEvent e, object sender, EventArgs arguments)
	{
		GamePlayer p = sender as GamePlayer;
		if (p != null && p.Titles.Contains(this))
		{
			p.UpdateCurrentTitle();
			return;
		}
		base.EventCallback(e, sender, arguments);
	}
}