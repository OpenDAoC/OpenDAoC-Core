using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of Tasks
	/// </summary>
	[DataTable(TableName = "LootGenerator")]
	public class DbLootGenerator : DataObject
	{
		/// <summary>
		/// Trigger Mob
		/// </summary>
		protected string m_mobName = string.Empty;
		/// <summary>
		/// Trigger Guild
		/// </summary>
		protected string m_mobGuild = string.Empty;
		/// <summary>
		/// Trigger Faction
		/// </summary>
		protected string m_mobFaction = string.Empty;
		/// <summary>
		/// Trigger Region
		/// </summary>
		protected ushort m_regionID = 0;
		/// <summary>
		/// Class of the Loot Generator
		/// </summary>
		protected string m_lootGeneratorClass = string.Empty;
		/// <summary>
		/// Exclusive Priority
		/// </summary>
		protected int m_exclusivePriority = 0;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbLootGenerator()
		{
		}

		/// <summary>
		/// MobName
		/// </summary>
		[DataElement(AllowDbNull = true, Unique = false)]
		public String MobName
		{
			get { return m_mobName; }
			set
			{
				Dirty = true;
				m_mobName = value;
			}
		}

		/// <summary>
		/// MobGuild
		/// </summary>
		[DataElement(AllowDbNull = true, Unique = false)]
		public string MobGuild
		{
			get { return m_mobGuild; }
			set
			{
				Dirty = true;
				m_mobGuild = value;
			}
		}

		/// <summary>
		/// MobFaction
		/// </summary>
		[DataElement(AllowDbNull = true, Unique = false)]
		public string MobFaction
		{
			get { return m_mobFaction; }
			set
			{
				Dirty = true;
				m_mobFaction = value;
			}
		}

		/// <summary>
		/// Mobs Region ID
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
		public ushort RegionID
		{
			get { return m_regionID; }
			set
			{
				Dirty = true;
				m_regionID = value;
			}
		}

		/// <summary>
		/// LootGeneratorClass
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
		public string LootGeneratorClass
		{
			get { return m_lootGeneratorClass; }
			set
			{
				Dirty = true;
				m_lootGeneratorClass = value;
			}
		}

		/// <summary>
		/// ExclusivePriority
		/// </summary>
		[DataElement(AllowDbNull = false, Unique = false)]
		public int ExclusivePriority
		{
			get { return m_exclusivePriority; }
			set
			{
				Dirty = true;
				m_exclusivePriority = value;
			}
		}

	}
}
