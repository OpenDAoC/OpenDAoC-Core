using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DOL.Database.Attributes;
using DOL.Database.UniqueID;

namespace DOL.Database
{
    public abstract class DataObject : ICloneable, IEquatable<DataObject>
    {
        private DataObject _snapshot;
        private bool _allowAdd = true;
        private bool _allowDelete = true;
        private string _objectId;
        private int? _cachedHash;

        public virtual bool UsesPreCaching => AttributeUtil.GetPreCachedFlag(GetType());

        [Browsable(false)]
        public virtual string TableName => AttributeUtil.GetTableName(GetType());

        [Browsable(false)]
        public bool IsPersisted { get; set; }

        [Browsable(false)]
        public virtual bool AllowAdd
        {
            get => _allowAdd;
            set => _allowAdd = value;
        }

        [Browsable(false)]
        public virtual bool AllowDelete
        {
            get => _allowDelete;
            set => _allowDelete = value;
        }

        [Browsable(false)]
        public string ObjectId
        {
            get => _objectId;
            set
            {
                _objectId = value;
                _cachedHash = null; // Just in case. ObjectId should never be changed after creation.
            }
        }

        [Browsable(false)]
        public virtual bool Dirty { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [DataElement(AllowDbNull = false, Index = false)]
        public DateTime LastTimeRowUpdated { get; set; }

        protected DataObject()
        {
            ObjectId = IdGenerator.GenerateID();
            IsPersisted = false;
            AllowAdd = true;
            AllowDelete = true;
            IsDeleted = false;
        }

        public void TakeSnapshot()
        {
            // Called when an object has been created and its properties initialized.
            // Creates a copy of itself to be able to keep track of dirty properties.
            _snapshot = (DataObject) MemberwiseClone();
            _snapshot.Dirty = false;
        }

        public List<ElementBinding> GetDirtyBindings(DataTableHandler tableHandler)
        {
            // If there's no snapshot, we can't know what changed.
            if (_snapshot == null)
            {
                LastTimeRowUpdated = DateTime.UtcNow;
                return tableHandler.UpdateElementBindings.ToList();
            }

            List<ElementBinding> dirtyBindings = new();

            foreach (ElementBinding binding in tableHandler.UpdateElementBindings)
            {
                object currentValue = binding.GetValue(this);
                object originalValue = binding.GetValue(_snapshot);

                if (!Equals(currentValue, originalValue))
                    dirtyBindings.Add(binding);
            }

            if (dirtyBindings.Count > 0 && tableHandler.LastUpdatedBinding != null)
            {
                LastTimeRowUpdated = DateTime.UtcNow;
                dirtyBindings.Add(tableHandler.LastUpdatedBinding);
            }

            return dirtyBindings;
        }

        public object Clone()
        {
            var obj = (DataObject) MemberwiseClone();
            obj.IsPersisted = false;
            obj.ObjectId = IdGenerator.GenerateID();
            obj._snapshot = null;
            return obj;
        }

        public override string ToString()
        {
            return $"DataObject: {TableName}, ObjectId{{{ObjectId}}}";
        }

        public override int GetHashCode()
        {
            if (_cachedHash.HasValue)
                return _cachedHash.Value;

            _cachedHash = ObjectId.GetHashCode();
            return _cachedHash.Value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DataObject);
        }

        public bool Equals(DataObject other)
        {
            if (other is null)
                return false;

            return ReferenceEquals(this, other) || ObjectId == other.ObjectId;
        }
    }
}
