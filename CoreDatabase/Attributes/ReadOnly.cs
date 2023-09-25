using System;

namespace DOL.Database.Attributes
{
	/// <summary>
	/// Attribute to indicate the column is read only once created
	/// </summary>
	/// 
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class ReadOnly : Attribute
	{
		/// <summary>
		/// Constructor for Attribute
		/// </summary>
		public ReadOnly()
		{
		}
	}
}
