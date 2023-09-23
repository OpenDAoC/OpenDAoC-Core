using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Rank rules in guild
	/// </summary>
	[DataTable(TableName="GuildRank")]
	public class DbGuildRanks : DataObject
	{
		private string	m_guildid;
		private string	m_title;
		private byte	m_ranklevel;
		
		private bool	m_alli;
		private bool	m_emblem;
		private bool	m_gchear;
		private bool	m_gcspeak;
		private bool	m_ochear;
		private bool	m_ocspeak;
		private bool	m_achear;
		private bool	m_acspeak ;
		private bool	m_invite;
		private bool	m_promote;
		private bool	m_remove;
		private bool	m_view;//gc info
		private bool	m_claim;
		private bool	m_upgrade;
		private bool	m_release;
		private bool 	m_buff;
		private bool 	m_dues;
		private bool 	m_withdraw;

		/// <summary>
		/// create rank rules
		/// </summary>
		public DbGuildRanks()
		{
			m_guildid = string.Empty;
			m_title = string.Empty;
			m_ranklevel = 0;
			m_alli = false;
			m_emblem  = false;
			m_gchear  = false;
			m_gcspeak = false;
			m_ochear  = false;
			m_ocspeak = false;
			m_achear  = false;
			m_acspeak = false;
			m_invite  = false;
			m_promote = false;
			m_remove  = false;
			m_buff = false;
			m_dues = false;
			m_withdraw = false;
		}

		/// <summary>
		/// ID of guild
		/// </summary>
		[DataElement(AllowDbNull = true, Index=true)]
		public string GuildID
		{
			get
			{
				return m_guildid;
			}
			set
			{
				Dirty = true;
				m_guildid = value;
			}
		}

		/// <summary>
		/// Title of rank
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Title
		{
			get
			{
				return m_title;
			}
			set
			{
				Dirty = true;
				m_title = value;
			}
		}

		/// <summary>
		/// rank level between 1 and 10
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte RankLevel
		{
			get
			{
				return m_ranklevel;
			}
			set
			{
				Dirty = true;
				m_ranklevel = value;
			}
		}

		/// <summary>
		/// Is player allowed to make alliance
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Alli
		{
			get
			{
				return m_alli;
			}
			set
			{
				Dirty = true;
				m_alli = value;
			}
		}

		/// <summary>
		/// is member alowed to wear alliance
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Emblem
		{
			get
			{
				return m_emblem;
			}
			set
			{
				Dirty = true;
				m_emblem = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public bool Buff
		{
			get
			{
				return m_buff;
			}
			set
			{
				Dirty = true;
				m_buff = value;
			}
		}
		/// <summary>
		/// Can player with this rank hear guild chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool GcHear
		{
			get
			{
				return m_gchear;
			}
			set
			{
				Dirty = true;
				m_gchear = value;
			}
		}

		/// <summary>
		/// Can player with this rank talk on guild chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool GcSpeak
		{
			get
			{
				return m_gcspeak;
			}
			set
			{
				Dirty = true;
				m_gcspeak = value;
			}
		}

		/// <summary>
		/// Can player with this rank hear officier chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool OcHear
		{
			get
			{
				return m_ochear;
			}
			set
			{
				Dirty = true;
				m_ochear = value;
			}
		}

		/// <summary>
		/// Can player with this rank talk on officier chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool OcSpeak
		{
			get
			{
				return m_ocspeak;
			}
			set
			{
				Dirty = true;
				m_ocspeak = value;
			}
		}

		/// <summary>
		/// Can player with this rank hear alliance chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool AcHear
		{
			get
			{
				return m_achear;
			}
			set
			{
				Dirty = true;
				m_achear = value;
			}
		}

		/// <summary>
		/// Can player with this rank talk on alliance chat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool AcSpeak
		{
			get
			{
				return m_acspeak;
			}
			set
			{
				Dirty = true;
				m_acspeak = value;
			}
		}

		/// <summary>
		/// Can player with this rank invite player to join the guild
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Invite
		{
			get
			{
				return m_invite;
			}
			set
			{
				Dirty = true;
				m_invite = value;
			}
		}

		/// <summary>
		/// Can player with this rank promote player in the guild
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Promote
		{
			get
			{
				return m_promote;
			}
			set
			{
				Dirty = true;
				m_promote = value;
			}
		}

		/// <summary>
		/// Can player with this rank removed player from the guild
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Remove
		{
			get
			{
				return m_remove;
			}
			set
			{
				Dirty = true;
				m_remove = value;
			}
		}

		/// <summary>
		/// Can player with this rank view player in the guild
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool View
		{
			get
			{
				return m_view;
			}
			set
			{
				Dirty = true;
				m_view = value;
			}
		}

		/// <summary>
		/// Can player with this rank claim keep
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Claim
		{
			get
			{
				return m_claim;
			}
			set
			{
				Dirty = true;
				m_claim = value;
			}
		}

		/// <summary>
		/// Can player with this rank upgrade keep
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Upgrade
		{
			get
			{
				return m_upgrade;
			}
			set
			{
				Dirty = true;
				m_upgrade = value;
			}
		}

		/// <summary>
		/// Can player with this rank released the keep claimed
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool Release
		{
			get
			{
				return m_release;
			}
			set
			{
				Dirty = true;
				m_release = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public bool Dues
		{
			get
			{
				return m_dues;
			}
			set
			{
				Dirty = true;
				m_dues = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public bool Withdraw
		{
			get
			{
				return m_withdraw;
			}
			set
			{
				Dirty = true;
				m_withdraw = value;
			}
		}
	}
}