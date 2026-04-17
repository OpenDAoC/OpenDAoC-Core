using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "ScheduledRollovers")]
    public class DbScheduledRollover : DataObject
    {
        private int _rolloverIntervalKey;
        private DateTime _lastRollover;

        [DataElement(AllowDbNull = false)]
        public DateTime LastRollover
        {
            get => _lastRollover;
            set
            {
                Dirty = true;
                _lastRollover = value;
            }
        }

        [DataElement(AllowDbNull = false, Unique = true)]
        public int RolloverIntervalKey
        {
            get => _rolloverIntervalKey;
            set
            {
                Dirty = true;
                _rolloverIntervalKey = value;
            }
        }
    }
}
