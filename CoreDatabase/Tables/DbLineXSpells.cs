using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName="LineXSpell")]
	public class DbLineXSpells : DataObject
	{
		protected string m_line_name;
		protected int m_spellid;
		protected int m_level;

		public DbLineXSpells()
		{
			AllowAdd = false;
		}

		[DataElement(AllowDbNull=false, Index=true)]
		public string LineName
		{
			get
			{
				return m_line_name;
			}
			set
			{
				Dirty = true;
				m_line_name = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int SpellID
		{
			get
			{
				return m_spellid;
			}
			set
			{
				Dirty = true;
				m_spellid = value;
			}
		}

		[DataElement(AllowDbNull=false)]
		public int Level
		{
			get
			{
				return m_level;
			}
			set
			{
				Dirty = true;
				m_level = value;
			}
		}
	}
}