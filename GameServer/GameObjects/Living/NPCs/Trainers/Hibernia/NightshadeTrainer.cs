using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Nightshade Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Nightshade Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class NightshadeTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Nightshade; }
	}

	public NightshadeTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"Here for a bit of training, " + player.Name + "? Step up and get it!\"", EChatType.CT_System, EChatLoc.CL_PopupWindow); //popup window on live
		} 
		else 
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"You have thought this through, I'm sure. Tell me now if you wish to train as a [Nightshade] and follow the Path of Essence.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
		case "Nightshade":
			// promote player to other class
			if (CanPromotePlayer(player)) {
				PromotePlayer(player, (int)EPlayerClass.Nightshade, "Some would think you mad, choosing to walk through life as a Nightshade. It is not meant for everyone, but I think it will suit you, " + source.GetName(0, false) + ". Here, from me, a small gift to aid you in your journeys.", null);	// TODO: gifts
				//"You receive  Nightshade Initiate Boots from Lierna!"
			}
			break;
		}
		return true;		
	}
}