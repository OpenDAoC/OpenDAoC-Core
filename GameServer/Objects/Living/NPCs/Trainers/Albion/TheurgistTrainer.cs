using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer
{
	/// <summary>
	/// Theurgist Trainer
	/// </summary>
	[NpcGuild("Theurgist Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Theurgist Trainer" NPC's in Albion (multiple guilds are possible for one script)
	public class TheurgistTrainer : GameTrainer
	{
		public override ECharacterClass TrainedClass
		{
			get { return ECharacterClass.Theurgist; }
		}

		public const string WEAPON_ID = "theurgist_item";

		public TheurgistTrainer() : base()
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
			if (player.CharacterClass.ID == (int)TrainedClass)
			{
				OfferTraining(player);
			}
			else
			{
				// perhaps player can be promoted
				if (CanPromotePlayer(player))
				{
					player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Defenders of Albion] and feel the magic of creation as a Theurgist?\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
			
			switch (text) {
				case "join the Defenders of Albion":
					// promote player to other class
					if (CanPromotePlayer(player)) {
						PromotePlayer(player, (int)ECharacterClass.Theurgist, "I know you shall do your best to guard the realm from those that would harm it! To help you with this task, here is a gift from the Defenders! Use it well!", null);
						player.ReceiveItem(this,WEAPON_ID);
					}
					break;
			}
			return true;
		}
	}
}