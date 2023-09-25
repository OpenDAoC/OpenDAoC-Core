using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Specialization Table
	/// </summary>
	[DataTable(TableName="Specialization")]
	public class DbSpecialization : DataObject
	{
		protected int m_SpecializationID;
		protected string m_keyName;
		protected string m_name = "unknown spec";
		protected string m_description = "no description";
		protected ushort m_icon = 0;
		protected string m_implementation;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public DbSpecialization()
		{
			AllowAdd = false;
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement=true)]
		public int SpecializationID {
			get { return m_SpecializationID; }
			set { Dirty = true; m_SpecializationID = value; }
		}
		
		/// <summary>
		/// Specialization Unique Key Name (Primary Key)
		/// </summary>
		[DataElement(AllowDbNull=false, Unique=true, Varchar=100)]
		public string KeyName {
			get {	return m_keyName;	}
			set	{
				Dirty = true;
				m_keyName = value;
			}
		}

		/// <summary>
		/// Specizalization Display Name
		/// </summary>
		[DataElement(AllowDbNull=false, Varchar=255)]
		public string Name
		{
			get {	return m_name;	}
			set {
				Dirty = true;
				m_name = value;
			}
		}

		/// <summary>
		/// Specialization Icon ID (0 = disabled)
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public ushort Icon
		{
			get {	return m_icon;	}
			set {
				Dirty = true;
				m_icon = value;
			}
		}

		/// <summary>
		/// Specialization Description
		/// </summary>
		[DataElement(AllowDbNull=true)]
		public string Description
		{
			get {	return m_description;	}
			set {
				Dirty = true;
				m_description = value;
			}
		}

		/// <summary>
		/// Implementation of this Specialization.
		/// </summary>
		[DataElement(AllowDbNull=true, Varchar=255)]
		public string Implementation {
			get { return m_implementation; }
			set { Dirty = true; m_implementation = value; }
		}
		
		/// <summary>
		/// Styles attached to this Specizalization
		/// </summary>
		[Relation(LocalField = "KeyName", RemoteField = "SpecKeyName", AutoLoad = true, AutoDelete=true)]
		public DbStyle[] Styles;
		
		/// <summary>
		/// Spell Lines attached to this Specialization
		/// </summary>
		[Relation(LocalField = "KeyName", RemoteField = "Spec", AutoLoad = true, AutoDelete=false)]
		public DbSpellLine[] SpellLines;
		
		/// <summary>
		/// Ability Lines Constraints attached to this Specialization
		/// </summary>
		[Relation(LocalField = "KeyName", RemoteField = "Spec", AutoLoad = true, AutoDelete=true)]
		public DbSpecXAbility[] AbilityConstraints;
	}
}