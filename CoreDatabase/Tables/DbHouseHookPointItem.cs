using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "househookpointitem")]
	public class DbHouseHookPointItem : DataObject
	{
		private long m_id;
		private int m_houseNumber; // the number of the house
		private uint m_hookpointID;
		private ushort m_heading;
		private string m_templateID; // the item template id of the item placed
		private byte m_index;

		public DbHouseHookPointItem(){}

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


		[DataElement(AllowDbNull = false, Index = true)]
		public int HouseNumber
		{
			get { return m_houseNumber; }
			set
			{
				Dirty = true;
				m_houseNumber = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public uint HookpointID
		{
			get { return m_hookpointID; }
			set
			{
				Dirty = true;
				m_hookpointID = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public ushort Heading
		{
			get { return m_heading; }
			set
			{
				Dirty = true;
				m_heading = value;
			}
		}

		[DataElement(AllowDbNull = true)]
		public string ItemTemplateID
		{
			get { return m_templateID; }
			set
			{
				Dirty = true;
				m_templateID = value;
			}
		}

		/// <summary>
		/// Index of this item in case there is more than 1
		/// of the same type.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public byte Index
		{
			get { return m_index; }
			set
			{
				Dirty = true;
				m_index = value;
			}
		}

		private object m_gameObject = null;
		/// <summary>
		/// The game object attached to this hookpoint
		/// </summary>
		public object GameObject
		{
			get { return m_gameObject; }
			set { m_gameObject = value; }
		}
	}
}
