using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties; // Hinzugefügt, um auf Properties zugreifen zu können

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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // --- NEUE 120-MINUTEN LOGIK START ---
        
        // Timer für die automatische Überprüfung verlorener Relikte (alle 5 Minuten)
        private static Timer _relicCheckTimer;
        // Intervall in Millisekunden (121 Minuten check, to be sure timer is already over)
        private const int RELIC_CHECK_INTERVAL = 121 * 60 * 1000; 

        /// <summary>
        /// Gibt die maximal erlaubte Trage-/Bodenzeit für ein Relikt zurück (aus ServerProperties).
        /// </summary>
        public static TimeSpan MaxRelicCarryTime
        {
            // Geht davon aus, dass RELIC_RETURN_TIME in ServerProperties die Dauer in Minuten enthält.
            get { return TimeSpan.FromMinutes(ServerProperties.Properties.RELIC_RETURN_TIME); } 
        }

        // --- NEUE 120-MINUTEN LOGIK ENDE ---

        /// <summary>
        /// load all relics from DB
        /// </summary>
        public static bool Init()
        {
            // ... (Initialisierungs-Code bleibt unverändert) ...
            TempleRelicPadsLoader.LoadTemplePads();

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
                    pad.RemoveRelics();
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

                    foreach (GameRelicPad pad in m_relicPads)
                    {
                        if (pad.Realm == returnRealm && pad.PadType == lostRelic.RelicType && lostRelic.RelicPadTakesOver(pad, true))
                        {
                            if (log.IsDebugEnabled)
                                log.Debug($"Lost relic '{lostRelic.Name}' has returned to last pad '{pad.Name}'");
                        }
                    }
                }

                // Final cleanup.  If any relic is still unmounted then mount the damn thing to any empty pad

                foreach (GameRelic lostRelic in lostRelics)
                {
                    if (lostRelic.CurrentRelicPad == null)
                    {
                        foreach (GameRelicPad pad in m_relicPads)
                        {
                            if (pad.PadType == lostRelic.RelicType && lostRelic.RelicPadTakesOver(pad, true))
                            {
                                if (log.IsDebugEnabled)
                                    log.Debug($"Lost relic '{lostRelic.Name}' auto assigned to pad '{pad.Name}'");
                            }
                        }
                    }
                }
            }

            log.Debug(m_relicPads.Count + " relicpads" + ((m_relicPads.Count > 1) ? "s were" : " was") + " loaded.");
            log.Debug(m_relics.Count + " relic" + ((m_relics.Count > 1) ? "s were" : " was") + " loaded.");
            
            // Startet den Timer für die Rückkehr nach maximaler Zeit
            StartRelicCheckTimer();
            
            return true;
        }
        
        // --- 120-MINUTEN LOGIK START ---

        /// <summary>
        /// Startet den Timer, der alle Relikte auf maximale Trage-/Bodenzeit überprüft.
        /// </summary>
        private static void StartRelicCheckTimer()
        {
            if (_relicCheckTimer == null)
            {
                _relicCheckTimer = new Timer(CheckForLostRelics, null, RELIC_CHECK_INTERVAL, RELIC_CHECK_INTERVAL);
                log.Info($"Relic check timer started. Checks every {RELIC_CHECK_INTERVAL / 1000} seconds.");
            }
        }

        /// <summary>
        /// Überprüft, ob ein Relikt die maximale Trage-/Bodenzeit überschritten hat.
        /// </summary>
        private static void CheckForLostRelics(object state)
        {
            if (log.IsDebugEnabled)
                log.Debug("Checking relics for exceeding max carry time.");

            // Die Überprüfung der DB-Property findet hier statt, um den aktuellen Wert zu nutzen.
            TimeSpan maxTime = MaxRelicCarryTime; 

            lock (_relicsLock)
            {
                foreach (GameRelic relic in m_relics.Values)
                {
                    // lastTimePickedUp > DateTime.MinValue bedeutet: Das Relikt wurde einmal aufgenommen und ist nicht montiert.
                    if (relic.LastTimePickedUp > DateTime.MinValue)
                    {
                        TimeSpan carryDuration = DateTime.Now.Subtract(relic.LastTimePickedUp);

                        if (carryDuration > maxTime)
                        {
                            string carrierName = relic.CurrentCarrier?.Name ?? "Nobody (On Ground)";
                            log.Warn($"Relic '{relic.Name}' carried by {carrierName} for too long ({carryDuration.TotalMinutes:F1} minutes). Returning it automatically.");
                            
                            relic.ReturnToSourcePad(); 
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gibt das ursprüngliche RelicPad für ein gegebenes Reich und einen gegebenen Typ zurück.
        /// </summary>
        public static GameRelicPad GetHomePad(eRealm realm, eRelicType relicType)
        {
            lock (_relicPadsLock)
            {
                foreach (GameRelicPad pad in m_relicPads)
                {
                    if (pad.Realm == realm && pad.PadType == relicType)
                    {
                        return pad;
                    }
                }
            }
            log.Error($"Could not find home pad for realm {realm} and type {relicType}.");
            return null;
        }


        /// <summary>
		/// Berechnet die verbleibende Zeit bis zur automatischen Rückkehr des Relikts in vollen Minuten.
		/// </summary>
		/// <param name="relic">Das zu prüfende Relikt.</param>
		/// <returns>Die Anzahl der verbleibenden Minuten (int). Gibt 0 zurück, wenn die Zeit abgelaufen ist.</returns>
		public static int GetRelicReturnMinutesRemaining(GameRelic relic)
		{
			// 1. Prüfen, ob das Relikt überhaupt "unterwegs" ist (nicht montiert)
			if (relic.IsMounted || relic.LastTimePickedUp <= DateTime.MinValue)
			{
				return -1; // -1 als Indikator, dass das Relikt montiert ist oder ungültigen Status hat.
			}
			
			// 2. Maximale Tragezeit aus der RelicMgr-Eigenschaft abrufen
			TimeSpan maxCarryTime = RelicMgr.MaxRelicCarryTime;
			
			// 3. Vergangene Zeit seit der letzten Aufnahme berechnen
			TimeSpan timeElapsed = DateTime.Now.Subtract(relic.LastTimePickedUp); 
			
			// 4. Verbleibende Zeit berechnen
			TimeSpan timeRemaining = maxCarryTime.Subtract(timeElapsed);
			
			// 5. Ergebnis in Minuten umwandeln und behandeln
			if (timeRemaining <= TimeSpan.Zero)
			{
				return 0; // Zeit ist abgelaufen oder fast abgelaufen
			}
			else
			{
				// TotalMinutes gibt double zurück. Wir runden auf die nächste volle Minute auf (Ceiling), 
				// um immer die korrekte, volle Minute anzuzeigen.
				return (int)Math.Ceiling(timeRemaining.TotalMinutes);
			}
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

        // ... (GetPadAtRelicLocation, getRelic, Helper-Methoden bleiben unverändert) ...
        private static GameRelicPad GetPadAtRelicLocation(GameRelic relic)
        {
            lock (_relicPadsLock)
            {
                foreach (GameRelicPad pad in m_relicPads)
                {
                    if (relic.IsWithinRadius(pad, 200))
                        return pad;
                }
                return null;
            }
        }

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

        public static double GetRelicBonusModifier(eRealm realm, eRelicType type)
        {
            double bonus = 0.0;
            bool owningSelf = false;

            foreach (GameRelic rel in getRelics(realm, type))
            {
                if (rel.Realm == rel.OriginalRealm)
                    owningSelf = true;
                else
                    bonus += ServerProperties.Properties.RELIC_OWNING_BONUS * 0.01;
            }

            return owningSelf ? bonus : 0.0;
        }

        public static bool CanPickupRelicFromShrine(GamePlayer player, GameRelic relic)
        {
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