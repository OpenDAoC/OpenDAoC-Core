﻿using System;
using System.Linq;
using DOL.Database;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class AttackAction
    {
        // Next tick interval for when the current tick doesn't result in an attack.
        protected const int TICK_INTERVAL_FOR_NON_ATTACK = 100;
        private const int MINIMUM_MELEE_DELAY_AFTER_RANGED_ATTACK = 750;

        protected DbInventoryItem _weapon;
        protected DbInventoryItem _leftWeapon;
        protected Style _combatStyle;
        protected GameObject _target;
        protected double _effectiveness;
        protected int _ticksToTarget;
        protected int _attackInterval;
        protected int _interval;
        private GameLiving _owner;
        private long _nextMeleeTick;
        private long _nextRangedTick;
        private bool _firstTick = true;

        // Set to current time when a round doesn't result in an attack. Used to prevent combat log spam and kept until reset in AttackComponent.SendAttackingCombatMessages().
        public long RoundWithNoAttackTime { get; set; }
        public AttackData LastAttackData { get; set; }
        public long NextTick => _owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance ? _nextMeleeTick : _nextRangedTick;
        protected AttackComponent AttackComponent => _owner.attackComponent;
        protected StyleComponent StyleComponent => _owner.styleComponent;

        protected AttackAction(GameLiving owner)
        {
            _owner = owner;
        }

        public static AttackAction Create(GameLiving living)
        {
            if (living is GameNPC npc)
                return new NpcAttackAction(npc);
            else if (living is GamePlayer player)
                return new PlayerAttackAction(player);
            else
                return new AttackAction(living);
        }

        public bool Tick()
        {
            if (_firstTick)
            {
                _nextMeleeTick = Math.Max(_nextMeleeTick, GameLoop.GameLoopTime);
                _nextRangedTick = Math.Max(_nextRangedTick, GameLoop.GameLoopTime);
                _firstTick = false;
            }

            if (!ShouldTick())
                return true;

            // This must be checked after `ShouldTick` so that the last attack data remains valid for the whole attack interval.
            if (!AttackComponent.AttackState)
            {
                CleanUp();
                return false;
            }

            _weapon = _owner.ActiveWeapon;
            _leftWeapon = _owner.ActiveLeftWeapon;
            _effectiveness = _owner.Effectiveness;

            if (_owner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                TickMeleeAttack();
            else
                TickRangedAttack();

            return true;
        }

        private void TickMeleeAttack()
        {
            _target = _owner.TargetObject;

            if (PrepareMeleeAttack())
            {
                PerformMeleeAttack();
                FinalizeMeleeAttack();
            }

            if (AttackComponent.AttackState)
                _nextMeleeTick += _interval;
        }

        private void TickRangedAttack()
        {
            _target = _owner.rangeAttackComponent.AutoFireTarget ?? _owner.TargetObject;

            if (PrepareRangedAttack())
            {
                PerformRangedAttack();
                FinalizeRangedAttack();
            }

            if (AttackComponent.AttackState)
                _nextRangedTick += _interval;
        }

        public void OnStopAttack()
        {
            // This method serves three purposes:
            // * Ensure we're not buffering (NPCs) or delaying (players) ranged attacks. For this, we need to update `_nextRangedTick` so that it's never higher than `GameLoop.GameLoopTime`.
            // * Delay the next melee attack a little after aborting a ranged attack, or getting interrupted.
            // * Clean up ranged attack type and state.

            _nextRangedTick = GameLoop.GameLoopTime;

            if (_owner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                return;

            RangeAttackComponent rangeAttackComponent = _owner.rangeAttackComponent;

            if (rangeAttackComponent.RangedAttackState is not eRangedAttackState.None)
            {
                long _nextDelayedMeleeTick = GameLoop.GameLoopTime + MINIMUM_MELEE_DELAY_AFTER_RANGED_ATTACK;
                _nextMeleeTick = Math.Max(_nextMeleeTick, _nextDelayedMeleeTick);
            }

            rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
            rangeAttackComponent.RangedAttackState = eRangedAttackState.None;
        }

        public void OnHeadingUpdate()
        {
            if (!AttackComponent.AttackState || _owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                return;

            AttackData ad = LastAttackData;

            if (ad == null || !ad.IsMeleeAttack || (ad.AttackResult is not eAttackResult.TargetNotVisible and not eAttackResult.OutOfRange))
                return;

            if (ad.Target != null && _owner.IsObjectInFront(ad.Target, 120) && _owner.IsWithinRadius(ad.Target, AttackComponent.AttackRange))
                _firstTick = true;
        }

        public virtual bool CheckInterruptTimer()
        {
            if (!_owner.IsBeingInterruptedIgnoreSelfInterrupt)
                return false;

            _owner.attackComponent.StopAttack();
            OnAimInterrupt(_owner.LastInterrupter);
            return true;
        }

        public virtual void OnAimInterrupt(GameObject attacker) { }

        public virtual void OnForcedWeaponSwitch() { }

        public virtual bool OnOutOfRangeOrNoLosRangedAttack()
        {
            return true;
        }

        private bool ShouldTick()
        {
            if (!IsTickDue())
                return false;

            if (!IsAllowedToTick())
            {
                _firstTick = true; // Important to not buffer attacks.
                return false;
            }

            return true;

            bool IsTickDue()
            {
                return _owner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance ? ServiceUtils.ShouldTick(_nextMeleeTick) : ServiceUtils.ShouldTick(_nextRangedTick);
            }

            bool IsAllowedToTick()
            {
                // 1.82 changed the reactionary window to a fixed 3 seconds. This made placing reactionary styles easier against fast attacks,
                // but it's also suspected that this is when it became impossible to spam them when the target is stunned.
                return !_owner.IsCrowdControlled && !_owner.IsEngaging && (_owner.CurrentSpellHandler?.Spell.Uninterruptible) != false;
            }
        }

        protected virtual bool PrepareMeleeAttack()
        {
            if (_combatStyle != null && _combatStyle.WeaponTypeRequirement == (int) eObjectType.Shield)
                _weapon = _leftWeapon;

            bool clearOldStyles = false;

            if (LastAttackData != null)
            {
                switch (LastAttackData.AttackResult)
                {
                    case eAttackResult.OutOfRange:
                    case eAttackResult.TargetNotVisible:
                    {
                        // We allow styles to stay registered for about 250 milliseconds on out of range / not visible attack result.
                        // Live doesn't seem to be very consistent in that regard, but neither are we because of `TICK_INTERVAL_FOR_NON_ATTACK`.
                        clearOldStyles = ServiceUtils.ShouldTick(StyleComponent.NextCombatStyleTime + 250);
                        break;
                    }
                    case eAttackResult.NotAllowed_ServerRules:
                    case eAttackResult.TargetDead:
                    case eAttackResult.NoValidTarget:
                    {
                        clearOldStyles = true;
                        break;
                    }
                }
            }

            if (clearOldStyles)
            {
                // Cancel the styles if they were registered too long ago.
                // Nature's Shield stays active forever and falls back to a non-backup style.
                if (StyleComponent.NextCombatBackupStyle?.Procs.Where(x => x.Spell.SpellType is eSpellType.NaturesShield).FirstOrDefault() != null)
                    StyleComponent.NextCombatStyle = StyleComponent.NextCombatBackupStyle;
                else if (StyleComponent.NextCombatStyle?.Procs.Where(x => x.Spell.SpellType is eSpellType.NaturesShield).FirstOrDefault() == null)
                    StyleComponent.NextCombatStyle = null;

                StyleComponent.NextCombatBackupStyle = null;
            }

            // Styles must be checked before the target.
            if (_target == null)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }

            // Damage is doubled on sitting players, but only with melee weapons; arrows and magic do normal damage.
            if (_target is GamePlayer playerTarget && playerTarget.IsSitting)
                _effectiveness *= 2;

            _attackInterval = AttackComponent.AttackSpeed(_weapon);
            return true;
        }

        protected virtual bool PrepareRangedAttack()
        {
            int attackSpeed = _owner.attackComponent.AttackSpeed(_weapon);

            if (_owner.rangeAttackComponent.RangedAttackState == eRangedAttackState.None)
            {
                _owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;

                if (_owner is not GamePlayer || !_owner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
                {
                    // The 'stance' parameter appears to be used to tell whether or not the animation should be held, and doesn't seem to be related to the weapon speed.
                    foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendCombatAnimation(_owner, null, (ushort) (_weapon != null ? _weapon.Model : 0), 0, player.Out.BowPrepare, 0x1A, 0x00, 0x00);

                    _interval = attackSpeed;
                }

                return false;
            }

            eCheckRangeAttackStateResult rangeCheckResult = _owner.rangeAttackComponent.CheckRangeAttackState(_target);

            if (rangeCheckResult == eCheckRangeAttackStateResult.Hold)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }
            else if (rangeCheckResult == eCheckRangeAttackStateResult.Stop || _target == null)
            {
                AttackComponent.StopAttack();
                return false;
            }

            _interval = attackSpeed;
            _attackInterval = _interval;
            _ticksToTarget = _owner.GetDistanceTo(_target) * 1000 / RangeAttackComponent.PROJECTILE_FLIGHT_SPEED;
            int model = _weapon == null ? 0 : _weapon.Model;
            byte flightDuration = (byte)(_ticksToTarget > 350 ? 1 + (_ticksToTarget - 350) / 75 : 1);
            bool cancelPrepareAnimation = _owner.ActiveWeapon.Object_Type == (int)eObjectType.Thrown;

            foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                // Special case for thrown weapons (bows and crossbows don't need this).
                // For some obscure reason, their 'BowShoot' animation doesn't cancel their 'BowPrepare', and 'BowPrepare' resumes after 'BowShoot'.
                if (cancelPrepareAnimation)
                    player.Out.SendInterruptAnimation(_owner);

                // The 'stance' parameter appears to be used to indicate the time it should take for the arrow's model to reach its target.
                // 0 doesn't display any arrow.
                // 1 means roughly 350ms (the lowest time possible), then each increment adds about 75ms (needs testing).
                // Using ticksToTarget, we can make the arrow take more time to reach its target the farther it is.
                player.Out.SendCombatAnimation(_owner, _target, (ushort)model, 0x00, player.Out.BowShoot, flightDuration, 0x00, ((GameLiving)_target).HealthPercent);
            }

            switch (_owner.rangeAttackComponent.RangedAttackType)
            {
                case eRangedAttackType.Critical:
                {
                    // Reduced effectiveness against higher level targets.
                    double levelModifier = 2 + (_owner.EffectiveLevel - _target.EffectiveLevel) * 0.075;
                    _effectiveness *= Math.Clamp(levelModifier, 1.1, 2.0);
                    break;
                }

                case eRangedAttackType.SureShot:
                {
                    _effectiveness *= 0.5;
                    break;
                }

                case eRangedAttackType.RapidFire:
                {
                    // Source : http://www.camelotherald.com/more/888.shtml
                    // - (About Rapid Fire) If you release the shot 75% through the normal timer, the shot (if it hits) does 75% of its normal damage. If you
                    // release 50% through the timer, you do 50% of the damage, and so forth - The faster the shot, the less damage it does.

                    // Source : http://www.camelotherald.com/more/901.shtml
                    // Related note about Rapid Fire interrupts are determined by the speed of the bow is fired, meaning that the time of interruptions for each shot will be scaled
                    // down proportionally to bow speed. If that made your eyes bleed, here's an example from someone who would know: "I fire a 5.0 spd bow. Because I am buffed and have
                    // stat bonuses, I fire that bow at 3.0 seconds. The resulting interrupt on the caster will last 3.0 seconds. If I rapid fire that same bow, I will fire at 1.5 seconds,
                    // and the resulting interrupt will last 1.5 seconds."

                    // We need the attack speed unmodified by Rapid Fire to calculate the damage effectiveness, so we temporarily change the attack type to normal.
                    // This is dirty, but I believe this is the simplest solution.
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
                    double preRapidFireAttackSpeed = AttackComponent.AttackSpeed(_weapon);
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                    long elapsedTime = GameLoop.GameLoopTime - _owner.rangeAttackComponent.AttackStartTime;

                    if (elapsedTime < preRapidFireAttackSpeed)
                    {
                        _effectiveness *= elapsedTime / preRapidFireAttackSpeed;
                        _attackInterval = (int) (_attackInterval * _effectiveness);
                    }

                    break;
                }
            }

            return true;
        }

        protected virtual void PerformMeleeAttack()
        {
            AttackComponent.weaponAction = new WeaponAction(_owner, _target, _weapon, _leftWeapon, _effectiveness, _attackInterval, _combatStyle);
            AttackComponent.weaponAction.Execute();
        }

        protected virtual void PerformRangedAttack()
        {
            AttackComponent.weaponAction = new WeaponAction(_owner, _target, _weapon, _effectiveness, _attackInterval, _owner.rangeAttackComponent.RangedAttackType, _owner.rangeAttackComponent.Ammo);

            if (_owner.rangeAttackComponent.RangedAttackType is eRangedAttackType.Critical)
                _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

            // A positive ticksToTarget means the effects of our attack will be delayed. Typically used for ranged attacks.
            if (_ticksToTarget > 0)
                new ECSGameTimer(_owner, new ECSGameTimer.ECSTimerCallback(AttackComponent.weaponAction.Execute), _ticksToTarget);
            else
                AttackComponent.weaponAction.Execute();
        }

        protected virtual bool FinalizeMeleeAttack()
        {
            // Melee weapons tick every TICK_INTERVAL_FOR_NON_ATTACK if they didn't attack.
            if (LastAttackData != null)
            {
                switch (LastAttackData.AttackResult)
                {
                    case eAttackResult.OutOfRange:
                    case eAttackResult.TargetNotVisible:
                    case eAttackResult.NotAllowed_ServerRules:
                    case eAttackResult.TargetDead:
                    case eAttackResult.NoValidTarget:
                    {
                        _interval = TICK_INTERVAL_FOR_NON_ATTACK;

                        if (RoundWithNoAttackTime == 0)
                            RoundWithNoAttackTime = GameLoop.GameLoopTime;

                        return false;
                    }
                    case eAttackResult.Fumbled:
                    {
                        StyleComponent.NextCombatStyle = null;
                        StyleComponent.NextCombatBackupStyle = null;
                        _interval = AttackComponent.AttackSpeed(_weapon, _leftWeapon) * 2;
                        return true;
                    }
                }
            }

            _interval = AttackComponent.AttackSpeed(_weapon, _leftWeapon);
            return true;
        }

        protected virtual bool FinalizeRangedAttack()
        {
            if (CheckInterruptTimer())
                return false;

            _owner.rangeAttackComponent.AttackStartTime = GameLoop.GameLoopTime;
            _owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;

            if (_owner.rangeAttackComponent.RangedAttackType is eRangedAttackType.Long)
                (EffectListService.GetEffectOnTarget(_owner, eEffect.TrueShot) as TrueShotECSGameEffect)?.Cancel(true);
            else
            {
                _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

                if (_owner.effectListComponent.ContainsEffectForEffectType(eEffect.SureShot))
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                else if (_owner.effectListComponent.ContainsEffectForEffectType(eEffect.RapidFire))
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                else if (_owner.effectListComponent.ContainsEffectForEffectType(eEffect.TrueShot))
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;
            }

            // Must be done after changing `RangedAttackType`.
            _interval = AttackComponent.AttackSpeed(_weapon);

            // The 'stance' parameter appears to be used to tell whether or not the animation should be held, and doesn't seem to be related to the weapon speed.
            foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(_owner, null, (ushort) (_weapon != null ? _weapon.Model : 0), 0x00, player.Out.BowPrepare, 0x1A, 0x00, 0x00);

            return true;
        }

        public virtual void CleanUp()
        {
            LastAttackData = null;
            _target = null;
            _firstTick = true;
        }
    }
}
