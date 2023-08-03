
using DOL.GS.PacketHandler;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// GameKeepTower is the tower in New frontiere link to keep
	/// </summary>
	public class GameKeepTower : AbstractGameKeep
	{
		private GameKeep m_keep;
		/// <summary>
		/// The towers keep
		/// </summary>
		public GameKeep Keep
		{
			set { m_keep = value; }
			get { return m_keep; }
		}

		private int m_ownerKeepID;

		/// <summary>
		/// This is the computed ID of the keep that owns this tower.
		/// Owner Keep may not exist but this number is needed to find tower doors
		/// </summary>
		public int OwnerKeepID
		{
			set { m_ownerKeepID = value; }
			get { return m_ownerKeepID; }
		}

		/// <summary>
		/// The time for a tower to upgrade
		/// </summary>
		/// <returns></returns>
		public override int CalculateTimeToUpgrade()
		{
			return 12 * 60 * 1000;
		}

		/// <summary>
		/// The checks we need to run before we allow a player to claim
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CheckForClaim(GamePlayer player)
		{
			//let gms do everything
			if (player.Client.Account.PrivLevel > 1)
				return true;

			if (player.Group == null)
			{
				player.Out.SendMessage("You must be in a group to claim.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			if (player.Group.MemberCount < ServerProperties.ServerProperties.CLAIM_NUM / 2)
			{
				player.Out.SendMessage("You need " + ServerProperties.ServerProperties.CLAIM_NUM / 2 + " players to claim.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}

			return base.CheckForClaim(player);
		}

		/// <summary>
		/// The RP reward for claiming based on difficulty level
		/// </summary>
		/// <returns></returns>
		public override int CalculRP()
		{
			return ServerProperties.ServerProperties.TOWER_RP_CLAIM_MULTIPLIER * DifficultyLevel;
		}
	}
}