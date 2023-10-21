using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.Database;
using Core.Events;
using log4net;

namespace Core.GS
{
	public sealed class RelicMgr
	{
		/// <summary>
		/// table of all relics, id as key
		/// </summary>
		private static readonly Hashtable m_relics = new Hashtable();


		/// <summary>
		/// list of all relicPads
		/// </summary>
		private static readonly ArrayList m_relicPads = new ArrayList();


		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// load all relics from DB
		/// </summary>
		/// <returns></returns>
		public static bool Init()
		{
			lock (m_relics.SyncRoot)
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
					ERealm returnRealm = (ERealm)lostRelic.LastRealm;

					if (returnRealm == ERealm.None)
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
		/// <param name="pad"></param>
		public static void AddRelicPad(GameRelicPad pad)
		{
			lock (m_relicPads.SyncRoot)
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

			lock (m_relicPads.SyncRoot)
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
		/// <param name="id">id of relic</param>
		/// <returns> Relic object with relicid = id</returns>
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
		/// <param name="Realm"></param>
		/// <returns></returns>
		public static IEnumerable getRelics(ERealm Realm)
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
		/// <param name="Realm"></param>
		/// <param name="RelicType"></param>
		/// <returns></returns>
		public static IEnumerable getRelics(ERealm Realm, ERelicType RelicType)
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
		/// <param name="realm"></param>
		/// <returns></returns>
		public static int GetRelicCount(ERealm realm)
		{
			int index = 0;
			lock (m_relics.SyncRoot)
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
		/// <param name="realm"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int GetRelicCount(ERealm realm, ERelicType type)
		{
			int index = 0;
			lock (m_relics.SyncRoot)
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
		/// <param name="realm"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static double GetRelicBonusModifier(ERealm realm, ERelicType type)
		{
			double bonus = 0.0;
			bool owningSelf = false;
			//only playerrealms can get bonus
			foreach (GameRelic rel in getRelics(realm, type))
			{
				if (rel.Realm == rel.OriginalRealm)
				{
					owningSelf = true;
				}
				else
				{
					var cache = bonus;
					switch (GetDaysSinceCapture(rel))
					{
						case <1:
							bonus += ServerProperties.Properties.RELIC_OWNING_BONUS*0.01 * 2;
							break;
						case <3:
							bonus += ServerProperties.Properties.RELIC_OWNING_BONUS*0.01 * 1.5;
							break;
						case < 7:
							bonus += ServerProperties.Properties.RELIC_OWNING_BONUS * 0.01;
							break;
						default:
							bonus += ServerProperties.Properties.RELIC_OWNING_BONUS*0.01 * 0.5;
							break;
					}
				}
			}

			// Bonus apply only if owning original relic
			if (owningSelf)
				return bonus;
			
			return 0.0;
		}

		/// <summary>
		/// Returns if a player is allowed to pick up a mounted relic (depends if they own their own relic of the same type)
		/// </summary>
		/// <param name="player"></param>
		/// <param name="relic"></param>
		/// <returns></returns>
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
		/// <returns></returns>
		public static Hashtable GetAllRelics()
		{
			lock (m_relics.SyncRoot)
			{
				return (Hashtable)m_relics.Clone();
			}
		}
		#endregion


		[ScriptLoadedEvent]
		private static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			Init();
		}
	}
}
