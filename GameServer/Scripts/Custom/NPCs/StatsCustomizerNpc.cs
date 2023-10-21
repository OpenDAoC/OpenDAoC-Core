using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS
{
	public class StatsCustomizerNpc : GameNpc
	{

		private const string StatsResetKey = "StatsReset";
		
		/// <summary>
		/// Constructor
		/// </summary>
		public StatsCustomizerNpc () : base()
		{
		}

		/// <summary>
		/// Called when a player right clicks on the npc
		/// </summary>
		/// <param name="player">Player that interacting</param>
		/// <returns>True if succeeded</returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			TurnTo(player, 5000);
			
			var alreadyReset = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
				.IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(StatsResetKey)));
			
			if(alreadyReset == null)
			{
				SayTo(player, EChatLoc.CL_PopupWindow, $"Hello {player.PlayerClass.Name}, I can grant you a [stats respec] if you need one." );
			}
			else
			{
				SayTo(player, EChatLoc.CL_PopupWindow, "You have already been granted a reset.\n If you haven't used it yet, logout to customise your stats.");
			}

			return true;
		}

		/// <summary>
		/// This function is called when the Living receives a whispered text
		/// </summary>
		/// <param name="source">GameLiving that was whispering</param>
		/// <param name="text">string that was whispered</param>
		/// <returns>true if the string correctly processed</returns>
		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text))
				return false;
			
			GamePlayer player = source as GamePlayer;
			if (player == null)
				return false;
			
			var alreadyReset = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
				.IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(StatsResetKey)));

			if (alreadyReset == null && text == "stats respec")
			{
				SayTo(player, EChatLoc.CL_PopupWindow, "There it is done! Now, you must leave this world for a short time for the magic to work. (You must log out to change your appearance.)");
				player.CustomisationStep = 3;
				
				DbCoreCharacterXCustomParam statsReset = new DbCoreCharacterXCustomParam();
				statsReset.DOLCharactersObjectId = player.ObjectId;
				statsReset.KeyName = StatsResetKey;
				statsReset.Value = "1";
				GameServer.Database.AddObject(statsReset);
				
			}
			return true;
		}
	}
}