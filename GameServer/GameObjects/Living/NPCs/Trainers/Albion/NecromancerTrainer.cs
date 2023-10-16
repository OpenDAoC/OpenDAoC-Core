using System;

namespace DOL.GS.Trainer;

[NpcGuildScript("Necromancer Trainer", ERealm.Albion)]
public class NecromancerTrainer : GameTrainer
{
	public override EPlayerClass TrainedClass
	{
		get { return EPlayerClass.Necromancer; }
	}

	public const string WEAPON_ID = "necromancer_item";

	public NecromancerTrainer()
		: base() { }

	/// <summary>
	/// Interact with trainer.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player)) return false;
		
		// If the player is a necromancer, offer training, if it is a disciple,
		// offer a promotion. Otherwise send them somewhere else.
		if (player.PlayerClass.ID == (int)TrainedClass)
		{
			OfferTraining(player);
		}
		else
		{
			if (CanPromotePlayer(player))
			{
				String message = "Hail, young Disciple. Have you come seeking to imbue yourself with the power of ";
				message += "Arawn and serve as one of his [Necromancers]?";
				SayTo(player, message);
				if (!player.IsLevelRespecUsed)
				{
					OfferRespecialize(player);
				}
			}
			else
				CheckChampionTraining(player);
		}
		return true;
	}

	/// <summary>
	/// Talk to the trainer.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="text"></param>
	/// <returns></returns>
	public override bool WhisperReceive(GameLiving source, string text)
	{
		if (!base.WhisperReceive(source, text)) return false;
		GamePlayer player = source as GamePlayer;
		
		switch (text.ToLower())
		{
			case "necromancers":
				String message = "The necromancer is a cloth wearing priest of Arawn, lord of the underworld. ";
				message += "Due to their allegiance to the dark master, they are granted spellcasting and combat ";
				message += "prowess that is unique in Albion. Unlike other casters, a Necromancer is powerless ";
				message += "until it calls upon the dark magic of Arawn to transform itself into a fearsome ";
				message += "servant, controlled by an ethereal \"shade\". As a shade, the Necro appears as a ";
				message += "translucent shadowy, floating ghost that cannot be wounded by melee or spells while ";
				message += "the servant carries out their dark commands. When the servant is slain or released ";
				message += "in combat, the Necromancer returns to mortal form, alive, but with a very small ";
				message += "amount of health. Necromancers have the power to wound their enemies and the pain ";
				message += "they inflict brings them a surge of power or restores life to their servant. Do you ";
				message += "wish to dedicate yourself to Lord Arawn and [join the Temple of Arawn]?";
				SayTo(player, message);
				break;

			case "join the temple of arawn":
				if (CanPromotePlayer(player))
				{
					PromotePlayer(player, (int)EPlayerClass.Necromancer,
					              "Lord Arawn has accepted you into his Temple. Here is his gift to you. Use it well, Disciple.", null);
					player.ReceiveItem(this, WEAPON_ID);
				}
				break;
		}
		return true;
	}
}