using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "ServerProperty")]
	public class DbServerProperty: DataObject
	{
		private string m_category;
		private string m_key;
		private string m_description;
		private string m_defaultValue;
		private string m_value;

		public DbServerProperty()
		{
			m_category = string.Empty;
			m_key = string.Empty;
			m_description = string.Empty;

			m_defaultValue = string.Empty; ;
			m_value = string.Empty;
		}

		[DataElement(AllowDbNull = false)]
		public string Category
		{
			get
			{
				return m_category;
			}
			set
			{
				m_category = value;
				Dirty = true;
			}
		}

		[PrimaryKey]
		public string Key
		{
			get
			{
				return m_key;
			}
			set
			{
				m_key = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Description
		{
			get
			{
				return m_description;
			}
			set
			{
				m_description = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string DefaultValue
		{
			get
			{
				return m_defaultValue;
			}
			set
			{
				m_defaultValue = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Value
		{
			get
			{
				return m_value;
			}
			set
			{
				m_value = value;
				Dirty = true;
			}
		}
	}
}
