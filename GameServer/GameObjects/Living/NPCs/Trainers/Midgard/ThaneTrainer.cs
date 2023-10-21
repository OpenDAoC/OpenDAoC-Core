using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Thane Trainer", ERealm.Midgard)]		// this attribute instructs DOL to use this script for all "Thane Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class ThaneTrainer : GameTrainer
{

	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Thane; }
	}

	public const string WEAPON_ID = "thane_item";

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
				player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the House of Thor] and defend our realm as a Thane?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
			case "join the House of Thor":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Thane, "Welcome young Thane! May your time in Midgard army be rewarding!", null);
					player.ReceiveItem(this, WEAPON_ID);
				}
				break;
		}
		return true;
	}
}