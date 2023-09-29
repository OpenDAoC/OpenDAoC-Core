using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DBBoat is database of Player Boats
	/// </summary>
	[DataTable(TableName = "PlayerBoats")]
	public class DbPlayerBoat : DataObject
	{
		private string boat_id;
		private string boat_owner;
		private string boat_name;
		private ushort boat_model;
		private short boat_maxspeedbase;

		public DbPlayerBoat()
		{
			boat_id = string.Empty;
			boat_owner = string.Empty;
			boat_name = string.Empty;
			boat_model = 0;
			boat_maxspeedbase = 0;
		}

		/// <summary>
		/// The ID of the boat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string BoatID
		{
			get
			{
				return boat_id;
			}
			set
			{
				Dirty = true;
				boat_id = value;
			}
		}

		/// <summary>
		/// The Owner of the boat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string BoatOwner
		{
			get
			{
				return boat_owner;
			}
			set
			{
				Dirty = true;
				boat_owner = value;
			}
		}

		/// <summary>
		/// The Name of the boat
		/// </summary>
		[PrimaryKey]
		public string BoatName
		{
			get
			{
				return boat_name;
			}
			set
			{
				Dirty = true;
				boat_name = value;
			}
		}
		
		/// <summary>
		/// The Model of the boat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public ushort BoatModel
		{
			get
			{
				return boat_model;
			}
			set
			{
				Dirty = true;
				boat_model = value;
			}
		}


		/// <summary>
		/// The Max speed base of the boat
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public short BoatMaxSpeedBase
		{
			get
			{
				return boat_maxspeedbase;
			}
			set
			{
				Dirty = true;
				boat_maxspeedbase = value;
			}
		}
	}
}
