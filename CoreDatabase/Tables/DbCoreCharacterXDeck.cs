using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the different characters and guilds that have been given permissions to a house.
	/// </summary>
	[DataTable(TableName = "DOLCharactersXDeck")]
	public class DbCoreCharacterXDeck : DataObject
	{
		//important data
		private int m_DOLCharactersXDeck_ID;
		private string m_deck;
		private string m_dOLCharactersObjectId;
		private DateTime m_lastModifiedTime;
		
		public DbCoreCharacterXDeck()
		{
			m_lastModifiedTime = DateTime.Now;
		}
		
		/// <summary>
		/// DOLCharacters Table ObjectId Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
		public string DOLCharactersObjectId
		{
			get { return m_dOLCharactersObjectId; }
			set
			{
				Dirty = true;
				m_dOLCharactersObjectId = value;
			}
		}

		/// <summary>
		/// DOLCharacters Table Deck Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false, Varchar = 0)]
		public string Deck
		{
			get { return m_deck; }
			set
			{
				Dirty = true;
				m_deck = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int DOLCharactersXDeck_ID
		{
			get { return m_DOLCharactersXDeck_ID; }
			set
			{
				Dirty = true;
				m_DOLCharactersXDeck_ID = value;
			}
		}

		/// <summary>
		/// Gets or sets the the time this mapping was created.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public DateTime LastModifiedTime
		{
			get { return m_lastModifiedTime; }
			set 
			{
				Dirty = true;
				m_lastModifiedTime = value;
			}
		}
	}
}