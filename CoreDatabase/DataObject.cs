using System;
using System.ComponentModel;
using DOL.Database.Attributes;
using DOL.Database.UniqueID;

namespace DOL.Database
{
	/// <summary>
	/// Abstract Baseclass for all DataObject's. All Classes that are derived from this class
	/// are stored in a Datastore
	/// </summary>
	public abstract class DataObject : ICloneable
	{
		bool m_allowAdd = true;
		bool m_allowDelete = true;

		/// <summary>
		/// Default-Construktor that generates a new Object-ID and set
		/// Dirty and Persisted to <c>false</c>
		/// </summary>
		protected DataObject()
		{
			ObjectId = IdGenerator.GenerateID();
			IsPersisted = false;
			AllowAdd = true;
			AllowDelete = true;
			IsDeleted = false;
		}

		/// <summary>
		/// The table name which own he object 
		/// </summary>
		[Browsable(false)]
		public virtual string TableName
		{
			get
			{
				return AttributeUtil.GetTableName(GetType());
			}
		}

		/// <summary>
		/// Load object in cache or not?
		/// </summary>
		[Browsable(false)]
		public virtual bool UsesPreCaching
		{
			get
			{
				return AttributeUtil.GetPreCachedFlag(GetType());
			}
		}

		/// <summary>
		/// Is this object also in the database?
		/// </summary>
		[Browsable(false)]
		public bool IsPersisted { get; set; }

		/// <summary>
		/// Can this object added to the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool AllowAdd 
		{
			get { return m_allowAdd; }
			set { m_allowAdd = value; }
		}

		/// <summary>
		/// Can this object be deleted from the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool AllowDelete
		{
			get { return m_allowDelete; }
			set { m_allowDelete = value; }
		}

		/// <summary>
		/// Index of the object in his table
		/// </summary>
		[Browsable(false)]
		public string ObjectId { get; set; }

		/// <summary>
		/// Is object different than object in the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool Dirty { get; set; }

		/// <summary>
		/// Has this object been deleted from the database
		/// </summary>
		[Browsable(false)]
		public virtual bool IsDeleted { get; set; }

		/// <summary>
		/// Default field added to all DataObject.
		/// Last time this record was updated.
		/// Return UTC Now to update table's "LastTimeRowUpdated"
		/// for Maintenance purpose.
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false)]
		public DateTime LastTimeRowUpdated 
		{
			get { return DateTime.UtcNow; }
			set { Dirty = true; }
		}

		#region ICloneable Member

		/// <summary>
		/// Clone the current object and return the copy
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var obj = (DataObject) MemberwiseClone();
			obj.ObjectId = IdGenerator.GenerateID();
			return obj;
		}

		#endregion

		public override string ToString()
		{
			return string.Format("DataObject: {0}, ObjectId{{{1}}}", TableName, ObjectId);
		}
	}

	// Used to allow manipulation of inventory items and delay their database action.
	public enum PendingDatabaseAction
	{
		NONE,
		SAVE,
		ADD,
		DELETE
	}
}
