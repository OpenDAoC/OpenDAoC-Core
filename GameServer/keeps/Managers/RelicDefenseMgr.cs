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
using DOL.GS.Spells;
using DOL.AI.Brain;

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

            // RPs for Casts in fights
            // Calculated like this: DmgAmount * value / 10 (=> 1x CC Spells * 75 / 10 = 7.5rps)
            public double GetTotalScore() =>
                (DamageDealt * 0.667) +    // Damage Dealer
                (HealingDone * 0.8) +      // Healers
                (DamageTaken * 0.5) +      // Tanks
                (CCsPerformed * 75.0) +    // CC
                (Kills * 500.0);           // Kills
        }

        private class SiegeState
        {
            public DateTime LastCombatAction = DateTime.Now;
            public ConcurrentDictionary<GamePlayer, PlayerContribution> Contributions = new ConcurrentDictionary<GamePlayer, PlayerContribution>();
            public int EnemyDeaths = 0;
            public ECSGameTimer PendingHandout = null;
        }

        private static readonly ConcurrentDictionary<uint, SiegeState> _activeSieges = new ConcurrentDictionary<uint, SiegeState>();
        private const int DEFENSE_RADIUS = 6000;
        private const int MAX_RP_CAP = 2500;
        private const int MIN_RP = 250;
        private const int MIN_KILLS = 16; // 16 enemies have to die at least to get defend tick
        private const int NO_FIGHT_TIME_MIN = 5; // how long we wait until we had handout rp ticks

        public static bool Init()
        {
            try
            {
                GameEventMgr.AddHandler(GameLivingEvent.TakeDamage, new DOLEventHandler(OnTakeDamage));
                GameEventMgr.AddHandler(GameLivingEvent.CastFinished, new DOLEventHandler(OnCastFinished));
                GameEventMgr.AddHandler(GameLivingEvent.EnemyKilled, new DOLEventHandler(OnEnemyKilled));

                if (log.IsInfoEnabled)
                    log.Info("RelicDefenseMgr successfully initialized.");

                return true;
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error("Error starting RelicDefenseManager", ex);

                return false;
            }
        }

        // This is called when players gets damage
        private static void OnTakeDamage(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not TakeDamageEventArgs tArgs || sender is not GameLiving victim) return;

            var relic = FindNearbyRelic(victim);
            if (relic == null) return;

            var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());

            // Care about pet damage
            GamePlayer attacker = null;
            if (tArgs.DamageSource is GamePlayer p) attacker = p;
            else if (tArgs.DamageSource is ISpellHandler spell) attacker = spell.Caster as GamePlayer;
            else if (tArgs.DamageSource is GameNPC npc && npc.Brain is IControlledBrain brain) attacker = brain.Owner as GamePlayer;

            // We care about tanks
            if (victim is GamePlayer defender && defender.Realm == relic.Realm)
            {
                var c = state.Contributions.GetOrAdd(defender, new PlayerContribution());
                Interlocked.Add(ref c.DamageTaken, tArgs.DamageAmount);
            }

            // Defender does damage, casting dd or melee
            if (attacker != null && attacker.Realm == relic.Realm && victim.Realm != attacker.Realm)
            {
                var c = state.Contributions.GetOrAdd(attacker, new PlayerContribution());
                Interlocked.Add(ref c.DamageDealt, tArgs.DamageAmount);
                // We only start/reset timer if there is action from defenders
                ResetHandoutTimer(relic.ObjectID, state, relic);
            }
        }

        // This gets triggered if players are casting
        private static void OnCastFinished(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not CastingEventArgs cArgs || sender is not GamePlayer caster) return;

            ISpellHandler handler = cArgs.SpellHandler;
            if (handler == null || handler.Spell == null) return;

            GameLiving target = cArgs.Target ?? caster.TargetObject as GameLiving;
            if (target == null) return;

            var relic = FindNearbyRelic(target) ?? FindNearbyRelic(caster);
            if (relic == null) return;

            eSpellType type = handler.Spell.SpellType;

            // CC & Debuffs
            // Targets MUST be enemies
            if (target.Realm != caster.Realm)
            {
                // Check Attack Result (Only count hits)
                if (cArgs.LastAttackData != null)
                {
                    var result = (eAttackResult)cArgs.LastAttackData.AttackResult;
                    if (result != eAttackResult.HitUnstyled && result != eAttackResult.HitStyle) return;
                }

                bool isCC = type == eSpellType.Mesmerize || type == eSpellType.Nearsight ||
                            type == eSpellType.Stun || type == eSpellType.Confusion ||
                            type == eSpellType.SpeedDecrease || type == eSpellType.Amnesia ||
                            type == eSpellType.Disease || type.ToString().Contains("Debuff");

                if (isCC)
                {
                    var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());
                    var c = state.Contributions.GetOrAdd(caster, new PlayerContribution());
                    Interlocked.Increment(ref c.CCsPerformed);
                    ResetHandoutTimer(relic.ObjectID, state, relic);
                    return; // Exit here if CC was handled
                }
            }

            // Supporter
            // Targets are realm mates
            bool isHeal = type == eSpellType.CombatHeal || type == eSpellType.Heal ||
                          type == eSpellType.HealOverTime || type == eSpellType.SpreadHeal ||
                          type == eSpellType.PBAoEHeal || type == eSpellType.OmniHeal ||
                          type == eSpellType.Resurrect || type.ToString().Contains("Cure");

            if (isHeal)
            {
                var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());
                var c = state.Contributions.GetOrAdd(caster, new PlayerContribution());

                // IMPORTANT: Add the actual HP amount, not just "1"
                long healAmount = (long)handler.Spell.Value;

                Interlocked.Add(ref c.HealingDone, healAmount);
                ResetHandoutTimer(relic.ObjectID, state, relic);
            }
        }

        private static void OnEnemyKilled(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not EnemyKilledEventArgs ekArgs || ekArgs.Target is not GameLiving victim) return;
            // Check if action happens in relic defend radius
            var relic = FindNearbyRelic(victim);
            if (relic == null) return;
            var state = _activeSieges.GetOrAdd(relic.ObjectID, id => new SiegeState());

            // When enemy dies compared to relic realm
            if (victim.Realm != relic.Realm)
            {
                // Count overall deaths from attackers of relic
                Interlocked.Increment(ref state.EnemyDeaths);

                if (sender is GamePlayer killer && killer.Realm == relic.Realm)
                {
                    var c = state.Contributions.GetOrAdd(killer, new PlayerContribution());
                    // Add a kill to players stats
                    Interlocked.Increment(ref c.Kills);
                    ResetHandoutTimer(relic.ObjectID, state, relic); // Reset handout timer, on action with enemy
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
                        // We only look for relics which are mounted on a temple
                        if (relic.CurrentRelicPad is GameTempleRelicPad)
                        {
                            return relic;
                        }
                    }
                }
            }

            return null;
        }

        private static void ResetHandoutTimer(uint relicId, SiegeState state, GameObject owner)
        {
            log.Info("TIMER START");
            if (state.PendingHandout != null)
            {
                log.Info("TIMER STOP");
                state.PendingHandout.Stop();
                state.PendingHandout = null;
            }

            state.PendingHandout = new ECSGameTimer(owner);
            state.PendingHandout.Callback = (timer) =>
            {
                DistributeRewards(relicId);
                log.Info("HANDOUT TEMPLE");
                return 0; // Stop timer after handout
            };

            state.PendingHandout.Start(NO_FIGHT_TIME_MIN * 60 * 1000);
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
                    if (p != null && p.Client != null && p.ObjectState == GameObject.eObjectState.Active && p.Level >= 40)
                    {
                        int rp = (int)(entry.Value.GetTotalScore() / 10);
                        rp = rp + MIN_RP;
                        p.GainRealmPoints(Math.Min(rp, MAX_RP_CAP), true, false, true);
                        p.Out.SendMessage($"You received {rp} realm points for defending the relic temple.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }
    }
}