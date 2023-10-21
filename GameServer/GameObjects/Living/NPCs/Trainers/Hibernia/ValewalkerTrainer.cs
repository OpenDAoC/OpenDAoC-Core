using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Valewalker Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Valewalker Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class ValewalkerTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Valewalker; }
	}

	public const string WEAPON_ID1 = "valewalker_item";
	public ValewalkerTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"Training makes for a strong, healthy Hero! Keep up the good work, " + player.Name + "!\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"You wish to follow the [Path of Affinity] and walk as a Valewalker?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Path of Affinity":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Valewalker, "Welcome, then, to the Path of Affinity. Here is a gift. Consider it a welcoming gesture. Welcome, " + source.GetName(0, false) + ".", null);
					player.ReceiveItem(this,WEAPON_ID1);
				}
				break;
		}
		return true;
	}
}