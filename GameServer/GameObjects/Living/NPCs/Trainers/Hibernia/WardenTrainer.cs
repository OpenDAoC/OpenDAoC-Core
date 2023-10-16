using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer;

[NpcGuildScript("Warden Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Warden Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class WardenTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Warden; }
	}

	/// <summary>
	/// The free starter armor from trainer
	/// </summary>
	public const string ARMOR_ID1 = "warden_item";


	/// <summary>
	/// Interact with trainer
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player)) return false;
		
		// check if class matches.
		if (player.PlayerClass.ID == (int) TrainedClass)
		{
			player.Out.SendMessage(this.Name + " says, \"Do you wish to learn some more, " + player.Name + "? Step up and receive your training!\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"Nidewst! You wish to follow the Path of Focus and train as a [Warden]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
		
		switch (text) {
			case "Warden":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Warden, "Good then! Welcome to the ways of the Warden! Here, take this as a gift, to start you on the path of a Warden.", null);	// TODO: gifts
					player.ReceiveItem(this,ARMOR_ID1);
				}
				break;
		}
		return true;
	}
}