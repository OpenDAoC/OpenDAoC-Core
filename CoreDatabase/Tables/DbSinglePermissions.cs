using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName="SinglePermission")]
	public class DbSinglePermissions : DataObject
	{
		private string	m_playerID;
		private string	m_command;

		public DbSinglePermissions()
		{
			m_playerID = string.Empty;
			m_command = string.Empty;
		}

		[DataElement(AllowDbNull = false, Index=true)]
		public string PlayerID
		{
			get
			{
				return m_playerID;
			}
			set
			{
				Dirty = true;
				m_playerID = value;
			}
		}

		[DataElement(AllowDbNull = false, Index=true)]
		public string Command
		{
			get
			{
				return m_command;
			}
			set
			{
				Dirty = true;
				m_command = value;
			}
		}
		
	}
}