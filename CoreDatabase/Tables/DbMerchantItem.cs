using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName="MerchantItem")]
	public class DbMerchantItem : DataObject
	{
		private string		m_item_list_ID;
		private string		m_id_nb;
		private int			m_page_number;
		private int			m_slot_pos;


		public DbMerchantItem()
		{
		}

		[DataElement(AllowDbNull=false, Index=true)]
		public string ItemListID
		{
			get
			{
				return m_item_list_ID;
			}
			set
			{
				Dirty = true;
				m_item_list_ID = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public string ItemTemplateID
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

		[DataElement(AllowDbNull=false, Index=true)]
		public int PageNumber
		{
			get
			{
				return m_page_number;
			}
			set
			{
				Dirty = true;
				m_page_number = value;
			}
		}

		[DataElement(AllowDbNull=false, Index=true)]
		public int SlotPosition
		{
			get
			{
				return m_slot_pos;
			}
			set
			{
				Dirty = true;
				m_slot_pos = value;
			}
		}
	}
}
