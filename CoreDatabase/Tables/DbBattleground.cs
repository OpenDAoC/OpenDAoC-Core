using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Stores battleground info
	/// </summary>
	[DataTable(TableName = "Battleground")]
	public class DbBattleground : DataObject
	{
		private ushort m_region;
		private byte m_minLevel;
		private byte m_maxLevel;
		private byte m_maxRealmLevel;

		/// <summary>
		/// Battleground region ID
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public ushort RegionID
		{
			get
			{
				return m_region;
			}
			set
			{
				Dirty = true;
				m_region = value;
			}
		}

		/// <summary>
		/// The minimum level allowed in the battleground
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte MinLevel
		{
			get
			{
				return m_minLevel;
			}
			set
			{
				Dirty = true;
				m_minLevel = value;
			}
		}

		/// <summary>
		/// The maximum level allowed in the battleground
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte MaxLevel
		{
			get
			{
				return m_maxLevel;
			}
			set
			{
				Dirty = true;
				m_maxLevel = value;
			}
		}

		/// <summary>
		/// The maximum realm level allowed in the battleground
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte MaxRealmLevel
		{
			get
			{
				return m_maxRealmLevel;
			}
			set
			{
				Dirty = true;
				m_maxRealmLevel = value;
			}
		}
	}
}
