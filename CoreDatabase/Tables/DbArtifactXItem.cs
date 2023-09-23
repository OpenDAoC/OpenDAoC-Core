using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Relation between artifacts and the actual items.
	/// </summary>
	[DataTable(TableName = "ArtifactXItem")]
	public class DbArtifactXItem : DataObject
	{
		private string m_artifactID;
		private string m_itemID;
		private string m_version;
		private int m_realm;

		/// <summary>
		/// Create a new artifact/item relation.
		/// </summary>
		public DbArtifactXItem()
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
		/// The artifact ID.
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
		/// The item ID.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string ItemID
		{
			get { return m_itemID; }
			set
			{
				Dirty = true;
				m_itemID = value;
			}
		}

		/// <summary>
		/// The artifact version.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string Version
		{
			get { return m_version; }
			set
			{
				Dirty = true;
				m_version = value;
			}
		}

		/// <summary>
		/// The realm.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Realm
		{
			get { return m_realm; }
			set
			{
				Dirty = true;
				m_realm = value;
			}
		}
	}
}
