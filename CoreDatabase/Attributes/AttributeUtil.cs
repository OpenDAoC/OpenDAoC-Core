using System;
using System.Linq;
using System.Reflection;

namespace DOL.Database.Attributes
{
	/// <summary>
	/// Utils Method for Handling DOL Database Attributes
	/// </summary>
	public static class AttributeUtil
	{
		/// <summary>
		/// Returns the TableName from Type if DataTable Attribute is found 
		/// </summary>
		/// <param name="type">Type inherited from DataObject</param>
		/// <returns>Table Name from DataTable Attribute or ClassName</returns>
		public static string GetTableName(Type type)
		{
			// Check if Type is Element
			if (type.HasElementType)
				type = type.GetElementType();
			
			var dataTable = type.GetCustomAttributes<DataTable>(true).FirstOrDefault();
			
			if (dataTable != null && !string.IsNullOrEmpty(dataTable.TableName))
				return dataTable.TableName;
			
			return type.Name;
		}

		/// <summary>
		/// Returns the ViewName from Type if DataTable Attribute is found 
		/// </summary>
		/// <param name="type">Type inherited from DataObject</param>
		/// <returns>View Name from DataTable Attribute or null</returns>
		public static string GetViewName(Type type)
		{
			// Check if Type is Element
			if (type.HasElementType)
				type = type.GetElementType();
			
			var dataTable = type.GetCustomAttributes<DataTable>(true).FirstOrDefault();
			
			if (dataTable != null && !string.IsNullOrEmpty(dataTable.ViewName))
				return dataTable.ViewName;
			
			return null;
		}
		
		/// <summary>
		/// Returns the View Select As Query from Type if DataTable Attribute is found 
		/// </summary>
		/// <param name="type">Type inherited from DataObject</param>
		/// <returns>View Select As Query from DataTable Attribute or null</returns>
		public static string GetViewAs(Type type)
		{
			// Check if Type is Element
			if (type.HasElementType)
				type = type.GetElementType();
			
			var dataTable = type.GetCustomAttributes<DataTable>(true).FirstOrDefault();
			
			if (dataTable != null && !string.IsNullOrEmpty(dataTable.ViewAs))
				return dataTable.ViewAs;
			
			return null;
		}

		/// <summary>
		/// Return Table View or Table Name
		/// </summary>
		/// <param name="type">Type inherited from DataObject</param>
		/// <returns>View Name if available, Table Name default</returns>
		public static string GetTableOrViewName(Type type)
		{
			// Graveen: introducing view selection hack (before rewriting the layer :D)
			// basically, a view must exist and is created with the following:
			//
			//	[DataTable(TableName="InventoryItem",ViewName = "MarketItem")]
			//	public class SomeMarketItems : InventoryItem {};
			//
			//  here, we rely on the view called MarketItem,
			//  based on the InventoryItem table. We have to tell to the code
			//  only to bypass the id generated with FROM by the above
			//  code.
			return GetViewName(type) ?? GetTableName(type);
		}
		
		/// <summary>
		/// Is this Data Table Pre-Cached on startup?
		/// </summary>
		/// <param name="type">Type inherited from DataObject</param>
		/// <returns>True if Pre-Cached Flag is set</returns>
		public static bool GetPreCachedFlag(Type type)
		{
			var dataTable = type.GetCustomAttributes<DataTable>(true).FirstOrDefault();
			
			if (dataTable != null)
				return dataTable.PreCache;

			return false;
		}
	}
}
