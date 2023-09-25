using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of Tasks
	/// </summary>
	[DataTable(TableName="Task")]
	public class DbTask : DataObject
	{
		protected string	m_characterid = string.Empty;
		protected DateTime	m_TimeOut = DateTime.Now.AddHours(2);
		protected String	m_TaskType = null;		// name of classname
		protected int		m_TasksDone = 0;
		protected string	m_customPropertiesString = null;

		public DbTask()
		{
		}

		[PrimaryKey]
		public string Character_ID
		{
			get {return m_characterid;}
			set
			{
				Dirty = true;
				m_characterid = value;
			}
		}

		[DataElement(AllowDbNull=true,Unique=false)]
		public DateTime TimeOut
		{
			get {return m_TimeOut;}
			set
			{
				Dirty = true;
				m_TimeOut = value;
			}
		}

		[DataElement(AllowDbNull=true,Unique=false)]
		public string TaskType
		{
			get {return m_TaskType;}
			set
			{
				Dirty = true;
				m_TaskType = value;
			}
		}

		[DataElement(AllowDbNull= false, Unique=false)]
		public int TasksDone
		{
			get {return m_TasksDone;}
			set
			{
				Dirty = true;
				m_TasksDone = value;
			}
		}

		[DataElement(AllowDbNull=true,Unique=false)]
		public string CustomPropertiesString
		{
			get
			{
				return m_customPropertiesString;
			}
			set
			{
				Dirty = true;
				m_customPropertiesString = value;
			}
		}
	}
}
