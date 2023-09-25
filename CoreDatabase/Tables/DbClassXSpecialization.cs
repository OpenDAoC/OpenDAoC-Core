using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Cross Reference Table Between character Class and Specialization Careers.
	/// </summary>
	[DataTable(TableName="ClassXSpecialization")]
	public class DbClassXSpecialization : DataObject
	{
		private int m_classXSpecializationID;
		
		/// <summary>
		/// Table Primary Key
		/// </summary>
		[PrimaryKey(AutoIncrement=true)]
		public int ClassXSpecializationID {
			get { return m_classXSpecializationID; }
			set { Dirty = true; m_classXSpecializationID = value; }
		}
		
		private int m_classID;
		
		/// <summary>
		/// Class ID attached to this specialization (0 = all)
		/// </summary>
		[DataElement(AllowDbNull= false, Index=true)]
		public int ClassID {
			get { return m_classID; }
			set { Dirty = true; m_classID = value; }
		}
		
		private string m_specKeyName;
		
		/// <summary>
		/// Specialization Key
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true, Varchar=100)]
		public string SpecKeyName {
			get { return m_specKeyName; }
			set { Dirty = true; m_specKeyName = value; }
		}
		
		private int m_levelAcquired;
		
		/// <summary>
		/// Level at which Specialization is enabled. (default 0 = always enabled)
		/// </summary>
		[DataElement(AllowDbNull= false, Index=true)]
		public int LevelAcquired {
			get { return m_levelAcquired; }
			set { Dirty = true; m_levelAcquired = value; }
		}
				
		/// <summary>
		/// Constructor
		/// </summary>
		public DbClassXSpecialization()
		{
		}
	}
}
