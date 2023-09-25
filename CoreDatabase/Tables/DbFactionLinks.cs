using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Factions object for database
	/// </summary>
	[DataTable(TableName="LinkedFaction")]
	public class DbFactionLinks : DataObject
	{
		private int	m_factionID;
		private int	m_linkedFactionID;
		private bool	m_friend;

		/// <summary>
		/// Create faction linked to an other
		/// </summary>
		public DbFactionLinks()
		{
			m_factionID = 0;
			m_linkedFactionID = 0;
			m_friend = true;
			AllowAdd = true;//Sinswolf 26.08.2011
		}

		/// <summary>
		/// Index of faction
		/// </summary>
		[DataElement(AllowDbNull=false,Unique=false)]
		public int FactionID
		{
			get
			{
				return m_factionID;
			}
			set
			{
				Dirty = true;
				m_factionID = value;
			}
		}

		/// <summary>
		/// The linked faction index
		/// </summary>
		[DataElement(AllowDbNull= false, Unique=false)]
		public int LinkedFactionID
		{
			get
			{
				return m_linkedFactionID;
			}
			set
			{
				Dirty = true;
				m_linkedFactionID = value;
			}
		}

		/// <summary>
		/// Is faction linked is friend or enemy
		/// </summary>
		[DataElement(AllowDbNull= false, Unique=false)]
		public bool IsFriend
		{
			get
			{
				return m_friend;
			}
			set
			{
				Dirty = true;
				m_friend = value;
			}
		}
	}
}
