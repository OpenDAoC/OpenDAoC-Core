using Core.GS.Enums;

namespace Core.GS.Trainer;

[NpcGuildScript("Heretic Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Heretic Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class HereticTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Heretic; }
	}

	public const string WEAPON_ID1 = "chrush_sword_item";

	public HereticTrainer()
		: base()
	{
	}

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
				player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Temple of Arawn] and defend our realm as a Heretic?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
			case "join the Temple of Arawn":
				// promote player to other class
				if (CanPromotePlayer(player))
				{
					PromotePlayer(player, (int)EPlayerClass.Heretic, "Welcome to the Temple of Arawn, " + player.Name + ".", null);
				}
				break;
		}
		return true;
	}
}