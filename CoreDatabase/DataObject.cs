using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DOL.Database.Attributes;
using DOL.Database.UniqueID;
using DOL.Logging;

namespace DOL.Database
{
    public abstract class DataObject : ICloneable
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private DataObject _snapshot;
        private bool _allowAdd = true;
        private bool _allowDelete = true;
        private DateTime _lastTimeRowUpdated;

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
        public string ObjectId { get; set; }

        [Browsable(false)]
        public virtual bool Dirty { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [DataElement(AllowDbNull = false, Index = false)]
        public DateTime LastTimeRowUpdated
        {
            get => Dirty ? DateTime.UtcNow : _lastTimeRowUpdated;
            set => _lastTimeRowUpdated = value;
        }

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
            // Called when an object as been created and its properties initialized.
            // Creates a copy of itself to be able to keep track of dirty properties.
            _snapshot = (DataObject) MemberwiseClone();
            _snapshot.Dirty = false;
        }

        public List<ElementBinding> GetDirtyBindings(DataTableHandler tableHandler)
        {
            // If there's no snapshot, we can't know what changed.
            // To be safe, we return all updateable columns (legacy behavior).
            if (_snapshot == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"No snapshot was found for {this}");

                return tableHandler.FieldElementBindings.Where(Predicate).ToList();
            }

            List<ElementBinding> dirtyBindings = new();

            // Iterate through all columns that can be part of an UPDATE statement.
            foreach (ElementBinding binding in tableHandler.FieldElementBindings.Where(bind => bind.PrimaryKey == null && bind.ReadOnly == null))
            {
                if (!Predicate(binding))
                    continue;

                object currentValue = binding.GetValue(this);
                object originalValue = binding.GetValue(_snapshot);

                // If the values are not equal, this property is dirty.
                if (!Equals(currentValue, originalValue))
                    dirtyBindings.Add(binding);
            }

            return dirtyBindings;

            static bool Predicate(ElementBinding binding)
            {
                return binding.PrimaryKey == null && binding.ReadOnly == null;
            }
        }

        public object Clone()
        {
            var obj = (DataObject) MemberwiseClone();
            obj.IsPersisted = false;
            obj.ObjectId = IdGenerator.GenerateID();
            return obj;
        }

        public override string ToString()
        {
            return $"DataObject: {TableName}, ObjectId{{{ObjectId}}}";
        }
    }
}
