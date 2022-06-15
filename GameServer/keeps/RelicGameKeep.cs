using DOL.GS.PacketHandler;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// GameKeep is the keep in New Frontiere
	/// </summary>
	public class RelicGameKeep : AbstractGameKeep
	{
		public RelicGameKeep()
			: base()
		{
		}

		/// <summary>
		/// time to upgrade from one level to another
		/// </summary>
		///

		public static int[] UpgradeTime =
		{
			12*60*1000, // 0 12min
			12*60*1000, // 1 12min
			12*60*1000, // 2 12min
			12*60*1000, // 3 12min
			12*60*1000, // 4 12min
			24*60*1000, // 5 24min
			60*60*1000, // 6 60min 1h
			120*60*1000, // 7 120min 2h
			240*60*1000, // 8 240min 4h
			480*60*1000, // 9 480min 8h
			960*60*1000, // 10 960min 16h
		};

		/// <summary>
		/// The time to upgrade a keep
		/// </summary>
		/// <returns></returns>
		public override int CalculateTimeToUpgrade()
		{
			if (Level < 10)
				return UpgradeTime[this.Level + 1];
			else 
				return UpgradeTime[this.Level - 1];
		}

		/// <summary>
		/// The checks we need to run before allowing claim
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CheckForClaim(GamePlayer player)
		{

			player.Out.SendMessage("Relic keeps cannot be claimed.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return false;

		}

		/// <summary>
		/// The RP reward for claiming based on difficulty level
		/// </summary>
		/// <returns></returns>
		public override int CalculRP()
		{
			return 0;
		}
	}
}
