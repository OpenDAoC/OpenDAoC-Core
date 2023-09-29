using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "BugReport")]
	public class DbBugReport : DataObject
	{
		private int m_reportID;
		private string m_message;
		private string m_category;
		private string m_submitter;
		private DateTime m_dateSubmitted;
		private string m_closedBy;
		private DateTime m_dateClosed;

		public DbBugReport()
		{
			m_message = string.Empty;
			m_submitter = string.Empty;
			m_dateSubmitted = DateTime.Now;
			m_closedBy = string.Empty;
			m_category = string.Empty;
		}

		[PrimaryKey]//DataElement(AllowDbNull = false, Unique = true)]
		public int ID
		{
			get { return m_reportID; }
			set
			{
				m_reportID = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Message
		{
			get { return m_message; }
			set
			{
				Dirty = true;
				m_message = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Submitter
		{
			get { return m_submitter; }
			set
			{
				Dirty = true;
				m_submitter = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public DateTime DateSubmitted
		{
			get { return m_dateSubmitted; }
			set
			{
				Dirty = true;
				m_dateSubmitted = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string ClosedBy
		{
			get { return m_closedBy; }
			set
			{
				Dirty = true;
				m_closedBy = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public DateTime DateClosed
		{
			get { return m_dateClosed; }
			set
			{
				Dirty = true;
				m_dateClosed = value;
			}
		}
		
		[DataElement(AllowDbNull = true)]
		public string Category {
			get { return m_category; }
			set {
				Dirty = true;
				m_category = value;
			}
		}
	}
}
