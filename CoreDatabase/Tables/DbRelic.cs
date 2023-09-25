using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DB relic is database of relic
	/// </summary>
	[DataTable(TableName = "Relic")]
	public class DbRelic : DataObject
	{
		private int m_relicID;
		private int m_region;
		private int m_x;
		private int m_y;
		private int m_z;
		private int m_heading;
		private int m_realm;
		private int m_originalRealm;
		private int m_lastRealm;
		private int m_type;
		private DateTime m_lastCaptureDate;


		/// <summary>
		/// Create a relic row
		/// </summary>
		public DbRelic(){}

		/// <summary>
		/// Index of relic
		/// </summary>
		[PrimaryKey]
		public int RelicID
		{
			get
			{
				return m_relicID;
			}
			set
			{
				Dirty = true;
				m_relicID = value;
			}
		}

		/// <summary>
		/// Region of relic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Region
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
		/// X position of relic
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
		/// Y position of relic
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
		/// Z position of relic
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
		/// heading of relic
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
		/// Realm of relic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Realm
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


		/// <summary>
		/// The default realm of this relic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int OriginalRealm
		{
			get
			{
				return m_originalRealm;
			}
			set
			{
				Dirty = true;
				m_originalRealm = value;
			}
		}

		/// <summary>
		/// The last realm that captured this relic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int LastRealm
		{
			get
			{
				return m_lastRealm;
			}
			set
			{
				Dirty = true;
				m_lastRealm = value;
			}
		}


		/// <summary>
		/// relic type, 0 is melee, 1 is magic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int relicType
		{
			get
			{
				return m_type;
			}
			set
			{
				Dirty = true;
				m_type = value;
			}
		}
		
		[DataElement(AllowDbNull=true)]
		public DateTime LastCaptureDate
		{
			get
			{
				return m_lastCaptureDate;
			}
			set
			{
				Dirty = true;
				m_lastCaptureDate = value;
			}
		}
	}
}
