using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DB Keep component is database of keep
	/// </summary>
	[DataTable(TableName="KeepComponent")]
	public class DbKeepComponent : DataObject
	{
		private int m_skin;//todo eskin
		private int m_x;
		private int m_y;
		private int m_heading;
		private int m_height;
		private int m_health;
		private int m_keepID;
		private int m_keepComponentID;
		private string m_createInfo;

		/// <summary>
		/// Create a component of keep (wall, tower,gate, ...)
		/// </summary>
		public DbKeepComponent()
		{
			m_skin = 0;
			m_x = 0;
			m_y = 0;
			m_heading = 0;
			m_height = 0;
			m_health = 0;
			m_keepID = 0;
			m_keepComponentID = 0;
			m_createInfo = string.Empty;
		}

		/// <summary>
		/// Create a component of keep (wall, tower,gate, ...)
		/// </summary>
		public DbKeepComponent(int componentID, int componentSkinID, int componentX, int componentY, int componentHead, int componentHeight, int componentHealth, int keepid, string createInfo) : this()
		{
			m_skin = componentSkinID;
			m_x = componentX;
			m_y = componentY;
			m_heading = componentHead;
			m_height = componentHeight;
			m_health = componentHealth;
			m_keepID = keepid;
			m_keepComponentID = componentID;
			m_createInfo = createInfo;
		}

		/// <summary>
		/// X position of component
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int X
		{
			get
			{
				return m_x;
			}
			set
			{
				Dirty = true;
				m_x = value;
			}
		}

		/// <summary>
		/// Y position of component
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int Y
		{
			get
			{
				return m_y;
			}
			set
			{
				Dirty = true;
				m_y = value;
			}
		}

		/// <summary>
		/// Heading of component
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int Heading
		{
			get
			{
				return m_heading;
			}
			set
			{
				Dirty = true;
				m_heading = value;
			}
		}

		/// <summary>
		/// Health of component
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Health
		{
			get
			{
				return m_health;
			}
			set
			{
				Dirty = true;
				m_health = value;
			}
		}
		
		/// <summary>
		/// Skin of component (see enum skin in GameKeepComponent)
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int Skin
		{
			get
			{
				return m_skin;
			}
			set
			{
				Dirty = true;
				m_skin = value;
			}
		}

		/// <summary>
		/// Index of keep
		/// </summary>
		[DataElement(AllowDbNull= false, Index=true)]
		public int KeepID
		{
			get
			{
				return m_keepID;
			}
			set
			{
				Dirty = true;
				m_keepID = value;
			}
		}

		/// <summary>
		/// Index of component
		/// </summary>
		[DataElement(AllowDbNull= false, Index = true)]
		public int ID
		{
			get
			{
				return m_keepComponentID;
			}
			set
			{
				Dirty = true;
				m_keepComponentID = value;
			}
		}

		[DataElement(AllowDbNull = false, Varchar = 255)]
		public string CreateInfo
		{
			get
			{
				return m_createInfo;
			}
			set
			{
				Dirty = true; m_createInfo = value;
			}
		}
	}
}
