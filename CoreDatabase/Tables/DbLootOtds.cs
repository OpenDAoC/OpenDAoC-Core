using DOL.Database.Attributes;

namespace DOL
{
	namespace Database
	{
		[DataTable(TableName="LootOTD")]
		public class DbLootOtds : DataObject
		{
			private string m_itemTemplateID;
			private int m_minLevel;
			private string m_mobName;

			public DbLootOtds()
			{
			}

			/// <summary>
			/// Name of the mob to drop this item
			/// </summary>
			[DataElement(AllowDbNull = false, Varchar = 100, Index = true)]
			public string MobName
			{
				get
				{
					return m_mobName;
				}
				set
				{
					Dirty = true;
					m_mobName = value;
				}
			}

			/// <summary>
			/// The item template id of the OTD
			/// </summary>
			[DataElement(AllowDbNull = false, Varchar = 100, Index = true)]
			public string ItemTemplateID
			{
				get
				{
					return m_itemTemplateID;
				}
				set
				{
					Dirty = true;
					m_itemTemplateID = value;
				}
			}

			/// <summary>
			/// The minimum level required to get drop
			/// </summary>
			[DataElement(AllowDbNull=false)]
			public int MinLevel
			{
				get
				{
					return m_minLevel;
				}
				set
				{
					Dirty = true;
					m_minLevel = value;
				}
			}
		}
	}
}
