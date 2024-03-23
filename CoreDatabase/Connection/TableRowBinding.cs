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
		public string ColumnName { get; }
		/// <summary>
		/// Column Type
		/// </summary>
		public string ColumnType { get; }
		/// <summary>
		/// Column Allow Null
		/// </summary>
		public bool AllowDbNull { get; }
		/// <summary>
		/// Column Allow Null
		/// </summary>
		public bool Primary { get; }
		
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
