using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DBalliance is table for alliance of guild
	/// </summary>
	[DataTable(TableName="GuildAlliance")]
	public class DbGuildAlliance : DataObject
	{
		private string	m_allianceName;
		private string	m_motd;
		private string m_leaderGuildID;

		/// <summary>
		/// create an alliance
		/// </summary>
		public DbGuildAlliance()
		{
			m_allianceName = "default alliance name";
		}

		/// <summary>
		/// Name of the alliance
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string AllianceName
		{
			get
			{
				return m_allianceName;
			}
			set
			{
				Dirty = true;
				m_allianceName = value;
			}
		}

		/// <summary>
		/// Message Of The Day  of the Alliance
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Motd
		{
			get
			{
				return m_motd;
			}
			set
			{
				Dirty = true;
				m_motd = value;
			}
		}
		
		
		/// <summary>
		/// Leader Guild for this Alliance
		/// </summary>
		[DataElement(AllowDbNull = true, Unique = true)]
		public string LeaderGuildID
		{
			get { return m_leaderGuildID; }
			set { Dirty = true; m_leaderGuildID = value; }
		}

		/// <summary>
		/// Guild leader of alliance
		/// </summary>
		[Relation(LocalField = "LeaderGuildID", RemoteField = "GuildID", AutoLoad = true, AutoDelete = false)]
		public DbGuild DBguildleader;

		/// <summary>
		/// All guild in this alliance
		/// </summary>
		[Relation(LocalField = "GuildAlliance_ID", RemoteField = "AllianceID", AutoLoad = true, AutoDelete = false)]
		public DbGuild[] DBguilds;
	}
}