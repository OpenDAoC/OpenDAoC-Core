using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Account table
	/// </summary>
	[DataTable(TableName = "News")]
	public class DbNews : DataObject
	{
		private DateTime m_creationDate;
		private byte m_type;
		private byte m_realm;
		private string m_text;

		/// <summary>
		/// Create account row in DB
		/// </summary>
		public DbNews()
		{
			m_creationDate = DateTime.Now;
			m_type = 0;
			m_realm = 0;
			m_text = string.Empty;
		}

		[DataElement(AllowDbNull = false)]
		public DateTime CreationDate
		{
			get
			{
				return m_creationDate;
			}
			set
			{
				m_creationDate = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public byte Type
		{
			get
			{
				return m_type;
			}
			set
			{
				m_type = value;
				Dirty = true;
			}
		}

		[DataElement(AllowDbNull = false)]
		public byte Realm
		{
			get
			{
				return m_realm;
			}
			set
			{
				Dirty = true;
				m_realm = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Text
		{
			get
			{
				return m_text;
			}
			set
			{
				Dirty = true;
				m_text = value;
			}
		}
	}
}
