using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Bans table
	/// </summary>
	[DataTable(TableName="Ban")]
	public class DbBans : DataObject
	{
		private string	m_author;
		private string  m_type;
		private string	m_ip;
		private string	m_account;
		private DateTime m_dateban;
		private string	m_reason;

		/// <summary>
		/// Who have ban player
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string Author
		{
			get
			{
				return m_author;
			}
			set
			{
				m_author=value;
			}
		}

		/// <summary>
		/// type of ban (I=ip, A=account, B=both)
		/// </summary>
		[DataElement(AllowDbNull=false, Index = true)]
		public string Type
		{
			get
			{
				return m_type;
			}
			set
			{
				m_type=value;
			}
		}

		/// <summary>
		/// IP banned
		/// </summary>
		[DataElement(AllowDbNull=false, Index = true)]
		public string Ip
		{
			get
			{
				return m_ip;
			}
			set
			{
				m_ip=value;
			}
		}

		/// <summary>
		/// Account banned
		/// </summary>
		[DataElement(AllowDbNull=false, Index=true)]
		public string Account
		{
			get
			{
				return m_account;
			}
			set
			{
				m_account=value;
			}
		}

		/// <summary>
		/// When have been ban this account/IP
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public DateTime DateBan
		{
			get
			{
				return m_dateban;
			}
			set
			{
				m_dateban = value;
			}
		}

		/// <summary>
		/// reason of ban
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string Reason
		{
			get
			{
				return m_reason;
			}
			set
			{
				m_reason=value;
			}
		}
	}
}
