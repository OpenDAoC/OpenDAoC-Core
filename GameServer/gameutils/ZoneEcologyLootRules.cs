using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// Shared DB-backed rules for zone ecology loot and reward adjustments.
	/// </summary>
	public static class ZoneEcologyLootRules
	{
		private static readonly object CacheLock = new object();
		private static Dictionary<ZoneMobKey, List<DbZoneEcologyLoot>> s_mappings;
		private static Dictionary<string, List<DbLootTemplate>> s_templates;

		public static void Refresh()
		{
			lock (CacheLock)
			{
				s_mappings = null;
				s_templates = null;
			}
		}

		public static bool TryGetMapping(GameNPC mob, out DbZoneEcologyLoot mapping)
		{
			mapping = null;
			if (mob == null)
				return false;

			Zone zone = mob.CurrentZone;
			if (zone == null || string.IsNullOrEmpty(mob.Name))
				return false;

			if (!GetMappings().TryGetValue(new ZoneMobKey(mob.CurrentRegionID, zone.ID, mob.Name), out List<DbZoneEcologyLoot> mappings))
				return false;

			int level = mob.Level;
			DbZoneEcologyLoot bestMatch = null;
			int bestRange = int.MaxValue;

			foreach (DbZoneEcologyLoot row in mappings)
			{
				if (level < row.MinLevel || level > row.MaxLevel)
					continue;

				int range = row.MaxLevel - row.MinLevel;
				if (bestMatch == null || range < bestRange || (range == bestRange && row.MinLevel > bestMatch.MinLevel))
				{
					bestMatch = row;
					bestRange = range;
				}
			}

			mapping = bestMatch;
			return mapping != null;
		}

		public static bool TryGetTemplateRows(string templateName, out List<DbLootTemplate> rows)
		{
			rows = null;
			if (string.IsNullOrEmpty(templateName))
				return false;

			return GetTemplates().TryGetValue(templateName, out rows);
		}

		public static double GetXpMultiplier(GameNPC mob)
		{
			if (!TryGetMapping(mob, out DbZoneEcologyLoot mapping))
				return 1.0;

			return mapping.IsNamed ? mapping.NamedXpMultiplier : 1.0;
		}

		public static double GetXpCapMultiplier(GameNPC mob)
		{
			if (!TryGetMapping(mob, out DbZoneEcologyLoot mapping))
				return 1.0;

			return mapping.IsNamed ? mapping.NamedXpCapMultiplier : 1.0;
		}

		private static Dictionary<ZoneMobKey, List<DbZoneEcologyLoot>> GetMappings()
		{
			lock (CacheLock)
			{
				if (s_mappings != null)
					return s_mappings;

				Dictionary<ZoneMobKey, List<DbZoneEcologyLoot>> mappings = new Dictionary<ZoneMobKey, List<DbZoneEcologyLoot>>();
				foreach (DbZoneEcologyLoot row in GameServer.Database.SelectAllObjects<DbZoneEcologyLoot>())
				{
					if (row == null || !row.Enabled || string.IsNullOrEmpty(row.MobName) || string.IsNullOrEmpty(row.LootTemplateName))
						continue;

					ZoneMobKey key = new ZoneMobKey(row.RegionID, row.ZoneID, row.MobName);
					if (!mappings.TryGetValue(key, out List<DbZoneEcologyLoot> rows))
					{
						rows = new List<DbZoneEcologyLoot>();
						mappings[key] = rows;
					}

					rows.Add(row);
				}

				s_mappings = mappings;
				return s_mappings;
			}
		}

		private static Dictionary<string, List<DbLootTemplate>> GetTemplates()
		{
			lock (CacheLock)
			{
				if (s_templates != null)
					return s_templates;

				HashSet<string> mappedTemplateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (List<DbZoneEcologyLoot> mappings in GetMappings().Values)
				{
					foreach (DbZoneEcologyLoot mapping in mappings)
					{
						mappedTemplateNames.Add(mapping.LootTemplateName);

						if (!string.IsNullOrEmpty(mapping.MaterialLootTemplateName))
							mappedTemplateNames.Add(mapping.MaterialLootTemplateName);
					}
				}

				Dictionary<string, List<DbLootTemplate>> templates = new Dictionary<string, List<DbLootTemplate>>(StringComparer.OrdinalIgnoreCase);
				foreach (DbLootTemplate row in GameServer.Database.SelectAllObjects<DbLootTemplate>())
				{
					if (row == null || string.IsNullOrEmpty(row.TemplateName) || !mappedTemplateNames.Contains(row.TemplateName))
						continue;

					if (!templates.TryGetValue(row.TemplateName, out List<DbLootTemplate> templateRows))
					{
						templateRows = new List<DbLootTemplate>();
						templates[row.TemplateName] = templateRows;
					}

					templateRows.Add(row);
				}

				s_templates = templates;
				return s_templates;
			}
		}

		private readonly struct ZoneMobKey : IEquatable<ZoneMobKey>
		{
			private readonly ushort m_regionID;
			private readonly int m_zoneID;
			private readonly string m_mobName;

			public ZoneMobKey(ushort regionID, int zoneID, string mobName)
			{
				m_regionID = regionID;
				m_zoneID = zoneID;
				m_mobName = mobName ?? string.Empty;
			}

			public bool Equals(ZoneMobKey other)
			{
				return m_regionID == other.m_regionID
					&& m_zoneID == other.m_zoneID
					&& string.Equals(m_mobName, other.m_mobName, StringComparison.OrdinalIgnoreCase);
			}

			public override bool Equals(object obj)
			{
				return obj is ZoneMobKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				int hash = m_regionID.GetHashCode();
				hash = (hash * 397) ^ m_zoneID.GetHashCode();
				hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(m_mobName);
				return hash;
			}
		}
	}
}
