using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Cleric Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Cleric Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class ClericTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Cleric; }
	}

	/// <summary>
	/// The crush sword template ID
	/// </summary>
	public const string WEAPON_ID1 = "crush_sword_item";


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
				player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Church of Albion] and walk the path of a Cleric?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
			case "join the Church of Albion":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Cleric, "Welcome my child! Walk the path of light, shout to all the words of our beloved church and rid the land of the faithless! Here is your Mace of the Initiate. It is our standard gift to all new members.", null);
					player.ReceiveItem(this,WEAPON_ID1);
				}
				break;
		}
		return true;
	}
}