using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// The Database Entry for an Outdoor Housing Item
	/// </summary>
	[DataTable(TableName = "DBOutdoorItem")]
	public class DbHouseOutdoorItem : DataObject
	{
		private int m_housenumber;
		private int m_model;
		private int m_position;
		private int m_rotation;

		private string m_baseitemid;

		/// <summary>
		/// The Constructor
		/// </summary>
		public DbHouseOutdoorItem()
		{
		}

		/// <summary>
		/// The House Number
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public int HouseNumber
		{
			get
			{
				return m_housenumber;
			}
			set
			{
				Dirty = true;
				m_housenumber = value;
			}
		}

		/// <summary>
		/// The Model
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Model
		{
			get
			{
				return m_model;
			}
			set
			{
				Dirty = true;
				m_model = value;
			}
		}

		/// <summary>
		/// The Position
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Position
		{
			get
			{
				return m_position;
			}
			set
			{
				Dirty = true;
				m_position = value;
			}
		}

		/// <summary>
		/// The Rotation
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Rotation
		{
			get
			{
				return m_rotation;
			}
			set
			{
				Dirty = true;
				m_rotation = value;
			}
		}

		/// <summary>
		/// The Base Item ID
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string BaseItemID
		{
			get
			{
				return m_baseitemid;
			}
			set
			{
				Dirty = true;
				m_baseitemid = value;
			}
		}
	}
}
