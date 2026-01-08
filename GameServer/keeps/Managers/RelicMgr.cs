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

                    if (WorldMgr.GetRegion((ushort) dbRelic.Region) == null)
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

            return true;
        }

        public static int GetDaysSinceCapture(GameRelic relic)
        {
            TimeSpan daysPassed = DateTime.Now.Subtract(relic.LastCaptureDate);
            return daysPassed.Days;
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

        [ScriptLoadedEvent]
        private static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            Init();
        }
    }
}
