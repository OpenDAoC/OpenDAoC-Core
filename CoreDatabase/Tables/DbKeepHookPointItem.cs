using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "KeepHookPointItem")]
	public class DbKeepHookPointItem : DataObject
	{
		private int m_keepID;
		private int m_componentID;
		private int m_hookPointID;
		private string m_classType;

		public DbKeepHookPointItem()
			: base()
		{ }

		public DbKeepHookPointItem(int keepID, int componentID, int hookPointID, string classType)
			: base()
		{
			m_keepID = keepID;
			m_componentID = componentID;
			m_hookPointID = hookPointID;
			m_classType = classType;
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int KeepID
		{
			get { return m_keepID; }
			set
			{
				Dirty = true;
				m_keepID = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int ComponentID
		{
			get { return m_componentID; }
			set
			{
				Dirty = true;
				m_componentID = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int HookPointID
		{
			get { return m_hookPointID; }
			set
			{
				Dirty = true;
				m_hookPointID = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string ClassType
		{
			get { return m_classType; }
			set
			{
				Dirty = true;
				m_classType = value;
			}
		}
	}
}