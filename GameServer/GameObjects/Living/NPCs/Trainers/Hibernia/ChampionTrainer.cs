using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Champion Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Champion Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class ChampionTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Champion; }
	}

	public const string ARMOR_ID1 = "champion_item";

	public ChampionTrainer() : base()
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
			player.Out.SendMessage(this.Name + " says, \"I'm glad to see you taking an interest in your training, " + player.Name + ". There is always room to grow and learn!\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"Champions follow the Path of Essence. Choose now to become a [Champion], and I will train you in our ways, and the ways of the Path we follow.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Champion":
				// promote player to other class
				if (CanPromotePlayer(player)) {
					PromotePlayer(player, (int)EPlayerClass.Champion, "Welcome " + source.GetName(0, false) + ". Let us see if you will become a worthy Champion. Take this gift, " + source.GetName(0, false) + ". It is to aid you while you grow into a true Champion.", null);
					player.ReceiveItem(this,ARMOR_ID1);
				}
				break;
		}
		return true;
	}
}