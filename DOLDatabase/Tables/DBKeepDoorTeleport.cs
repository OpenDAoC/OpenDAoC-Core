using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// DB KeepdoorTeleport is database of keepdoor teleport
    /// </summary>
    [DataTable(TableName = "Keepdoorteleport")]
    public class DBKeepDoorTeleport : DataObject
    {
        private ushort m_region;
        private int m_x;
        private int m_y;
        private int m_z;
        private ushort m_heading;
        private int m_keepID;
        private string m_teleportText;
        private string m_createInfo;
        private string m_teleportType;

        public DBKeepDoorTeleport()
        {
            m_region = 0;;
            m_x = 1;
            m_y = 1;
            m_z = 1;
            m_heading = 0;
            m_keepID = 0;
            m_teleportText = "";
            m_createInfo = "";
            m_teleportType = "";
        }

        /// <summary>
        /// Region to move players
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Region
        {
            get
            {
                return m_region;
            }
            set
            {
                Dirty = true;
                m_region = value;
            }
        }

        /// <summary>
        /// X position to move players
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int X
        {
            get
            {
                return m_x;
            }
            set
            {
                Dirty = true;
                m_x = value;
            }
        }

        /// <summary>
        /// Y position to move players
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Y
        {
            get
            {
                return m_y;
            }
            set
            {
                Dirty = true;
                m_y = value;
            }
        }

        /// <summary>
        /// Z to move players
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Z
        {
            get
            {
                return m_z;
            }
            set
            {
                Dirty = true;
                m_z = value;
            }
        }

        /// <summary>
        /// heading to move players
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Heading
        {
            get
            {
                return m_heading;
            }
            set
            {
                Dirty = true;
                m_heading = value;
            }
        }

        /// <summary>
        /// Index of keep to move players
        /// </summary>
        [PrimaryKey]
        public int KeepID
        {
            get
            {
                return m_keepID;
            }
            set
            {
                Dirty = true;
                m_keepID = value;
            }
        }

        /// <summary>
        /// Text to Whisper
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get
            {
                return m_teleportText;
            }
            set
            {
                Dirty = true;
                m_teleportText = value;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string CreateInfo
        {
            get
            {
                return m_createInfo;
            }
            set
            {
                Dirty = true; m_createInfo = value;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string TeleportType
        {
            get
            {
                return m_teleportType;
            }
            set
            {
                Dirty = true; m_teleportType = value;
            }
        }
    }
}
