using Core.GS.PacketHandler;

namespace Core.GS.Trainer;

[NpcGuildScript("Viking Trainer", ERealm.Midgard)]		// this attribute instructs DOL to use this script for all "Acolyte Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class VikingTrainer : GameTrainer
{
	public const string PRACTICE_WEAPON_ID = "training_axe";

	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Viking; }
	}

	public VikingTrainer() : base(eChampionTrainerType.Viking)
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
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Warrior], [Berserker], [Skald] or [Thane]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
			case "Warrior":
				if(player.Race == (int) ERace.Dwarf || player.Race == (int) ERace.Kobold || player.Race == (int) ERace.Norseman || player.Race == (int) ERace.Troll || player.Race == (int) ERace.Valkyn || player.Race == (int)ERace.MidgardMinotaur){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Warrior is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Berserker":
				if(player.Race == (int)ERace.Dwarf || player.Race == (int)ERace.Troll || player.Race == (int)ERace.Norseman || player.Race == (int)ERace.Valkyn || player.Race == (int)ERace.MidgardMinotaur)
				{
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Berserker is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Skald":
				if(player.Race == (int) ERace.Dwarf || player.Race == (int) ERace.Kobold || player.Race == (int) ERace.Norseman || player.Race == (int) ERace.Troll){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Skald is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Thane":
				if(player.Race == (int) ERace.Dwarf || player.Race == (int) ERace.Frostalf || player.Race == (int) ERace.Norseman || player.Race == (int) ERace.Troll)
				{
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else
				{
					player.Out.SendMessage(this.Name + " says, \"The path of a Thane is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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