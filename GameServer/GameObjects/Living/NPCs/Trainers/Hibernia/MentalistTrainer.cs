using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Trainer;

[NpcGuildScript("Mentalist Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Mentalist Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class MentalistTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Mentalist; }
	}

	public const string WEAPON_ID1 = "mentalist_item";

	public MentalistTrainer() : base()
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
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.Interact.Text2", this.Name, player.GetName(0, false)), EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		} 
		else 
		{
			// perhaps player can be promoted
			if (CanPromotePlayer(player)) 
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.Interact.Text1", this.Name, player.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
        String lowerCase = text.ToLower();

        if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.WhisperReceiveCase.Text1"))
        {
            // promote player to other class
            if (CanPromotePlayer(player))
            {
                PromotePlayer(player, (int)EPlayerClass.Mentalist, LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                player.ReceiveItem(this, WEAPON_ID1);
            }
        }
		return true;		
	}
}