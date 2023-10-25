using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Fighter Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Fighter Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class FighterTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Fighter; }
	}

	/// <summary>
	/// The practice weapon template ID
	/// </summary>
	public const string PRACTICE_WEAPON_ID = "practice_sword";
	/// <summary>
	/// The practice shield template ID
	/// </summary>
	public const string PRACTICE_SHIELD_ID = "small_training_shield";

	public FighterTrainer() : base(EChampionTrainerType.Fighter)
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
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Armsman], [Paladin], or [Mercenary]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
			if (player.Inventory.GetFirstItemByID(PRACTICE_SHIELD_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [training shield]?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
			case "Armsman":
				if(player.Race == (int)ERace.Avalonian || player.Race == (int)ERace.Briton || player.Race == (int)ERace.HalfOgre || player.Race == (int)ERace.Highlander || player.Race == (int)ERace.Inconnu || player.Race == (int)ERace.Saracen || player.Race == (int)ERace.AlbionMinotaur)
				{
					player.Out.SendMessage(this.Name + " says, \"Ah! An Armsmen is it? Good solid fighters they are! Their fighting prowess is a great asset to Albion. To become an armsman you must enlist with the Defenders of Albion.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of an Armsman is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Mercenary":
				if(player.Race == (int)ERace.Avalonian || player.Race == (int)ERace.Briton || player.Race == (int)ERace.HalfOgre || player.Race == (int)ERace.Highlander || player.Race == (int)ERace.Inconnu || player.Race == (int)ERace.Saracen || player.Race == (int)ERace.AlbionMinotaur)
				{
					player.Out.SendMessage(this.Name + " says, \"You wish to become a Mercenary do you? Roguish fighters in nature, solid warriors in battle, their ability to quickly evade enemy attacks has made them a valuable asset to the Guild of Shadows.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Mercenary is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Paladin":
				if(player.Race == (int) ERace.Avalonian || player.Race == (int) ERace.Briton || player.Race == (int) ERace.Highlander || player.Race == (int) ERace.Saracen){
					player.Out.SendMessage(this.Name + " says, \"You wish to be a defender of the faith I take it? Many a Paladin has led our fighters into battle with victory not far behind. Their never-ending sacrifice proves that the Church of Albion will remain for many centuries!\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Paladin is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
					player.ReceiveItem(this, PRACTICE_SHIELD_ID);
				}
				return true;
		}
		return true;
	}
}