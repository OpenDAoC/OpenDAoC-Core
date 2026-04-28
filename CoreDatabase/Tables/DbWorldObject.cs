using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "WorldObject")]
    public class DbWorldObject : DataObject
    {
        [DataElement(AllowDbNull = true)]
        public string ClassType
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public string TranslationId
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

        [DataElement(AllowDbNull = true)]
        public string ExamineArticle
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
        public int Emblem
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public byte Realm
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int RespawnInterval
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
