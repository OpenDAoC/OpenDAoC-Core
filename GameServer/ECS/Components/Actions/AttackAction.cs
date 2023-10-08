﻿using System;
using System.Linq;
using DOL.Database;
using DOL.GS.Styles;

namespace DOL.GS
{
    public abstract class AttackAction
    {
        // Next tick interval for when the current tick doesn't result in an attack.
        protected const int TICK_INTERVAL_FOR_NON_ATTACK = 100;

        protected AttackComponent _attackComponent;
        protected AttackData _lastAttackData;
        protected DbInventoryItem _weapon;
        protected DbInventoryItem _leftWeapon;
        protected Style _combatStyle;
        protected StyleComponent _styleComponent;
        protected GameObject _target;
        protected double _effectiveness;
        protected int _ticksToTarget;
        protected int _interruptDuration;
        protected int _interval;
        private GameLiving _owner;
        private long _startTime;

        // Set to current time when a round doesn't result in an attack. Used to prevent combat log spam and kept until reset in AttackComponent.SendAttackingCombatMessages().
        public long RoundWithNoAttackTime { get; set; }

        public long StartTime
        {
            get => _startTime;
            set => _startTime = value + GameLoop.GameLoopTime;
        }

        protected AttackAction(GameLiving owner)
        {
            _owner = owner;
            _attackComponent = _owner.attackComponent;
            _styleComponent = _owner.styleComponent;
        }

        public static AttackAction Create(GameLiving gameLiving)
        {
            if (gameLiving is GameNPC gameNpc)
                return new NpcAttackAction(gameNpc);
            else if (gameLiving is GamePlayer gamePlayer)
                return new PlayerAttackAction(gamePlayer);

            return null;
        }

        public void Tick(long time)
        {
            if (time <= StartTime)
                return;

            if (!CheckAttackState())
                return;

            if (!CanPerformAction())
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return;
            }

            _weapon = _owner.ActiveWeapon;
            _leftWeapon = _owner.Inventory?.GetItem(EInventorySlot.LeftHandWeapon);
            _effectiveness = _owner.Effectiveness;

            if (_owner.ActiveWeaponSlot != EActiveWeaponSlot.Distance)
            {
                _target = _owner.TargetObject;

                if (PrepareMeleeAttack())
                {
                    PerformMeleeAttack();
                    FinalizeMeleeAttack();
                }
            }
            else
            {
                _target = _owner.rangeAttackComponent.AutoFireTarget ?? _owner.TargetObject;

                if (PrepareRangedAttack())
                {
                    PerformRangedAttack();
                    FinalizeRangedAttack();
                }
            }

            StartTime = _interval;
        }

        public virtual bool CheckInterruptTimer()
        {
            return false;
        }

        public virtual void OnAimInterrupt(GameObject attacker) { }

        protected virtual bool CheckAttackState()
        {
            _lastAttackData = _owner.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);

            if (!_attackComponent.AttackState || _owner.ObjectState != GameObject.eObjectState.Active)
            {
                _owner.TempProperties.RemoveProperty(GameLiving.LAST_ATTACK_DATA);
                _attackComponent.attackAction.CleanUp();
                return false;
            }

            return true;
        }

        protected virtual bool CanPerformAction()
        {
            if (_owner.IsMezzed || _owner.IsStunned || _owner.IsEngaging)
                return false;

            if (_owner.CurrentSpellHandler?.Spell.Uninterruptible == false)
                return false;

            return true;
        }

        protected virtual bool PrepareMeleeAttack()
        {
            bool clearOldStyles = false;

            if (_lastAttackData != null)
            {
                switch (_lastAttackData.AttackResult)
                {
                    case EAttackResult.Fumbled:
                    {
                        // Skip this attack if the last one fumbled.
                        _styleComponent.NextCombatStyle = null;
                        _styleComponent.NextCombatBackupStyle = null;
                        _lastAttackData.AttackResult = EAttackResult.Missed;
                        _interval = _attackComponent.AttackSpeed(_weapon) * 2;
                        return false;
                    }
                    case EAttackResult.OutOfRange:
                    case EAttackResult.TargetNotVisible:
                    case EAttackResult.NotAllowed_ServerRules:
                    case EAttackResult.TargetDead:
                    {
                        clearOldStyles = true;
                        break;
                    }
                }
            }

            if (_combatStyle != null && _combatStyle.WeaponTypeRequirement == (int) EObjectType.Shield)
                _weapon = _leftWeapon;

            int mainHandAttackSpeed = _attackComponent.AttackSpeed(_weapon);

            if (clearOldStyles || _styleComponent.NextCombatStyleTime + mainHandAttackSpeed < GameLoop.GameLoopTime)
            {
                // Cancel the styles if they were registered too long ago.
                // Nature's Shield stays active forever and falls back to a non-backup style.
                if (_styleComponent.NextCombatBackupStyle?.ID == 394)
                    _styleComponent.NextCombatStyle = _styleComponent.NextCombatBackupStyle;
                else if (_styleComponent.NextCombatStyle?.ID != 394)
                    _styleComponent.NextCombatStyle = null;

                _styleComponent.NextCombatBackupStyle = null;
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

            _interruptDuration = mainHandAttackSpeed;
            return true;
        }

        protected virtual bool PrepareRangedAttack()
        {
            ECheckRangeAttackStateResult rangeCheckresult = _owner.rangeAttackComponent.CheckRangeAttackState(_target);

            if (rangeCheckresult == ECheckRangeAttackStateResult.Hold)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }
            else if (rangeCheckresult == ECheckRangeAttackStateResult.Stop || _target == null)
            {
                _attackComponent.StopAttack();
                _attackComponent.attackAction?.CleanUp();
                return false;
            }

            _interval = _attackComponent.AttackSpeed(_weapon);
            _interruptDuration = _interval;
            _ticksToTarget = _owner.GetDistanceTo(_target) * 1000 / RangeAttackComponent.PROJECTILE_FLIGHT_SPEED;
            int model = _weapon == null ? 0 : _weapon.Model;
            byte flightDuration = (byte)(_ticksToTarget > 350 ? 1 + (_ticksToTarget - 350) / 75 : 1);
            bool cancelPrepareAnimation = _owner.ActiveWeapon.Object_Type == (int)EObjectType.Thrown;

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
                case ERangedAttackType.Critical:
                {
                    double tmpEffectiveness = 2 - 0.3 * _owner.GetConLevel(_target);

                    if (tmpEffectiveness > 2)
                        _effectiveness *= 2;
                    else if (tmpEffectiveness < 1.1)
                        _effectiveness *= 1.1;
                    else
                        _effectiveness *= tmpEffectiveness;

                    break;
                }

                case ERangedAttackType.SureShot:
                {
                    _effectiveness *= 0.5;
                    break;
                }

                case ERangedAttackType.RapidFire:
                {
                    // Source : http://www.camelotherald.com/more/888.shtml
                    // - (About Rapid Fire) If you release the shot 75% through the normal timer, the shot (if it hits) does 75% of its normal damage. If you
                    // release 50% through the timer, you do 50% of the damage, and so forth - The faster the shot, the less damage it does.

                    // Source : http://www.camelotherald.com/more/901.shtml
                    // Related note about Rapid Fire interrupts are determined by the speed of the bow is fired, meaning that the time of interruptions for each shot will be scaled
                    // down proportionally to bow speed. If that made your eyes bleed, here's an example from someone who would know: "I fire a 5.0 spd bow. Because I am buffed and have
                    // stat bonuses, I fire that bow at 3.0 seconds. The resulting interrupt on the caster will last 3.0 seconds. If I rapid fire that same bow, I will fire at 1.5 seconds,
                    // and the resulting interrupt will last 1.5 seconds."

                    long rapidFireMaxDuration = _attackComponent.AttackSpeed(_weapon);
                    long elapsedTime = GameLoop.GameLoopTime - _owner.TempProperties.GetProperty<long>(RangeAttackComponent.RANGED_ATTACK_START); // elapsed time before ready to fire

                    if (elapsedTime < rapidFireMaxDuration)
                    {
                        _effectiveness *= 0.25 + elapsedTime * 0.5 / rapidFireMaxDuration;
                        _interruptDuration = (int)(_interruptDuration * _effectiveness);
                    }

                    break;
                }
            }

            // Calculate Penetrating Arrow damage reduction.
            if (_target is GameLiving livingTarget)
            {
                int PALevel = _owner.GetAbilityLevel(Abilities.PenetratingArrow);

                if ((PALevel > 0) && (_owner.rangeAttackComponent.RangedAttackType != ERangedAttackType.Long))
                {
                    EcsGameSpellEffect bladeturn = livingTarget.effectListComponent.GetSpellEffects(EEffect.Bladeturn)?.FirstOrDefault();

                    if (bladeturn != null && _target != bladeturn.SpellHandler.Caster)
                        _effectiveness *= 0.25 + PALevel * 0.25;
                }
            }

            return true;
        }

        protected virtual void PerformMeleeAttack()
        {
            _attackComponent.weaponAction = new WeaponAction(_owner, _target, _weapon, _leftWeapon, _effectiveness, _interruptDuration, _combatStyle);
            _attackComponent.weaponAction.Execute();
            _lastAttackData = _owner.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);
        }

        protected virtual void PerformRangedAttack()
        {
            _attackComponent.weaponAction = new WeaponAction(_owner, _target, _weapon, _effectiveness, _interruptDuration, _owner.rangeAttackComponent.RangedAttackType);

            if (_owner.rangeAttackComponent.RangedAttackType == ERangedAttackType.Critical)
                _owner.rangeAttackComponent.RangedAttackType = ERangedAttackType.Normal;

            // A positive ticksToTarget means the effects of our attack will be delayed. Typically used for ranged attacks.
            if (_ticksToTarget > 0)
            {
                new EcsGameTimer(_owner, new EcsGameTimer.EcsTimerCallback(_attackComponent.weaponAction.Execute), _ticksToTarget);

                // This is done in weaponAction.Execute(), but we musn't wait for the attack to reach our target.
                _attackComponent.weaponAction.AttackFinished = true;
            }
            else
                _attackComponent.weaponAction.Execute();

            _lastAttackData = _owner.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);
        }

        protected virtual bool FinalizeMeleeAttack()
        {
            // Melee weapons tick every TICK_INTERVAL_FOR_NON_ATTACK if they didn't attack.
            if (_lastAttackData != null &&
                _lastAttackData.AttackResult is not EAttackResult.Missed
                and not EAttackResult.HitUnstyled
                and not EAttackResult.HitStyle
                and not EAttackResult.Evaded
                and not EAttackResult.Blocked
                and not EAttackResult.Parried)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;

                if (RoundWithNoAttackTime == 0)
                    RoundWithNoAttackTime = GameLoop.GameLoopTime;

                return false;
            }
            else
            {
                _interval = _attackComponent.AttackSpeed(_weapon, _leftWeapon);
                _styleComponent.NextCombatStyle = null;
                _styleComponent.NextCombatBackupStyle = null;
                return true;
            }
        }

        protected virtual bool FinalizeRangedAttack()
        {
            if (CheckInterruptTimer())
                return false;

            // Need to find a way to not have to call 'AttackSpeed()' again (currently needed
            _interval = _attackComponent.AttackSpeed(_weapon);
            _owner.rangeAttackComponent.RangedAttackState = ERangedAttackState.Aim;

            if (_owner.rangeAttackComponent.RangedAttackType != ERangedAttackType.Long)
            {
                _owner.rangeAttackComponent.RangedAttackType = ERangedAttackType.Normal;

                if (EffectListService.GetAbilityEffectOnTarget(_owner, EEffect.SureShot) != null)
                    _owner.rangeAttackComponent.RangedAttackType = ERangedAttackType.SureShot;
                if (EffectListService.GetAbilityEffectOnTarget(_owner, EEffect.RapidFire) != null)
                {
                    _owner.rangeAttackComponent.RangedAttackType = ERangedAttackType.RapidFire;
                    _interval = Math.Max(1500, _interval /= 2);
                }
                if (EffectListService.GetAbilityEffectOnTarget(_owner, EEffect.TrueShot) != null)
                    _owner.rangeAttackComponent.RangedAttackType = ERangedAttackType.Long;
            }

            // The 'stance' parameter appears to be used to tell whether or not the animation should be held, and doesn't seem to be related to the weapon speed.
            foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(_owner, null, (ushort)(_weapon != null ? _weapon.Model : 0), 0x00, player.Out.BowPrepare, 0x1A, 0x00, 0x00);

            return true;
        }

        public virtual void CleanUp()
        {
            _owner.attackComponent.attackAction = null;
        }
    }
}
