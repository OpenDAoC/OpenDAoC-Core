using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Teleport location table.
	/// </summary>
	[DataTable(TableName = "Teleport")]
	public class DbTeleport : DataObject
	{
	    /// <summary>
		/// Create a new teleport location.
		/// </summary>
		public DbTeleport()
		{
			Type = string.Empty;
			TeleportID = "UNDEFINED";
			Realm = 0;
			RegionID = 0;
			X = 0;
			Y = 0;
			Z = 0;
			Heading = 0;
		}

		/// <summary>
		/// Teleporter type.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string Type { get; set; }

	    /// <summary>
		/// ID for this teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)] // Dre: Index or Unique ?
		public string TeleportID { get; set; }

	    /// <summary>
		/// Realm for this teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Realm { get; set; }

	    /// <summary>
		/// Realm for this teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int RegionID { get; set; }

	    /// <summary>
		/// X coordinate for teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int X { get; set; }

	    /// <summary>
		/// Y coordinate for teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Y { get; set; }

	    /// <summary>
		/// Z coordinate for teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Z { get; set; }

	    /// <summary>
		/// Heading for teleport location.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Heading { get; set; }
	}
}