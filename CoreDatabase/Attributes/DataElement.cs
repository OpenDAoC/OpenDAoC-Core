using System;

namespace DOL.Database.Attributes
{
	/// <summary>
	/// Attribute that Marks a Property or Field as Column of the Table
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DataElement : Attribute
	{
		public DataElement()
		{
			AllowDbNull = true;
			Unique = false;
			UniqueColumns = string.Empty;
			Index = false;
			IndexColumns = string.Empty;
			Varchar = 0;
		}

		/// <summary>
		/// Indicates if a value of null is allowed for this Collumn
		/// </summary>
		/// <value><c>true</c> if <c>null</c> is allowed</value>
		public bool AllowDbNull { get; set; }

		/// <summary>
		/// Indicates if a Value has to be Unique in the Table
		/// </summary>
		/// <value><c>true</c> if a Value as to be Unique</value>
		public bool Unique { get; set; }

		/// <summary>
		/// Indicates what columns are added to an Unique Field that includes this column
		/// </summary>
		/// <value><c>'ColumnName,ColumnName...'</c></value>
		public string UniqueColumns { get; set; }

		/// <summary>
		/// Indicates if the value gets indexed in sql databases
		/// for optimizing performance
		/// </summary>
		/// <value><c>true</c>if column of value should be indexed</value>
		public bool Index { get; set; }

		/// <summary>
		/// Indicates what columns are added to an index that includes this column
		/// <value><c>'ColumnName,ColumnName...'</c></value>
		/// </summary>
		public string IndexColumns { get; set; }

		/// <summary>
		/// Indicates that a string column will be created as type VarChar with the length provided
		/// Varchar = 0 will force creation of TEXT datatype
		/// </summary>
		public byte Varchar { get; set; }
	}
}