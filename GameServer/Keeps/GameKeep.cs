
using System.Collections;
using DOL.GS.PacketHandler;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// GameKeep is the keep in New Frontiere
	/// </summary>
	public class GameKeep : AbstractGameKeep
	{
		public GameKeep()
			: base()
		{
		}

		/// <summary>
		/// time to upgrade from one level to another
		/// </summary>
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
		
		private ArrayList m_towers = new ArrayList(4);
		/// <summary>
		/// The Keep Towers
		/// </summary>
		public ArrayList Towers
		{
			get { return m_towers; }
			set { m_towers = value; }
		}

		public bool OwnsAllTowers
		{
			get
			{
				foreach (GameKeepTower tower in this.Towers)
				{
					if (tower.Realm != this.Realm)
						return false;
				}
				return true;
			}
		}

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
			//let gms do everything
			if (player.Client.Account.PrivLevel > 1)
				return true;

			if (player.Group == null)
			{
				player.Out.SendMessage("You must be in a group to claim.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return false;
			}
			if (player.Group.MemberCount < ServerProperties.ServerProperties.CLAIM_NUM)
			{
				player.Out.SendMessage("You need " + ServerProperties.ServerProperties.CLAIM_NUM + " players to claim.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
			return ServerProperties.ServerProperties.KEEP_RP_CLAIM_MULTIPLIER * DifficultyLevel;
		}

		/// <summary>
		/// Add a tower to the keep
		/// </summary>
		/// <param name="tower"></param>
		public void AddTower(GameKeepTower tower)
		{
			if (!m_towers.Contains(tower))
				m_towers.Add(tower);
		}
	}
}