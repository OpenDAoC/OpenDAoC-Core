using System;
using DOL.Database.Attributes;

namespace DOL.Database
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

		public enum ID
		{
			Min = 0,
			MinStat = 0,
			Bonus1 = 0,
			Bonus2 = 1,
			Bonus3 = 2,
			Bonus4 = 3,
			Bonus5 = 4,
			Bonus6 = 5,
			Bonus7 = 6,
			Bonus8 = 7,
			Bonus9 = 8,
			Bonus10 = 9,
			MaxStat = 9,
			MinSpell = 10,
			Spell = 10,
			Spell1 = 11,
			ProcSpell = 12,
			ProcSpell1 = 13,
			MaxSpell = 13,
			Max = 13
		};

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