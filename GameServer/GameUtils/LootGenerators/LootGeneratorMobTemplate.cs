using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// LootGeneratorMobTemplate
	/// This implementation uses LootTemplates to relate loots to a specific mob type.
	/// Used DB Tables: 
	///				MobDropTemplate  (Relation between Mob and loottemplate
	///				DropTemplateXItemTemplate	(loottemplate containing possible loot items)
	/// </summary>
	public class LootGeneratorMobTemplate : LootGeneratorBase
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Map holding a list of ItemTemplateIDs for each TemplateName
		/// 1:n mapping between loottemplateName and loottemplate entries
		/// </summary>
		protected static Dictionary<string, Dictionary<string, DbDropTemplateXItemTemplates>> m_lootTemplates;

		/// <summary>
		/// Map holding the corresponding LootTemplateName for each MobName
		/// 1:n Mapping between Mob and LootTemplate
		/// </summary>
		protected static Dictionary<string, List<DbMobDropTemplates>> m_mobXLootTemplates;

		/// <summary>
		/// Construct a new templategenerate and load its values from database.
		/// </summary>
		public LootGeneratorMobTemplate()
		{
			PreloadLootTemplates();
		}

		public static void ReloadLootTemplates()
		{
			m_lootTemplates = null;
			m_mobXLootTemplates = null;
			PreloadLootTemplates();
		}

		/// <summary>
		/// Loads the DropTemplateXItemTemplate
		/// </summary>
		/// <returns></returns>
		protected static bool PreloadLootTemplates()
		{
			if (m_lootTemplates == null)
			{
				m_lootTemplates = new Dictionary<string, Dictionary<string, DbDropTemplateXItemTemplates>>();

				lock (m_lootTemplates)
				{
					IList<DbDropTemplateXItemTemplates> dbLootTemplates;

					try
					{
						// TemplateName (typically the mob name), ItemTemplateID, Chance
						dbLootTemplates = GameServer.Database.SelectAllObjects<DbDropTemplateXItemTemplates>();
					}
					catch (Exception e)
					{
						if (log.IsErrorEnabled)
						{
							log.Error("LootGeneratorMobTemplate: DropTemplateXItemTemplate could not be loaded:", e);
						}
						return false;
					}

					if (dbLootTemplates != null)
					{
						Dictionary<string, DbDropTemplateXItemTemplates> loot;

						foreach (DbDropTemplateXItemTemplates dbTemplate in dbLootTemplates)
						{
							if (!m_lootTemplates.TryGetValue(dbTemplate.TemplateName.ToLower(), out loot))
							{
								loot = new Dictionary<string, DbDropTemplateXItemTemplates>();
								m_lootTemplates[dbTemplate.TemplateName.ToLower()] = loot;
							}

							DbItemTemplates drop = GameServer.Database.FindObjectByKey<DbItemTemplates>(dbTemplate.ItemTemplateID);

							if (drop == null)
							{
								if (log.IsErrorEnabled)
									log.Error("ItemTemplate: " + dbTemplate.ItemTemplateID + " is not found, it is referenced from DropTemplateXItemTemplate: " + dbTemplate.TemplateName);
							}
							else
							{
								if (!loot.ContainsKey(dbTemplate.ItemTemplateID.ToLower()))
									loot.Add(dbTemplate.ItemTemplateID.ToLower(), dbTemplate);
							}
						}
					}
				}

				log.Info("DropTemplateXItemTemplates pre-loaded.");
			}

			if (m_mobXLootTemplates == null)
			{
				m_mobXLootTemplates = new Dictionary<string, List<DbMobDropTemplates>>();

				lock (m_mobXLootTemplates)
				{
					IList<DbMobDropTemplates> dbMobXLootTemplates;

					try
					{
						// MobName, LootTemplateName, DropCount
						dbMobXLootTemplates = GameServer.Database.SelectAllObjects<DbMobDropTemplates>();
					}
					catch (Exception e)
					{
						if (log.IsErrorEnabled)
						{
							log.Error("LootGeneratorMobTemplate: MobDropTemplate could not be loaded", e);
						}
						return false;
					}

					if (dbMobXLootTemplates != null)
					{
						foreach (DbMobDropTemplates dbMobXTemplate in dbMobXLootTemplates)
						{
							// There can be multiple MobDropTemplate for a mob, each pointing to a different loot template
							List<DbMobDropTemplates> mobxLootTemplates;
							if (!m_mobXLootTemplates.TryGetValue(dbMobXTemplate.MobName.ToLower(), out mobxLootTemplates))
							{
								mobxLootTemplates = new List<DbMobDropTemplates>();
								m_mobXLootTemplates[dbMobXTemplate.MobName.ToLower()] = mobxLootTemplates;
							}
							mobxLootTemplates.Add(dbMobXTemplate);
						}
					}
				}

				log.Info("MobDropTemplates pre-loaded.");
			}

			return true;
		}

		/// <summary>
		/// Reload the loot templates for this mob
		/// </summary>
		/// <param name="mob"></param>
		public override void Refresh(GameNpc mob)
		{
			if (mob == null)
				return;

			bool isDefaultLootTemplateRefreshed = false;

			// First see if there are any MobXLootTemplates associated with this mob
			IList<DbMobDropTemplates> mxlts = CoreDb<DbMobDropTemplates>.SelectObjects(DB.Column("MobName").IsEqualTo(mob.Name));

			if (mxlts != null)
			{
				lock (m_mobXLootTemplates)
				{
					foreach (DbMobDropTemplates mxlt in mxlts)
						m_mobXLootTemplates.Remove(mxlt.MobName.ToLower());
					foreach (DbMobDropTemplates mxlt in mxlts)
					{
						List<DbMobDropTemplates> mobxLootTemplates;
						if (!m_mobXLootTemplates.TryGetValue(mxlt.MobName.ToLower(), out mobxLootTemplates))
						{
							mobxLootTemplates = new List<DbMobDropTemplates>();
							m_mobXLootTemplates[mxlt.MobName.ToLower()] = mobxLootTemplates;
						}
						mobxLootTemplates.Add(mxlt);

						RefreshLootTemplate(mxlt.LootTemplateName);


						if (mxlt.LootTemplateName.ToLower() == mob.Name.ToLower())
							isDefaultLootTemplateRefreshed = true;
					}
				}
			}

			// now force a refresh of the mobs default loot template
			if (isDefaultLootTemplateRefreshed == false)
				RefreshLootTemplate(mob.Name);
		}

		protected void RefreshLootTemplate(string templateName)
		{
			var lootTemplates = CoreDb<DbDropTemplateXItemTemplates>.SelectObjects(DB.Column("TemplateName").IsEqualTo(templateName));

			if (lootTemplates != null)
			{
				lock (m_lootTemplates)
				{
					m_lootTemplates.Remove(templateName.ToLower());

					var lootList = new Dictionary<string, DbDropTemplateXItemTemplates>();
					foreach (DbDropTemplateXItemTemplates lt in lootTemplates)
					{
						if (lootList.ContainsKey(lt.ItemTemplateID.ToLower()) == false)
						{
							lootList.Add(lt.ItemTemplateID.ToLower(), lt);
						}
					}

					m_lootTemplates.Add(templateName.ToLower(), lootList);
				}
			}
		}

		public override LootList GenerateLoot(GameNpc mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);

			try
			{
				GamePlayer player = killer as GamePlayer;
				if (killer is GameNpc && ((GameNpc)killer).Brain is IControlledBrain)
					player = ((ControlledNpcBrain)((GameNpc)killer).Brain).GetPlayerOwner();
				if (player == null)
					return loot;

				// allow the leader to decide the loot realm
				if (player.Group != null)
					player = player.Group.Leader;

				List<DbMobDropTemplates> killedMobXLootTemplates;
				// MobDropTemplate contains a loot template name and the max number of drops allowed for that template.
				// We don't need an entry in MobDropTemplate in order to drop loot, only to control the max number of drops.

				// DropTemplateXItemTemplate contains a template name and an itemtemplateid (id_nb).
				// TemplateName usually equals Mob name, so if you want to know what drops for a mob:
				// select * from DropTemplateXItemTemplate where templatename = 'mob name';
				// It is possible to have TemplateName != MobName but this works only if there is an entry in MobDropTemplate for the MobName.
				if (!m_mobXLootTemplates.TryGetValue(mob.Name.ToLower(), out killedMobXLootTemplates))
				{
					Dictionary<string, DbDropTemplateXItemTemplates> lootTemplatesToDrop;
					// We can use DropTemplateXItemTemplate.Count to determine how many of a item can drop
					if (m_lootTemplates.TryGetValue(mob.Name.ToLower(), out lootTemplatesToDrop))
					{
						foreach (DbDropTemplateXItemTemplates lootTemplate in lootTemplatesToDrop.Values)
						{
							DbItemTemplates drop = GameServer.Database.FindObjectByKey<DbItemTemplates>(lootTemplate.ItemTemplateID);

							if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
							{
								if (lootTemplate.Chance == 100)
									loot.AddFixed(drop, lootTemplate.Count);
								else
									loot.AddRandom(lootTemplate.Chance, drop, lootTemplate.Count);
							}
						}
					}
				}
				else
				{
					// MobDropTemplate exists and tells us the max number of items that can drop.
					// Because we are restricting the max number of items to drop we need to traverse the list
					// and add every 100% chance items to the loots Fixed list and add the rest to the Random list
					// due to the fact that 100% items always drop regardless of the drop limit
					List<DbDropTemplateXItemTemplates> lootTemplatesToDrop = new List<DbDropTemplateXItemTemplates>();
					foreach (DbMobDropTemplates mobXLootTemplate in killedMobXLootTemplates)
					{
						loot = GenerateLootFromMobXLootTemplates(mobXLootTemplate, lootTemplatesToDrop, loot, player);
						loot.DropCount = Math.Max(mobXLootTemplate.DropCount, loot.DropCount);
						foreach (DbDropTemplateXItemTemplates lootTemplate in lootTemplatesToDrop)
						{
							DbItemTemplates drop = GameServer.Database.FindObjectByKey<DbItemTemplates>(lootTemplate.ItemTemplateID);

							if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
								loot.AddRandom(lootTemplate.Chance, drop, lootTemplate.Count);
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Error in LootGeneratorTemplate for mob {0}.  Exception: {1}", mob.Name, ex.Message);
			}

			return loot;
		}

		/// <summary>
		/// Add all loot templates specified in MobDropTemplate for an entry in DropTemplateXItemTemplate
		/// If the item has a 100% drop chance add it as a fixed drop to the loot list.  
		/// </summary>
		/// <param name="mobXLootTemplate">Entry in MobDropTemplate.</param>
		/// <param name="lootTemplates">List of all itemtemplates this mob can drop and the chance to drop</param>
		/// <param name="lootList">List to hold loot.</param>
		/// <param name="player">Player used to determine realm</param>
		/// <returns>lootList (for readability)</returns>
		private static LootList GenerateLootFromMobXLootTemplates(DbMobDropTemplates mobXLootTemplates, List<DbDropTemplateXItemTemplates> lootTemplates, LootList lootList, GamePlayer player)
		{
			if (mobXLootTemplates == null || lootTemplates == null || player == null)
				return lootList;

			Dictionary<string, DbDropTemplateXItemTemplates> templateList = null;
			if (m_lootTemplates.ContainsKey(mobXLootTemplates.LootTemplateName.ToLower()))
				templateList = m_lootTemplates[mobXLootTemplates.LootTemplateName.ToLower()];

			if (templateList != null)
			{
				foreach (DbDropTemplateXItemTemplates lootTemplate in templateList.Values)
				{
					DbItemTemplates drop = GameServer.Database.FindObjectByKey<DbItemTemplates>(lootTemplate.ItemTemplateID);

					if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
					{
						if (lootTemplate.Chance == 100)
							lootList.AddFixed(drop, lootTemplate.Count);
						else
							lootTemplates.Add(lootTemplate);
					}
				}
			}

			return lootList;
		}

	}
}