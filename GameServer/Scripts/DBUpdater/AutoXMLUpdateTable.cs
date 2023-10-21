using Core.Database;

namespace Core.GS.DatabaseUpdate
{
	/// <summary>
	/// DataTable to track already registered XML Loading Files Played
	/// Prevent Loading XML package Multiple times on the same database.
	/// </summary>
	[DataTable(TableName="AutoXMLUpdate")]
	public class AutoXmlUpdateRecord : DataObject
	{
		protected int m_autoXMLUpdateID;
		protected string m_filePackage;
		protected string m_fileHash;
		protected string m_loadResult;
		
		/// <summary>
		/// Primary Key AutoInc
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int AutoXMLUpdateID {
			get { return m_autoXMLUpdateID; }
			set { Dirty = true; m_autoXMLUpdateID = value; }
		}

		/// <summary>
		/// FileName from which XML Data has been loaded.
		/// </summary>
		[DataElement(Varchar = 255, AllowDbNull = false, Index = true)]
		public string FilePackage {
			get { return m_filePackage; }
			set { Dirty = true; m_filePackage = value; }
		}

		/// <summary>
		/// File Hash to track for changes.
		/// </summary>
		[DataElement(Varchar = 255, AllowDbNull = false, Index = true)]
		public string FileHash {
			get { return m_fileHash; }
			set { Dirty = true; m_fileHash = value; }
		}

		/// <summary>
		/// Last Loading Result
		/// </summary>
		[DataElement(Varchar = 255, AllowDbNull = false)]
		public string LoadResult {
			get { return m_loadResult; }
			set { Dirty = true; m_loadResult = value; }
		}
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		public AutoXmlUpdateRecord()
		{
		}
	}
}
