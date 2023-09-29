using System;

namespace DOL.Database.Attributes
{
	/// <summary>
	/// Attribute to mark a Derived Class of DataObject as Table
	/// Mainly to set the TableName different to Classname
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class DataTable : Attribute
	{
		/// <summary>
		/// Constructor of DataTable sets the TableName-Property to null.
		/// </summary>
		public DataTable()
		{
			ViewName = null;
			TableName = null;
			PreCache = false;
		}

		/// <summary>
		/// TableName-Property if null the Classname is used as Tablename.
		/// </summary>
		/// <value>The TableName that sould be used or <c>null</c> for Classname</value>
		public string TableName { get; set; }

		/// <summary>
		/// The View Name, Make this DataObject act as View, based on the TableName table for DML
		/// </summary>
		public string ViewName { get; set; }

		/// <summary>
		/// The View Query, this is mandatory when handling a View !
		/// Evaluated Params : {0} replaced by TableName
		/// </summary>
		public string ViewAs { get; set; }
		
		/// <summary>
		/// If preloading data is required for performance in Findobjectbykey
		/// Uses more memory then
		/// </summary>
		/// <value>true if enabled</value>
		public bool PreCache { get; set; }
	}
}
