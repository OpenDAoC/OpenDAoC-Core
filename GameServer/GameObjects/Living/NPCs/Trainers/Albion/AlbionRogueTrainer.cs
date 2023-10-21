using Core.GS.Enums;

namespace Core.GS.Trainer;

[NpcGuildScript("Rogue Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Rogue Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class AlbionRogueTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.AlbionRogue; }
	}

	public const string PRACTICE_WEAPON_ID = "practice_dirk";
	
	public AlbionRogueTrainer() : base(eChampionTrainerType.AlbionRogue)
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
		
		// check if class matches
		if (player.PlayerClass.ID == (int)TrainedClass)
		{
			// player can be promoted
			if (player.Level>=5)
			{
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Infiltrator], [Minstrel], or [Scout]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				OfferTraining(player);
			}

			// ask for basic equipment if player doesnt own it
			if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [practice weapon]?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
			}
		}
		else
		{
			CheckChampionTraining(player);
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
			case "Infiltrator":
				if(player.Race == (int) ERace.Briton || player.Race == (int) ERace.Saracen || player.Race == (int) ERace.Inconnu){
					player.Out.SendMessage(this.Name + " says, \"You seek a tough life if you go that path. The life of an Infiltrator involves daily use of his special skills. The Guild of Shadows has made its fortune by using them to sneak, hide, disguise, backstab, and spy on the enemy. Without question they are an invaluable asset to Albion.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of an Infiltrator is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Minstrel":
				if(player.Race == (int) ERace.Briton || player.Race == (int) ERace.Saracen || player.Race == (int) ERace.Highlander){
					player.Out.SendMessage(this.Name + " says, \"Ah! To sing the victories of Albion! To give praise to those who fight to keep the light of Camelot eternal. Many have studied their skill within the walls of The Academy and come forth to defend this realm. Without their magical songs, many would not be here today.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Minstrel is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Scout":
				if(player.Race == (int) ERace.Briton || player.Race == (int) ERace.Saracen || player.Race == (int) ERace.Highlander || player.Race == (int) ERace.Inconnu){
					player.Out.SendMessage(this.Name + " says, \"Ah! You wish to join the Defenders of Albion eh? That is quite a good choice for a Rogue. A Scouts accuracy with a bow is feared by all our enemies and has won Albion quite a few battles.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Scout is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "practice weapon":
				if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == null)
				{
					player.ReceiveItem(this,PRACTICE_WEAPON_ID);
				}
				return true;
		}
		return true;
	}
}