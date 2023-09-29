using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName="CharacterXMasterLevel")]
	public class DbCharacterXMasterLevel : DataObject
	{
		protected string m_character_id;
		protected int m_mllevel;				// ML number
		protected int m_step;					// ML step number
		protected bool m_stepcompleted;			// ML completition flag
		protected DateTime m_validationdate;	// Validation date (for tracking purpose)

		public DbCharacterXMasterLevel()
		{
		}

		// Owner ID
		[DataElement(AllowDbNull = false, Index = true)]
		public string Character_ID
		{
			get { return m_character_id; }
			set
			{
				Dirty = true;
				m_character_id = value;
			}
		}
		
		// ML number
		[DataElement(AllowDbNull = false)]
		public int MLLevel
		{
			get { return m_mllevel; }
			set
			{
				Dirty = true;
				m_mllevel = value;
			}
		}

		// ML step number
		[DataElement(AllowDbNull = false)]
		public int MLStep
		{
			get { return m_step; }
			set
			{
				Dirty = true;
				m_step = value;
			}
		}
		
		// ML completition flag
		[DataElement(AllowDbNull = false)]
		public bool StepCompleted
		{
			get { return m_stepcompleted; }
			set
			{
				Dirty = true;
				m_stepcompleted = value;
			}
		}

		// Validation date (for tracking purpose)
		[DataElement(AllowDbNull = true)]
		public DateTime ValidationDate
		{
			get { return m_validationdate; }
			set
			{
				Dirty = true;
				m_validationdate = value;
			}
		}
	}
}
