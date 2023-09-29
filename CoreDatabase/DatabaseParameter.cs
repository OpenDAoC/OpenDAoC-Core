using System;

namespace DOL.Database
{
	/// <summary>
	/// Parameter for Prepared Queries
	/// </summary>
	public sealed class QueryParameter : Tuple<string, object, Type>
	{
		/// <summary>
		/// Parameter Name
		/// </summary>
		public string Name { get { return Item1; } }
		
		/// <summary>
		/// Parameter Value
		/// </summary>
		public object Value { get { return Item2; } }
		
		/// <summary>
		/// Parameter Value
		/// </summary>
		public Type ValueType { get { return Item3; } }
		
		/// <summary>
		/// Create an instance of <see cref="QueryParameter"/>
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="Value">Parameter Value</param>
		public QueryParameter(string Name, object Value)
			: base(Name, Value, null)
		{
		}
		
		/// <summary>
		/// Create a Typed instance of <see cref="QueryParameter"/>
		/// </summary>
		/// <param name="Name">Parameter Name</param>
		/// <param name="Value">Parameter Value</param>
		/// <param name="Type">Parameter Type</param>
		public QueryParameter(string Name, object Value, Type Type)
			: base(Name, Value, Type)
		{
		}
		
		/// <summary>
		/// Create a default instance of <see cref="QueryParameter"/>
		/// </summary>
		public QueryParameter()
			: base(null, null, null)
		{
		}
	}
}
