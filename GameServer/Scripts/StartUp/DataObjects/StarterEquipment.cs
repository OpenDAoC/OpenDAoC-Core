using DOL.Database;
using DOL.Database.Attributes;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// Holds starter equipment
	/// </summary>
	[DataTable(TableName = "StarterEquipment")]
	public class StarterEquipment : DataObject
	{
		private int m_starterEquipmentID;
		private string m_class;
		private string m_templateID;

		/// <summary>
		/// Primary Key Autoincrement
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int StarterEquipmentID
		{
			get
			{
				return m_starterEquipmentID;
			}
			set
			{
				Dirty = true;
				m_starterEquipmentID = value;
			}
		}

		
		/// <summary>
		/// Serialized classes this item should be given to (separator ';' and range '-')
		/// 0 for all classes
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string Class
		{
			get
			{
				return m_class;
			}
			set
			{
				Dirty = true;
				m_class = value;
			}
		}

		/// <summary>
		/// The template ID of free item
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar=255)]
		public string TemplateID
		{
			get
			{
				return m_templateID;
			}
			set
			{
				m_templateID = value;
				Dirty = true;
			}
		}

		/// <summary>
		/// The ItemTemplate for this record
		/// </summary>
		[Relation(LocalField = "TemplateID", RemoteField = "Id_nb", AutoLoad = true, AutoDelete = false)]
		public DbItemTemplate Template;
	}
}
