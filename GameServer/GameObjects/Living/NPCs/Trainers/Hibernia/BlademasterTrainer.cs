using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Blademaster Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Blademaster Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class BlademasterTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Blademaster; }
	}

	public const string ARMOR_ID1 = "blademaster_item";

	public BlademasterTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"Do you wish to learn some more, " + player.Name + "? Step up and receive your training!\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"" + player.Name + ", do you choose the Path of Harmony, and life as a [Blademaster]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Blademaster":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Blademaster, "Very well, " + source.GetName(0, false) + ". I gladly take your training into my hands. Congratulations, from this day forth, you are a Blademaster. Here, take this gift to aid you.", null);
					player.ReceiveItem(this,ARMOR_ID1);
				}
				break;
		}
		return true;
	}
}