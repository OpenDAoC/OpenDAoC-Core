using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table of BindPoint where player pop when they die and released
	/// </summary>
	[DataTable(TableName="BindPoint")]
	public class DbBindPoint : DataObject
	{
		//This needs to be uint and ushort!

	    /// <summary>
		/// Create a bind point
		/// </summary>
		public DbBindPoint()
		{
		}

		/// <summary>
		/// The X position of bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int X { get; set; }

	    /// <summary>
		/// The Y position of bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Y { get; set; }

	    /// <summary>
		/// The Z position of bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Z { get; set; }

	    /// <summary>
		/// The radius of bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public ushort Radius { get; set; }

	    /// <summary>
		/// The region of bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Region { get; set; }

	    /// <summary>
		/// The realm of this bind
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Realm { get; set; }
	}
}
