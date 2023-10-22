using Core.GS.Enums;

namespace Core.GS;

[NpcGuildScript("Acolyte Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Acolyte Trainer" NPC's in Albion (multiple guilds are possible for one script)
public class AcolyteTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Acolyte; }
	}

	public const string PRACTICE_WEAPON_ID = "training_mace";
	public const string PRACTICE_SHIELD_ID = "small_training_shield";

	public AcolyteTrainer()
		: base(EChampionTrainerType.Acolyte)
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
			if (player.Level >= 5)
			{
				player.Out.SendMessage(this.Name + " says, \"You must now seek your training elsewhere. Which path would you like to follow? [Cleric], [Heretic], or [Friar]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				OfferTraining(player);
			}

			// ask for basic equipment if player doesnt own it
			if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [practice weapon]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			if (player.Inventory.GetFirstItemByID(PRACTICE_SHIELD_ID, EInventorySlot.MinEquipable, EInventorySlot.LastBackpack) == null)
			{
				player.Out.SendMessage(this.Name + " says, \"Do you require a [training shield]?\"", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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

		switch (text)
		{
			case "Cleric":
				if (player.Race == (int)ERace.Avalonian || player.Race == (int)ERace.Briton || player.Race == (int)ERace.Highlander)
				{
					this.SayTo(player, "So, you wish to serve the Church as healer, defender and leader of our faith. The Church of Albion will welcome one of your skill. Perhaps in time, your commitment will lead others to join our order.");
				}
				else
				{
					this.SayTo(player, "The path of a Cleric is not available to your race. Please choose another.");
				}
				return true;
			case "Friar":
				if (player.Race == (int)ERace.Briton)
				{
					this.SayTo(player, "Members of a brotherhood, you will find more than a community should you join ranks with the Defenders of Albion. Deadly with a Quarterstaff, and proficient with the healing of wounds, the army is in constant need of new recruits such as you.");
				}
				else
				{
					this.SayTo(player, "The path of a Friar is not available to your race. Please choose another.");
				}
				return true;
			case "Heretic":
				if (player.Race == (int)ERace.Briton || player.Race == (int)ERace.Avalonian || player.Race == (int)ERace.Inconnu || player.Race == (int)ERace.AlbionMinotaur)
				{
					this.SayTo(player, "Members of a brotherhood, you will find more than a community should you join ranks with the Defenders of Albion. Deadly with a Quarterstaff, and proficient with the healing of wounds, the army is in constant need of new recruits such as you.");
				}
				else
				{
					this.SayTo(player, "The path of a Friar is not available to your race. Please choose another.");
				}
				return true;
			case "practice weapon":
				if (player.Inventory.GetFirstItemByID(PRACTICE_WEAPON_ID, EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == null)
				{
					player.ReceiveItem(this, PRACTICE_WEAPON_ID);
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