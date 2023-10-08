using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// This implementation uses ItemTemplates to fetch a random item in the range of LEVEL_RANGE to moblevel   
	/// </summary>
	public class LootGeneratorRandom : LootGeneratorBase
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Map holding the corresponding lootTemplateName for each Moblevel group
		/// groups are rounded down to 1-5, 5-10, 10-15, 15-20, 20-25, etc...
		/// 1:n Mapping between Moblevel and LootTemplate
		/// </summary>

		protected static DbItemTemplate[][] m_itemTemplatesAlb = new DbItemTemplate[LEVEL_SIZE + 1][];
		protected static DbItemTemplate[][] m_itemTemplatesMid = new DbItemTemplate[LEVEL_SIZE + 1][];
		protected static DbItemTemplate[][] m_itemTemplatesHib = new DbItemTemplate[LEVEL_SIZE + 1][];

		protected const int LEVEL_RANGE = 5; // 
		protected const int LEVEL_SIZE = 10; // 10*LEVEL_RANGE = up to level 50


		static LootGeneratorRandom()
        {
			PreloadItemTemplates();

		}

		static void PreloadItemTemplates()
		{
			IList<DbItemTemplate> itemTemplates = null;

			for (int i = 0; i <= LEVEL_SIZE; i++)
			{
				try
				{
					var filterLevel = DB.Column("Level").IsGreaterOrEqualTo(i * LEVEL_RANGE).And(DB.Column("Level").IsLessOrEqualTo((i + 1) * LEVEL_RANGE));
					var filterByFlags = DB.Column("IsPickable").IsEqualTo(1).And(DB.Column("IsDropable").IsEqualTo(1)).And(DB.Column("CanDropAsLoot").IsEqualTo(1));
					var filterBySlot = DB.Column("Item_Type").IsGreaterOrEqualTo((int)EInventorySlot.MinEquipable).And(DB.Column("Item_Type").IsLessOrEqualTo((int)EInventorySlot.MaxEquipable));
					itemTemplates = CoreDb<DbItemTemplate>.SelectObjects(filterLevel.And(filterByFlags).And(filterBySlot));
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("LootGeneratorRandom: ItemTemplates could not be loaded", e);
					return;
				}

				List<DbItemTemplate> templatesAlb = new List<DbItemTemplate>();
				List<DbItemTemplate> templatesHib = new List<DbItemTemplate>();
				List<DbItemTemplate> templatesMid = new List<DbItemTemplate>();

				foreach (DbItemTemplate itemTemplate in itemTemplates)
				{
					switch (itemTemplate.Realm)
					{
						case (int)ERealm.Albion:
							templatesAlb.Add(itemTemplate);
							break;
						case (int)ERealm.Hibernia:
							templatesHib.Add(itemTemplate);
							break;
						case (int)ERealm.Midgard:
							templatesMid.Add(itemTemplate);
							break;
						default:
								templatesAlb.Add(itemTemplate);
								templatesHib.Add(itemTemplate);
								templatesMid.Add(itemTemplate);
								break;
					}
				}

				m_itemTemplatesAlb[i] = templatesAlb.ToArray();
				m_itemTemplatesHib[i] = templatesHib.ToArray();
				m_itemTemplatesMid[i] = templatesMid.ToArray();
			} // for
		}

		public override LootList GenerateLoot(GameNPC mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);

			if (Util.Chance(10))
			{
				DbItemTemplate[] itemTemplates = null;

				ERealm realm = mob.CurrentZone.Realm;

				if (realm < ERealm._FirstPlayerRealm || realm > ERealm._LastPlayerRealm)
					realm = (ERealm)Util.Random((int)ERealm._FirstPlayerRealm, (int)ERealm._LastPlayerRealm);

				switch (realm)
				{
					case ERealm.Albion:
						{
							int index = Math.Min(m_itemTemplatesAlb.Length - 1, mob.Level / LEVEL_RANGE);
							itemTemplates = m_itemTemplatesAlb[index];
						}
						break;
					case ERealm.Hibernia:
						{
							int index = Math.Min(m_itemTemplatesHib.Length - 1, mob.Level / LEVEL_RANGE);
							itemTemplates = m_itemTemplatesHib[index];
							break;
						}
					case ERealm.Midgard:
						{
							int index = Math.Min(m_itemTemplatesHib.Length - 1, mob.Level / LEVEL_RANGE);
							itemTemplates = m_itemTemplatesMid[index];
							break;
						}
				}

				if (itemTemplates != null && itemTemplates.Length > 0)
				{
					DbItemTemplate itemTemplate = itemTemplates[Util.Random(itemTemplates.Length - 1)];
					loot.AddFixed(itemTemplate,1);
				}
			}

			return loot;
		}
	}
}