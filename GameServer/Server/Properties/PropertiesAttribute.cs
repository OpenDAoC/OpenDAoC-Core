using System;

namespace Core.GS.ServerProperties
{
	/// <summary>
	/// The server property attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class PropertiesAttribute : Attribute
	{
		private string m_category;
		private string m_key;
		private string m_description;
		private object m_defaultValue;

		/// <summary>
		/// Constructor of serverproperty
		/// </summary>
		/// <param name="key">property name</param>
		/// <param name="description">property desc</param>
		/// <param name="defaultValue">property default value</param>
		/// <param name="category">property category (previously area)</param>
		public PropertiesAttribute(string category, string key, string description, object defaultValue)
		{
			m_category = category;
			m_key = key;
			m_description = description;
			m_defaultValue = defaultValue;
		}

		/// <summary>
		/// The property category
		/// </summary>
		public string Category
		{
			get
			{
				return m_category;
			}
		}

		/// <summary>
		/// The property key
		/// </summary>
		public string Key
		{
			get
			{
				return m_key;
			}
		}

		/// <summary>
		/// The property description
		/// </summary>
		public string Description
		{
			get
			{
				return m_description;
			}
		}

		/// <summary>
		/// The property default value
		/// </summary>
		public object DefaultValue
		{
			get
			{
				return m_defaultValue;
			}
		}
	}
}