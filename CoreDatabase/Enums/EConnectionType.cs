namespace DOL.Database.Connection
{
	/// <summary>
	/// Enum what Datatstorage should be used
	/// </summary>
	public enum EConnectionType
	{
		/// <summary>
		/// Use XML-Files as Database
		/// </summary>
		DATABASE_XML,
		/// <summary>
		/// Use the internal MySQL-Driver for Database
		/// </summary>
		DATABASE_MYSQL,
		/// <summary>
		/// Use the internal SQLite-Driver for Database
		/// </summary>
		DATABASE_SQLITE,
		/// <summary>
		/// Use Microsoft SQL-Server
		/// </summary>
		DATABASE_MSSQL,
		/// <summary>
		/// Use an ODBC-Datasource
		/// </summary>
		DATABASE_ODBC,
		/// <summary>
		/// Use an OLEDB-Datasource
		/// </summary>
		DATABASE_OLEDB
	}
}