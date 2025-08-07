using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace DOL.Database.Attributes
{
    /// <summary>
    /// Utils Method for Handling DOL Database Attributes
    /// </summary>
    public static class AttributeUtil
    {
        private static readonly ConcurrentDictionary<Type, DataTable> _dataTableCache = new();

        /// <summary>
        /// A private helper method that gets the DataTable attribute for a given type,
        /// using a cache to avoid repeated reflection calls.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>The cached DataTable attribute, or null if not found.</returns>
        private static DataTable GetCachedDataTableAttribute(Type type)
        {
            // Normalize the type first, so we use the same key for both a type and an array of that type.
            Type normalizedType = type.HasElementType ? type.GetElementType() : type;
            return _dataTableCache.GetOrAdd(normalizedType, static t => t.GetCustomAttributes<DataTable>(true).FirstOrDefault());
        }

        /// <summary>
        /// Returns the TableName from Type if DataTable Attribute is found 
        /// </summary>
        /// <param name="type">Type inherited from DataObject</param>
        /// <returns>Table Name from DataTable Attribute or ClassName</returns>
        public static string GetTableName(Type type)
        {
            DataTable dataTable = GetCachedDataTableAttribute(type);

            if (dataTable != null && !string.IsNullOrEmpty(dataTable.TableName))
                return dataTable.TableName;

            // Fallback to the class name (use the normalized type name).
            return (type.HasElementType ? type.GetElementType() : type).Name;
        }

        /// <summary>
        /// Returns the ViewName from Type if DataTable Attribute is found 
        /// </summary>
        /// <param name="type">Type inherited from DataObject</param>
        /// <returns>View Name from DataTable Attribute or null</returns>
        public static string GetViewName(Type type)
        {
            DataTable dataTable = GetCachedDataTableAttribute(type);
            return dataTable != null && !string.IsNullOrEmpty(dataTable.ViewName) ? dataTable.ViewName : null;
        }

        /// <summary>
        /// Returns the View Select As Query from Type if DataTable Attribute is found 
        /// </summary>
        /// <param name="type">Type inherited from DataObject</param>
        /// <returns>View Select As Query from DataTable Attribute or null</returns>
        public static string GetViewAs(Type type)
        {
            DataTable dataTable = GetCachedDataTableAttribute(type);
            return dataTable != null && !string.IsNullOrEmpty(dataTable.ViewAs) ? dataTable.ViewAs : null;
        }

        /// <summary>
        /// Return Table View or Table Name
        /// </summary>
        /// <param name="type">Type inherited from DataObject</param>
        /// <returns>View Name if available, Table Name default</returns>
        public static string GetTableOrViewName(Type type)
        {
            return GetViewName(type) ?? GetTableName(type);
        }

        /// <summary>
        /// Is this Data Table Pre-Cached on startup?
        /// </summary>
        /// <param name="type">Type inherited from DataObject</param>
        /// <returns>True if Pre-Cached Flag is set</returns>
        public static bool GetPreCachedFlag(Type type)
        {
            DataTable dataTable = GetCachedDataTableAttribute(type);
            return dataTable?.PreCache ?? false;
        }
    }
}
