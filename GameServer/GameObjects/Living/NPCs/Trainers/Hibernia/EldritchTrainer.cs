using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Eldritch Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Eldritch Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class EldritchTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Eldritch; }
	}

	public const string WEAPON_ID1 = "eldritch_item";

	public EldritchTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"Drink up this knowledge, " + player.Name + ", and remember it, for there shall be a day when I no longer rise in the morning, and you may be required to take my place.\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"Greetings, " + player.Name + ". It is my understanding that you have chosen the Path of Focus, and wish to train as an [Eldritch].\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Eldritch":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Eldritch, "I can give you the gift of knowledge, but wisdom you must seek on your own. I welcome you, " + source.GetName(0, false) + ". Here, take this welcoming gift. Use it wisely.", null);
					player.ReceiveItem(this,WEAPON_ID1);
				}
				break;
		}
		return true;
	}
}