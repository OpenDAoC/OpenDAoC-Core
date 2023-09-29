using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// This table represents instances, with an entry for each element (instance type, objects, mobs, entrances, etc) in an instance.
	/// </summary>
	[DataTable(TableName = "InstanceXElement")]
	public class DbInstanceXElement : DataObject
	{
	    public DbInstanceXElement()
		{
		}

		/// <summary>
		/// The unique name of this instance. Eg 'My Task Dungeon'
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public string InstanceID { get; set; }

	    [DataElement(AllowDbNull = true)]
		public string ClassType { get; set; }

	    [DataElement(AllowDbNull = false)]
		public int X { get; set; }

	    [DataElement(AllowDbNull = false)]
		public int Y { get; set; }

	    [DataElement(AllowDbNull = false)]
		public int Z { get; set; }

	    [DataElement(AllowDbNull = false)]
		public ushort Heading { get; set; }

	    /// <summary>
		/// Where applicable, the npc template to create this mob from.
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar = 255)]
		public string NPCTemplate { get; set; }

	    /// <summary>
		/// Convert the NPCTemplate to/from int, assuming a single ID
		/// </summary>
		public int NPCTemplateID
		{
			get
			{
				int i = 0;
				int.TryParse(NPCTemplate, out i);
				return i;
			}

			set
			{
				NPCTemplate = value.ToString();
			}
		}

	}
}
