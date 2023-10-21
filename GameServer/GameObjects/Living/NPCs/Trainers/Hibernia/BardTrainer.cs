using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer;

[NpcGuildScript("Bard Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Bard Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class BardTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Bard; }
	}

	public const string WEAPON_ID1 = "bard_item";

	public BardTrainer() : base()
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
		if (player.PlayerClass.ID == (int) TrainedClass)
		{
			player.Out.SendMessage(this.Name + " says, \"Ahh, well met " + player.Name + ". Back for more lore and knowledge, eh.\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"Do you wish to walk the Path of Essence, pursuing a life of storytelling and [song]? Of brave deeds which grow braver with every telling?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "song":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Bard, "Welcome then, " + source.GetName(0, false) + ", to the Bard's life. Here, take this. Keep it well, " + source.GetName(0, false) + ", for the tools of our trade can be quite expensive.", null);
					player.ReceiveItem(this,WEAPON_ID1);
				}
				break;
		}
		return true;
	}
}