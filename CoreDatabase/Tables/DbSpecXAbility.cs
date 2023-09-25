using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Defines what abilities are available at what speclevels for a given Specialization
	/// </summary>
	[DataTable(TableName="SpecXAbility")]
	public class DbSpecXAbility : DataObject
	{
		protected int m_specXabilityID;
		protected string m_spec;
		protected string m_abilitykey;
		protected int m_abilitylevel;
		protected int m_speclevel;
		private int m_classId;
		
		public DbSpecXAbility()
		{
			AllowAdd = false;
		}

		/// <summary>
		/// Primary Key Auto Increment
		/// </summary>
		[PrimaryKey(AutoIncrement=true)]
		public int SpecXabilityID {
			get { return m_specXabilityID; }
			set { Dirty = true; m_specXabilityID = value; }
		}

		/// <summary>
		/// Spec KeyName
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true, Varchar=100)]
		public string Spec
		{
			get { return m_spec; }
			set { m_spec = value; Dirty = true; }
		}

		/// <summary>
		/// Spec Level at which ability is acquired.
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int SpecLevel
		{
			get { return m_speclevel; }
			set {
				Dirty = true;
				m_speclevel = value;
			}
		}

		/// <summary>
		/// Ability Key Name
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true, Varchar=100)]
		public string AbilityKey
		{
			get { return m_abilitykey; }
			set { m_abilitykey = value; Dirty = true; }
		}

		/// <summary>
		/// Ability Spec Level earned.
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int AbilityLevel
		{
			get { return m_abilitylevel; }
			set {
				Dirty = true;
				m_abilitylevel = value;
			}
		}
		
		/// <summary>
		/// Class Hint, 0 = Every class
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int ClassId {
			get { return m_classId; }
			set { m_classId = value; }
		}

	}
}
