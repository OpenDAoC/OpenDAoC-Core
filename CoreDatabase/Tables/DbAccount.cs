using System;

namespace Core.Database.Tables
{
	/// <summary>
	/// Account table
	/// </summary>
	[DataTable(TableName="Account")]
	public class DbAccount : DataObject
	{
		private string m_name;
		private string m_password;
		private DateTime m_creationDate;
		private DateTime m_lastLogin;
		private int m_realm;
		private uint m_plvl;
		private int m_state;
		private String m_mail;
		private string m_lastLoginIP;
		private string m_language;
		private string m_lastClientVersion;
		private bool m_isMuted;
		private String m_notes;
		private bool m_isWarned;
		private bool m_isTester;
		private int m_charactersTraded;
		private int m_soloCharactersTraded;
		private string m_discordID;
		private int m_realm_timer_realm;
		private DateTime m_realm_timer_last_combat;
		private DateTime m_lastDisconnected;
		
		/// <summary>
		/// Create account row in DB
		/// </summary>
		public DbAccount()
		{
			m_name = null;
			m_password = null;
			m_creationDate = DateTime.Now;
			m_plvl = 1;
			m_realm = 0;
			m_isMuted = false;
			m_isTester = false;
		}

		/// <summary>
		/// The name of the account (login)
		/// </summary>
		[PrimaryKey]
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				Dirty = true;
				m_name = value;
			}
		}

		/// <summary>
		/// The password of this account encode in MD5 or clear when start with ##
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string Password
		{
			get
			{
				return m_password;
			}
			set
			{
				Dirty = true;
				m_password = value;
			}
		}

		/// <summary>
		/// The date of creation of this account
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public DateTime CreationDate
		{
			get
			{
				return m_creationDate;
			}
			set
			{
				m_creationDate = value;
				Dirty = true;
			}
		}

		/// <summary>
		/// The date of last login of this account
		/// </summary>
		[DataElement(AllowDbNull=true)]
		public DateTime LastLogin
		{
			get
			{
				return m_lastLogin;
			}
			set
			{
				Dirty = true;
				m_lastLogin = value;
			}
		}

		/// <summary>
		/// The realm of this account
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Realm
		{
			get
			{
				return m_realm;
			}
			set
			{
				Dirty = true;
				m_realm = value;
			}
		}

		/// <summary>
		/// The private level of this account (admin=3, GM=2 or player=1)
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public uint PrivLevel
		{
			get
			{
				return m_plvl;
			}
			set
			{
				m_plvl = value;
				Dirty = true;
			}
		}
		
		/// <summary>
		/// Status of this account
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Status {
			get { return m_state; }
			set { Dirty = true; m_state = value; }
		}

		/// <summary>
		/// The mail of this account
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Mail {
			get { return m_mail; }
			set { Dirty = true; m_mail = value; }
		}

		/// <summary>
		/// The last IP logged onto this account
		/// </summary>
		[DataElement(AllowDbNull = true, Index = true)]
		public string LastLoginIP
		{
			get { return m_lastLoginIP; }
			set { m_lastLoginIP = value; }
		}

		/// <summary>
		/// The last Client Version used
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string LastClientVersion
		{
			get { return m_lastClientVersion; }
			set { m_lastClientVersion = value; }
		}

		/// <summary>
		/// The player language
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Language
		{
			get { return m_language; }
			set { Dirty = true; m_language = value.ToUpper(); }
		}

		/// <summary>
		/// Is this account muted from public channels?
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool IsMuted
		{
			get { return m_isMuted; }
			set { Dirty = true; m_isMuted = value; }
		}
		
		/// <summary>
		/// Is this account warned
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool IsWarned
		{
			get { return m_isWarned; }
			set { Dirty = true; m_isWarned = value; }
		}
		
		/// <summary>
		/// Account notes
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Notes {
			get { return m_notes; }
			set { Dirty = true; m_notes = value; }
		}
		
		/// <summary>
		/// Is this account allowed to connect to PTR
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool IsTester
		{
			get { return m_isTester; }
			set { Dirty = true; m_isTester = value; }
		}

		/// <summary>
		/// Number of characters turned in for the challenge titles
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int CharactersTraded
		{
			get { return m_charactersTraded; }
			set { Dirty = true; m_charactersTraded = value; }
		}
		
		/// <summary>
		/// Number of characters turned in for the challenge titles
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int SoloCharactersTraded
		{
			get { return m_soloCharactersTraded; }
			set { Dirty = true; m_soloCharactersTraded = value; }
		}
		
		/// <summary>
		/// Gets the account DiscordID
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string DiscordID
		{
			get { return m_discordID; }
			set { m_discordID = value; }
		}

		/// <summary>
		/// The realm timer current realm of this account
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Realm_Timer_Realm
		{
			get
			{
				return m_realm_timer_realm;
			}
			set
			{
				Dirty = true;
				m_realm_timer_realm = value;
			}
		}

		/// <summary>
		/// The date time of the last pvp combat of this account
		/// </summary>
		[DataElement(AllowDbNull=true)]
		public DateTime Realm_Timer_Last_Combat
		{
			get
			{
				return m_realm_timer_last_combat;
			}
			set
			{
				Dirty = true;
				m_realm_timer_last_combat = value;
			}
		}
		
		/// <summary>
		/// The date time of the last disconnection
		/// </summary>
		[DataElement(AllowDbNull=true)]
		public DateTime LastDisconnected
		{
			get
			{
				return m_lastDisconnected;
			}
			set
			{
				Dirty = true;
				m_lastDisconnected = value;
			}
		}

		/// <summary>
		/// List of characters on this account
		/// </summary>
		[Relation(LocalField = "Name", RemoteField = "AccountName", AutoLoad = true, AutoDelete=true)]
		public DbCoreCharacter[] Characters;

		/// <summary>
		/// List of bans on this account
		/// </summary>
		[Relation(LocalField = "Name", RemoteField = "Account", AutoLoad = true, AutoDelete = true)]
		public DbBans[] BannedAccount;
		
		/// <summary>
		/// List of Custom Params for this account
		/// </summary>
		[Relation(LocalField = "Name", RemoteField = "Name", AutoLoad = true, AutoDelete = true)]
		public DbAccountXCustomParam[] CustomParams;
	}
}
