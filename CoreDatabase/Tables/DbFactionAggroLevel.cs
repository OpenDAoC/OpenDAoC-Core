using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Aggro level of faction against character
	/// </summary>
	/// 
	[DataTable(TableName = "FactionAggroLevel")]
	public class DbFactionAggroLevel : DataObject
	{
		private string m_characterID;
		private int m_factionID;
		private int m_AggroLevel;

		/// <summary>
		/// Create faction aggro level against character
		/// </summary>
		public DbFactionAggroLevel()
		{
			m_characterID = string.Empty;
			m_factionID = 0;
			m_AggroLevel = 0;
		}

		/// <summary>
		/// Character
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar = 100, Index = true)]
		public string CharacterID
		{
			get
			{
				return m_characterID;
			}
			set
			{
				Dirty = true;
				m_characterID = value;
			}
		}

		/// <summary>
		/// index of this faction
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
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
		/// aggro level/ relationship of faction against character
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
		public int AggroLevel
		{
			get
			{
				return m_AggroLevel;
			}
			set
			{
				Dirty = true;
				m_AggroLevel = value;
			}
		}
	}
}
