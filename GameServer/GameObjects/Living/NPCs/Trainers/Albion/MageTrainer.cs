using Core.GS.Enums;

namespace Core.GS.Trainer;

[NpcGuildScript("Mage Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Mage Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class MageTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Mage; }
	}

	public const string PRACTICE_WEAPON_ID = "trimmed_branch";
	
	public MageTrainer() : base(eChampionTrainerType.Mage)
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
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Cabalist] or [Sorcerer]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				OfferTraining(player);				}

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
			case "Cabalist":
				if(player.Race == (int) ERace.Avalonian || player.Race == (int) ERace.Briton || player.Race == (int) ERace.HalfOgre || player.Race == (int) ERace.Inconnu || player.Race == (int) ERace.Saracen){
					player.Out.SendMessage(this.Name + " says, \"So, you seek to embrace a darker side of magic do you? A Cabalist craving to summon up Golems out of inanimate matter is a true asset to Albion. Yet, because of their thirst for greater power many will not teach this skill. Therefore should one wish to follow this path; they must seek out the Guild of Shadows.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Cabalist is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				return true;
			case "Sorcerer":
				if(player.Race == (int) ERace.Briton || player.Race == (int) ERace.Avalonian || player.Race == (int) ERace.HalfOgre || player.Race == (int) ERace.Inconnu || player.Race == (int) ERace.Saracen){
					player.Out.SendMessage(this.Name + " says, \"So you wish to focus your training towards a pragmatic magic. Sorcerers prove their worth to The Academy by summoning up spells that disrupt, disable, and damage their enemies. In time you may even conjure up beasts to do your bidding.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
				}
				else{
					player.Out.SendMessage(this.Name + " says, \"The path of a Sorcerer is not available to your race. Please choose another.\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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