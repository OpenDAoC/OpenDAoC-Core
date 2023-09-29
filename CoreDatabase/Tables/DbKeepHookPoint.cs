using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// keep hook point in DB
	/// </summary>
	/// 
	[DataTable(TableName = "KeepHookPoint")]
	public class DbKeepHookPoint : DataObject
	{
		private int m_x;
		private int m_y;
		private int m_z;
		private int m_heading;
		private int m_keepComponentSkinID;
		private int m_hookPointID;
		private int m_height;

		public DbKeepHookPoint()
		{
		}

		/// <summary>
		/// Hook Point
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false, Index = true)]
		public int HookPointID
		{
			get
			{
				return m_hookPointID;
			}
			set
			{
				Dirty = true;
				m_hookPointID = value;
			}
		}

		/// <summary>
		/// skin of component with hookpoint is linked
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
		public int KeepComponentSkinID
		{
			get
			{
				return m_keepComponentSkinID;
			}
			set
			{
				Dirty = true;
				m_keepComponentSkinID = value;
			}
		}

		/// <summary>
		/// Z position of door
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Z
		{
			get
			{
				return m_z;
			}
			set
			{
				Dirty = true;
				m_z = value;
			}
		}

		/// <summary>
		/// Y position of door
		/// </summary>
		[DataElement(AllowDbNull = false)]
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
		/// X position of door
		/// </summary>
		[DataElement(AllowDbNull = false)]
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
		/// Heading of door
		/// </summary>
		[DataElement(AllowDbNull = false)]
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
		/// Height of door
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public int Height
		{
			set
			{
				Dirty = true;
				m_height = value;
			}
			get { return m_height; }
		}
	}
}
