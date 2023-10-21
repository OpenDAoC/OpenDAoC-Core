using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Ranger Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Ranger Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class RangerTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Ranger; }
	}

	public const string WEAPON_ID1 = "ranger_item";

	public RangerTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"You wish to learn more of our ways? Fine then.\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"You know, the way of a [Ranger] is not for everyone. Are you sure this is your choice?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Ranger":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Ranger, "Good then. Your path as a Ranger is before you. Walk it with care, friend. Take these, " + source.GetName(0, false) + ", to help make walking the path a bit easier.", null);
					player.ReceiveItem(this,WEAPON_ID1);
				}
				break;
		}
		return true;
	}
}