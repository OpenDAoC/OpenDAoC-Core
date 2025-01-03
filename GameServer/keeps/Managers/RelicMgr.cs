using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// RelicManager
	/// The manager that keeps track of the relics.
	/// </summary>
	public sealed class RelicMgr
	{
		/// <summary>
		/// table of all relics, id as key
		/// </summary>
		private static readonly Hashtable m_relics = new Hashtable();
		private static readonly Lock _relicsLock = new Lock();

        /// <summary>
        /// list of all relicPads
        /// </summary>
        private static readonly ArrayList m_relicPads = new ArrayList();
		private static readonly Lock _relicPadsLock = new Lock();

        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// load all relics from DB
		/// </summary>
		public static bool Init()
		{
			lock (_relicsLock)
			{
				//at first remove all relics
				foreach (GameRelic rel in m_relics.Values)
				{
					rel.SaveIntoDatabase();
					rel.RemoveFromWorld();
				}

				//then clear the hashtable
				m_relics.Clear();

				//then we remove all relics from the pads
				foreach (GameRelicPad pad in m_relicPads)
				{
					pad.RemoveRelic();
				}

				// if relics are on the ground during init we will return them to their owners
				List<GameRelic> lostRelics = new List<GameRelic>();

				var relics = GameServer.Database.SelectAllObjects<DbRelic>();
				foreach (DbRelic datarelic in relics)
				{
					if (datarelic.relicType < 0 || datarelic.relicType > 1
						|| datarelic.OriginalRealm < 1 || datarelic.OriginalRealm > 3)
					{
						log.Warn("DBRelic: Could not load " + datarelic.RelicID + ": Realm or Type missmatch.");
						continue;
					}

					if (WorldMgr.GetRegion((ushort)datarelic.Region) == null)
					{
						log.Warn("DBRelic: Could not load " + datarelic.RelicID + ": Region missmatch.");
						continue;
					}
					GameRelic relic = new GameRelic(datarelic);
					m_relics.Add(datarelic.RelicID, relic);

					relic.AddToWorld();
					GameRelicPad pad = GetPadAtRelicLocation(relic);
					if (pad != null)
					{
						if (relic.RelicType == pad.PadType)
						{
							relic.RelicPadTakesOver(pad, true);
							log.Debug("DBRelic: " + relic.Name + " has been loaded and added to pad " + pad.Name + ".");
						}
					}
					else
					{
						lostRelics.Add(relic);
					}
				}

				foreach (GameRelic lostRelic in lostRelics)
				{
					eRealm returnRealm = (eRealm)lostRelic.LastRealm;

					if (returnRealm == eRealm.None)
					{
						returnRealm = lostRelic.OriginalRealm;
					}

					// ok, now we have a realm to return the relic too, lets find a pad

					foreach (GameRelicPad pad in m_relicPads)
					{
						if (pad.MountedRelic == null && pad.Realm == returnRealm && pad.PadType == lostRelic.RelicType)
						{
							lostRelic.RelicPadTakesOver(pad, true);
							log.Debug("Lost Relic: " + lostRelic.Name + " has returned to last pad: " + pad.Name + ".");
						}
					}
				}

				// Final cleanup.  If any relic is still unmounted then mount the damn thing to any empty pad

				foreach (GameRelic lostRelic in lostRelics)
				{
					if (lostRelic.CurrentRelicPad == null)
					{
						foreach (GameRelicPad pad in m_relicPads)
						{
							if (pad.MountedRelic == null && pad.PadType == lostRelic.RelicType)
							{
								lostRelic.RelicPadTakesOver(pad, true);
								log.Debug("Lost Relic: " + lostRelic.Name + " auto assigned to pad: " + pad.Name + ".");
							}
						}
					}
				}
			}

			log.Debug(m_relicPads.Count + " relicpads" + ((m_relicPads.Count > 1) ? "s were" : " was") + " loaded.");
			log.Debug(m_relics.Count + " relic" + ((m_relics.Count > 1) ? "s were" : " was") + " loaded.");
			return true;
		}
		
		public static int GetDaysSinceCapture(GameRelic relic)
		{
			TimeSpan daysPassed = DateTime.Now.Subtract(relic.LastCaptureDate);
			return daysPassed.Days;
		}

		/// <summary>
		/// This is called when the GameRelicPads are added to world
		/// </summary>
		public static void AddRelicPad(GameRelicPad pad)
		{
			lock (_relicPadsLock)
			{
				if (!m_relicPads.Contains(pad))
					m_relicPads.Add(pad);
			}
		}

		/// <summary>
		/// This is called on during the loading. It looks for relicpads and where it could be stored.
		/// </summary>
		/// <returns>null if no GameRelicPad was found at the relic's position.</returns>
		private static GameRelicPad GetPadAtRelicLocation(GameRelic relic)
		{
			lock (_relicPadsLock)
			{
				foreach (GameRelicPad pad in m_relicPads)
				{
					if (relic.IsWithinRadius(pad, 200))
						//if (pad.X == relic.X && pad.Y == relic.Y && pad.Z == relic.Z && pad.CurrentRegionID == relic.CurrentRegionID)
						return pad;
				}
				return null;
			}
		}

		/// <summary>
		/// get relic by ID
		/// </summary>
		public static GameRelic getRelic(int id)
		{
			return m_relics[id] as GameRelic;
		}

		#region Helpers

		public static IList getNFRelics()
		{
			ArrayList myRelics = new ArrayList();
			foreach (GameRelic relic in m_relics.Values)
			{
				myRelics.Add(relic);
			}
			return myRelics;
		}

		/// <summary>
		/// Returns an enumeration with all mounted Relics of an realm
		/// </summary>
		public static IEnumerable getRelics(eRealm Realm)
		{
			ArrayList realmRelics = new ArrayList();
			lock (m_relics)
			{
				foreach (GameRelic relic in m_relics.Values)
				{
					if (relic.Realm == Realm && relic.IsMounted)
						realmRelics.Add(relic);
				}
			}
			return realmRelics;
		}

		/// <summary>
		/// Returns an enumeration with all mounted Relics of an realm by a specified RelicType
		/// </summary>
		public static IEnumerable getRelics(eRealm Realm, eRelicType RelicType)
		{
			ArrayList realmTypeRelics = new ArrayList();
			foreach (GameRelic relic in getRelics(Realm))
			{
				if (relic.RelicType == RelicType)
					realmTypeRelics.Add(relic);
			}
			return realmTypeRelics;
		}

		/// <summary>
		/// get relic count by realm
		/// </summary>
		public static int GetRelicCount(eRealm realm)
		{
			int index = 0;
			lock (_relicsLock)
			{
				foreach (GameRelic relic in m_relics.Values)
				{
					if ((relic.Realm == realm) && (relic is GameRelic))
						index++;
				}
			}
			return index;
		}

		/// <summary>
		/// get relic count by realm and relictype
		/// </summary>
		public static int GetRelicCount(eRealm realm, eRelicType type)
		{
			int index = 0;
			lock (_relicsLock)
			{
				foreach (GameRelic relic in m_relics.Values)
				{
					if ((relic.Realm == realm) && (relic.RelicType == type) && (relic is GameRelic))
						index++;
				}
			}
			return index;
		}

		/// <summary>
		/// Gets the bonus modifier for a realm/relictype.
		/// </summary>
		public static double GetRelicBonusModifier(eRealm realm, eRelicType type)
		{
			double bonus = 0.0;
			bool owningSelf = false;

			//only playerrealms can get bonus
			foreach (GameRelic rel in getRelics(realm, type))
			{
				if (rel.Realm == rel.OriginalRealm)
					owningSelf = true;
				else
					bonus += ServerProperties.Properties.RELIC_OWNING_BONUS * 0.01;
			}

			// Bonus apply only if owning original relic
			return owningSelf ? bonus : 0.0;
		}

		/// <summary>
		/// Returns if a player is allowed to pick up a mounted relic (depends if they own their own relic of the same type)
		/// </summary>
		public static bool CanPickupRelicFromShrine(GamePlayer player, GameRelic relic)
		{
			//debug: if (player == null || relic == null) return false;
			//their own relics can always be picked up.
			if (player.Realm == relic.OriginalRealm)
				return true;
			IEnumerable list = getRelics(player.Realm, relic.RelicType);
			foreach (GameRelic curRelic in list)
			{
				if (curRelic.Realm == curRelic.OriginalRealm)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Gets a copy of the current relics table, keyvalue is the relicId
		/// </summary>
		public static Hashtable GetAllRelics()
		{
			lock (_relicsLock)
			{
				return (Hashtable)m_relics.Clone();
			}
		}

		#endregion

		[ScriptLoadedEvent]
		private static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			Init();
		}
	}
}
