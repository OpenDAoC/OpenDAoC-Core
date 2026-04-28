using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "Gravestone")]
    public class DbGravestone : DataObject
    {
        [DataElement(AllowDbNull = false, Index = true)]
        public string OwnerId
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string Name
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int X
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Y
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Z
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public ushort Heading
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false, Index = true)]
        public ushort Region
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public ushort Model
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long XpValue
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public DateTime CreationTime
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }
    }
}
