using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// The NPCEqupment table holds standard equipment
	/// templates that npcs may wear!
	/// </summary>
	[DataTable(TableName="NPCEquipment")]
	public class DbNpcEquipment : DataObject
	{
		/// <summary>
		/// The Equipment Template ID
		/// </summary>
		protected string	m_templateID;
		/// <summary>
		/// The Item Slot
		/// </summary>
		protected int		m_slot;
		/// <summary>
		/// The Item Model
		/// </summary>
		protected int		m_model;
		/// <summary>
		/// The Item Color
		/// </summary>
		protected int		m_color;
		/// <summary>
		/// The Item Effect
		/// </summary>
		protected int		m_effect;
		/// <summary>
		/// The Item Extension
		/// </summary>
		protected int		m_extension;
		/// <summary>
		/// The Item Emblem
		/// </summary>
		protected int		m_emblem;

		/// <summary>
		/// The Constructor
		/// </summary>
		public DbNpcEquipment()
		{
		}

		/// <summary>
		/// Template ID
		/// </summary>
		[DataElement(AllowDbNull=false, Index = true)]
		public string TemplateID
		{
			get
			{
				return m_templateID;
			}
			set
			{
				Dirty = true;
				m_templateID=value;
			}
		}

		/// <summary>
		/// Slot
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Slot
		{
			get
			{
				return m_slot;
			}
			set
			{
				Dirty = true;
				m_slot = value;
			}
		}

		/// <summary>
		/// Model
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public int Model
		{
			get
			{
				return m_model;
			}
			set
			{
				Dirty = true;
				m_model = value;
			}
		}

		/// <summary>
		/// Color
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int Color
		{
			get
			{
				return m_color;
			}
			set
			{
				Dirty = true;
				m_color = value;
			}
		}

		/// <summary>
		/// Effect
		/// </summary>
		[DataElement(AllowDbNull= false)]
		public int Effect
		{
			get
			{
				return m_effect;
			}
			set
			{
				Dirty = true;
				m_effect = value;
			}
		}

		/// <summary>
		/// Extension
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Extension
		{
			get
			{
				return m_extension;
			}
			set
			{
				Dirty = true;
				m_extension = value;
			}
		}

		/// <summary>
		/// Emblem
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int Emblem
		{
			get
			{
				return m_emblem;
			}
			set
			{
				Dirty = true;
				m_emblem = value;
			}
		}
	}
}