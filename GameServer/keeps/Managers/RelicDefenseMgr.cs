using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.Events;
using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class RelicDefenseMgr
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private class PlayerContribution
        {
            public long DamageDealt = 0;
            public long HealingDone = 0;
            public long DamageTaken = 0;
            public int Kills = 0;
            public int CCsPerformed = 0;

            // Gewichtung der Rollen
            public double GetTotalScore() =>
                (DamageDealt * 0.12) +       // Damage Dealer
                (HealingDone * 0.25) +        // Heiler (höher gewichtet, da seltener)
                (DamageTaken * 0.18) +        // Tanks
                (CCsPerformed * 200.0) +      // CCer (Mezzer/Stunner)
                (Kills * 450.0);              // Todesstöße
        }

        private class SiegeState
        {
            public DateTime LastCombatAction = DateTime.Now;
            public ConcurrentDictionary<GamePlayer, PlayerContribution> Contributions = new ConcurrentDictionary<GamePlayer, PlayerContribution>();
            public int EnemyDeaths = 0;
        }

        private static readonly ConcurrentDictionary<uint, SiegeState> _activeSieges = new ConcurrentDictionary<uint, SiegeState>();
        private const int DEFENSE_RADIUS = 6000;
        private const int MAX_RP_CAP = 2500;
        private const int MIN_KILLS = 16; // 16 enemies have to die at least to get defend tick

        public static bool Init()
        {
            try
            {
                GameEventMgr.AddHandler(GameLivingEvent.HealthChanged, new DOLEventHandler(OnHealthChanged));
                GameEventMgr.AddHandler(GameLivingEvent.CastSucceeded, new DOLEventHandler(OnCastSucceeded));
                GameEventMgr.AddHandler(GameLivingEvent.EnemyKilled, new DOLEventHandler(OnEnemyKilled));

                // Log-Ausgabe für die Konsole beim Serverstart
                if (log.IsInfoEnabled)
                    log.Info("RelicDefenseMgr erfolgreich initialisiert.");

                return true;
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error("Fehler beim Initialisieren des RelicDefenseMgr", ex);

                return false;
            }
        }

        private static void OnHealthChanged(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not HealthChangedEventArgs hArgs || sender is not GameLiving victim) return;
            // Check if action happens in relic defend radius
            var relic = FindNearbyRelic(victim);
            if (relic == null) return;

            var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());
            state.LastCombatAction = DateTime.Now;

            // --- TANK LOGIK ---
            // Loosing health
            if (victim is GamePlayer defender && defender.Realm == relic.Realm && hArgs.ChangeAmount < 0)
            {
                var c = state.Contributions.GetOrAdd(defender, new PlayerContribution());
                Interlocked.Add(ref c.DamageTaken, Math.Abs(hArgs.ChangeAmount));
            }

            // --- Damage & Supporter LOGIK ---
            if (hArgs.ChangeSource is GamePlayer source && source.Realm == relic.Realm)
            {
                var c = state.Contributions.GetOrAdd(source, new PlayerContribution());

                if (hArgs.ChangeAmount < 0 && victim.Realm != source.Realm)
                {
                    // Damage on Enemies
                    Interlocked.Add(ref c.DamageDealt, Math.Abs(hArgs.ChangeAmount));
                }
                else if (hArgs.ChangeAmount > 0 && victim.Realm == source.Realm)
                {
                    // Healing on own realm players
                    Interlocked.Add(ref c.HealingDone, hArgs.ChangeAmount);
                }
            }
        }

        private static void OnCastSucceeded(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer caster) return;
            // Check if action happens in relic defend radius
            var relic = FindNearbyRelic(caster);
            if (relic == null) return;

            // --- CC LOGIK ---
            // Check if cast was on enemy
            if (caster.TargetObject is GameLiving target && target.Realm != caster.Realm)
            {
                var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());
                var c = state.Contributions.GetOrAdd(caster, new PlayerContribution());

                // Count cast on enemy as CC
                Interlocked.Increment(ref c.CCsPerformed);
            }
        }

        private static void OnEnemyKilled(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not EnemyKilledEventArgs ekArgs || ekArgs.Target is not GameLiving victim) return;
            // Check if action happens in relic defend radius
            var relic = FindNearbyRelic(victim);
            if (relic == null) return;
            log.Info("RelicDefenseMgr EnemyKilled & Relik");
            var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());

            // When enemy dies compared to relic realm
            if (victim.Realm != relic.Realm)
            {
                Interlocked.Increment(ref state.EnemyDeaths);

                if (sender is GamePlayer killer && killer.Realm == relic.Realm)
                {
                    var c = state.Contributions.GetOrAdd(killer, new PlayerContribution());
                    Interlocked.Increment(ref c.Kills);
                }
            }
        }

        // Checks if relic is in defend_radius
        private static GameRelic FindNearbyRelic(GameLiving obj)
        {
            if (obj == null || obj.ObjectState != GameObject.eObjectState.Active)
                return null;

            List<GameStaticItem> items = obj.GetItemsInRadius((ushort)DEFENSE_RADIUS);

            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is GameRelic relic)
                    {
                        return relic;
                    }
                }
            }

            return null;
        }

        // Handout defend participation
        public static void DistributeRewards(uint relicId)
        {
            if (_activeSieges.TryRemove(relicId, out var state))
            {
                // Minimum kills of enemies
                if (state.EnemyDeaths < MIN_KILLS) return;

                foreach (var entry in state.Contributions)
                {
                    GamePlayer p = entry.Key;
                    if (p != null && p.Client != null && p.ObjectState == GameObject.eObjectState.Active)
                    {
                        int rp = (int)(entry.Value.GetTotalScore() / 10);
                        if (rp > 5)
                        {
                            p.GainRealmPoints(Math.Min(rp, MAX_RP_CAP)); // Cap RP
                            p.Out.SendMessage($"You received {rp} for defending the relic temple.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        }
                    }
                }
            }
        }
    }
}