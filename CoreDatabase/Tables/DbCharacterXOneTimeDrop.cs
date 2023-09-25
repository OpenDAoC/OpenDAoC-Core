using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// List of characters and the one time drops they have received.
	/// </summary>
	[DataTable(TableName="CharacterXOneTimeDrop")]
	public class DbCharacterXOneTimeDrop : DataObject
	{
		private string m_characterID;
		private string m_itemTemplateID;

		public DbCharacterXOneTimeDrop()
		{
			m_itemTemplateID = string.Empty;
			m_characterID = string.Empty;
		}

		/// <summary>
		/// The DOLCharacters_ID of the player who gets the drop
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar = 100, Index = true)]
		public string CharacterID
		{
			get
			{
				return m_characterID;
			}
			set
			{
				Dirty = true;
				m_characterID = value;
			}
		}

		/// <summary>
		/// The item id_nb that was dropped
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar = 100, Index = true)]
		public string ItemTemplateID
		{
			get
			{
				return m_itemTemplateID;
			}
			set
			{
				Dirty = true;
				m_itemTemplateID = value;
			}
		}
	}
}
