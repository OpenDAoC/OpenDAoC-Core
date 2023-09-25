using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "househookpointoffset")]
	public class DbHouseHookPointOffset : DataObject
	{
		private long m_id;
		private int m_houseModel;
		private int m_hookpointID;
		private int m_x;
		private int m_y;
		private int m_z;
		private int m_heading;

		public DbHouseHookPointOffset()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public long ID
		{
			get { return m_id; }
			set
			{
				Dirty = true;
				m_id = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int HouseModel
		{
			get { return m_houseModel; }
			set
			{
				Dirty = true;
				m_houseModel = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int HookpointID
		{
			get { return m_hookpointID; }
			set
			{
				Dirty = true;
				m_hookpointID = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int X
		{
			get { return m_x; }
			set
			{
				Dirty = true;
				m_x = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Y
		{
			get { return m_y; }
			set
			{
				Dirty = true;
				m_y = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Z
		{
			get { return m_z; }
			set
			{
				Dirty = true;
				m_z = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Heading
		{
			get { return m_heading; }
			set
			{
				Dirty = true;
				m_heading = value;
			}
		}
	}
}
