using System;
using System.Reflection;
using System.Linq;

using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Data Element Binding
	/// </summary>
	public sealed class ElementBinding
	{
		/// <summary>
		/// Column Name of this Element Binding
		/// </summary>
		public string ColumnName { get; }
		/// <summary>
		/// Value's Type of this Element Binding
		/// </summary>
		public Type ValueType { get; }
		/// <summary>
		/// Get Value Object
		/// </summary>
		public Func<DataObject, object> GetValue { get; }
		/// <summary>
		/// Set Value Object
		/// </summary>
		public Action<DataObject, object> SetValue { get; }
		/// <summary>
		/// Get DataElement Attribute
		/// </summary>
		public DataElement DataElement { get; }
		/// <summary>
		/// Get Relation Attribute
		/// </summary>
		public Relation Relation { get; }
		/// <summary>
		/// Get Primary Key Attribute
		/// </summary>
		public PrimaryKey PrimaryKey { get; }
		/// <summary>
		/// Get ReadOnly Attribute
		/// </summary>
		public ReadOnly ReadOnly { get; }
		/// <summary>
		/// Check if this Element Binding Implement any Data Attribute
		/// </summary>
		public bool IsDataElementBinding { get { return DataElement != null || Relation != null || PrimaryKey != null; } }
		
		/// <summary>
		/// Create a new instance of <see cref="ElementBinding"/>
		/// </summary>
		/// <param name="Member">MemberInfo for this ElementBinding</param>
		public ElementBinding(MemberInfo Member)
		{
			switch (Member.MemberType)
			{
				case MemberTypes.Property:
					var property = Member as PropertyInfo;
					ValueType = property.PropertyType;
					GetValue = obj => property.GetValue(obj, null);
					SetValue = (obj, val) => property.SetValue(obj, val, null);
					break;
				case MemberTypes.Field:
					var field = Member as FieldInfo;
					ValueType = field.FieldType;
					GetValue = field.GetValue;
					SetValue = field.SetValue;
					break;
				default:
					return;
			}
			
			ColumnName = Member.Name;
			DataElement = Member.GetCustomAttributes<DataElement>().FirstOrDefault();
			Relation = Member.GetCustomAttributes<Relation>().FirstOrDefault();
			PrimaryKey = Member.GetCustomAttributes<PrimaryKey>().FirstOrDefault();
			ReadOnly = Member.GetCustomAttributes<ReadOnly>().FirstOrDefault();
		}
		
		/// <summary>
		/// Create a custom instance of <see cref="ElementBinding"/>
		/// </summary>
		/// <param name="Member">MemberInfo for this ElementBinding</param>
		/// <param name="DataElement">Custom DataElement Attached</param>
		/// <param name="ColumnName">Custom ColumnName</param>
		public ElementBinding(MemberInfo Member, DataElement DataElement, string ColumnName)
			: this(Member)
		{
			this.DataElement = DataElement;
			this.ColumnName = ColumnName;
		}
		
		/// <summary>
		/// Create a custom instance of <see cref="ElementBinding"/> with Primary Key
		/// </summary>
		/// <param name="Member">MemberInfo for this ElementBinding</param>
		/// <param name="PrimaryKey">Custom PrimaryKey Attached</param>
		/// <param name="ColumnName">Custom ColumnName</param>
		public ElementBinding(MemberInfo Member, PrimaryKey PrimaryKey, string ColumnName)
			: this(Member)
		{
			this.PrimaryKey = PrimaryKey;
			this.ColumnName = ColumnName;
		}
	}
}
