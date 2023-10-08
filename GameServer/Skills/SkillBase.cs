using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Database;
using DOL.GS.RealmAbilities;
using DOL.GS.Styles;
using DOL.Language;
using log4net;

namespace DOL.GS
{
	public class SkillBase
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Flag to Check if SkillBase has been pre-loaded.
		/// </summary>
		private static bool m_loaded = false;

		private static ReaderWriterLockSlim m_syncLockUpdates = new();
		private static object m_loadingLock = new();

		#region caches and static indexes

		// Career Dictionary, Spec Attached to Character class ID, auto loaded on char creation !!
		protected static readonly Dictionary<int, IDictionary<string, int>> m_specsByClass = new();

		// Specialization dict KeyName => Spec Tuple to instanciate.
		protected static readonly Dictionary<string, Tuple<Type, string, ushort, int>> m_specsByName = new();

		// Specialization X SpellLines Dict<"string spec keyname", "List<"Tuple<"SpellLine line", "int classid"> line constraint"> list of lines">
		protected static readonly Dictionary<string, IList<Tuple<SpellLine, int>>> m_specsSpellLines = new();

		// global table for spec => List of styles, Dict <"string spec keyname", "Dict <"int classid", "List<"Tuple<"Style style", "byte requiredLevel"> Style Constraint" StyleByClass">
		protected static readonly Dictionary<string, IDictionary<int, List<Tuple<Style, byte>>>> m_specsStyles = new();

		// Specialization X Ability Cache Dict<"string spec keyname", "List<"Tuple<"string abilitykey", "byte speclevel", "int ab Level", "int class hint"> ab constraint"> list ab's>">
		protected static readonly Dictionary<string, List<Tuple<string, byte, int, int>>> m_specsAbilities = new();

		// Ability Cache Dict KeyName => DBAbility Object (to instanciate)
		protected static readonly Dictionary<string, DbAbility> m_abilityIndex = new();

		// class id => realm abilitykey list
		protected static readonly Dictionary<int, IList<string>> m_classRealmAbilities = new();

		// SpellLine Cache Dict KeyName => SpellLine Object
		protected static readonly Dictionary<string, SpellLine> m_spellLineIndex = new();

		// SpellLine X Spells Dict<"string spellline", "IList<"Spell spell"> spell list">
		protected static readonly Dictionary<string, List<Spell>> m_lineSpells = new();

		// Spells Cache Dict SpellID => Spell
		protected static readonly Dictionary<int, Spell> m_spellIndex = new();

		// Spells Tooltip Dict ToolTipID => SpellID
		protected static readonly Dictionary<ushort, int> m_spellToolTipIndex = new();

		// lookup table for styles, faster access when invoking a char styleID with classID
		protected static readonly Dictionary<KeyValuePair<int, int>, Style> m_styleIndex = new();

		// Ability Action Handler Dictionary Index, Delegate to instanciate on demand
		protected static readonly Dictionary<string, Func<IAbilityActionHandler>> m_abilityActionHandler = new();

		// Spec Action Handler Dictionary Index, Delegate to instanciate on demand
		protected static readonly Dictionary<string, Func<ISpecActionHandler>> m_specActionHandler = new();

		#endregion

		#region class initialize

		static SkillBase()
		{
			RegisterPropertyNames();
			InitArmorResists();
			InitPropertyTypes();
			InitializeObjectTypeToSpec();
			InitializeSpecToSkill();
			InitializeSpecToFocus();
			InitializeRaceResists();
		}

		public static void LoadSkills()
		{
			lock (m_loadingLock)
			{
				if (!m_loaded)
				{
					LoadSpells();
					LoadSpellLines();
					LoadAbilities();
					LoadClassRealmAbilities();
					// Load Spec, SpecXAbility, SpecXSpellLine, SpecXStyle, Styles, StylesProcs...
					// Need Spell, SpellLines, Abilities Loaded (including RealmAbilities...) !
					LoadSpecializations();
					LoadClassSpecializations();
					LoadAbilityHandlers();
					LoadSkillHandlers();
					m_loaded = true;
				}
			}
		}

		/// <summary>
		/// Load Spells From Database
		/// This will wipe any scripted spells !
		/// </summary>
		public static void LoadSpells()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				//load all spells
				if (log.IsInfoEnabled)
					log.Info("Loading spells...");

				IList<DbSpell> spelldb = GameServer.Database.SelectAllObjects<DbSpell>();

				if (spelldb != null)
				{

					// clean cache
					m_spellIndex.Clear();
					m_spellToolTipIndex.Clear();

					foreach (DbSpell spell in spelldb)
					{
						try
						{
							m_spellIndex.Add(spell.SpellID, new Spell(spell, 1));
							// Update tooltip index.
							if (spell.TooltipId != 0 && !m_spellToolTipIndex.ContainsKey(spell.TooltipId))
								m_spellToolTipIndex.Add(spell.TooltipId, spell.SpellID);
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("{0} with spellid = {1} spell.TS= {2}", e.Message, spell.SpellID, spell.ToString());
						}
					}

					if (log.IsInfoEnabled)
						log.InfoFormat("Spells loaded: {0}", m_spellIndex.Count);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Load SpellLines and Line X Spell relation from Database
		/// This will wipe any Script Loaded Lines or LineXSpell Relation !
		/// </summary>
		public static void LoadSpellLines()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				//load all spellline
				if (log.IsInfoEnabled)
					log.Info("Loading Spell Lines...");

				// load all spell lines
				IList<DbSpellLine> dbo = GameServer.Database.SelectAllObjects<DbSpellLine>();

				if (dbo != null)
				{
					// clean cache
					m_spellLineIndex.Clear();

					foreach(DbSpellLine line in dbo)
					{
						try
						{
							m_spellLineIndex.Add(line.KeyName, new SpellLine(line.KeyName, line.Name, line.SpellLineID, line.Spec, line.IsBaseLine));
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("{0} with Spell Line = {1} line.TS= {2}", e.Message, line.KeyName, line.ToString());
						}
					}

					dbo = null;
				}

				if (log.IsInfoEnabled)
					log.InfoFormat("Spell Lines loaded: {0}", m_spellLineIndex.Count);

				//load spell relation
				if (log.IsInfoEnabled)
					log.Info("Loading Spell Lines X Spells Relation...");

				IList<DbLineXSpell> dbox = GameServer.Database.SelectAllObjects<DbLineXSpell>();

				int count = 0;

				if (dbox != null)
				{
					// Clean cache
					m_lineSpells.Clear();

					foreach (DbLineXSpell lxs in dbox)
					{
						try
						{
							if (!m_lineSpells.ContainsKey(lxs.LineName))
								m_lineSpells.Add(lxs.LineName, new List<Spell>());

							Spell spl = (Spell)m_spellIndex[lxs.SpellID].Clone();

							spl.Level = Math.Max(1, lxs.Level);

							m_lineSpells[lxs.LineName].Add(spl);
							count++;
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("LineXSpell Spell Adding Error : {0}, Line {1}, Spell {2}, Level {3}", e.Message, lxs.LineName, lxs.SpellID, lxs.Level);

						}
					}

					dbox = null;
				}

				// sort spells
				foreach (string sps in m_lineSpells.Keys.ToList())
					m_lineSpells[sps] = m_lineSpells[sps].OrderBy(e => e.Level).ThenBy(e => e.ID).ToList();

				if (log.IsInfoEnabled)
					log.InfoFormat("Total spell lines X Spell loaded: {0}", count);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Reload all the DB spells from the database.
		/// Useful to load new spells added in preperation for ReloadSpellLine(linename) to update a spell line live
		/// We want to add any new spells in the DB to the global spell list, m_spells, but not remove any added by scripts
		/// </summary>
		public static void ReloadDBSpells()
		{
			// lock skillbase for writes
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				//load all spells
				if (log.IsInfoEnabled)
					log.Info("Reloading DB spells...");

				IList<DbSpell> spelldb = GameServer.Database.SelectAllObjects<DbSpell>();

				if (spelldb != null)
				{

					int count = 0;

					foreach (DbSpell spell in spelldb)
					{
						if (m_spellIndex.ContainsKey(spell.SpellID) == false)
						{
							// Add new spell
							m_spellIndex.Add(spell.SpellID, new Spell(spell, 1));
							count++;
						}
						else
						{
							// Replace Spell
							m_spellIndex[spell.SpellID] = new Spell(spell, 1);
						}

						// Update tooltip index
						if (spell.TooltipId != 0)
						{
							if (m_spellToolTipIndex.ContainsKey(spell.TooltipId))
								m_spellToolTipIndex[spell.TooltipId] = spell.SpellID;
							else
							{
								m_spellToolTipIndex.Add(spell.TooltipId, spell.SpellID);
								count++;
							}
						}
					}

					if (log.IsInfoEnabled)
					{
						log.Info("Spells loaded from DB: " + spelldb.Count);
						log.Info("Spells added to global spell list: " + count);
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Reload Spell Line Spells from Database without wiping Line collection
		/// This allow to reload from database without wiping scripted spells.
		/// </summary>
		/// <param name="lineName"></param>
		/// <returns></returns>
		[RefreshCommand]
		public static int ReloadSpellLines()
		{
			int count = 0;
			// lock skillbase for writes
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				foreach (string lineName in m_spellLineIndex.Keys)
				{
					// Get SpellLine X Spell relation
					var spells = CoreDb<DbLineXSpell>.SelectObjects(DB.Column("LineName").IsEqualTo(lineName));

					// Load them if any records.
					if (spells != null)
					{
						if (!m_lineSpells.ContainsKey(lineName))
							m_lineSpells.Add(lineName, new List<Spell>());

						foreach (DbLineXSpell lxs in spells)
						{
							try
							{
								// Clone Spell to change Level to relation Level's
								Spell spl = (Spell)m_spellIndex[lxs.SpellID].Clone();

								spl.Level = Math.Max(1, lxs.Level);

								// Look for existing spell for replacement
								bool added = false;

								for (int r = 0; r < m_lineSpells[lineName].Count; r++)
								{
									if (m_lineSpells[lineName][r] != null && m_lineSpells[lineName][r].ID == lxs.SpellID)
									{
										m_lineSpells[lineName][r] = spl;
										added = true;
										break;
									}
								}

								// no replacement then add this
								if (!added)
								{
									m_lineSpells[lineName].Add(spl);
									count++;
								}
							}
							catch (Exception e)
							{
								if (log.IsErrorEnabled)
									log.ErrorFormat("LineXSpell Adding Error : {0}, Line {1}, Spell {2}, Level {3}", e.Message, lxs.LineName, lxs.SpellID, lxs.Level);

							}
						}

						// Line can need a sort...
						m_lineSpells[lineName] = m_lineSpells[lineName].OrderBy(e => e.Level).ThenBy(e => e.ID).ToList();
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}

			return count;
		}

		/// <summary>
		/// This Load Ability from Database
		/// Will wipe any registered "Scripted" Abilities
		/// </summary>
		private static void LoadAbilities()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				// load Abilities
				if (log.IsInfoEnabled)
					log.Info("Loading Abilities...");

				IList<DbAbility> abilities = GameServer.Database.SelectAllObjects<DbAbility>();

				if (abilities != null)
				{
					// Clean Cache
					m_abilityIndex.Clear();

					foreach (DbAbility dba in abilities)
					{
						try
						{
							// test only...
							Ability ability = GetNewAbilityInstance(dba);

							m_abilityIndex.Add(ability.KeyName, dba);

							if (log.IsDebugEnabled)
								log.DebugFormat("Ability {0} successfuly instanciated from {1} (expeted {2})", dba.KeyName, dba.Implementation, ability.GetType());

						}
						catch (Exception e)
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Error while Loading Ability {0} with Class {1} : {2}", dba.KeyName, dba.Implementation, e);
						}
					}
				}

				if (log.IsInfoEnabled)
				{
					log.InfoFormat("Total abilities loaded: {0}", m_abilityIndex.Count);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Load Class Realm Abilities Relations
		/// Wipes any script loaded Relation
		/// </summary>
		private static void LoadClassRealmAbilities()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				// load class RA
				m_classRealmAbilities.Clear();

				if (log.IsInfoEnabled)
					log.Info("Loading class to realm ability associations...");

				IList<DbClassXRealmAbility> classxra = GameServer.Database.SelectAllObjects<DbClassXRealmAbility>();

				if (classxra != null)
				{
					foreach (DbClassXRealmAbility cxra in classxra)
					{
						if (!m_classRealmAbilities.ContainsKey(cxra.CharClass))
							m_classRealmAbilities.Add(cxra.CharClass, new List<string>());

						try
						{
							DbAbility dba = m_abilityIndex[cxra.AbilityKey];

							if (!m_classRealmAbilities[cxra.CharClass].Contains(dba.KeyName))
								m_classRealmAbilities[cxra.CharClass].Add(dba.KeyName);
						}
						catch (Exception e)
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Error while Adding RealmAbility {0} to Class {1} : {2}", cxra.AbilityKey, cxra.CharClass, e);

						}
					}
				}

				log.Info("Realm Abilities assigned to classes!");
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Load Specialization from Database
		/// Also load Relation SpecXAbility, SpecXSpellLine, SpecXStyle
		/// This is Master Loader for Styles, Styles can't work without an existing Database Spec !
		/// Wipe Specs Index, SpecXAbility, SpecXSpellLine, StyleDict, StyleIndex
		/// Anything loaded in this from scripted behavior can be lost... (try to not use Scripted Career !!)
		/// </summary>
		/// <returns>number of specs loaded.</returns>
		[RefreshCommand]
		public static int LoadSpecializations()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				IList<DbSpecialization> specs = GameServer.Database.SelectAllObjects<DbSpecialization>();

				int count = 0;

				if (specs != null)
				{
					// Clear Spec Cache
					m_specsByName.Clear();

					// Clear SpecXAbility Cache (Ability Career)
					m_specsAbilities.Clear();

					// Clear SpecXSpellLine Cache (Spell Career)
					m_specsSpellLines.Clear();

					// Clear Style Cache (Style Career)
					m_specsStyles.Clear();

					// Clear Style ID Cache (Utils...)
					m_styleIndex.Clear();

					foreach (DbSpecialization spec in specs)
					{
						StringBuilder str = new("Specialization ");
						str.AppendFormat("{0} - ", spec.KeyName);

						Specialization gameSpec = null;
						if (!string.IsNullOrEmpty(spec.Implementation))
						{
							gameSpec = GetNewSpecializationInstance(spec.KeyName, spec.Implementation, spec.Name, spec.Icon, spec.SpecializationID);
						}
						else
						{
							gameSpec = new Specialization(spec.KeyName, spec.Name, spec.Icon, spec.SpecializationID);
						}

						if (log.IsDebugEnabled)
							log.DebugFormat("Specialization {0} successfuly instanciated from {1} (expected {2})", spec.KeyName, gameSpec.GetType().FullName, spec.Implementation);

						Tuple<Type, string, ushort, int> entry = new(gameSpec.GetType(), spec.Name, spec.Icon, spec.SpecializationID);

						// Now we have an instanciated Specialization, Cache their properties in Skillbase to prevent using too much memory
						// As most skill objects are duplicated for every game object use...

						// Load SpecXAbility
						count = 0;
						if (spec.AbilityConstraints != null)
						{
							if (!m_specsAbilities.ContainsKey(spec.KeyName))
								m_specsAbilities.Add(spec.KeyName, new List<Tuple<string, byte, int, int>>());

							foreach (DbSpecXAbility specx in spec.AbilityConstraints)
							{

								try
								{
									m_specsAbilities[spec.KeyName].Add(new Tuple<string, byte, int, int>(m_abilityIndex[specx.AbilityKey].KeyName, (byte)specx.SpecLevel, specx.AbilityLevel, specx.ClassId));
									count++;
								}
								catch (Exception e)
								{
									if (log.IsWarnEnabled)
										log.WarnFormat("Specialization : {0} while adding Spec X Ability {1}, from Spec {2}({3}), Level {4}", e.Message, specx.AbilityKey, specx.Spec, specx.SpecLevel, specx.AbilityLevel);
								}
							}

							// sort them according to required levels
							m_specsAbilities[spec.KeyName].Sort((i, j) => i.Item2.CompareTo(j.Item2));
						}

						str.AppendFormat("{0} Ability Constraint, ", count);

						// Load SpecXSpellLine
						count = 0;
						if (spec.SpellLines != null)
						{
							foreach (DbSpellLine line in spec.SpellLines)
							{
								if (!m_specsSpellLines.ContainsKey(spec.KeyName))
									m_specsSpellLines.Add(spec.KeyName, new List<Tuple<SpellLine, int>>());

								try
								{
									m_specsSpellLines[spec.KeyName].Add(new Tuple<SpellLine, int>(m_spellLineIndex[line.KeyName], line.ClassIDHint));
									count++;
								}
								catch (Exception e)
								{
									if (log.IsWarnEnabled)
										log.WarnFormat("Specialization : {0} while adding Spec X SpellLine {1} from Spec {2}, ClassID {3}", e.Message, line.KeyName, line.Spec, line.ClassIDHint);
								}
							}
						}

						str.AppendFormat("{0} Spell Line, ", count);

						// Load DBStyle
						count = 0;
						if (spec.Styles != null)
						{
							foreach (DbStyle specStyle in spec.Styles)
							{
								// Update Style Career
								if (!m_specsStyles.ContainsKey(spec.KeyName))
								{
									m_specsStyles.Add(spec.KeyName, new Dictionary<int, List<Tuple<Style, byte>>>());
								}

								if (!m_specsStyles[spec.KeyName].ContainsKey(specStyle.ClassId))
								{
									m_specsStyles[spec.KeyName].Add(specStyle.ClassId, new List<Tuple<Style, byte>>());
								}

								Style newStyle = new(specStyle, null);

								m_specsStyles[spec.KeyName][specStyle.ClassId].Add(new Tuple<Style, byte>(newStyle, (byte)specStyle.SpecLevelRequirement));

								// Update Style Index.

								KeyValuePair<int, int> styleKey = new(newStyle.ID, specStyle.ClassId);

								if (!m_styleIndex.ContainsKey(styleKey))
								{
									m_styleIndex.Add(styleKey, newStyle);
									count++;
								}
								else
								{
									if (log.IsWarnEnabled)
										log.WarnFormat("Specialization {0} - Duplicate Style Key, StyleID {1} : ClassID {2}, Ignored...", spec.KeyName, newStyle.ID, specStyle.ClassId);
								}

								// Load Procs.
								if (specStyle.AttachedProcs != null)
								{
									foreach (DbStyleXSpell styleProc in specStyle.AttachedProcs)
									{
										if (m_spellIndex.TryGetValue(styleProc.SpellID, out Spell spell))
											newStyle.Procs.Add((spell, styleProc.ClassID, styleProc.Chance));
									}
								}
							}
						}

						// We've added all the styles to their respective lists. Now lets go through and sort them by their level.
						foreach (string keyname in m_specsStyles.Keys)
						{
							foreach (int classid in m_specsStyles[keyname].Keys)
								m_specsStyles[keyname][classid].Sort((i, j) => i.Item2.CompareTo(j.Item2));
						}

						str.AppendFormat("{0} Styles", count);

						if (log.IsDebugEnabled)
							log.Debug(str.ToString());

						// Add spec to global Spec Index Cache
						if (!m_specsByName.ContainsKey(spec.KeyName))
						{
							m_specsByName.Add(spec.KeyName, entry);
						}
						else
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Specialization {0} is duplicated ignoring...", spec.KeyName);
						}
					}

					specs = null;

				}

				if (log.IsInfoEnabled)
					log.InfoFormat("Total specializations loaded: {0}", m_specsByName.Count);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}

			return m_specsByName.Count;
		}

		/// <summary>
		/// Load (or Reload) Class Career, Appending each Class a Specialization !
		/// </summary>
		public static void LoadClassSpecializations()
		{
			// lock skillbase for write
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				if (log.IsInfoEnabled)
					log.Info("Loading Class Specialization's Career...");

				//Retrieve from DB
				IList<DbClassXSpecialization> dbo = GameServer.Database.SelectAllObjects<DbClassXSpecialization>();
				Dictionary<int, StringBuilder> summary = new();

				if (dbo != null)
				{
					// clear
					m_specsByClass.Clear();

					foreach (DbClassXSpecialization career in dbo)
					{
						if (!m_specsByClass.ContainsKey(career.ClassID))
						{
							m_specsByClass.Add(career.ClassID, new Dictionary<string, int>());
							summary.Add(career.ClassID, new StringBuilder());
							summary[career.ClassID].AppendFormat("Career for Class {0} - ", career.ClassID);
						}

						if (!m_specsByClass[career.ClassID].ContainsKey(career.SpecKeyName))
						{
							m_specsByClass[career.ClassID].Add(career.SpecKeyName, career.LevelAcquired);
							summary[career.ClassID].AppendFormat("{0}({1}), ", career.SpecKeyName, career.LevelAcquired);
						}
						else
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Duplicate Sepcialization Key {0} for Class Career : {1}", career.SpecKeyName, career.ClassID);
						}
					}
				}

				if (log.IsInfoEnabled)
					log.Info("Finished loading Class Specialization's Career !");

				if (log.IsDebugEnabled)
				{
					// print summary
					foreach (KeyValuePair<int, StringBuilder> entry in summary)
						log.Debug(entry.Value.ToString());
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Load Ability Handler for Action Ability.
		/// </summary>
		private static void LoadAbilityHandlers()
		{
			m_syncLockUpdates.EnterWriteLock();

			try
			{
				// load Ability actions handlers
				m_abilityActionHandler.Clear();

				//Search for ability handlers in the gameserver first
				if (log.IsInfoEnabled)
					log.Info("Searching ability handlers in GameServer");

				IList<KeyValuePair<string, Type>> ht = ScriptMgr.FindAllAbilityActionHandler(Assembly.GetExecutingAssembly());

				foreach (KeyValuePair<string, Type> entry in ht)
				{
					if (log.IsDebugEnabled)
						log.DebugFormat("\tFound ability handler for {0}", entry.Key);

					if (m_abilityActionHandler.ContainsKey(entry.Key))
					{
						if (log.IsWarnEnabled)
							log.WarnFormat("Duplicate type handler for: ", entry.Key);
					}
					else
					{
						try
						{
							m_abilityActionHandler.Add(entry.Key, GetNewAbilityActionHandlerConstructor(entry.Value));
						}
						catch (Exception ex)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("Error While instantiacting IAbilityHandler {0} using {1} in GameServer : {2}", entry.Key, entry.Value, ex);
						}
					}
				}

				//Now search ability handlers in the scripts directory and overwrite the ones
				//found from gameserver
				if (log.IsInfoEnabled)
					log.Info("Searching AbilityHandlers in Scripts");

				foreach (Assembly asm in ScriptMgr.Scripts)
				{
					ht = ScriptMgr.FindAllAbilityActionHandler(asm);
					foreach (KeyValuePair<string, Type> entry in ht)
					{
						string message = string.Empty;

						try
						{
							if (m_abilityActionHandler.ContainsKey(entry.Key))
								message = "\tFound new ability handler for " + entry.Key;
							else
								message = "\tFound ability handler for " + entry.Key;

							m_abilityActionHandler[entry.Key] = GetNewAbilityActionHandlerConstructor(entry.Value);
						}
						catch (Exception ex)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("Error While instantiacting IAbilityHandler {0} using {1} in GameServerScripts : {2}", entry.Key, entry.Value, ex);
						}

						if (log.IsDebugEnabled)
							log.Debug(message);
					}
				}

				if (log.IsInfoEnabled)
					log.Info("Total ability handlers loaded: " + m_abilityActionHandler.Count);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Load Skill Handler for Action Spec Icon
		/// </summary>
		private static void LoadSkillHandlers()
		{
			m_syncLockUpdates.EnterWriteLock();

			try
			{
				//load skill action handlers
				m_specActionHandler.Clear();

				//Search for skill handlers in gameserver first
				if (log.IsInfoEnabled)
					log.Info("Searching skill handlers in GameServer.");

				IList<KeyValuePair<string, Type>> ht = ScriptMgr.FindAllSpecActionHandler(Assembly.GetExecutingAssembly());

				foreach (KeyValuePair<string, Type> entry in ht)
				{
					if (log.IsDebugEnabled)
						log.Debug("\tFound skill handler for " + entry.Key);

					if (m_specActionHandler.ContainsKey(entry.Key))
					{
						if (log.IsWarnEnabled)
							log.WarnFormat("Duplicate type Skill handler for: ", entry.Key);
					}
					else
					{
						try
						{
							m_specActionHandler.Add(entry.Key, GetNewSpecActionHandlerConstructor(entry.Value));
						}
						catch (Exception ex)
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Error While instantiacting ISpecActionHandler {0} using {1} in GameServer : {2}", entry.Key, entry.Value, ex);
						}
					}
				}

				//Now search skill handlers in the scripts directory and overwrite the ones
				//found from the gameserver

				if (log.IsInfoEnabled)
					log.Info("Searching skill handlers in Scripts.");

				foreach (Assembly asm in ScriptMgr.Scripts)
				{
					ht = ScriptMgr.FindAllSpecActionHandler(asm);

					foreach (KeyValuePair<string, Type> entry in ht)
					{
						string message = string.Empty;

						try
						{
							if (m_specActionHandler.ContainsKey(entry.Key))
								message = "\tFound new spec handler for " + entry.Key;
							else
								message = "\tFound spec handler for " + entry.Key;

							m_specActionHandler[entry.Key] = GetNewSpecActionHandlerConstructor(entry.Value);
						}
						catch (Exception ex)
						{
							if (log.IsWarnEnabled)
								log.WarnFormat("Error While instantiacting ISpecActionHandler {0} using {1} in GameServerScripts : {2}", entry.Key, entry.Value, ex);
						}

						if (log.IsDebugEnabled)
							log.Debug(message);
					}
				}

				if (log.IsInfoEnabled)
					log.Info("Total skill handlers loaded: " + m_specActionHandler.Count);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		#endregion

		#region Initialization Tables

		/// <summary>
		/// Holds object type to spec convertion table
		/// </summary>
		protected static readonly Dictionary<EObjectType, string> m_objectTypeToSpec = new();

		/// <summary>
		/// Holds spec to skill table
		/// </summary>
		protected static readonly Dictionary<string, EProperty> m_specToSkill = new();

		/// <summary>
		/// Holds spec to focus table
		/// </summary>
		protected static readonly Dictionary<string, EProperty> m_specToFocus = new();

		/// <summary>
		/// Holds all property types
		/// </summary>
		private static readonly EPropertyType[] m_propertyTypes = new EPropertyType[(int)EProperty.MaxProperty+1];

		/// <summary>
		/// table for property names
		/// </summary>
		protected static readonly Dictionary<EProperty, string> m_propertyNames = new();

		/// <summary>
		/// Table to hold the race resists
		/// </summary>
		protected static readonly Dictionary<int, int[]> m_raceResists = new();

		/// <summary>
		/// Initialize the object type hashtable
		/// </summary>
		private static void InitializeObjectTypeToSpec()
		{
			m_objectTypeToSpec.Add(EObjectType.Staff, Specs.Staff);
			m_objectTypeToSpec.Add(EObjectType.Fired, Specs.ShortBow);

			m_objectTypeToSpec.Add(EObjectType.FistWraps, Specs.Fist_Wraps);
			m_objectTypeToSpec.Add(EObjectType.MaulerStaff, Specs.Mauler_Staff);

			//alb
			m_objectTypeToSpec.Add(EObjectType.CrushingWeapon, Specs.Crush);
			m_objectTypeToSpec.Add(EObjectType.SlashingWeapon, Specs.Slash);
			m_objectTypeToSpec.Add(EObjectType.ThrustWeapon, Specs.Thrust);
			m_objectTypeToSpec.Add(EObjectType.TwoHandedWeapon, Specs.Two_Handed);
			m_objectTypeToSpec.Add(EObjectType.PolearmWeapon, Specs.Polearms);
			m_objectTypeToSpec.Add(EObjectType.Flexible, Specs.Flexible);
			m_objectTypeToSpec.Add(EObjectType.Crossbow, Specs.Crossbow);

			// RDSandersJR: Check to see if we are using old archery if so, use RangedDamge
			if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
			{
				m_objectTypeToSpec.Add(EObjectType.Longbow, Specs.Longbow);
			}
			// RDSandersJR: If we are NOT using old archery it should be SpellDamage
			else if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
			{
				m_objectTypeToSpec.Add(EObjectType.Longbow, Specs.Archery);
			}

			//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown); break);

			//mid
			m_objectTypeToSpec.Add(EObjectType.Hammer, Specs.Hammer);
			m_objectTypeToSpec.Add(EObjectType.Sword, Specs.Sword);
			m_objectTypeToSpec.Add(EObjectType.LeftAxe, Specs.Left_Axe);
			m_objectTypeToSpec.Add(EObjectType.Axe, Specs.Axe);
			m_objectTypeToSpec.Add(EObjectType.HandToHand, Specs.HandToHand);
			m_objectTypeToSpec.Add(EObjectType.Spear, Specs.Spear);
			m_objectTypeToSpec.Add(EObjectType.Thrown, Specs.Thrown_Weapons);

			// RDSandersJR: Check to see if we are using old archery if so, use RangedDamge
			if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
			{
				m_objectTypeToSpec.Add(EObjectType.CompositeBow, Specs.CompositeBow);
			}
			// RDSandersJR: If we are NOT using old archery it should be SpellDamage
			else if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
			{
				m_objectTypeToSpec.Add(EObjectType.CompositeBow, Specs.Archery);
			}

			//hib
			m_objectTypeToSpec.Add(EObjectType.Blunt, Specs.Blunt);
			m_objectTypeToSpec.Add(EObjectType.Blades, Specs.Blades);
			m_objectTypeToSpec.Add(EObjectType.Piercing, Specs.Piercing);
			m_objectTypeToSpec.Add(EObjectType.LargeWeapons, Specs.Large_Weapons);
			m_objectTypeToSpec.Add(EObjectType.CelticSpear, Specs.Celtic_Spear);
			m_objectTypeToSpec.Add(EObjectType.Scythe, Specs.Scythe);
			m_objectTypeToSpec.Add(EObjectType.Shield, Specs.Shields);
			m_objectTypeToSpec.Add(EObjectType.Poison, Specs.Envenom);

			// RDSandersJR: Check to see if we are using old archery if so, use RangedDamge
			if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
			{
				m_objectTypeToSpec.Add(EObjectType.RecurvedBow, Specs.RecurveBow);
			}
			// RDSandersJR: If we are NOT using old archery it should be SpellDamage
			else if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
			{
				m_objectTypeToSpec.Add(EObjectType.RecurvedBow, Specs.Archery);
			}
		}

		/// <summary>
		/// Initialize the spec to skill table
		/// </summary>
		private static void InitializeSpecToSkill()
		{
			#region Weapon Specs

			//Weapon specs
			//Alb
			m_specToSkill.Add(Specs.Thrust, EProperty.Skill_Thrusting);
			m_specToSkill.Add(Specs.Slash, EProperty.Skill_Slashing);
			m_specToSkill.Add(Specs.Crush, EProperty.Skill_Crushing);
			m_specToSkill.Add(Specs.Polearms, EProperty.Skill_Polearms);
			m_specToSkill.Add(Specs.Two_Handed, EProperty.Skill_Two_Handed);
			m_specToSkill.Add(Specs.Staff, EProperty.Skill_Staff);
			m_specToSkill.Add(Specs.Dual_Wield, EProperty.Skill_Dual_Wield);
			m_specToSkill.Add(Specs.Flexible, EProperty.Skill_Flexible_Weapon);
			m_specToSkill.Add(Specs.Longbow, EProperty.Skill_Long_bows);
			m_specToSkill.Add(Specs.Crossbow, EProperty.Skill_Cross_Bows);
			//Mid
			m_specToSkill.Add(Specs.Sword, EProperty.Skill_Sword);
			m_specToSkill.Add(Specs.Axe, EProperty.Skill_Axe);
			m_specToSkill.Add(Specs.Hammer, EProperty.Skill_Hammer);
			m_specToSkill.Add(Specs.Left_Axe, EProperty.Skill_Left_Axe);
			m_specToSkill.Add(Specs.Spear, EProperty.Skill_Spear);
			m_specToSkill.Add(Specs.CompositeBow, EProperty.Skill_Composite);
			m_specToSkill.Add(Specs.Thrown_Weapons, EProperty.Skill_Thrown_Weapons);
			m_specToSkill.Add(Specs.HandToHand, EProperty.Skill_HandToHand);
			//Hib
			m_specToSkill.Add(Specs.Blades, EProperty.Skill_Blades);
			m_specToSkill.Add(Specs.Blunt, EProperty.Skill_Blunt);
			m_specToSkill.Add(Specs.Piercing, EProperty.Skill_Piercing);
			m_specToSkill.Add(Specs.Large_Weapons, EProperty.Skill_Large_Weapon);
			m_specToSkill.Add(Specs.Celtic_Dual, EProperty.Skill_Celtic_Dual);
			m_specToSkill.Add(Specs.Celtic_Spear, EProperty.Skill_Celtic_Spear);
			m_specToSkill.Add(Specs.RecurveBow, EProperty.Skill_RecurvedBow);
			m_specToSkill.Add(Specs.Scythe, EProperty.Skill_Scythe);

			#endregion

			#region Magic Specs

			//Magic specs
			//Alb
			m_specToSkill.Add(Specs.Matter_Magic, EProperty.Skill_Matter);
			m_specToSkill.Add(Specs.Body_Magic, EProperty.Skill_Body);
			m_specToSkill.Add(Specs.Spirit_Magic, EProperty.Skill_Spirit);
			m_specToSkill.Add(Specs.Rejuvenation, EProperty.Skill_Rejuvenation);
			m_specToSkill.Add(Specs.Enhancement, EProperty.Skill_Enhancement);
			m_specToSkill.Add(Specs.Smite, EProperty.Skill_Smiting);
			m_specToSkill.Add(Specs.Instruments, EProperty.Skill_Instruments);
			m_specToSkill.Add(Specs.Deathsight, EProperty.Skill_DeathSight);
			m_specToSkill.Add(Specs.Painworking, EProperty.Skill_Pain_working);
			m_specToSkill.Add(Specs.Death_Servant, EProperty.Skill_Death_Servant);
			m_specToSkill.Add(Specs.Chants, EProperty.Skill_Chants);
			m_specToSkill.Add(Specs.Mind_Magic, EProperty.Skill_Mind);
			m_specToSkill.Add(Specs.Earth_Magic, EProperty.Skill_Earth);
			m_specToSkill.Add(Specs.Cold_Magic, EProperty.Skill_Cold);
			m_specToSkill.Add(Specs.Fire_Magic, EProperty.Skill_Fire);
			m_specToSkill.Add(Specs.Wind_Magic, EProperty.Skill_Wind);
			m_specToSkill.Add(Specs.Soulrending, EProperty.Skill_SoulRending);
			//Mid
			m_specToSkill.Add(Specs.Darkness, EProperty.Skill_Darkness);
			m_specToSkill.Add(Specs.Suppression, EProperty.Skill_Suppression);
			m_specToSkill.Add(Specs.Runecarving, EProperty.Skill_Runecarving);
			m_specToSkill.Add(Specs.Summoning, EProperty.Skill_Summoning);
			m_specToSkill.Add(Specs.BoneArmy, EProperty.Skill_BoneArmy);
			m_specToSkill.Add(Specs.Mending, EProperty.Skill_Mending);
			m_specToSkill.Add(Specs.Augmentation, EProperty.Skill_Augmentation);
			m_specToSkill.Add(Specs.Pacification, EProperty.Skill_Pacification);
			m_specToSkill.Add(Specs.Subterranean, EProperty.Skill_Subterranean);
			m_specToSkill.Add(Specs.Beastcraft, EProperty.Skill_BeastCraft);
			m_specToSkill.Add(Specs.Stormcalling, EProperty.Skill_Stormcalling);
			m_specToSkill.Add(Specs.Battlesongs, EProperty.Skill_Battlesongs);
			m_specToSkill.Add(Specs.Savagery, EProperty.Skill_Savagery);
			m_specToSkill.Add(Specs.OdinsWill, EProperty.Skill_OdinsWill);
			m_specToSkill.Add(Specs.Cursing, EProperty.Skill_Cursing);
			m_specToSkill.Add(Specs.Hexing, EProperty.Skill_Hexing);
			m_specToSkill.Add(Specs.Witchcraft, EProperty.Skill_Witchcraft);

			//Hib
			m_specToSkill.Add(Specs.Arboreal_Path, EProperty.Skill_Arboreal);
			m_specToSkill.Add(Specs.Creeping_Path, EProperty.Skill_Creeping);
			m_specToSkill.Add(Specs.Verdant_Path, EProperty.Skill_Verdant);
			m_specToSkill.Add(Specs.Regrowth, EProperty.Skill_Regrowth);
			m_specToSkill.Add(Specs.Nurture, EProperty.Skill_Nurture);
			m_specToSkill.Add(Specs.Music, EProperty.Skill_Music);
			m_specToSkill.Add(Specs.Valor, EProperty.Skill_Valor);
			m_specToSkill.Add(Specs.Nature, EProperty.Skill_Nature);
			m_specToSkill.Add(Specs.Light, EProperty.Skill_Light);
			m_specToSkill.Add(Specs.Void, EProperty.Skill_Void);
			m_specToSkill.Add(Specs.Mana, EProperty.Skill_Mana);
			m_specToSkill.Add(Specs.Enchantments, EProperty.Skill_Enchantments);
			m_specToSkill.Add(Specs.Mentalism, EProperty.Skill_Mentalism);
			m_specToSkill.Add(Specs.Nightshade_Magic, EProperty.Skill_Nightshade);
			m_specToSkill.Add(Specs.Pathfinding, EProperty.Skill_Pathfinding);
			m_specToSkill.Add(Specs.Dementia, EProperty.Skill_Dementia);
			m_specToSkill.Add(Specs.ShadowMastery, EProperty.Skill_ShadowMastery);
			m_specToSkill.Add(Specs.VampiiricEmbrace, EProperty.Skill_VampiiricEmbrace);
			m_specToSkill.Add(Specs.EtherealShriek, EProperty.Skill_EtherealShriek);
			m_specToSkill.Add(Specs.PhantasmalWail, EProperty.Skill_PhantasmalWail);
			m_specToSkill.Add(Specs.SpectralForce, EProperty.Skill_SpectralForce);
			m_specToSkill.Add(Specs.SpectralGuard, EProperty.Skill_SpectralGuard);

			#endregion

			#region Other

			//Other
			m_specToSkill.Add(Specs.Critical_Strike, EProperty.Skill_Critical_Strike);
			m_specToSkill.Add(Specs.Stealth, EProperty.Skill_Stealth);
			m_specToSkill.Add(Specs.Shields, EProperty.Skill_Shields);
			m_specToSkill.Add(Specs.Envenom, EProperty.Skill_Envenom);
			m_specToSkill.Add(Specs.Parry, EProperty.Skill_Parry);
			m_specToSkill.Add(Specs.ShortBow, EProperty.Skill_ShortBow);
			m_specToSkill.Add(Specs.Mauler_Staff, EProperty.Skill_MaulerStaff);
			m_specToSkill.Add(Specs.Fist_Wraps, EProperty.Skill_FistWraps);
			m_specToSkill.Add(Specs.Aura_Manipulation, EProperty.Skill_Aura_Manipulation);
			m_specToSkill.Add(Specs.Magnetism, EProperty.Skill_Magnetism);
			m_specToSkill.Add(Specs.Power_Strikes, EProperty.Skill_Power_Strikes);

			m_specToSkill.Add(Specs.Archery, EProperty.Skill_Archery);

			#endregion
		}

		/// <summary>
		/// Initialize the spec to focus tables
		/// </summary>
		private static void InitializeSpecToFocus()
		{
			m_specToFocus.Add(Specs.Darkness, EProperty.Focus_Darkness);
			m_specToFocus.Add(Specs.Suppression, EProperty.Focus_Suppression);
			m_specToFocus.Add(Specs.Runecarving, EProperty.Focus_Runecarving);
			m_specToFocus.Add(Specs.Spirit_Magic, EProperty.Focus_Spirit);
			m_specToFocus.Add(Specs.Fire_Magic, EProperty.Focus_Fire);
			m_specToFocus.Add(Specs.Wind_Magic, EProperty.Focus_Air);
			m_specToFocus.Add(Specs.Cold_Magic, EProperty.Focus_Cold);
			m_specToFocus.Add(Specs.Earth_Magic, EProperty.Focus_Earth);
			m_specToFocus.Add(Specs.Light, EProperty.Focus_Light);
			m_specToFocus.Add(Specs.Body_Magic, EProperty.Focus_Body);
			m_specToFocus.Add(Specs.Mind_Magic, EProperty.Focus_Mind);
			m_specToFocus.Add(Specs.Matter_Magic, EProperty.Focus_Matter);
			m_specToFocus.Add(Specs.Void, EProperty.Focus_Void);
			m_specToFocus.Add(Specs.Mana, EProperty.Focus_Mana);
			m_specToFocus.Add(Specs.Enchantments, EProperty.Focus_Enchantments);
			m_specToFocus.Add(Specs.Mentalism, EProperty.Focus_Mentalism);
			m_specToFocus.Add(Specs.Summoning, EProperty.Focus_Summoning);
			// SI
			m_specToFocus.Add(Specs.BoneArmy, EProperty.Focus_BoneArmy);
			m_specToFocus.Add(Specs.Painworking, EProperty.Focus_PainWorking);
			m_specToFocus.Add(Specs.Deathsight, EProperty.Focus_DeathSight);
			m_specToFocus.Add(Specs.Death_Servant, EProperty.Focus_DeathServant);
			m_specToFocus.Add(Specs.Verdant_Path, EProperty.Focus_Verdant);
			m_specToFocus.Add(Specs.Creeping_Path, EProperty.Focus_CreepingPath);
			m_specToFocus.Add(Specs.Arboreal_Path, EProperty.Focus_Arboreal);
			// Catacombs
			m_specToFocus.Add(Specs.EtherealShriek, EProperty.Focus_EtherealShriek);
			m_specToFocus.Add(Specs.PhantasmalWail, EProperty.Focus_PhantasmalWail);
			m_specToFocus.Add(Specs.SpectralForce, EProperty.Focus_SpectralForce);
			m_specToFocus.Add(Specs.Cursing, EProperty.Focus_Cursing);
			m_specToFocus.Add(Specs.Hexing, EProperty.Focus_Hexing);
			m_specToFocus.Add(Specs.Witchcraft, EProperty.Focus_Witchcraft);
		}

		/// <summary>
		/// Init property types table
		/// </summary>
		private static void InitPropertyTypes()
		{
			#region Resist

			// resists
			m_propertyTypes[(int)EProperty.Resist_Natural] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Body] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Cold] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Crush] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Energy] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Heat] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Matter] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Slash] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Spirit] = EPropertyType.Resist;
			m_propertyTypes[(int)EProperty.Resist_Thrust] = EPropertyType.Resist;

			#endregion

			#region Focus

			// focuses
			m_propertyTypes[(int)EProperty.Focus_Darkness] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Suppression] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Runecarving] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Spirit] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Fire] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Air] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Cold] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Earth] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Light] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Body] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Matter] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Mind] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Void] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Mana] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Enchantments] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Mentalism] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Summoning] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_BoneArmy] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_PainWorking] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_DeathSight] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_DeathServant] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Verdant] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_CreepingPath] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Arboreal] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_EtherealShriek] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_PhantasmalWail] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_SpectralForce] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Cursing] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Hexing] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.Focus_Witchcraft] = EPropertyType.Focus;
			m_propertyTypes[(int)EProperty.AllFocusLevels] = EPropertyType.Focus;

			#endregion

			/*
			 * http://www.camelotherald.com/more/1036.shtml
			 * "- ALL melee weapon skills - This bonus will increase your
			 * skill in many weapon types. This bonus does not increase shield,
			 * parry, archery skills, or dual wield skills (hand to hand is the
			 * exception, as this skill is also the main weapon skill associated
			 * with hand to hand weapons, and not just the off-hand skill). If
			 * your item has "All melee weapon skills: +3" and your character
			 * can train in hammer, axe and sword, your item should give you
			 * a +3 increase to all three."
			 */

			#region Melee Skills

			// skills
			m_propertyTypes[(int)EProperty.Skill_Two_Handed] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Critical_Strike] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Crushing] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Flexible_Weapon] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Polearms] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Slashing] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Staff] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Thrusting] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Sword] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Hammer] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Axe] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Spear] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Blades] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Blunt] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Piercing] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Large_Weapon] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Celtic_Spear] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Scythe] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_Thrown_Weapons] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_HandToHand] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_FistWraps] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;
			m_propertyTypes[(int)EProperty.Skill_MaulerStaff] = EPropertyType.Skill | EPropertyType.SkillMeleeWeapon;

			m_propertyTypes[(int)EProperty.Skill_Dual_Wield] = EPropertyType.Skill | EPropertyType.SkillDualWield;
			m_propertyTypes[(int)EProperty.Skill_Left_Axe] = EPropertyType.Skill | EPropertyType.SkillDualWield;
			m_propertyTypes[(int)EProperty.Skill_Celtic_Dual] = EPropertyType.Skill | EPropertyType.SkillDualWield;

			#endregion

			#region Magical Skills

			m_propertyTypes[(int)EProperty.Skill_Power_Strikes] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Magnetism] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Aura_Manipulation] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Body] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Chants] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Death_Servant] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_DeathSight] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Earth] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Enhancement] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Fire] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Cold] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Instruments] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Matter] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Mind] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Pain_working] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Rejuvenation] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Smiting] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_SoulRending] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Spirit] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Wind] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Mending] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Augmentation] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Darkness] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Suppression] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Runecarving] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Stormcalling] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_BeastCraft] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Light] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Void] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Mana] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Battlesongs] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Enchantments] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Mentalism] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Regrowth] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Nurture] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Nature] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Music] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Valor] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Subterranean] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_BoneArmy] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Verdant] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Creeping] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Arboreal] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Pacification] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Savagery] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Nightshade] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Pathfinding] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Summoning] = EPropertyType.Skill | EPropertyType.SkillMagical;

			// no idea about these
			m_propertyTypes[(int)EProperty.Skill_Dementia] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_ShadowMastery] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_VampiiricEmbrace] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_EtherealShriek] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_PhantasmalWail] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_SpectralForce] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_SpectralGuard] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_OdinsWill] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Cursing] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Hexing] = EPropertyType.Skill | EPropertyType.SkillMagical;
			m_propertyTypes[(int)EProperty.Skill_Witchcraft] = EPropertyType.Skill | EPropertyType.SkillMagical;

			#endregion

			#region Other

			m_propertyTypes[(int)EProperty.Skill_Long_bows] = EPropertyType.Skill | EPropertyType.SkillArchery;
			m_propertyTypes[(int)EProperty.Skill_Composite] = EPropertyType.Skill | EPropertyType.SkillArchery;
			m_propertyTypes[(int)EProperty.Skill_RecurvedBow] = EPropertyType.Skill | EPropertyType.SkillArchery;

			m_propertyTypes[(int)EProperty.Skill_Parry] = EPropertyType.Skill;
			m_propertyTypes[(int)EProperty.Skill_Shields] = EPropertyType.Skill;

			m_propertyTypes[(int)EProperty.Skill_Stealth] = EPropertyType.Skill;
			m_propertyTypes[(int)EProperty.Skill_Cross_Bows] = EPropertyType.Skill;
			m_propertyTypes[(int)EProperty.Skill_ShortBow] = EPropertyType.Skill;
			m_propertyTypes[(int)EProperty.Skill_Envenom] = EPropertyType.Skill;
			m_propertyTypes[(int)EProperty.Skill_Archery] = EPropertyType.Skill | EPropertyType.SkillArchery;

			#endregion
		}

		/// <summary>
		/// Initializes the race resist table
		/// </summary>
		public static void InitializeRaceResists()
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				// http://camelot.allakhazam.com/Start_Stats.html
				IList<DbRace> races;

				try
				{
					races = GameServer.Database.SelectAllObjects<DbRace>();
				}
				catch
				{
					m_raceResists.Clear();
					return;
				}

				if (races != null)
				{

					m_raceResists.Clear();

					foreach (DbRace race in races)
					{
						m_raceResists.Add(race.ID, new int[10]);
						m_raceResists[race.ID][0] = race.ResistBody;
						m_raceResists[race.ID][1] = race.ResistCold;
						m_raceResists[race.ID][2] = race.ResistCrush;
						m_raceResists[race.ID][3] = race.ResistEnergy;
						m_raceResists[race.ID][4] = race.ResistHeat;
						m_raceResists[race.ID][5] = race.ResistMatter;
						m_raceResists[race.ID][6] = race.ResistSlash;
						m_raceResists[race.ID][7] = race.ResistSpirit;
						m_raceResists[race.ID][8] = race.ResistThrust;
						m_raceResists[race.ID][9] = race.ResistNatural;
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		private static void RegisterPropertyNames()
		{
			#region register...
			m_propertyNames.Add(EProperty.Strength, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                   "SkillBase.RegisterPropertyNames.Strength"));
			m_propertyNames.Add(EProperty.Dexterity, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.Dexterity"));
			m_propertyNames.Add(EProperty.Constitution, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Constitution"));
			m_propertyNames.Add(EProperty.Quickness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.Quickness"));
			m_propertyNames.Add(EProperty.Intelligence, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Intelligence"));
			m_propertyNames.Add(EProperty.Piety, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                "SkillBase.RegisterPropertyNames.Piety"));
			m_propertyNames.Add(EProperty.Empathy, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                  "SkillBase.RegisterPropertyNames.Empathy"));
			m_propertyNames.Add(EProperty.Charisma, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                   "SkillBase.RegisterPropertyNames.Charisma"));

			m_propertyNames.Add(EProperty.MaxMana, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                  "SkillBase.RegisterPropertyNames.Power"));
			m_propertyNames.Add(EProperty.MaxHealth, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.Hits"));

			// resists (does not say "resist" on live server)
			m_propertyNames.Add(EProperty.Resist_Body, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Body"));
			m_propertyNames.Add(EProperty.Resist_Natural, "Essence");
			m_propertyNames.Add(EProperty.Resist_Cold, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Cold"));
			m_propertyNames.Add(EProperty.Resist_Crush, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Crush"));
			m_propertyNames.Add(EProperty.Resist_Energy, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Energy"));
			m_propertyNames.Add(EProperty.Resist_Heat, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Heat"));
			m_propertyNames.Add(EProperty.Resist_Matter, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Matter"));
			m_propertyNames.Add(EProperty.Resist_Slash, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Slash"));
			m_propertyNames.Add(EProperty.Resist_Spirit, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Spirit"));
			m_propertyNames.Add(EProperty.Resist_Thrust, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Thrust"));

			// Eden - Mythirian bonus
			m_propertyNames.Add(EProperty.BodyResCapBonus, "Body cap");
			m_propertyNames.Add(EProperty.ColdResCapBonus, "Cold cap");
			m_propertyNames.Add(EProperty.CrushResCapBonus, "Crush cap");
			m_propertyNames.Add(EProperty.EnergyResCapBonus, "Energy cap");
			m_propertyNames.Add(EProperty.HeatResCapBonus, "Heat cap");
			m_propertyNames.Add(EProperty.MatterResCapBonus, "Matter cap");
			m_propertyNames.Add(EProperty.SlashResCapBonus, "Slash cap");
			m_propertyNames.Add(EProperty.SpiritResCapBonus, "Spirit cap");
			m_propertyNames.Add(EProperty.ThrustResCapBonus, "Thrust cap");
			m_propertyNames.Add(EProperty.MythicalSafeFall, "Mythical Safe Fall");
			m_propertyNames.Add(EProperty.MythicalDiscumbering, "Mythical Discumbering");
			m_propertyNames.Add(EProperty.MythicalCoin, "Mythical Coin");
			m_propertyNames.Add(EProperty.SpellLevel, "Spell Focus");
			//Eden - special actifacts bonus
			m_propertyNames.Add(EProperty.Conversion, "Conversion");
			m_propertyNames.Add(EProperty.ExtraHP, "Extra Health Points");
			m_propertyNames.Add(EProperty.StyleAbsorb, "Style Absorb");
			m_propertyNames.Add(EProperty.ArcaneSyphon, "Arcane Syphon");
			m_propertyNames.Add(EProperty.RealmPoints, "Realm Points");
			//[Freya] Nidel
			m_propertyNames.Add(EProperty.BountyPoints, "Bounty Points");
			m_propertyNames.Add(EProperty.XpPoints, "Experience Points");

			// skills
			m_propertyNames.Add(EProperty.Skill_Two_Handed, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.TwoHanded"));
			m_propertyNames.Add(EProperty.Skill_Body, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.BodyMagic"));
			m_propertyNames.Add(EProperty.Skill_Chants, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Chants"));
			m_propertyNames.Add(EProperty.Skill_Critical_Strike, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                "SkillBase.RegisterPropertyNames.CriticalStrike"));
			m_propertyNames.Add(EProperty.Skill_Cross_Bows, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.Crossbows"));
			m_propertyNames.Add(EProperty.Skill_Crushing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Crushing"));
			m_propertyNames.Add(EProperty.Skill_Death_Servant, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.DeathServant"));
			m_propertyNames.Add(EProperty.Skill_DeathSight, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.Deathsight"));
			m_propertyNames.Add(EProperty.Skill_Dual_Wield, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.DualWield"));
			m_propertyNames.Add(EProperty.Skill_Earth, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.EarthMagic"));
			m_propertyNames.Add(EProperty.Skill_Enhancement, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Enhancement"));
			m_propertyNames.Add(EProperty.Skill_Envenom, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Envenom"));
			m_propertyNames.Add(EProperty.Skill_Fire, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.FireMagic"));
			m_propertyNames.Add(EProperty.Skill_Flexible_Weapon, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                "SkillBase.RegisterPropertyNames.FlexibleWeapon"));
			m_propertyNames.Add(EProperty.Skill_Cold, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.ColdMagic"));
			m_propertyNames.Add(EProperty.Skill_Instruments, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Instruments"));
			m_propertyNames.Add(EProperty.Skill_Long_bows, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Longbows"));
			m_propertyNames.Add(EProperty.Skill_Matter, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.MatterMagic"));
			m_propertyNames.Add(EProperty.Skill_Mind, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.MindMagic"));
			m_propertyNames.Add(EProperty.Skill_Pain_working, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Painworking"));
			m_propertyNames.Add(EProperty.Skill_Parry, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Parry"));
			m_propertyNames.Add(EProperty.Skill_Polearms, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Polearms"));
			m_propertyNames.Add(EProperty.Skill_Rejuvenation, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Rejuvenation"));
			m_propertyNames.Add(EProperty.Skill_Shields, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Shields"));
			m_propertyNames.Add(EProperty.Skill_Slashing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Slashing"));
			m_propertyNames.Add(EProperty.Skill_Smiting, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Smiting"));
			m_propertyNames.Add(EProperty.Skill_SoulRending, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Soulrending"));
			m_propertyNames.Add(EProperty.Skill_Spirit, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.SpiritMagic"));
			m_propertyNames.Add(EProperty.Skill_Staff, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Staff"));
			m_propertyNames.Add(EProperty.Skill_Stealth, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Stealth"));
			m_propertyNames.Add(EProperty.Skill_Thrusting, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Thrusting"));
			m_propertyNames.Add(EProperty.Skill_Wind, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.WindMagic"));
			m_propertyNames.Add(EProperty.Skill_Sword, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Sword"));
			m_propertyNames.Add(EProperty.Skill_Hammer, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Hammer"));
			m_propertyNames.Add(EProperty.Skill_Axe, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.Axe"));
			m_propertyNames.Add(EProperty.Skill_Left_Axe, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.LeftAxe"));
			m_propertyNames.Add(EProperty.Skill_Spear, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Spear"));
			m_propertyNames.Add(EProperty.Skill_Mending, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Mending"));
			m_propertyNames.Add(EProperty.Skill_Augmentation, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Augmentation"));
			m_propertyNames.Add(EProperty.Skill_Darkness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Darkness"));
			m_propertyNames.Add(EProperty.Skill_Suppression, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Suppression"));
			m_propertyNames.Add(EProperty.Skill_Runecarving, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Runecarving"));
			m_propertyNames.Add(EProperty.Skill_Stormcalling, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Stormcalling"));
			m_propertyNames.Add(EProperty.Skill_BeastCraft, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.BeastCraft"));
			m_propertyNames.Add(EProperty.Skill_Light, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.LightMagic"));
			m_propertyNames.Add(EProperty.Skill_Void, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.VoidMagic"));
			m_propertyNames.Add(EProperty.Skill_Mana, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.ManaMagic"));
			m_propertyNames.Add(EProperty.Skill_Composite, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Composite"));
			m_propertyNames.Add(EProperty.Skill_Battlesongs, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Battlesongs"));
			m_propertyNames.Add(EProperty.Skill_Enchantments, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Enchantment"));

			m_propertyNames.Add(EProperty.Skill_Blades, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Blades"));
			m_propertyNames.Add(EProperty.Skill_Blunt, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Blunt"));
			m_propertyNames.Add(EProperty.Skill_Piercing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Piercing"));
			m_propertyNames.Add(EProperty.Skill_Large_Weapon, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.LargeWeapon"));
			m_propertyNames.Add(EProperty.Skill_Mentalism, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Mentalism"));
			m_propertyNames.Add(EProperty.Skill_Regrowth, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Regrowth"));
			m_propertyNames.Add(EProperty.Skill_Nurture, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Nurture"));
			m_propertyNames.Add(EProperty.Skill_Nature, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Nature"));
			m_propertyNames.Add(EProperty.Skill_Music, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Music"));
			m_propertyNames.Add(EProperty.Skill_Celtic_Dual, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.CelticDual"));
			m_propertyNames.Add(EProperty.Skill_Celtic_Spear, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.CelticSpear"));
			m_propertyNames.Add(EProperty.Skill_RecurvedBow, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.RecurvedBow"));
			m_propertyNames.Add(EProperty.Skill_Valor, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.Valor"));
			m_propertyNames.Add(EProperty.Skill_Subterranean, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.CaveMagic"));
			m_propertyNames.Add(EProperty.Skill_BoneArmy, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.BoneArmy"));
			m_propertyNames.Add(EProperty.Skill_Verdant, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Verdant"));
			m_propertyNames.Add(EProperty.Skill_Creeping, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Creeping"));
			m_propertyNames.Add(EProperty.Skill_Arboreal, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Arboreal"));
			m_propertyNames.Add(EProperty.Skill_Scythe, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Scythe"));
			m_propertyNames.Add(EProperty.Skill_Thrown_Weapons, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.ThrownWeapons"));
			m_propertyNames.Add(EProperty.Skill_HandToHand, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.HandToHand"));
			m_propertyNames.Add(EProperty.Skill_ShortBow, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.ShortBow"));
			m_propertyNames.Add(EProperty.Skill_Pacification, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.Pacification"));
			m_propertyNames.Add(EProperty.Skill_Savagery, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Savagery"));
			m_propertyNames.Add(EProperty.Skill_Nightshade, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.NightshadeMagic"));
			m_propertyNames.Add(EProperty.Skill_Pathfinding, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.Pathfinding"));
			m_propertyNames.Add(EProperty.Skill_Summoning, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Summoning"));
			m_propertyNames.Add(EProperty.Skill_Archery, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Archery"));

			// Mauler
			m_propertyNames.Add(EProperty.Skill_FistWraps, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.FistWraps"));
			m_propertyNames.Add(EProperty.Skill_MaulerStaff, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.MaulerStaff"));
			m_propertyNames.Add(EProperty.Skill_Power_Strikes, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.PowerStrikes"));
			m_propertyNames.Add(EProperty.Skill_Magnetism, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.Magnetism"));
			m_propertyNames.Add(EProperty.Skill_Aura_Manipulation, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                  "SkillBase.RegisterPropertyNames.AuraManipulation"));

			//Catacombs skills
			m_propertyNames.Add(EProperty.Skill_Dementia, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.Dementia"));
			m_propertyNames.Add(EProperty.Skill_ShadowMastery, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.ShadowMastery"));
			m_propertyNames.Add(EProperty.Skill_VampiiricEmbrace, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                 "SkillBase.RegisterPropertyNames.VampiiricEmbrace"));
			m_propertyNames.Add(EProperty.Skill_EtherealShriek, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.EtherealShriek"));
			m_propertyNames.Add(EProperty.Skill_PhantasmalWail, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.PhantasmalWail"));
			m_propertyNames.Add(EProperty.Skill_SpectralForce, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.SpectralForce"));
			m_propertyNames.Add(EProperty.Skill_SpectralGuard, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.SpectralGuard"));
			m_propertyNames.Add(EProperty.Skill_OdinsWill, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.OdinsWill"));
			m_propertyNames.Add(EProperty.Skill_Cursing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.Cursing"));
			m_propertyNames.Add(EProperty.Skill_Hexing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.Hexing"));
			m_propertyNames.Add(EProperty.Skill_Witchcraft, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.Witchcraft"));

			// Classic Focii
			m_propertyNames.Add(EProperty.Focus_Darkness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.DarknessFocus"));
			m_propertyNames.Add(EProperty.Focus_Suppression, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.SuppressionFocus"));
			m_propertyNames.Add(EProperty.Focus_Runecarving, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.RunecarvingFocus"));
			m_propertyNames.Add(EProperty.Focus_Spirit, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.SpiritMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Fire, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.FireMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Air, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.WindMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Cold, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.ColdMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Earth, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.EarthMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Light, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.LightMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Body, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.BodyMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Matter, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.MatterMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Mind, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.MindMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Void, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.VoidMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Mana, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.ManaMagicFocus"));
			m_propertyNames.Add(EProperty.Focus_Enchantments, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.EnchantmentFocus"));
			m_propertyNames.Add(EProperty.Focus_Mentalism, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.MentalismFocus"));
			m_propertyNames.Add(EProperty.Focus_Summoning, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.SummoningFocus"));
			// SI Focii
			// Mid
			m_propertyNames.Add(EProperty.Focus_BoneArmy, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.BoneArmyFocus"));
			// Alb
			m_propertyNames.Add(EProperty.Focus_PainWorking, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.PainworkingFocus"));
			m_propertyNames.Add(EProperty.Focus_DeathSight, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.DeathsightFocus"));
			m_propertyNames.Add(EProperty.Focus_DeathServant, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.DeathservantFocus"));
			// Hib
			m_propertyNames.Add(EProperty.Focus_Verdant, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.VerdantFocus"));
			m_propertyNames.Add(EProperty.Focus_CreepingPath, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.CreepingPathFocus"));
			m_propertyNames.Add(EProperty.Focus_Arboreal, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.ArborealFocus"));
			// Catacombs Focii
			m_propertyNames.Add(EProperty.Focus_EtherealShriek, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.EtherealShriekFocus"));
			m_propertyNames.Add(EProperty.Focus_PhantasmalWail, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.PhantasmalWailFocus"));
			m_propertyNames.Add(EProperty.Focus_SpectralForce, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                              "SkillBase.RegisterPropertyNames.SpectralForceFocus"));
			m_propertyNames.Add(EProperty.Focus_Cursing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.CursingFocus"));
			m_propertyNames.Add(EProperty.Focus_Hexing, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.HexingFocus"));
			m_propertyNames.Add(EProperty.Focus_Witchcraft, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.WitchcraftFocus"));

			m_propertyNames.Add(EProperty.MaxSpeed, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                   "SkillBase.RegisterPropertyNames.MaximumSpeed"));
			m_propertyNames.Add(EProperty.MaxConcentration, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.Concentration"));

			m_propertyNames.Add(EProperty.ArmorFactor, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.BonusToArmorFactor"));
			m_propertyNames.Add(EProperty.ArmorAbsorption, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                          "SkillBase.RegisterPropertyNames.BonusToArmorAbsorption"));

			m_propertyNames.Add(EProperty.HealthRegenerationRate, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                 "SkillBase.RegisterPropertyNames.HealthRegeneration"));
			m_propertyNames.Add(EProperty.PowerRegenerationRate, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                "SkillBase.RegisterPropertyNames.PowerRegeneration"));
			m_propertyNames.Add(EProperty.EnduranceRegenerationRate, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                    "SkillBase.RegisterPropertyNames.EnduranceRegeneration"));
			m_propertyNames.Add(EProperty.SpellRange, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.SpellRange"));
			m_propertyNames.Add(EProperty.ArcheryRange, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.ArcheryRange"));
			m_propertyNames.Add(EProperty.Acuity, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                 "SkillBase.RegisterPropertyNames.Acuity"));

			m_propertyNames.Add(EProperty.AllMagicSkills, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.AllMagicSkills"));
			m_propertyNames.Add(EProperty.AllMeleeWeaponSkills, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.AllMeleeWeaponSkills"));
			m_propertyNames.Add(EProperty.AllFocusLevels, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.ALLSpellLines"));
			m_propertyNames.Add(EProperty.AllDualWieldingSkills, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                "SkillBase.RegisterPropertyNames.AllDualWieldingSkills"));
			m_propertyNames.Add(EProperty.AllArcherySkills, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                           "SkillBase.RegisterPropertyNames.AllArcherySkills"));

			m_propertyNames.Add(EProperty.LivingEffectiveLevel, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.EffectiveLevel"));

			//Added by Fooljam : Missing TOA/Catacomb bonusses names in item properties.
			//Date : 20-Jan-2005
			//Missing bonusses begin
			m_propertyNames.Add(EProperty.EvadeChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.EvadeChance"));
			m_propertyNames.Add(EProperty.BlockChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.BlockChance"));
			m_propertyNames.Add(EProperty.ParryChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.ParryChance"));
			m_propertyNames.Add(EProperty.FumbleChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.FumbleChance"));
			m_propertyNames.Add(EProperty.MeleeDamage, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.MeleeDamage"));
			m_propertyNames.Add(EProperty.RangedDamage, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.RangedDamage"));
			m_propertyNames.Add(EProperty.MesmerizeDurationReduction, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.MesmerizeDuration"));
			m_propertyNames.Add(EProperty.StunDurationReduction, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.StunDuration"));
			m_propertyNames.Add(EProperty.SpeedDecreaseDurationReduction, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                "SkillBase.RegisterPropertyNames.SpeedDecreaseDuration"));
			m_propertyNames.Add(EProperty.BladeturnReinforcement, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                 "SkillBase.RegisterPropertyNames.BladeturnReinforcement"));
			m_propertyNames.Add(EProperty.DefensiveBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.DefensiveBonus"));
			m_propertyNames.Add(EProperty.PieceAblative, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.PieceAblative"));
			m_propertyNames.Add(EProperty.NegativeReduction, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.NegativeReduction"));
			m_propertyNames.Add(EProperty.ReactionaryStyleDamage, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                                 "SkillBase.RegisterPropertyNames.ReactionaryStyleDamage"));
			m_propertyNames.Add(EProperty.SpellPowerCost, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                         "SkillBase.RegisterPropertyNames.SpellPowerCost"));
			m_propertyNames.Add(EProperty.StyleCostReduction, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.StyleCostReduction"));
			m_propertyNames.Add(EProperty.ToHitBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.ToHitBonus"));
			m_propertyNames.Add(EProperty.ArcherySpeed, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.ArcherySpeed"));
			m_propertyNames.Add(EProperty.ArrowRecovery, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.ArrowRecovery"));
			m_propertyNames.Add(EProperty.BuffEffectiveness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.StatBuffSpells"));
			m_propertyNames.Add(EProperty.CastingSpeed, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.CastingSpeed"));
			m_propertyNames.Add(EProperty.OffhandDamageAndChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.OffhandChanceAndDamage"));
			m_propertyNames.Add(EProperty.DebuffEffectivness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                             "SkillBase.RegisterPropertyNames.DebuffEffectivness"));
			m_propertyNames.Add(EProperty.Fatigue, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                  "SkillBase.RegisterPropertyNames.Fatigue"));
			m_propertyNames.Add(EProperty.HealingEffectiveness, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                               "SkillBase.RegisterPropertyNames.HealingEffectiveness"));
			m_propertyNames.Add(EProperty.PowerPool, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.PowerPool"));
			//Magiekraftvorrat
			m_propertyNames.Add(EProperty.ResistPierce, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                       "SkillBase.RegisterPropertyNames.ResistPierce"));
			m_propertyNames.Add(EProperty.SpellDamage, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.MagicDamageBonus"));
			m_propertyNames.Add(EProperty.SpellDuration, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                        "SkillBase.RegisterPropertyNames.SpellDuration"));
			m_propertyNames.Add(EProperty.StyleDamage, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.StyleDamage"));
			m_propertyNames.Add(EProperty.MeleeSpeed, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                     "SkillBase.RegisterPropertyNames.MeleeSpeed"));
			//Missing bonusses end

			m_propertyNames.Add(EProperty.StrCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.StrengthBonusCap"));
			m_propertyNames.Add(EProperty.DexCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.DexterityBonusCap"));
			m_propertyNames.Add(EProperty.ConCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.ConstitutionBonusCap"));
			m_propertyNames.Add(EProperty.QuiCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.QuicknessBonusCap"));
			m_propertyNames.Add(EProperty.IntCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.IntelligenceBonusCap"));
			m_propertyNames.Add(EProperty.PieCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.PietyBonusCap"));
			m_propertyNames.Add(EProperty.ChaCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.CharismaBonusCap"));
			m_propertyNames.Add(EProperty.EmpCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.EmpathyBonusCap"));
			m_propertyNames.Add(EProperty.AcuCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.AcuityBonusCap"));
			m_propertyNames.Add(EProperty.MaxHealthCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.HitPointsBonusCap"));
			m_propertyNames.Add(EProperty.PowerPoolCapBonus, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                            "SkillBase.RegisterPropertyNames.PowerBonusCap"));
			m_propertyNames.Add(EProperty.WeaponSkill, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                      "SkillBase.RegisterPropertyNames.WeaponSkill"));
			m_propertyNames.Add(EProperty.AllSkills, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE,
			                                                                    "SkillBase.RegisterPropertyNames.AllSkills"));
			m_propertyNames.Add(EProperty.CriticalArcheryHitChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "SkillBase.RegisterPropertyNames.CriticalArcheryHit"));
			m_propertyNames.Add(EProperty.CriticalMeleeHitChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "SkillBase.RegisterPropertyNames.CriticalMeleeHit"));
			m_propertyNames.Add(EProperty.CriticalSpellHitChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "SkillBase.RegisterPropertyNames.CriticalSpellHit"));
			m_propertyNames.Add(EProperty.CriticalHealHitChance, LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "SkillBase.RegisterPropertyNames.CriticalHealHit"));

            //Forsaken Worlds: Mythical Stat Cap
            m_propertyNames.Add(EProperty.MythicalStrCapBonus, "Mythical Stat Cap (Strength)");
			m_propertyNames.Add(EProperty.MythicalDexCapBonus, "Mythical Stat Cap (Dexterity)");
			m_propertyNames.Add(EProperty.MythicalConCapBonus, "Mythical Stat Cap (Constitution)");
			m_propertyNames.Add(EProperty.MythicalQuiCapBonus, "Mythical Stat Cap (Quickness)");
			m_propertyNames.Add(EProperty.MythicalIntCapBonus, "Mythical Stat Cap (Intelligence)");
			m_propertyNames.Add(EProperty.MythicalPieCapBonus, "Mythical Stat Cap (Piety)");
			m_propertyNames.Add(EProperty.MythicalChaCapBonus, "Mythical Stat Cap (Charisma)");
			m_propertyNames.Add(EProperty.MythicalEmpCapBonus, "Mythical Stat Cap (Empathy)");
			m_propertyNames.Add(EProperty.MythicalAcuCapBonus, "Mythical Stat Cap (Acuity)");

            #endregion
		}

		#endregion

		#region Armor resists

		// lookup table for armor resists
		private const int REALM_BITCOUNT = 2;
		private const int DAMAGETYPE_BITCOUNT = 4;
		private const int ARMORTYPE_BITCOUNT = 3;
		private static readonly int[] m_armorResists = new int[1 << (REALM_BITCOUNT + DAMAGETYPE_BITCOUNT + ARMORTYPE_BITCOUNT)];

		/// <summary>
		/// Gets the natural armor resist to the give damage type
		/// </summary>
		/// <param name="armor"></param>
		/// <param name="damageType"></param>
		/// <returns>resist value</returns>
		public static int GetArmorResist(DbInventoryItem armor, EDamageType damageType)
		{
			if (armor == null)
				return 0;

			int realm = armor.Template.Realm - (int)ERealm._First;
			int armorType = armor.Template.Object_Type - (int)EObjectType._FirstArmor;
			int damage = damageType - EDamageType._FirstResist;

			if (realm < 0 || realm > ERealm._LastPlayerRealm - ERealm._First)
				return 0;

			if (armorType < 0 || armorType > EObjectType._LastArmor - EObjectType._FirstArmor)
				return 0;

			if (damage < 0 || damage > EDamageType._LastResist - EDamageType._FirstResist)
				return 0;

			const int realmBits = DAMAGETYPE_BITCOUNT + ARMORTYPE_BITCOUNT;

			//Console.WriteLine($"Realm {realm} armorType {armorType} damage {damage} input {(realm << realmBits) | (armorType << DAMAGETYPE_BITCOUNT) | damage} resistoutput {m_armorResists[(realm << realmBits) | (armorType << DAMAGETYPE_BITCOUNT) | damage]}");

			return m_armorResists[(realm << realmBits) | (armorType << DAMAGETYPE_BITCOUNT) | damage];
		}

		private static void InitArmorResists()
		{
			const int resistant		= 10;
			const int vulnerable	= -10;

			// melee resists (slash, crush, thrust)

			// alb armor - neutral to slash
			// plate and leather resistant to thrust
			// chain and studded vulnerable to thrust
			WriteMeleeResists(ERealm.Albion, EObjectType.Leather, 0, vulnerable, resistant);
			WriteMeleeResists(ERealm.Albion, EObjectType.Plate,   0, vulnerable, resistant);
			WriteMeleeResists(ERealm.Albion, EObjectType.Studded, 0, resistant,  vulnerable);
			WriteMeleeResists(ERealm.Albion, EObjectType.Chain,   0, resistant,  vulnerable);

			// hib armor - neutral to thrust
			// reinforced and leather vulnerable to crush
			// scale resistant to crush
			WriteMeleeResists( ERealm.Hibernia, EObjectType.Leather,    resistant,  vulnerable, 0 );
			WriteMeleeResists( ERealm.Hibernia, EObjectType.Reinforced, resistant,  vulnerable, 0 );
			WriteMeleeResists(ERealm.Hibernia,  EObjectType.Scale,      vulnerable, resistant, 0);

			// mid armor - neutral to crush
			// studded and leather resistant to thrust
			// chain vulnerabel to thrust
			WriteMeleeResists(ERealm.Midgard, EObjectType.Studded, vulnerable, 0, resistant);
			WriteMeleeResists(ERealm.Midgard, EObjectType.Leather, vulnerable, 0, resistant);
			WriteMeleeResists(ERealm.Midgard, EObjectType.Chain,   resistant,  0, vulnerable);

			// magical damage (Heat, Cold, Matter, Energy)
			// Leather
			WriteMagicResists(ERealm.Albion,   EObjectType.Leather, vulnerable, resistant, vulnerable, 0);
			WriteMagicResists(ERealm.Hibernia, EObjectType.Leather, vulnerable, resistant, vulnerable, 0);
			WriteMagicResists(ERealm.Midgard,  EObjectType.Leather, vulnerable, resistant, vulnerable, 0);

			// Reinforced/Studded
			WriteMagicResists(ERealm.Albion,   EObjectType.Studded,    resistant, vulnerable, vulnerable, vulnerable);
			WriteMagicResists(ERealm.Hibernia, EObjectType.Reinforced, resistant, vulnerable, vulnerable, vulnerable);
			WriteMagicResists(ERealm.Midgard,  EObjectType.Studded,    resistant, vulnerable, vulnerable, vulnerable);

			// Chain
			WriteMagicResists(ERealm.Albion,  EObjectType.Chain, resistant, 0, 0, vulnerable);
			WriteMagicResists(ERealm.Midgard, EObjectType.Chain, resistant, 0, 0, vulnerable);

			// Scale/Plate
			WriteMagicResists(ERealm.Albion,   EObjectType.Plate, resistant, vulnerable, resistant, vulnerable);
			WriteMagicResists(ERealm.Hibernia, EObjectType.Scale, resistant, vulnerable, resistant, vulnerable);
		}

		private static void WriteMeleeResists(ERealm realm, EObjectType armorType, int slash, int crush, int thrust)
		{
			if (realm < ERealm._First || realm > ERealm._LastPlayerRealm)
				throw new ArgumentOutOfRangeException(nameof(realm), realm, "Realm should be between _First and _LastPlayerRealm.");
			if (armorType < EObjectType._FirstArmor || armorType > EObjectType._LastArmor)
				throw new ArgumentOutOfRangeException(nameof(armorType), armorType, "Armor type should be between _FirstArmor and _LastArmor");

			int off = (realm - ERealm._First) << (DAMAGETYPE_BITCOUNT + ARMORTYPE_BITCOUNT);
			off |= (armorType - EObjectType._FirstArmor) << DAMAGETYPE_BITCOUNT;
			m_armorResists[off + (EDamageType.Slash - EDamageType._FirstResist)] = slash;
			m_armorResists[off + (EDamageType.Crush - EDamageType._FirstResist)] = crush;
			m_armorResists[off + (EDamageType.Thrust - EDamageType._FirstResist)] = thrust;
		}

		private static void WriteMagicResists(ERealm realm, EObjectType armorType, int heat, int cold, int matter, int energy)
		{
			if (realm < ERealm._First || realm > ERealm._LastPlayerRealm)
				throw new ArgumentOutOfRangeException(nameof(realm), realm, "Realm should be between _First and _LastPlayerRealm.");
			if (armorType < EObjectType._FirstArmor || armorType > EObjectType._LastArmor)
				throw new ArgumentOutOfRangeException(nameof(armorType), armorType, "Armor type should be between _FirstArmor and _LastArmor");

			int off = (realm - ERealm._First) << (DAMAGETYPE_BITCOUNT + ARMORTYPE_BITCOUNT);
			off |= (armorType - EObjectType._FirstArmor) << DAMAGETYPE_BITCOUNT;
			m_armorResists[off + (EDamageType.Heat - EDamageType._FirstResist)] = heat;
			m_armorResists[off + (EDamageType.Cold - EDamageType._FirstResist)] = cold;
			m_armorResists[off + (EDamageType.Matter - EDamageType._FirstResist)] = matter;
			m_armorResists[off + (EDamageType.Energy - EDamageType._FirstResist)] = energy;
		}

		#endregion

		/// <summary>
		/// Check if property belongs to all of specified types
		/// </summary>
		/// <param name="prop">The property to check</param>
		/// <param name="type">The types to check</param>
		/// <returns>true if property belongs to all types</returns>
		public static bool CheckPropertyType(EProperty prop, EPropertyType type)
		{
			int property = (int)prop;
			if (property < 0 || property >= m_propertyTypes.Length)
				return false;

			return (m_propertyTypes[property] & type) == type;
		}

		/// <summary>
		/// Gets a new AbilityActionHandler instance associated with given KeyName
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public static IAbilityActionHandler GetAbilityActionHandler(string keyName)
		{
			m_syncLockUpdates.EnterReadLock();
			Func<IAbilityActionHandler> handlerConstructor;

			try
			{
				m_abilityActionHandler.TryGetValue(keyName, out handlerConstructor);
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			return handlerConstructor != null ? handlerConstructor() : null;
		}

		/// <summary>
		/// Gets a new SpecActionHandler instance associated with given KeyName
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public static ISpecActionHandler GetSpecActionHandler(string keyName)
		{
			m_syncLockUpdates.EnterReadLock();
			Func<ISpecActionHandler> handlerConstructor;

			try
			{
				m_specActionHandler.TryGetValue(keyName, out handlerConstructor);
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			return handlerConstructor != null ? handlerConstructor() : null;
		}

		/// <summary>
		/// Register or Overwrite a Spell Line
		/// </summary>
		/// <param name="line"></param>
		public static void RegisterSpellLine(SpellLine line)
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				if (m_spellLineIndex.ContainsKey(line.KeyName))
					m_spellLineIndex[line.KeyName] = line;
				else
					m_spellLineIndex.Add(line.KeyName, line);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Add a new style to a specialization. If the specialization does not exist it will be created.
		/// After adding all styles call SortStyles to sort the list by level
		/// </summary>
		/// <param name="style"></param>
		public static void AddScriptedStyle(Specialization spec, DbStyle style)
		{
			m_syncLockUpdates.EnterWriteLock();

			try
			{
				if (!m_specsStyles.ContainsKey(spec.KeyName))
					m_specsStyles.Add(spec.KeyName, new Dictionary<int, List<Tuple<Style, byte>>>());

				if (!m_specsStyles[spec.KeyName].ContainsKey(style.ClassId))
					m_specsStyles[spec.KeyName].Add(style.ClassId, new List<Tuple<Style, byte>>());

				Style st = new(style, null);
				m_specsStyles[spec.KeyName][style.ClassId].Add(new Tuple<Style, byte>(st, (byte)style.SpecLevelRequirement));
				KeyValuePair<int, int> styleKey = new(st.ID, style.ClassId);

				if (!m_styleIndex.ContainsKey(styleKey))
					m_styleIndex.Add(styleKey, st);

				if (!m_specsByName.ContainsKey(spec.KeyName))
					RegisterSpec(spec);

				if (style.AttachedProcs != null)
				{
					foreach (DbStyleXSpell styleProc in style.AttachedProcs)
					{
						if (m_spellIndex.TryGetValue(styleProc.SpellID, out Spell spell))
							st.Procs.Add((spell, styleProc.ClassID, styleProc.Chance));
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Register or Overwrite a spec in Cache
		/// </summary>
		/// <param name="spec"></param>
		public static void RegisterSpec(Specialization spec)
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				Tuple<Type, string, ushort, int> entry = new(spec.GetType(), spec.Name, spec.Icon, spec.ID);

				if (m_specsByName.ContainsKey(spec.KeyName))
					m_specsByName[spec.KeyName] = entry;
				else
					m_specsByName.Add(spec.KeyName, entry);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Remove a Spell Line from Cache
		/// </summary>
		/// <param name="LineKeyName"></param>
		public static void UnRegisterSpellLine(string LineKeyName)
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				if (m_spellLineIndex.ContainsKey(LineKeyName))
					m_spellLineIndex.Remove(LineKeyName);
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// returns level 1 instantiated realm abilities, only for readonly use!
		/// </summary>
		/// <param name="classID"></param>
		/// <returns></returns>
		public static List<RealmAbility> GetClassRealmAbilities(int classID)
		{
			List<DbAbility> ras = new();
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_classRealmAbilities.ContainsKey(classID))
				{
					foreach (string str in m_classRealmAbilities[classID])
					{
						try
						{
							ras.Add(m_abilityIndex[str]);
						}
						catch
						{
						}
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			/// [Atlas - Takii] Order RAs by their PrimaryKey in the DB so we have control over their order, instead of base DOL implementation.
			//return ras.Select(e => GetNewAbilityInstance(e)).Where(ab => ab is RealmAbility).Cast<RealmAbility>().OrderByDescending(el => el.MaxLevel).ThenBy(el => el.KeyName).ToList();
			return ras.Select(e => GetNewAbilityInstance(e)).Where(ab => ab is RealmAbility).Cast<RealmAbility>().ToList();
		}

		/// <summary>
		/// Return this character class RR5 Ability Level 1 or null
		/// </summary>
		/// <param name="charclass"></param>
		/// <returns></returns>
		public static Ability GetClassRR5Ability(int charclass)
		{
			return GetClassRealmAbilities(charclass).Where(ab => ab is RR5RealmAbility).FirstOrDefault();
		}

		/// <summary>
		/// Get Ability by internal ID, used for Tooltip Details.
		/// </summary>
		/// <param name="internalID"></param>
		/// <returns></returns>
		public static Ability GetAbilityByInternalID(int internalID)
		{
			m_syncLockUpdates.EnterReadLock();
			string ability = null;
			try
			{
				ability = m_abilityIndex.Where(it => it.Value.InternalID == internalID).FirstOrDefault().Value.KeyName;
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (!string.IsNullOrEmpty(ability))
				return GetAbility(ability, 1);

			return GetAbility(string.Format("INTERNALID:{0}", internalID), 1);
		}

		/// <summary>
		/// Get Ability by Keyname
		/// </summary>
		/// <param name="keyname"></param>
		/// <returns></returns>
		public static Ability GetAbility(string keyname)
		{
			return GetAbility(keyname, 1);
		}

		/// <summary>
		/// Get Ability by dbid.
		/// </summary>
		/// <param name="keyname"></param>
		/// <returns></returns>
		public static Ability GetAbility(int databaseID)
		{
			m_syncLockUpdates.EnterReadLock();
			string ability = null;
			try
			{
				ability = m_abilityIndex.Where(it => it.Value.AbilityID == databaseID).FirstOrDefault().Value.KeyName;
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (!string.IsNullOrEmpty(ability))
				return GetAbility(ability, 1);

			return GetAbility(string.Format("DBID:{0}", databaseID), 1);
		}

		/// <summary>
		/// Get Ability by Keyname and Level
		/// </summary>
		/// <param name="keyname"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public static Ability GetAbility(string keyname, int level)
		{
			m_syncLockUpdates.EnterReadLock();
			DbAbility dbab = null;
			try
			{
				if (m_abilityIndex.ContainsKey(keyname))
				{
					dbab = m_abilityIndex[keyname];
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (dbab != null)
			{
				Ability dba = GetNewAbilityInstance(dbab);
				dba.Level = level;
				return dba;
			}

			if (log.IsWarnEnabled)
				log.Warn("Ability '" + keyname + "' unknown");

			return new Ability(keyname, "?" + keyname, "", 0, 0, level, 0);
		}

		/// <summary>
		/// return all spells for a specific spell-line
		/// if no spells are associated or spell-line is unknown the list will be empty
		/// </summary>
		/// <param name="spellLineID">KeyName of spell-line</param>
		/// <returns>list of spells, never null</returns>
		public static List<Spell> GetSpellList(string spellLineID)
		{
			List<Spell> spellList = new();
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_lineSpells.ContainsKey(spellLineID))
				{
					foreach (var element in m_lineSpells[spellLineID])
						spellList.Add((Spell)element.Clone());
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			return spellList;
		}

		/// <summary>
		/// Update or add a spell to the global spell list. Useful for adding procs and charges to items without restarting server.
		/// This will not update a spell in a spell line.
		/// </summary>
		/// <param name="spellID"></param>
		/// <returns></returns>
		public static bool UpdateSpell(int spellID)
		{
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				var dbSpell = CoreDb<DbSpell>.SelectObject(DB.Column("SpellID").IsEqualTo(spellID));

				if (dbSpell != null)
				{
					Spell spell = new(dbSpell, 1);

					if (m_spellIndex.ContainsKey(spellID))
					{
						m_spellIndex[spellID] = spell;
					}
					else
					{
						m_spellIndex.Add(spellID, spell);
					}

					// Update tooltip index
					if (spell.InternalID != 0)
					{
						if (m_spellToolTipIndex.ContainsKey((ushort)spell.InternalID))
							m_spellToolTipIndex[(ushort)spell.InternalID] = spell.ID;
						else
							m_spellToolTipIndex.Add((ushort)spell.InternalID, spell.ID);
					}

					return true;
				}

				return false;
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Get Spell Lines attached to a Spec (with class hint).
		/// </summary>
		/// <param name="specName">Spec Key Name</param>
		/// <returns></returns>
		public static IList<Tuple<SpellLine, int>> GetSpecsSpellLines(string specName)
		{
			IList<Tuple<SpellLine, int>> list = new List<Tuple<SpellLine, int>>();
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_specsSpellLines.ContainsKey(specName))
				{
					foreach(Tuple<SpellLine, int> entry in m_specsSpellLines[specName])
					{
						list.Add(new Tuple<SpellLine, int>((SpellLine)entry.Item1.Clone(), entry.Item2));
					}
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			return list;
		}

		/// <summary>
		/// Return the spell line, creating a temporary one if not found
		/// </summary>
		/// <param name="keyname"></param>
		/// <returns></returns>
		public static SpellLine GetSpellLine(string keyname)
		{
			return GetSpellLine(keyname, true);
		}

		/// <summary>
		/// Return a spell line
		/// </summary>
		/// <param name="keyname">The key name of the line</param>
		/// <param name="create">Should we create a temp spell line if not found?</param>
		/// <returns></returns>
		public static SpellLine GetSpellLine(string keyname, bool create)
		{
			SpellLine result = null;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_spellLineIndex.ContainsKey(keyname))
					result = (SpellLine)m_spellLineIndex[keyname].Clone();
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (result != null)
				return result;

			if (create)
			{
				if (log.IsWarnEnabled)
				{
					log.WarnFormat("Spell-Line {0} unknown, creating temporary line.", keyname);
				}

				return new SpellLine(keyname, string.Format("{0}?", keyname), "", true);
			}

			return null;
		}

		/// <summary>
		/// Add a scripted spell to a spellline
		/// will try to add to global spell list if not exists (preventing obvious harcoded errors...)
		/// </summary>
		/// <param name="spellLineID"></param>
		/// <param name="spell"></param>
		public static void AddScriptedSpell(string spellLineID, Spell spell)
		{
			// lock Skillbase for writes
			m_syncLockUpdates.EnterWriteLock();
			Spell spcp = null;
			try
			{
				if (spell.ID > 0 && !m_spellIndex.ContainsKey(spell.ID))
				{
					spcp = (Spell)spell.Clone();
					// Level 1 for storing...
					spcp.Level = 1;

					m_spellIndex.Add(spell.ID, spcp);

					// Add Tooltip Index
					if (spcp.InternalID != 0 && !m_spellToolTipIndex.ContainsKey((ushort)spcp.InternalID))
						m_spellToolTipIndex.Add((ushort)spcp.InternalID, spcp.ID);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}

			// let the base handler do this...
			if (spcp != null)
			{
				AddSpellToSpellLine(spellLineID, spell.ID, spell.Level);
				return;
			}

			m_syncLockUpdates.EnterWriteLock();
			try
			{
				// Cannot store it in spell index !! ID could be wrongly set we can't rely on it !
				if (!m_lineSpells.ContainsKey(spellLineID))
					m_lineSpells.Add(spellLineID, new List<Spell>());

				// search for duplicates
				bool added = false;
				for (int r = 0; r < m_lineSpells[spellLineID].Count; r++)
				{
					try
					{
						if ((m_lineSpells[spellLineID][r] != null &&
						    spell.ID > 0 && m_lineSpells[spellLineID][r].ID == spell.ID && m_lineSpells[spellLineID][r].Name.ToLower().Equals(spell.Name.ToLower()) && m_lineSpells[spellLineID][r].SpellType.ToString().ToLower().Equals(spell.SpellType.ToString().ToLower()))
						    || (m_lineSpells[spellLineID][r].Name.ToLower().Equals(spell.Name.ToLower()) && m_lineSpells[spellLineID][r].SpellType.ToString().ToLower().Equals(spell.SpellType.ToString().ToLower())))
						{
							m_lineSpells[spellLineID][r] = spell;
							added = true;
						}
					}
					catch
					{
					}
				}

				// try regular add (this could go wrong if duplicate detection is bad...)
				if (!added)
					m_lineSpells[spellLineID].Add(spell);

				m_lineSpells[spellLineID] = m_lineSpells[spellLineID].OrderBy(e => e.Level).ThenBy(e => e.ID).ToList();

			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Add an existing spell to a spell line, adding a new line if needed.
		/// Primarily used for Champion spells but can be used to make any custom spell list
		/// From spells already loaded from the DB
		/// </summary>
		/// <param name="spellLineID"></param>
		/// <param name="spellID"></param>
		public static void AddSpellToSpellLine(string spellLineID, int spellID, int level = 1)
		{
			// Lock SkillBase for writes
			m_syncLockUpdates.EnterWriteLock();
			try
			{
				// Add Spell Line if needed (doesn't create the spellline index...)
				if(!m_lineSpells.ContainsKey(spellLineID))
					m_lineSpells.Add(spellLineID, new List<Spell>());

				try
				{
					Spell spl = (Spell)m_spellIndex[spellID].Clone();
					spl.Level = level;

					// search if it exists
					bool added = false;
					for (int r = 0; r < m_lineSpells[spellLineID].Count ; r++)
					{
						if (m_lineSpells[spellLineID][r] != null && m_lineSpells[spellLineID][r].ID == spl.ID)
						{
							// Replace
							m_lineSpells[spellLineID][r] = spl;
							added = true;
						}
					}

					if (!added)
						m_lineSpells[spellLineID].Add(spl);

					m_lineSpells[spellLineID] = m_lineSpells[spellLineID].OrderBy(e => e.Level).ThenBy(e => e.ID).ToList();
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.ErrorFormat("Spell Line {1} Error {0} when adding Spell ID: {2}", e, spellLineID, spellID);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitWriteLock();
			}
		}

		/// <summary>
		/// Get Specialization By Internal ID, used for Tooltip
		/// </summary>
		/// <param name="internalID"></param>
		/// <returns></returns>
		public static Specialization GetSpecializationByInternalID(int internalID)
		{
			m_syncLockUpdates.EnterReadLock();
			string spec = null;
			try
			{
				spec = m_specsByName.Where(e => e.Value.Item4 == internalID).Select(e => e.Key).FirstOrDefault();
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (!string.IsNullOrEmpty(spec))
				return GetSpecialization(spec, false);

			return GetSpecialization(string.Format("INTERNALID:{0}", internalID), true);
		}

		/// <summary>
		/// Get a loaded specialization, warn if not found and create a dummy entry
		/// </summary>
		/// <param name="keyname"></param>
		/// <returns></returns>
		public static Specialization GetSpecialization(string keyname)
		{
			return GetSpecialization(keyname, true);
		}

		/// <summary>
		/// Get a specialization by Keyname
		/// </summary>
		/// <param name="keyname"></param>
		/// <param name="create">if not found generate a warning and create a dummy entry</param>
		/// <returns></returns>
		public static Specialization GetSpecialization(string keyname, bool create)
		{
			Specialization spec = null;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_specsByName.ContainsKey(keyname))
					spec = GetNewSpecializationInstance(keyname, m_specsByName[keyname]);
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (spec.KeyName == keyname)
				return spec;

			if (create)
			{
				if (log.IsWarnEnabled)
				{
					log.WarnFormat("Specialization {0} unknown", keyname);
				}

				// Untrainable Spec by default to prevent garbage in player display...
				return new UntrainableSpecialization(keyname, "?" + keyname, 0, 0);
			}

			return null;
		}

		public static List<Specialization> GetSpecializationByType(Type type)
		{
			List<Specialization> result = null;
			m_syncLockUpdates.EnterReadLock();

			try
			{
				result = m_specsByName.Where(ts => type.IsAssignableFrom(ts.Value.Item1))
					.Select(ts => GetNewSpecializationInstance(ts.Key, ts.Value)).ToList();
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			result ??= new List<Specialization>();
			return result;
		}

		/// <summary>
		/// Get a Class Specialization Career's to use data oriented Specialization Abilities and Skills.
		/// </summary>
		/// <param name="classID">Character Class ID</param>
		/// <returns>Dictionary of Specialization with their Level Requirement (including ClassId 0 for game wide specs)</returns>
		public static IDictionary<Specialization, int> GetSpecializationCareer(int classID)
		{
			Dictionary<Specialization, int> dictRes = new();
			m_syncLockUpdates.EnterReadLock();
			IDictionary<string, int> entries = new Dictionary<string, int>();
			try
			{
				if (m_specsByClass.ContainsKey(classID))
				{
					entries = new Dictionary<string, int>(m_specsByClass[classID]);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			foreach (KeyValuePair<string, int> constraint in entries)
			{
				try
				{
					Specialization spec = GetSpecialization(constraint.Key, false);
					spec.LevelRequired = constraint.Value;
					dictRes.Add(spec, constraint.Value);
				}
				catch
				{
				}
			}

			m_syncLockUpdates.EnterReadLock();
			entries = new Dictionary<string, int>();
			try
			{
				if (m_specsByClass.ContainsKey(0))
				{
					entries = new Dictionary<string, int>(m_specsByClass[0]);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			// all Character Career's (mainly for sprint...)
			foreach (KeyValuePair<string, int> constraint in entries)
			{
				try
				{
					Specialization spec = GetSpecialization(constraint.Key, false);
					spec.LevelRequired = constraint.Value;
					dictRes.Add(spec, constraint.Value);
				}
				catch
				{
				}
			}

			return dictRes;
		}

		/// <summary>
		/// return all styles for a specific specialization
		/// if no style are associated or spec is unknown the list will be empty
		/// </summary>
		/// <param name="specID">KeyName of spec</param>
		/// <param name="classId">ClassID for which style list is requested</param>
		/// <returns>list of styles, never null</returns>
		public static List<Style> GetStyleList(string specID, int classId)
		{
			m_syncLockUpdates.EnterReadLock();
			List<Tuple<Style, byte>> entries = new();
			try
			{
				if(m_specsStyles.ContainsKey(specID) && m_specsStyles[specID].ContainsKey(classId))
				{
					entries = new List<Tuple<Style, byte>>(m_specsStyles[specID][classId]);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			List<Style> styleRes = new();

			foreach(Tuple<Style, byte> constraint in entries)
				styleRes.Add((Style)constraint.Item1.Clone());

			return styleRes;
		}

		/// <summary>
		/// returns spec dependent abilities
		/// </summary>
		/// <param name="specID">KeyName of spec</param>
		/// <returns>list of abilities or empty list</returns>
		public static List<Ability> GetSpecAbilityList(string specID, int classID)
		{
			m_syncLockUpdates.EnterReadLock();
			List<Tuple<string, byte, int, int>> entries = new();
			try
			{
				if (m_specsAbilities.ContainsKey(specID))
				{
					entries = new List<Tuple<string, byte, int, int>>(m_specsAbilities[specID]);
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			List<Ability> abRes = new();
			foreach (Tuple<string, byte, int, int> constraint in entries)
			{
				if (constraint.Item4 != 0 && constraint.Item4 != classID)
					continue;

				Ability ab = GetNewAbilityInstance(constraint.Item1, constraint.Item3);
				ab.Spec = specID;
				ab.SpecLevelRequirement = constraint.Item2;
				abRes.Add(ab);
			}

			return abRes;
		}

		/// <summary>
		/// Find style by Internal ID, needed for Tooltip Use.
		/// </summary>
		/// <param name="internalID"></param>
		/// <returns></returns>
		public static Style GetStyleByInternalID(int internalID)
		{
			Style style = null;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				style = m_styleIndex.Where(e => e.Value.InternalID == internalID).Select(e => e.Value).FirstOrDefault();
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (style != null)
				return (Style)style.Clone();

			return style;
		}

		/// <summary>
		/// Find style with specific id and return a copy of it
		/// </summary>
		/// <param name="styleID">id of style</param>
		/// <param name="classId">ClassID for which style list is requested</param>
		/// <returns>style or null if not found</returns>
		public static Style GetStyleByID(int styleID, int classId)
		{
			KeyValuePair<int, int> styleKey = new(styleID, classId);
			Style style;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				m_styleIndex.TryGetValue(styleKey, out style);
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (style != null)
				return (Style)style.Clone();

			return style;
		}

		/// <summary>
		/// Returns spell with id, level of spell is always 1
		/// </summary>
		/// <param name="spellID"></param>
		/// <returns></returns>
		public static Spell GetSpellByID(int spellID)
		{
			Spell spell;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				m_spellIndex.TryGetValue(spellID, out spell);
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (spell != null)
				return (Spell)spell.Clone();

			return null;
		}

		/// <summary>
		/// Returns spell with id, level of spell is always 1
		/// </summary>
		/// <param name="spellID"></param>
		/// <returns></returns>
		public static Spell GetSpellByTooltipID(ushort ttid)
		{
			Spell spell;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_spellToolTipIndex.TryGetValue(ttid, out int spellid))
				{
					m_spellIndex.TryGetValue(spellid, out spell);
				}
				else
				{
					spell = null;
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (spell != null)
				return (Spell)spell.Clone();

			return null;
		}

		/// <summary>
		/// Will attempt to find either in the spell line given or in the list of all spells
		/// </summary>
		/// <param name="spellID"></param>
		/// <param name="line"></param>
		/// <returns></returns>
		public static Spell FindSpell(int spellID, SpellLine line)
		{
			Spell spell = null;

			if (line != null)
			{
				List<Spell> spells = GetSpellList(line.KeyName);
				foreach (Spell lineSpell in spells)
				{
					if (lineSpell.ID == spellID)
					{
						spell = lineSpell;
						break;
					}
				}
			}

			spell ??= GetSpellByID(spellID);
			return spell;
		}

		/// <summary>
		/// Get display name of property
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public static string GetPropertyName(EProperty prop)
		{
			if (!m_propertyNames.TryGetValue(prop, out string name))
			{
				name = "Property" + (int) prop;
			}

			return name;
		}

		/// <summary>
		/// determine race-dependent base resist
		/// </summary>
		/// <param name="race">Value must be greater than 0</param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int GetRaceResist(int race, EResist type)
		{
			if( race == 0 )
				return 0;

			int resistValue = 0;

			if (m_raceResists.ContainsKey(race))
			{
				int resistIndex;

				if (type == EResist.Natural)
					resistIndex = 9;
				else
					resistIndex = (int)type - (int)EProperty.Resist_First;

				if (resistIndex < m_raceResists[race].Length)
				{
					resistValue = m_raceResists[race][resistIndex];
				}
				else
				{
					log.WarnFormat("No resists defined for type: {0}", type.ToString());
				}
			}
			else
			{
				log.WarnFormat("No resists defined for race: {0}", race);
			}

			return resistValue;
		}

		/// <summary>
		/// Convert object type to spec needed to use that object
		/// </summary>
		/// <param name="objectType">type of the object</param>
		/// <returns>spec names needed to use that object type</returns>
		public static string ObjectTypeToSpec(EObjectType objectType)
		{
			if (!m_objectTypeToSpec.TryGetValue(objectType, out string res))
				if (log.IsWarnEnabled)
					log.Warn("Not found spec for object type " + objectType);
			return res;
		}

		/// <summary>
		/// Convert spec to skill property
		/// </summary>
		/// <param name="specKey"></param>
		/// <returns></returns>
		public static EProperty SpecToSkill(string specKey)
		{
			if (!m_specToSkill.TryGetValue(specKey, out EProperty res))
			{
				//if (log.IsWarnEnabled)
				//log.Warn("No skill property found for spec " + specKey);
				return EProperty.Undefined;
			}

			return res;
		}

		/// <summary>
		/// Convert spec to focus
		/// </summary>
		/// <param name="specKey"></param>
		/// <returns></returns>
		public static EProperty SpecToFocus(string specKey)
		{
			if (!m_specToFocus.TryGetValue(specKey, out EProperty res))
			{
				//if (log.IsWarnEnabled)
				//log.Warn("No skill property found for spec " + specKey);
				return EProperty.Undefined;
			}

			return res;
		}

		private static Func<ISpecActionHandler> GetNewSpecActionHandlerConstructor(Type type)
		{
			ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			return Expression.Lambda<Func<ISpecActionHandler>>(Expression.New(constructor, null), null).Compile();
		}

		private static Func<IAbilityActionHandler> GetNewAbilityActionHandlerConstructor(Type type)
		{
			ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			return Expression.Lambda<Func<IAbilityActionHandler>>(Expression.New(constructor, null), null).Compile();
		}

		private static Ability GetNewAbilityInstance(string keyname, int level)
		{
			Ability ab = null;
			DbAbility dba = null;
			m_syncLockUpdates.EnterReadLock();
			try
			{
				if (m_abilityIndex.ContainsKey(keyname))
				{
					dba = m_abilityIndex[keyname];
				}
			}
			finally
			{
				m_syncLockUpdates.ExitReadLock();
			}

			if (dba != null)
			{
				ab = GetNewAbilityInstance(dba);
				ab.Level = level;
			}

			return ab;
		}

		private static Ability GetNewAbilityInstance(DbAbility dba)
		{
			// try instanciating ability
			Ability ab = null;

			if (!string.IsNullOrEmpty(dba.Implementation))
			{
				// Try instanciating Ability
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					try
					{
						ab = (Ability)asm.CreateInstance(
							typeName: dba.Implementation, // string including namespace of the type
							ignoreCase: true,
							bindingAttr: BindingFlags.Default,
							binder: null,
							args: new object[] { dba, 0 },
							culture: null,
							activationAttributes: null);

						// instanciation worked
						if (ab != null)
						{
							break;
						}
					}
					catch
					{
					}
				}

				if (ab == null)
				{
					// Something Went Wrong when instanciating
					ab = new Ability(dba, 0);

					if (log.IsWarnEnabled)
						log.WarnFormat("Could not Instanciate Ability {0} from {1} reverting to default Ability...", dba.KeyName, dba.Implementation);
				}
			}
			else
			{
				ab = new Ability(dba, 0);
			}

			return ab;
		}

		private static Specialization GetNewSpecializationInstance(string keyname, Tuple<Type, string, ushort, int> entry)
		{
			Specialization gameSpec;

			try
			{
				gameSpec = (Specialization)entry.Item1.Assembly.CreateInstance(
						typeName: entry.Item1.FullName, // string including namespace of the type
						ignoreCase: true,
						bindingAttr: BindingFlags.Default,
						binder: null,
						args: new object[] { keyname, entry.Item2, entry.Item3, entry.Item4 },
						culture: null,
						activationAttributes: null);

					// instanciation worked
					if (gameSpec != null)
					{
						return gameSpec;
					}
			}
			catch
			{
			}

			return GetNewSpecializationInstance(keyname, entry.Item1.FullName, entry.Item2, entry.Item3, entry.Item4);
		}

		private static Specialization GetNewSpecializationInstance(string keyname, string type, string name, ushort icon, int id)
		{
			Specialization gameSpec = null;
			// Try instanciating Specialization
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					gameSpec = (Specialization)asm.CreateInstance(
						typeName: type, // string including namespace of the type
						ignoreCase: true,
						bindingAttr: BindingFlags.Default,
						binder: null,
						args: new object[] { keyname, name, icon, id },
						culture: null,
						activationAttributes: null);

					// instanciation worked
					if (gameSpec != null)
					{
						break;
					}
				}
				catch
				{
				}
			}

			if (gameSpec == null)
			{
				// Something Went Wrong when instanciating
				gameSpec = new Specialization(keyname, name, icon, id);

				if (log.IsErrorEnabled)
					log.ErrorFormat("Could not Instanciate Specialization {0} from {1} reverting to default Specialization...", keyname, type);
			}

			return gameSpec;
		}
	}
}
