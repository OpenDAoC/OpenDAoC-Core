using System;
using System.Linq;
using Core.GS.Events;
using Core.GS.Languages;
using Core.GS.Players.Specializations;

namespace Core.GS.Players.Titles;

public class MasterLevelTitle : EventPlayerTitle
{
	/// <summary>
	/// Map ML Spec on ML Translate ID
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	private int GetPlayerMLLine(GamePlayer player)
	{
		var mlspec = player.GetSpecList().FirstOrDefault(spec => spec is IMasterLevelsSpecialization);
		
		if(mlspec != null)
		{
			switch(mlspec.KeyName)
			{
				case "Banelord":
					return 1;
				case "Battlemaster":
					return 2;
				case "Convoker":
					return 3;
				case "Perfecter":
					return 4;
				case "Sojourner":
					return 5;
				case "Spymaster":
					return 6;
				case "Stormlord":
					return 7;
				case "Warlord":
					return 8;
				default:
					return 0;
			}
		}
		
		return 0;
	}
	
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
		if (player.MLGranted && player.MLLevel > 0)
		{
			// try get player ML Number
			int mlline = GetPlayerMLLine(player);
			return LanguageMgr.TryTranslateOrDefault(source, string.Format("!ML Title {0}!", mlline), string.Format("Titles.ML.Line{0}", mlline));
		}
		
		return LanguageMgr.TryTranslateOrDefault(source, "!None!", "DetailDisplayHandler.HandlePacket.None");
	}
	
	/// <summary>
	/// The event to hook.
	/// </summary>
	public override CoreEvent Event
	{
		get { return GamePlayerEvent.BecomeML; }
	}
	
	/// <summary>
	/// Verify whether the player is suitable for this title.
	/// </summary>
	/// <param name="player">The player to check.</param>
	/// <returns>true if the player is suitable for this title.</returns>
	public override bool IsSuitable(GamePlayer player)
	{
		return player.MLGranted && player.MLLevel > 0;
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