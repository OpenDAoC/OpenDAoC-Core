using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class holds all information that
	/// EVERY object in the game world needs!
	/// </summary>
	public class GameGravestone : GameStaticItem
	{
		/// <summary>
		/// how much xp are stored in this gravestone
		/// </summary>
		private long m_xpValue;
		/// <summary>
		/// Constructs a new empty Gravestone
		/// </summary>
		public GameGravestone(GamePlayer player, long xpValue):base()
		{
			//Objects should NOT be saved back to the DB
			//as standard! We want our mobs/items etc. at
			//the same startingspots when we restart!
			m_saveInDB = false;
			m_name = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGravestone.GameGravestone.Grave", player.Name);
			Heading = player.Heading;
			m_x = player.X;
			m_y = player.Y;
			m_z = player.Z;
			CurrentRegionID = player.CurrentRegionID;
			m_level = 0;

			if (player.Realm == eRealm.Albion)
				m_model = 145; //Albion Gravestone
			else if (player.Realm == eRealm.Midgard)
				m_model = 636; //Midgard Gravestone
			else if (player.Realm == eRealm.Hibernia)
				m_model = 637; //Hibernia Gravestone

			m_xpValue = xpValue;

			m_InternalID = player.InternalID;	// gravestones use the player unique id for themself
			ObjectState=eObjectState.Inactive;
		}
		/// <summary>
		/// returns the xpvalue of this gravestone
		/// </summary>
		public long XPValue
		{
			get
			{
				return m_xpValue;
			}
			set
			{
				m_xpValue = value;
			}
		}
	}
}
