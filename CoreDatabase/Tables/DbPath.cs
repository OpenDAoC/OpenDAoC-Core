using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName="Path")]
	public class DbPath : DataObject
	{
		protected ushort m_region = 0;
		protected string m_pathID = "invalid";
		protected int m_type;//etype
		
		public DbPath()
		{
		}

		public DbPath(string pathid, EPathType type)
		{
			m_pathID = pathid;
			m_type = (int)type;
		}

		[DataElement(AllowDbNull = false,Unique=true)]
		public string PathID {
			get { return m_pathID; }
			set { m_pathID = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int PathType {
			get { return m_type; }
			set { m_type = value; }
		}

		/// <summary>
		/// Used in PathDesigner tool, only. Not in DoL code
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public ushort RegionID
		{
			get { return m_region; }
			set { m_region = value; }
		}
	}
}
