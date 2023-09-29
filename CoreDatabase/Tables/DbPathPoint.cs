using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName="PathPoints")]
	public class DbPathPoint : DataObject
	{
		protected string m_pathID = "invalid";
		protected int m_step;
		protected int m_x;
		protected int m_y;
		protected int m_z;
		protected int m_maxspeed;
		protected int m_waitTime;

		public DbPathPoint()
		{
		}

		public DbPathPoint(int x, int y, int z, int maxspeed)
		{
			m_x = x;
			m_y = y;
			m_z = z;
			m_maxspeed = maxspeed;
			m_waitTime = 0;
		}

		[DataElement(AllowDbNull = false, Index=true)]
		public String PathID {
			get { return m_pathID; }
			set { m_pathID = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int Step {
			get { return m_step; }
			set { m_step = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int X {
			get { return m_x; }
			set { m_x = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int Y {
			get { return m_y; }
			set { m_y = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int Z {
			get { return m_z; }
			set { m_z = value; }
		}

		/// <summary>
		/// Maximum speed, 0 = no limit
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int MaxSpeed {
			get { return m_maxspeed; }
			set { m_maxspeed = value; }
		}

		[DataElement(AllowDbNull = false)]
		public int WaitTime
		{
			get { return m_waitTime; }
			set { m_waitTime = value; }
		}

	}
}
