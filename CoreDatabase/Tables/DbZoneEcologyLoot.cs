using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Zone-scoped ecology loot mapping.
	/// </summary>
	[DataTable(TableName = "ZoneEcologyLoot")]
	public class DbZoneEcologyLoot : DataObject
	{
		private ushort m_regionID;
		private int m_zoneID;
		private int m_minLevel;
		private int m_maxLevel = 255;
		private string m_mobName = string.Empty;
		private string m_lootTemplateName = string.Empty;
		private string m_archetype = string.Empty;
		private string m_materialLootTemplateName = string.Empty;
		private int m_dropCount = 1;
		private int m_materialDropChance;
		private int m_materialDropCount = 1;
		private bool m_dropsCoin;
		private bool m_isNamed;
		private double m_namedXpMultiplier = 1.0;
		private double m_namedXpCapMultiplier = 1.0;
		private int m_namedRogChance;
		private bool m_enabled = true;

		[DataElement(AllowDbNull = false, Index = true)]
		public ushort RegionID
		{
			get { return m_regionID; }
			set
			{
				Dirty = true;
				m_regionID = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int ZoneID
		{
			get { return m_zoneID; }
			set
			{
				Dirty = true;
				m_zoneID = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int MinLevel
		{
			get { return m_minLevel < 0 ? 0 : m_minLevel; }
			set
			{
				Dirty = true;
				m_minLevel = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public int MaxLevel
		{
			get { return m_maxLevel < MinLevel ? MinLevel : m_maxLevel; }
			set
			{
				Dirty = true;
				m_maxLevel = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public string MobName
		{
			get { return m_mobName; }
			set
			{
				Dirty = true;
				m_mobName = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public string LootTemplateName
		{
			get { return m_lootTemplateName; }
			set
			{
				Dirty = true;
				m_lootTemplateName = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public string Archetype
		{
			get { return m_archetype; }
			set
			{
				Dirty = true;
				m_archetype = value;
			}
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public string MaterialLootTemplateName
		{
			get { return m_materialLootTemplateName; }
			set
			{
				Dirty = true;
				m_materialLootTemplateName = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int DropCount
		{
			get { return m_dropCount < 1 ? 1 : m_dropCount; }
			set
			{
				Dirty = true;
				m_dropCount = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int MaterialDropChance
		{
			get
			{
				if (m_materialDropChance < 0)
					return 0;

				return m_materialDropChance > 100 ? 100 : m_materialDropChance;
			}
			set
			{
				Dirty = true;
				m_materialDropChance = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int MaterialDropCount
		{
			get { return m_materialDropCount < 1 ? 1 : m_materialDropCount; }
			set
			{
				Dirty = true;
				m_materialDropCount = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public bool DropsCoin
		{
			get { return m_dropsCoin; }
			set
			{
				Dirty = true;
				m_dropsCoin = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public bool IsNamed
		{
			get { return m_isNamed; }
			set
			{
				Dirty = true;
				m_isNamed = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public double NamedXpMultiplier
		{
			get { return m_namedXpMultiplier <= 0 ? 1.0 : m_namedXpMultiplier; }
			set
			{
				Dirty = true;
				m_namedXpMultiplier = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public double NamedXpCapMultiplier
		{
			get { return m_namedXpCapMultiplier <= 0 ? 1.0 : m_namedXpCapMultiplier; }
			set
			{
				Dirty = true;
				m_namedXpCapMultiplier = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int NamedRogChance
		{
			get
			{
				if (m_namedRogChance < 0)
					return 0;

				return m_namedRogChance > 100 ? 100 : m_namedRogChance;
			}
			set
			{
				Dirty = true;
				m_namedRogChance = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public bool Enabled
		{
			get { return m_enabled; }
			set
			{
				Dirty = true;
				m_enabled = value;
			}
		}
	}
}
