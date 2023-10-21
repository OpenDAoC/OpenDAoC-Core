using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer;

[NpcGuildScript("Naturalist Trainer", ERealm.Hibernia)]		// this attribute instructs DOL to use this script for all "Naturalist Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class NaturalistTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Naturalist; }
	}

	public const string PRACTICE_WEAPON_ID = "training_club";
	public const string PRACTICE_SHIELD_ID = "training_shield";

	public NaturalistTrainer() : base(eChampionTrainerType.Naturalist)
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
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Bard], [Druid] or [Warden]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
			else
			{
				OfferTraining(player);
			}

			// ask for basic equipment if player doesnt own it
			if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [practice weapon]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
			if (player.Inventory.GetFirstItemByID(PRACTICE_SHIELD_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [training shield]?\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
			case "Bard":
				if(player.Race == (int) ERace.Celt || player.Race == (int) ERace.Firbolg){
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Bard is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Druid":
				if(player.Race == (int)ERace.Celt || player.Race == (int)ERace.Firbolg || player.Race == (int)ERace.Sylvan || player.Race == (int)ERace.HiberniaMinotaur)
				{
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Druid is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Warden":
				if(player.Race == (int) ERace.Celt || player.Race == (int) ERace.Firbolg || player.Race == (int) ERace.Sylvan)
				{
					player.Out.SendMessage(this.Name + " says, \"I can't tell you something about this class.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				else
				{
					player.Out.SendMessage(this.Name + " says, \"The path of a Warden is not available to your race. Please choose another.\"", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				}
				return true;
			case "practice weapon":
				if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == null)
				{
					player.ReceiveItem(this,PRACTICE_WEAPON_ID);
				}
				return true;
			case "training shield":
				if (player.Inventory.GetFirstItemByID(PRACTICE_SHIELD_ID, EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == null)
				{
					player.ReceiveItem(this,PRACTICE_SHIELD_ID);
				}
				return true;
		}
		return true;
	}
}