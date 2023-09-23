using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of Mob LootTemplate Relation
	/// </summary>
	[DataTable(TableName = "MobXLootTemplate")]
	public class DbMobXLootTemplate : DataObject
	{
		private string m_MobName = string.Empty;
		private string m_LootTemplateName = string.Empty;
		private int m_dropCount;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbMobXLootTemplate()
		{
			m_MobName = string.Empty;
			m_LootTemplateName = string.Empty;
			m_dropCount = 1;
		}

		/// <summary>
		/// Mob Name
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public string MobName
		{
			get { return m_MobName; }
			set
			{
				Dirty = true;
				m_MobName = value;
			}
		}

		/// <summary>
		/// Loot Template Name
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public string LootTemplateName
		{
			get { return m_LootTemplateName; }
			set
			{
				Dirty = true;
				m_LootTemplateName = value;
			}
		}

		/// <summary>
		/// Drop Count
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int DropCount
		{
			get { return m_dropCount; }
			set
			{
				Dirty = true;
				m_dropCount = value;
			}
		}
	}
}
