using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Crafted item table
	/// </summary>
	[DataTable(TableName="CraftedItem")]
	public class DbCraftedItem : DataObject
	{
		private string m_craftedItemID;
		private int m_craftinglevel;
		private string m_id_nb;
		private int m_craftingSkillType;
		private bool m_makeTemplated;

		/// <summary>
		/// Create an crafted item
		/// </summary>
		public DbCraftedItem()
		{
			AllowAdd=false;
		}

		/// <summary>
		/// Crafting id of item to craft
		/// </summary>
		[PrimaryKey]
		public string CraftedItemID
		{
			get
			{
				return m_craftedItemID;
			}
			set
			{
				Dirty = true;
				m_craftedItemID = value;
			}
		}

		/// <summary>
		/// Index of item to craft
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true)]
		public string Id_nb
		{
			get
			{
				return m_id_nb;
			}
			set
			{
				Dirty = true;
				m_id_nb = value;
			}
		}

		/// <summary>
		/// Crafting level of this item
		/// </summary>
		[DataElement(AllowDbNull=false,Unique=false)]
		public int CraftingLevel
		{
			get
			{
				return m_craftinglevel;
			}
			set
			{
				Dirty = true;
				m_craftinglevel = value;
			}
		}
			
		/// <summary>
		/// Crafting skill needed to craft this item
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int CraftingSkillType
		{
			get
			{
				return m_craftingSkillType;
			}
			set
			{
				Dirty = true;
				m_craftingSkillType = value;
			}
		}

		/// <summary>
		/// Do we create a templated item or do we create an ItemUnique?
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public bool MakeTemplated
		{
			get
			{
				return m_makeTemplated;
			}
			set
			{
				Dirty = true;
				m_makeTemplated = value;
			}
		}
	}
}
