using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// raw materials for craft item
	/// </summary>
	[DataTable(TableName="CraftedXItem")]
	public class DbCraftedXItem : DataObject
	{
		private string m_ingredientId_nb;
		private int m_count;
		private string m_craftedItemId_nb;

		/// <summary>
		/// create a raw material
		/// </summary>
		public DbCraftedXItem()
		{
			AllowAdd=false;
		}

		/// <summary>
		/// the index
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true)]
		public string CraftedItemId_nb
		{
			get
			{
				return m_craftedItemId_nb;
			}
			set
			{
				Dirty = true;
				m_craftedItemId_nb = value;
			}
		}

		/// <summary>
		/// the raw material used to craft
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string IngredientId_nb
		{
			get
			{
				return m_ingredientId_nb;
			}
			set
			{
				Dirty = true;
				m_ingredientId_nb = value;
			}
		}

		/// <summary>
		/// The count of the raw material to use
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Count
		{
			get
			{
				return m_count;
			}
			set
			{
				Dirty = true;
				m_count = value;
			}
		}
	}
}
