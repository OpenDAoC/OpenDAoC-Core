using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Spell Lines Tables Referencing Spell from Base or Spec Lines, Attach to Specialization using Spec KeyName
	/// </summary>
	[DataTable(TableName="SpellLine")]
	public class DbSpellLine : DataObject
	{
		protected int m_spellLineID;
		protected string m_name="unknown";
		protected string m_keyname;
		protected string m_spec="unknown";
		protected bool m_isBaseLine=true;
		protected int m_classIDHint;
		
		public DbSpellLine()
		{
			AllowAdd = false;
		}

		/// <summary>
		/// Primary Key Auto Inc
		/// </summary>
		[PrimaryKey(AutoIncrement=true)]
		public int SpellLineID {
			get { return m_spellLineID; }
			set { Dirty = true; m_spellLineID = value; }
		}
		
		/// <summary>
		/// Spell Line Key Name
		/// </summary>
		[DataElement(AllowDbNull=false, Unique=true)]
		public string KeyName
		{
			get
			{
				return m_keyname;
			}
			set
			{
				Dirty = true;
				m_keyname = value;
			}
		}

		/// <summary>
		/// Spell Line Display Name
		/// </summary>
		[DataElement(AllowDbNull=true, Varchar=255)]
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				Dirty = true;
				m_name = value;
			}
		}

		/// <summary>
		/// Specialization Key Name for Reference. (FK)
		/// </summary>
		[DataElement(AllowDbNull=true, Varchar=100, Index=true)]
		public string Spec
		{
			get
			{
				return m_spec;
			}
			set
			{
				Dirty = true;
				m_spec = value;
			}
		}

		/// <summary>
		/// Baseline or Specline ?
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public bool IsBaseLine
		{
			get
			{
				return m_isBaseLine;
			}
			set
			{
				Dirty = true;
				m_isBaseLine = value;
			}
		}
		
		/// <summary>
		/// Class ID hint or other values used by Specialization Handler
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int ClassIDHint {
			get { return m_classIDHint; }
			set { m_classIDHint = value; }
		}


	}
}
