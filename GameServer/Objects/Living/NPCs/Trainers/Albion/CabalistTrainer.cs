using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer
{
	/// <summary>
	/// Cabalist Trainer
	/// </summary>
	[NpcGuild("Cabalist Trainer", ERealm.Albion)]		// this attribute instructs DOL to use this script for all "Cabalist Trainer" NPC's in Albion (multiple guilds are possible for one script)
	public class CabalistTrainer : GameTrainer
	{
		public override ECharacterClass TrainedClass
		{
			get { return ECharacterClass.Cabalist; }
		}

		public const string WEAPON_ID = "cabalist_item";

		public CabalistTrainer() : base()
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
					player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Guild of Shadows], become a Cabalist? Your constructs would put fear into the hearts of all who oppose us!\"",EChatType.CT_Say,EChatLoc.CL_PopupWindow);
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
				case "join the Guild of Shadows":
					// promote player to other class
					if (CanPromotePlayer(player)) {
						PromotePlayer(player, (int)ECharacterClass.Cabalist, "Welcome to our guild! You have much to learn, but I see greatness in your future! Here too is your guild weapon, a Staff of Focus!", null);
						player.ReceiveItem(this,WEAPON_ID);
					}
					break;
			}
			return true;
		}
	}
}