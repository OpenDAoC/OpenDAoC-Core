namespace DOL.Database.Connection
{
	/// <summary>
	/// Existing Table Row Binding
	/// </summary>
	public sealed class TableRowBinding
	{
		/// <summary>
		/// Column Name
		/// </summary>
		public string ColumnName { get; private set; }
		/// <summary>
		/// Column Type
		/// </summary>
		public string ColumnType { get; private set; }
		/// <summary>
		/// Column Allow Null
		/// </summary>
		public bool AllowDbNull { get; private set; }
		/// <summary>
		/// Column Allow Null
		/// </summary>
		public bool Primary { get; private set; }
		
		/// <summary>
		/// Create new instance of <see cref="TableRowBinding"/>
		/// </summary>
		/// <param name="ColumnName">Row Column Name</param>
		/// <param name="ColumnType">Row Column Type</param>
		/// <param name="AllowDbNull">Row DB Null</param>
		/// <param name="Primary">Row Primary</param>
		public TableRowBinding(string ColumnName, string ColumnType, bool AllowDbNull, bool Primary)
		{
			this.ColumnName = ColumnName;
			this.ColumnType = ColumnType;
			this.AllowDbNull = AllowDbNull;
			this.Primary = Primary;
		}
	}
}
