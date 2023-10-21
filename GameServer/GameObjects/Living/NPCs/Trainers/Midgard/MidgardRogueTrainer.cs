using Core.GS.Enums;

namespace Core.GS.Trainer;

[NpcGuildScript("Rogue Trainer", ERealm.Midgard)]		// this attribute instructs DOL to use this script for all "Rogue Trainer" NPC's in Midgard (multiple guilds are possible for one script)
public class MidgardRogueTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.MidgardRogue; }
	}

	public const string PRACTICE_WEAPON_ID = "training_sword_mid";
	
	public MidgardRogueTrainer() : base(eChampionTrainerType.MidgardRogue)
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
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Hunter] or [Shadowblade]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
			case "Hunter":
				if(player.Race == (int) ERace.Dwarf || player.Race == (int) ERace.Kobold || player.Race == (int) ERace.Frostalf || player.Race == (int) ERace.Norseman || player.Race == (int) ERace.Valkyn){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of an Hunter is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Shadowblade":
				if(player.Race == (int) ERace.Kobold || player.Race == (int) ERace.Norseman || player.Race == (int) ERace.Valkyn){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Shadowblade is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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