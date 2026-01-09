using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.Logging;

namespace DOL.GS
{
    public class RelicMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Relics are not expected to be modified after initialization.
        private static readonly Dictionary<int, GameRelic> _relicsMap = [];
        private static volatile GameRelic[] _relicsArray = [];

        // Relic pads are loaded concurrently via GameRelicPad.AddToWorld.
        private static readonly List<GameRelicPad> _relicPads = [];
        private static readonly Lock _relicPadsLock = new();
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

        public static bool Init()
        {
            lock (_relicPadsLock)
            {
                foreach (GameRelic relic in _relicsMap.Values)
                {
                    relic.SaveIntoDatabase();
                    relic.RemoveFromWorld();
                }

                _relicsMap.Clear();
                _relicsArray = [];

                foreach (GameRelicPad pad in _relicPads)
                    pad.RemoveRelics();

                List<GameRelic> lostRelics = [];
                IList<DbRelic> dbRelics = GameServer.Database.SelectAllObjects<DbRelic>();

                foreach (DbRelic dbRelic in dbRelics)
                {
                    if (dbRelic.relicType < 0 || dbRelic.relicType > 1 || dbRelic.OriginalRealm < 1 || dbRelic.OriginalRealm > 3)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Could not load {dbRelic.RelicID}: Realm or Type mismatch.");

                        continue;
                    }

                    if (WorldMgr.GetRegion((ushort)dbRelic.Region) == null)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Could not load {dbRelic.RelicID}: Region mismatch.");

                        continue;
                    }

                    GameRelic relic = new(dbRelic);
                    _relicsMap[dbRelic.RelicID] = relic;
                    relic.AddToWorld();
                    GameRelicPad pad = null;

                    foreach (GameRelicPad relicPad in _relicPads)
                    {
                        if (relic.IsWithinRadius(relicPad, 200))
                            pad = relicPad;
                    }

                    if (pad != null)
                    {
                        if (relic.RelicType == pad.PadType)
                        {
                            relic.RelicPadTakesOver(pad, true);

                            if (log.IsDebugEnabled)
                                log.Debug($"{relic.Name} has been loaded and added to pad {pad.Name}.");
                        }
                    }
                    else
                        lostRelics.Add(relic);
                }

                foreach (GameRelic lostRelic in lostRelics)
                {
                    eRealm returnRealm = lostRelic.LastRealm;

                    if (returnRealm is eRealm.None)
                        returnRealm = lostRelic.OriginalRealm;

                    foreach (GameRelicPad pad in _relicPads)
                    {
                        if (pad.Realm == returnRealm && pad.PadType == lostRelic.RelicType && lostRelic.RelicPadTakesOver(pad, true))
                        {
                            if (log.IsDebugEnabled)
                                log.Debug($"Lost relic '{lostRelic.Name}' has returned to last pad '{pad.Name}'");
                        }
                    }
                }

                foreach (GameRelic lostRelic in lostRelics)
                {
                    if (lostRelic.CurrentRelicPad == null)
                    {
                        foreach (GameRelicPad pad in _relicPads)
                        {
                            if (pad.PadType == lostRelic.RelicType && lostRelic.RelicPadTakesOver(pad, true))
                            {
                                if (log.IsDebugEnabled)
                                    log.Debug($"Lost relic '{lostRelic.Name}' auto assigned to pad '{pad.Name}'");
                            }
                        }
                    }
                }

                _relicsArray = [.. _relicsMap.Values];

                if (log.IsDebugEnabled)
                {
                    log.Debug($"{_relicPads.Count} relic pad{(_relicPads.Count > 1 ? "s were" : " was")} loaded.");
                    log.Debug($"{_relicsMap.Count} relic{(_relicsMap.Count > 1 ? "s were" : " was")} loaded.");
                }
            }
            
            // Timer starten, nachdem Init abgeschlossen ist
            StartRelicCheckTimer(); 

            return true;
        }

        public static void AddRelicPad(GameRelicPad pad)
        {
            lock (_relicPadsLock)
            {
                if (!_relicPads.Contains(pad))
                    _relicPads.Add(pad);
            }
        }

        public static void RemoveRelicPad(GameRelicPad pad)
        {
            lock (_relicPadsLock)
            {
                _relicPads.Remove(pad);
            }
        }

        public static GameRelic GetRelic(int id)
        {
            return _relicsMap.TryGetValue(id, out GameRelic relic) ? relic : null;
        }

        public static int GetRelicCount(eRealm realm)
        {
            int count = 0;
            GameRelic[] snapshot = _relicsArray;

            for (int i = 0; i < snapshot.Length; i++)
            {
                GameRelic relic = snapshot[i];

                if (relic.Realm == realm && relic.IsMounted)
                    count++;
            }

            return count;
        }

        public static int GetRelicCount(eRealm realm, eRelicType type)
        {
            int count = 0;
            GameRelic[] snapshot = _relicsArray;

            for (int i = 0; i < snapshot.Length; i++)
            {
                GameRelic relic = snapshot[i];

                if (relic.Realm == realm && relic.RelicType == type && relic.IsMounted)
                    count++;
            }

            return count;
        }

        public static GameRelic[] GetRelics()
        {
            return _relicsArray;
        }

        public static double GetRelicBonusModifier(GameLiving living, eRelicType type)
        {
            double modifier = 1.0;

            if (!living.BenefitsFromRelics)
                return modifier;

            bool owningSelf = false;
            eRealm realm = living.Realm;
            GameRelic[] snapshot = _relicsArray;

            for (int i = 0; i < snapshot.Length; i++)
            {
                GameRelic relic = snapshot[i];

                if (relic.Realm == realm && relic.RelicType == type && relic.IsMounted)
                {
                    if (relic.Realm == relic.OriginalRealm)
                        owningSelf = true;
                    else
                        modifier += ServerProperties.Properties.RELIC_OWNING_BONUS * 0.01;
                }
            }

            // Bonus applies only if owning original relic.
            return owningSelf ? modifier : 1.0;
        }

        public static bool CanPickupRelicFromShrine(GamePlayer player, GameRelic relic)
        {
            if (player == null || relic == null)
                return false;

            // A player can always pick up their own realm's original relic.
            if (player.Realm == relic.OriginalRealm)
                return true;

            eRealm playerRealm = player.Realm;
            eRelicType type = relic.RelicType;
            GameRelic[] snapshot = _relicsArray;

            // Ensure the player's realm possesses its original relic of the same type.
            for (int i = 0; i < snapshot.Length; i++)
            {
                GameRelic otherRelic = snapshot[i];

                if (otherRelic.Realm == playerRealm &&
                    otherRelic.OriginalRealm == playerRealm &&
                    otherRelic.RelicType == type &&
                    otherRelic.IsMounted)
                {
                    return true;
                }
            }

            return false;
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

            // Iteration über die RelicsMap, da diese die aktiven GameRelic-Objekte enthält.
            foreach (GameRelic relic in _relicsMap.Values) // KORRIGIERT: _relicsMap.Values für die Iteration
            {
                // lastTimePickedUp > DateTime.MinValue bedeutet: Das Relikt wurde einmal aufgenommen und ist nicht montiert.
                if (relic.LastTimePickedUp > DateTime.MinValue)
                {
                    // KORRIGIERT: LastCaptureDate ersetzt durch LastTimePickedUp
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

        /// <summary>
        /// Gibt das ursprüngliche RelicPad für ein gegebenes Reich und einen gegebenen Typ zurück.
        /// </summary>
        public static GameRelicPad GetHomePad(eRealm realm, eRelicType relicType)
        {
            // KORRIGIERT: _relicsLock ersetzt durch _relicPadsLock
            lock (_relicPadsLock)
            {
                // KORRIGIERT: m_relicPads ersetzt durch _relicPads
                foreach (GameRelicPad pad in _relicPads) 
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

        [ScriptLoadedEvent]
        private static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            Init();
        }
    }
}