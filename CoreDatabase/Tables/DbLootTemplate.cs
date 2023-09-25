using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of Tasks
	/// </summary>
	[DataTable(TableName="LootTemplate")]
	public class DbLootTemplate : DataObject
	{
		protected string	m_TemplateName = string.Empty;
		protected string	m_ItemTemplateID = string.Empty;
		protected int		m_Chance = 99;
		protected int		m_count = 1;
		
		public DbLootTemplate()
		{
		}
		
		[DataElement(AllowDbNull=false, Index=true)]
		public string TemplateName
		{
			get {return m_TemplateName;}
			set
			{
				Dirty = true;
				m_TemplateName = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public string ItemTemplateID
		{
			get {return m_ItemTemplateID;}
			set
			{
				Dirty = true;
				m_ItemTemplateID = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int Chance
		{
			get {return m_Chance;}
			set
			{
				Dirty = true;
				m_Chance = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Count
		{
			get
			{
				return Math.Max(1, m_count);
			}
			set
			{
				Dirty = true;
				m_count = value;
			}
		}
	}
}
