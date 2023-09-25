using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName="Quest")]
	public class DbQuest : DataObject
	{
		private string		m_name;
		private	string		m_characterid;
		private	int			m_step;
		private string		m_customPropertiesString;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbQuest() : this(string.Empty, 1, string.Empty)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">The quest name</param>
		/// <param name="step">The step number</param>
		/// <param name="charname">The character name</param>
		public DbQuest(string name, int step, string charname)
		{
			m_name = name;
			m_step = step;
			m_characterid = charname;
		}
		/// <summary>
		/// Quest Name
		/// </summary>
		[DataElement(AllowDbNull=false,Unique=false)]
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
		/// Quest Step
		/// </summary>
		[DataElement(AllowDbNull=false,Unique=false)]
		public int Step
		{
			get
			{
				return m_step;
			}
			set
			{
				Dirty = true;
				m_step = value;
			}
		}

		/// <summary>
		/// Character Name
		/// </summary>
		[DataElement(AllowDbNull=false,Unique=false,Index=true)]
		public string Character_ID
		{
			get
			{
				return m_characterid;
			}
			set
			{
				Dirty = true;
				m_characterid = value;
			}
		}

		/// <summary>
		/// Custom properties string
		/// </summary>
		[DataElement(AllowDbNull=true,Unique=false)]
		public string CustomPropertiesString
		{
			get
			{
				return m_customPropertiesString;
			}
			set
			{
				Dirty = true;
				m_customPropertiesString = value;
			}
		}
	}
}
