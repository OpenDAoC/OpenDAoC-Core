using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.Logging;
using DOL.GS.PacketHandler;

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
        /// Maximum Time until relic returns to last pad
        /// </summary>
        public static TimeSpan MaxRelicCarryTime
        {
            get { return TimeSpan.FromMinutes(ServerProperties.Properties.RELIC_RETURN_TIME); }
        }

        public static bool Init()
        {
            TempleRelicPadsLoader.LoadTemplePads();
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
                        if (log.IsDebugEnabled)
                            log.Debug($"{relic.Name} has been loaded is lost somewhere.");
                }

                // We dont want relics to return to last pad on server restart
                // This is not even working, relics are invisible, so would need a fix
                /*foreach (GameRelic lostRelic in lostRelics)
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
                }*/

                _relicsArray = [.. _relicsMap.Values];

                if (log.IsDebugEnabled)
                {
                    log.Debug($"{_relicPads.Count} relic pad{(_relicPads.Count > 1 ? "s were" : " was")} loaded.");
                    log.Debug($"{_relicsMap.Count} relic{(_relicsMap.Count > 1 ? "s were" : " was")} loaded.");
                }
            }

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
            // Dont wanne use that
            /*for (int i = 0; i < snapshot.Length; i++)
            {
                GameRelic otherRelic = snapshot[i];

                if (otherRelic.Realm == playerRealm &&
                    otherRelic.OriginalRealm == playerRealm &&
                    otherRelic.RelicType == type &&
                    otherRelic.IsMounted)
                {
                    return true;
                }
            }*/

            return true; // Always true, cause players should always be able to pickup
        }

        // --- 120-MINUTEN LOGIC ---

        /// <summary>
        /// Starts return timer
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
        /// Check if relic should return to last pad
        /// </summary>
        private static void CheckForLostRelics(object state)
        {
            if (log.IsDebugEnabled)
                log.Debug("Checking relics for exceeding max carry time.");

            TimeSpan maxTime = MaxRelicCarryTime;

            foreach (GameRelic relic in _relicsMap.Values)
            {
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

        /// <summary>
        /// Returns Homepad of relic
        /// </summary>
        public static GameRelicPad GetHomePad(eRealm realm, eRelicType relicType)
        {
            lock (_relicPadsLock)
            {
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
        /// Calculates time left for relic return.
        /// </summary>
        /// <param name="relic"></param>
        /// <returns></returns>
        public static int GetRelicReturnMinutesRemaining(GameRelic relic)
        {
            if (relic.IsMounted || relic.LastTimePickedUp <= DateTime.MinValue)
            {
                return -1;
            }

            TimeSpan maxCarryTime = RelicMgr.MaxRelicCarryTime;
            TimeSpan timeElapsed = DateTime.Now.Subtract(relic.LastTimePickedUp);
            TimeSpan timeRemaining = maxCarryTime.Subtract(timeElapsed);

            if (timeRemaining <= TimeSpan.Zero)
            {
                return 0;
            }
            else
            {
                return (int)Math.Ceiling(timeRemaining.TotalMinutes);
            }
        }

        /// <summary>
        /// Return Pad Name of relic
        /// </summary>
        public static GameRelicPad GetPadForRelic(GameRelic relic)
        {
            if (relic == null) return null;

            lock (_relicPadsLock)
            {
                foreach (GameRelicPad pad in _relicPads)
                {
                    if (pad == null) continue;

                    if (pad.MountedRelics != null && pad.MountedRelics.Contains(relic))
                        return pad;
                    if (relic.IsWithinRadius(pad, 200))
                        return pad;
                }
            }
            return null;
        }

        [ScriptLoadedEvent]
        private static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            Init();
        }
    }
}