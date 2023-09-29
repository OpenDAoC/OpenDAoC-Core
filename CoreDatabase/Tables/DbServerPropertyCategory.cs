using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "serverproperty_category")]
	public class DbServerPropertyCategory: DataObject
	{
		private string 	m_base_cat;
		private string 	m_parent_cat;
		private string 	m_display_name;

		public DbServerPropertyCategory()
		{
			m_base_cat = null;
			m_parent_cat = null;
			m_display_name = null;

		}
		
		[DataElement(AllowDbNull = false)]
		public string BaseCategory {
			get { return m_base_cat; }
			set { m_base_cat = value;Dirty=true;}
		}

		[DataElement(AllowDbNull = true)]
		public string ParentCategory {
			get { return m_parent_cat; }
			set { m_parent_cat = value; }
		}
		
		[DataElement(AllowDbNull = false)]
		public string DisplayName {
			get { return m_display_name; }
			set { m_display_name = value;}
		}
	}
}
