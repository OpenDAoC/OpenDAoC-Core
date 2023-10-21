using Core.GS.Enums;

namespace Core.GS.Trainer;

[NpcGuildScript("Infiltrator Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Infiltrator Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class InfiltratorTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Infiltrator; }
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
				player.Out.SendMessage(this.Name + " says, \"You have come far to find us! Is it your wish to [join the Guild of Shadows] and become our dagger of the night? An Infiltrator!\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
			case "join the Guild of Shadows":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Infiltrator, "TODO: (correct text) You joined the Guild of Shadows as an Infiltrator.", null);	// TODO: gifts
				}
				break;
		}
		return true;
	}
}