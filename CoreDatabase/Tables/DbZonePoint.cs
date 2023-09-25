using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName="ZonePoint")]
	public class DbZonePoint : DataObject
	{

		private ushort	m_id;
		private int	m_targetX;
		private int	m_targetY;
		private int	m_targetZ;
		private ushort	m_targetRegion;
		private ushort	m_realm;
		private ushort	m_targetHeading;
		private int m_sourceX;
		private int m_sourceY;
		private int m_sourceZ;
		private ushort m_sourceRegion;
		private string	m_classType = string.Empty;

		public DbZonePoint()
		{
			AllowAdd=false;
		}

		[DataElement(AllowDbNull=false, Index=true)]
		public ushort Id
		{
			get
			{
				return m_id;
			}
			set
			{
				Dirty = true;
				m_id = value;
			}
		}

		[DataElement(AllowDbNull= false)]
		public int TargetX
		{
			get
			{
				return m_targetX;
			}
			set
			{
				Dirty = true;
				m_targetX = value;
			}
		}

		[DataElement(AllowDbNull= false)]
		public int TargetY
		{
			get
			{
				return m_targetY;
			}
			set
			{
				Dirty = true;
				m_targetY = value;
			}
		}

		[DataElement(AllowDbNull= false)]
		public int TargetZ
		{
			get
			{
				return m_targetZ;
			}
			set
			{
				Dirty = true;
				m_targetZ = value;
			}
		}

		[DataElement(AllowDbNull= false)]
		public ushort TargetRegion
		{
			get
			{
				return m_targetRegion;
			}
			set
			{
				Dirty = true;
				m_targetRegion = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public ushort TargetHeading
		{
			get
			{
				return m_targetHeading;
			}
			set
			{
				Dirty = true;
				m_targetHeading = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SourceX
		{
			get
			{
				return m_sourceX;
			}
			set
			{
				Dirty = true;
				m_sourceX = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SourceY
		{
			get
			{
				return m_sourceY;
			}
			set
			{
				Dirty = true;
				m_sourceY = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SourceZ
		{
			get
			{
				return m_sourceZ;
			}
			set
			{
				Dirty = true;
				m_sourceZ = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public ushort SourceRegion
		{
			get
			{
				return m_sourceRegion;
			}
			set
			{
				Dirty = true;
				m_sourceRegion = value;
			}
		}

		[DataElement(AllowDbNull= false, Index=true)]
		public ushort Realm
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

		[DataElement(AllowDbNull=true)]
		public string ClassType
		{
			get
			{
				return m_classType;
			}
			set
			{
				Dirty = true;
				m_classType = value;
			}
		}
	}
}