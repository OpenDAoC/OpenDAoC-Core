namespace DOL.GS.Relics
{
	/// <summary>
	/// Class representing a relic pillar.
	/// </summary>
	public class GameRelicPillar : GameDoorBase
	{
		/// <summary>
		/// Creates a new relic pillar.
		/// </summary>
		public GameRelicPillar() : base()
		{
			Realm = 0;
			Close();
		}

		/// <summary>
		/// Object used for thread synchronization.
		/// </summary>
		private object m_syncPillar = new object();

		private int m_pillarID;

		/// <summary>
		/// ID for this pillar.
		/// </summary>
		public override int DoorID
		{
			get { return m_pillarID; }
			set { m_pillarID = value; }
		}

		/// <summary>
		/// Pillars behave like regular doors.
		/// </summary>
		public override uint Flag
		{
			get { return 0; }
			set { }
		}

		private eDoorState m_pillarState;

		/// <summary>
		/// State of this pillar (up == closed, down == open).
		/// </summary>
		public override eDoorState State
		{
			get { return m_pillarState; }
			set
			{
				if (m_pillarState != value)
				{
					lock (m_syncPillar)
					{
						m_pillarState = value;

						foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							player.SendDoorUpdate(this);
						}
					}
				}
			}
		}

		/// <summary>
		/// Make the pillar start moving down.
		/// </summary>
		public override void Open(GameLiving opener = null)
		{
			State = eDoorState.Open;
		}

		/// <summary>
		/// Reset pillar.
		/// </summary>
		public override void Close(GameLiving closer = null)
		{
			State = eDoorState.Closed;
		}

		/// <summary>
		/// NPCs cannot make pillars move.
		/// </summary>
		/// <param name="npc"></param>
		/// <param name="open"></param>
		public override void NPCManipulateDoorRequest(GameNpc npc, bool open)
		{
		}
	}
}