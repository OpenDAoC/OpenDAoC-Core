using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Paladin Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Paladin Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class PaladinTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Paladin; }
	}

	public const string WEAPON_ID1 = "slash_sword_item";
	public const string WEAPON_ID2 = "crush_sword_item";
	public const string WEAPON_ID3 = "thrust_sword_item";
	public const string WEAPON_ID4 = "twohand_sword_item";

	public PaladinTrainer() : base()
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
			OfferTraining(player);
		}
		else
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player))
			{
				player.Out.SendMessage(this.Name + " says, \"The church has called out to you young warrior! Will you hear its calling and [join the Church of Albion]? Thus, walking the path of a Paladin forever?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
		
		
		if (CanPromotePlayer(player))
		{
			switch (text)
			{
				case "join the Church of Albion":
					player.Out.SendMessage(this.Name + " says, \"Very well then! Choose your weapon, and your initiation into the Church of Albion will be complete. You may wield [slashing], [crushing], [thrusting] or [two handed] weapons.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
					break;
				case "slashing":
					PromotePlayer(player, (int)EPlayerClass.Paladin, "Here is your Sword of the Initiate. Welcome to the Church of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID1);
					break;
				case "crushing":
					PromotePlayer(player, (int)EPlayerClass.Paladin, "Here is your Mace of the Initiate. Welcome to the Church of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID2);
					break;
				case "thrusting":
					PromotePlayer(player, (int)EPlayerClass.Paladin, "Here is your Rapier of the Initiate. Welcome to the Church of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID3);
					break;
				case "two handed":
					PromotePlayer(player, (int)EPlayerClass.Paladin, "Here is your Great Sword of the Initiate. Welcome to the Church of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID4);
					break;
			}
		}
		return true;
	}
}