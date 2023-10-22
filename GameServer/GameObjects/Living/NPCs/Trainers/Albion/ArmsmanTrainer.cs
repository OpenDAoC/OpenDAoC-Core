using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Armsman Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Fighter Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class ArmsmanTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Armsman; }
	}
	/// <summary>
	/// The slash sword item template ID
	/// </summary>
	public const string WEAPON_ID1 = "slash_sword_item";
	/// <summary>
	/// The crush sword item template ID
	/// </summary>
	public const string WEAPON_ID2 = "crush_sword_item";
	/// <summary>
	/// The thrust sword item template ID
	/// </summary>
	public const string WEAPON_ID3 = "thrust_sword_item";
	/// <summary>
	/// The pike polearm item template ID
	/// </summary>
	public const string WEAPON_ID4 = "pike_polearm_item";

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
				player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Defenders of Albion] and defend our realm as an Armsman?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
				case "join the Defenders of Albion":
					
					player.Out.SendMessage(this.Name + " says, \"Very well. Choose a weapon, and you shall become one of us. Which would you have, [slashing], [crushing], [thrusting] or [polearms]?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
					
					break;
				case "slashing":
					
					PromotePlayer(player, (int)EPlayerClass.Armsman, "Here is your Sword of the Initiate. Welcome to the Defenders of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID1);
					
					break;
				case "crushing":
					
					PromotePlayer(player, (int)EPlayerClass.Armsman, "Here is your Mace of the Initiate. Welcome to the Defenders of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID2);
					
					break;
				case "thrusting":
					
					PromotePlayer(player, (int)EPlayerClass.Armsman, "Here is your Rapier of the Initiate. Welcome to the Defenders of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID3);
					
					break;
				case "polearms":
					
					PromotePlayer(player, (int)EPlayerClass.Armsman, "Here is your Pike of the Initiate. Welcome to the Defenders of Albion.", null);
					player.ReceiveItem(this,WEAPON_ID4);
					
					break;
			}
		}
		return true;
	}
}