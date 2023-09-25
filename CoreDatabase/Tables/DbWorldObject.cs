using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Objects as Tables, Lights, Bags non static in Game.
	/// </summary>
	[DataTable(TableName="WorldObject")]
	public class DbWorldObject : DataObject
	{
		private string		m_type;
        private string      m_translationId;
		private string		m_name;
        private string      m_examineArticle;
		private int			m_x;
		private int			m_y;
		private int			m_z;
		private ushort		m_heading;
		private ushort		m_model;
		private ushort		m_region;
		private int			m_emblem;
		private byte 		m_realm;
		private int			m_respawnInterval;
		

		public DbWorldObject()
		{
			m_type = "DOL.GS.GameItem";
		}

		[DataElement(AllowDbNull = true)]
		public string ClassType
		{
			get
			{
				return m_type;
			}
			set
			{
				Dirty = true;
				m_type = value;
			}
		}

        [DataElement(AllowDbNull = true)]
        public string TranslationId
        {
            get { return m_translationId; }
            set
            {
                Dirty = true;
                m_translationId = value;
            }
        }

		[DataElement(AllowDbNull=false)]
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				Dirty = true;
				m_name = value;
			}
		}

        /// <summary>
        /// Gets or sets the examine article.
        /// 
        /// You examine [the Forge].
        /// 
        /// the = the examine article.
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string ExamineArticle
        {
            get { return m_examineArticle; }
            set
            {
                Dirty = true;
                m_examineArticle = value;
            }
        }
		
		[DataElement(AllowDbNull=false)]
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
		
		[DataElement(AllowDbNull=false)]
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

		[DataElement(AllowDbNull=false)]
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
		
		[DataElement(AllowDbNull=false)]
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

		[DataElement(AllowDbNull=false, Index=true)]
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
		
		[DataElement(AllowDbNull=false)]
		public ushort Model
		{
			get
			{
				return m_model;
			}
			set
			{
				Dirty = true;
				m_model = value;
			}
		}
		
		[DataElement(AllowDbNull=false)]
		public int Emblem
		{
			get
			{
				return m_emblem;
			}
			set
			{
				Dirty = true;
				m_emblem = value;
			}
		}
		
		[DataElement(AllowDbNull = false)]
		public byte Realm
		{
			get
			{
				return m_realm;
			}
			set
			{
				Dirty = true;
				m_realm = value;
			}
		}

		/// <summary>
		/// Respawn interval, in seconds
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int RespawnInterval
		{
			get
			{
				return m_respawnInterval;
			}
			set
			{
				Dirty = true;
				m_respawnInterval = value;
			}
		}
	}
}
