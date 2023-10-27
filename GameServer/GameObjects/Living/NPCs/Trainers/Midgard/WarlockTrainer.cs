using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Warlock Trainer", ERealm.Midgard)]		// this attribute instructs DOL to use this script for all "Warlock Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class WarlockTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Warlock; }
	}

	public const string WEAPON_ID = "warlock_item";

	/// <summary>
	/// Interact with trainer
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player)) return false;

		// check if class matches.
		if (player.PlayerClass.ID == (int)TrainedClass)
		{
			OfferTraining(player);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the House of Hel] and defend our realm as a Warlock?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				if (!player.IsLevelRespecUsed)
				{
					OfferRespecialize(player);
				}
			}
			else
			{
				CheckChampionTraining(player);
			}
		}
		return true;
	}

	/// <summary>
	/// Talk to trainer
	/// </summary>
	/// <param name="source"></param>
	/// <param name="text"></param>
	/// <returns></returns>
	public override bool WhisperReceive(GameLiving source, string text)
	{
		if (!base.WhisperReceive(source, text)) return false;
		GamePlayer player = source as GamePlayer;

		switch (text)
		{
			case "join the House of Hel":
				// promote player to other class
				if (CanPromotePlayer(player))
				{
					PromotePlayer(player, (int)EPlayerClass.Warlock, "Welcome young Warlock! May your time in Midgard army be rewarding!", null);
					player.ReceiveItem(this, WEAPON_ID);
				}
				break;
		}
		return true;
	}
}