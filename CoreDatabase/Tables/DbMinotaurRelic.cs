using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DB relic is database of relic
	/// </summary>
	[DataTable(TableName = "Minotaurrelic", PreCache = true)]
	public class DbMinotaurRelic : DataObject
	{
		private string m_Name;
		private int m_relicID;
		private ushort m_Model;
		private int m_spawny;
		private int m_spawnx;
		private int m_spawnz;
		private int m_spawnheading;
		private int m_spawnregion;
		private string m_relictarget;
		private int m_spell;
		private int m_effect;
        private string m_protectorClassType;
        private bool m_spawnLocked;

		/// <summary>
		/// Create a relic row
		/// </summary>
		public DbMinotaurRelic() { }

		/// <summary>
		/// relic type, 0 is melee, 1 is magic
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int relicSpell
		{
			get { return m_spell; }
			set
			{
				Dirty = true;
				m_spell = value;
			}
        }

        [DataElement(AllowDbNull = false)]
        public bool SpawnLocked
        {
            get { return m_spawnLocked; }
            set
            {
                Dirty = true;
                m_spawnLocked = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string ProtectorClassType
        {
            get { return m_protectorClassType; }
            set
            {
                Dirty = true;
                m_protectorClassType = value;
            }
        }

		[DataElement(AllowDbNull = false)]
		public string relicTarget
		{
			get { return m_relictarget; }
			set
			{
				Dirty = true;
				m_relictarget = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public string Name
		{
			get { return m_Name; }
			set
			{
				Dirty = true;
				m_Name = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public ushort Model
		{
			get { return m_Model; }
			set
			{
				Dirty = true;
				m_Model = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SpawnX
		{
			get { return m_spawnx; }
			set
			{
				Dirty = true;
				m_spawnx = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SpawnY
		{
			get { return m_spawny; }
			set
			{
				Dirty = true;
				m_spawny = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SpawnZ
		{
			get { return m_spawnz; }
			set
			{
				Dirty = true;
				m_spawnz = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SpawnHeading
		{
			get { return m_spawnheading; }
			set
			{
				Dirty = true;
				m_spawnheading = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int SpawnRegion
		{
			get { return m_spawnregion; }
			set
			{
				Dirty = true;
				m_spawnregion = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Effect
		{
			get { return m_effect; }
			set
			{
				Dirty = true;
				m_effect = value;
			}
		}

		/// <summary>
		/// Index of relic
		/// </summary>
		[PrimaryKey]
		public int RelicID
		{
			get { return m_relicID; }
			set
			{
				Dirty = true;
				m_relicID = value;
			}
		}
	}
}
