using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Magician Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Magician Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class MagicianTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Magician; }
	}

	public const string PRACTICE_WEAPON_ID = "training_staff";
	
	public MagicianTrainer() : base(EChampionTrainerType.Magician)
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
		if (player.PlayerClass.ID == (int) TrainedClass)
		{
			// player can be promoted
			if (player.Level>=5)
			{
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Eldritch], [Enchanter] or [Mentalist]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
			else
			{
				OfferTraining(player);
			}

			// ask for basic equipment if player doesnt own it
			if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [practice staff]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Eldritch":
				if(player.Race == (int) ERace.Elf || player.Race == (int) ERace.Lurikeen){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Eldritch is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Enchanter":
				if(player.Race == (int) ERace.Elf || player.Race == (int) ERace.Lurikeen){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Enchanter is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Mentalist":
				if(player.Race == (int) ERace.Celt || player.Race == (int) ERace.Elf || player.Race == (int) ERace.Lurikeen || player.Race == (int) ERace.Shar)
				{
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else
				{
					player.Out.SendMessage(this.Name + " says, \"The path of a Mentalist is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "practice staff":
				if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == null)
				{
					player.ReceiveItem(this,PRACTICE_WEAPON_ID);
				}
				return true;
				
		}
		return true;
	}
}