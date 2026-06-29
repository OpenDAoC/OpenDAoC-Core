using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName="PathPoints")]
    public class DbPathPoint : DataObject
    {
        [DataElement(AllowDbNull = false, Index = true)]
        public string PathId { get; set; } = string.Empty;

        [DataElement(AllowDbNull = false)]
        public int Step { get; set; }

        [DataElement(AllowDbNull = false)]
        public int X { get; set; }

        [DataElement(AllowDbNull = false)]
        public int Y { get; set; }

        [DataElement(AllowDbNull = false)]
        public int Z { get; set; }

        [DataElement(AllowDbNull = false)]
        public int MaxSpeed { get; set; } // 0 = no limit.

        [DataElement(AllowDbNull = false)]
        public int WaitTime { get; set; }

        [DataElement(AllowDbNull = false)]
        public string TriggerName { get; set;}

        public DbPathPoint() { }

        public DbPathPoint(int x, int y, int z, int maxSpeed, int waitTime, string triggerName)
        {
            X = x;
            Y = y;
            Z = z;
            MaxSpeed = maxSpeed;
            WaitTime = waitTime;
            TriggerName = triggerName;
        }
    }
}
