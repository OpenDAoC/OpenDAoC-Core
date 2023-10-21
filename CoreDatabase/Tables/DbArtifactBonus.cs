using System;

namespace Core.Database.Tables
{
	/// <summary>
	/// Bonuses for artifacts.
	/// </summary>
	[DataTable(TableName = "ArtifactBonus")]
	public class DbArtifactBonus : DataObject
	{
		private String m_artifactID;
		private int m_bonusID;
		private int m_level;

		/// <summary>
		/// Create a new artifact bonus.
		/// </summary>
		public DbArtifactBonus()
			: base() { }

		/// <summary>
		/// Whether to auto-save this object or not.
		/// </summary>
		public override bool AllowAdd
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// The book ID.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string ArtifactID
		{
			get { return m_artifactID; }
			set
			{
				Dirty = true;
				m_artifactID = value;
			}
		}

		/// <summary>
		/// The ID of the bonus.
		/// 0-9: Stat bonuses 1 through 10.
		/// 10: SpellID
		/// 11: SpellID1
		/// 12: ProcSpellID
		/// 13: ProcSpellID1
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int BonusID
		{
			get { return m_bonusID; }
			set
			{
				Dirty = true;
				m_bonusID = value;
			}
		}

		/// <summary>
		/// The level this bonus is granted.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Level
		{
			get { return m_level; }
			set
			{
				Dirty = true;
				m_level = value;
			}
		}
	}
}