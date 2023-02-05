using System;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Styles;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public abstract class AttackAction
    {
        // Next tick interval for when the current tick doesn't result in an attack.
        protected const int TICK_INTERVAL_FOR_NON_ATTACK = 100;

        protected AttackComponent _attackComponent;
        protected AttackData _attackData;
        protected InventoryItem _weapon;
        protected InventoryItem _leftWeapon;
        protected Style _combatStyle;
        protected StyleComponent _styleComponent;
        protected GameObject _target;
        protected double _effectiveness;
        protected int _rangeBonus;
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
            _leftWeapon = _owner.Inventory?.GetItem(eInventorySlot.LeftHandWeapon);
            _effectiveness = _owner.Effectiveness;

            if (_owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
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
                // Must be done here because RangeAttackTarget is changed in CheckRangeAttackState.
                _target = _owner.rangeAttackComponent.Target;

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
            if (_owner.ObjectState != eObjectState.Active)
            {
                _attackComponent.attackAction?.CleanUp();
                return false;
            }

            _attackData = _owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;

            if (!_attackComponent.AttackState)
            {
                _owner.TempProperties.removeProperty(LAST_ATTACK_DATA);

                if (_attackData?.Target != null)
                    _attackData.Target.attackComponent.RemoveAttacker(_owner);

                _attackComponent.attackAction.CleanUp();
                return false;
            }

            return true;
        }

        protected virtual bool CanPerformAction()
        {
            if (_owner.IsMezzed || _owner.IsStunned)
                return false;

            if (_owner.IsCasting && !_owner.CurrentSpellHandler.Spell.Uninterruptible)
                return false;

            if (_owner.IsEngaging || _owner.TargetObject == null)
                return false;

            return true;
        }

        protected virtual bool PrepareMeleeAttack()
        {
            if (_attackData != null && _attackData.AttackResult is eAttackResult.Fumbled)
            {
                // Skip this attack if the last one fumbled.
                _styleComponent.NextCombatStyle = null;
                _styleComponent.NextCombatBackupStyle = null;
                _attackData.AttackResult = eAttackResult.Missed;
                _interval = _attackComponent.AttackSpeed(_weapon) * 2;
                StartTime = _interval;
                return false;
            }

            if (_combatStyle != null && _combatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                _weapon = _leftWeapon;

            int mainHandAttackSpeed = _attackComponent.AttackSpeed(_weapon);

            if (GameLoop.GameLoopTime > _styleComponent.NextCombatStyleTime + mainHandAttackSpeed)
            {
                // Cancel the styles if they were registered too long ago.
                // Nature's Shield stays active forever and falls back to a non-backup style.
                if (_styleComponent.NextCombatBackupStyle?.ID == 394)
                    _styleComponent.NextCombatStyle = _styleComponent.NextCombatBackupStyle;
                else if (_styleComponent.NextCombatStyle?.ID != 394)
                    _styleComponent.NextCombatStyle = null;

                _styleComponent.NextCombatBackupStyle = null;
            }

            // Damage is doubled on sitting players, but only with melee weapons; arrows and magic does normal damage.
            if (_target is GamePlayer playerTarget && playerTarget.IsSitting)
                _effectiveness *= 2;

            Spell proc = _combatStyle?.Procs?.FirstOrDefault()?.Item1;

            if (proc != null)
                _rangeBonus = proc.SpellType == (byte)eSpellType.StyleRange ? (int)proc.Value - _attackComponent.AttackRange : 0;

            _interruptDuration = mainHandAttackSpeed;

            return true;
        }

        protected virtual bool PrepareRangedAttack()
        {
            eCheckRangeAttackStateResult rangeCheckresult = _owner.rangeAttackComponent.CheckRangeAttackState(_target);

            if (rangeCheckresult == eCheckRangeAttackStateResult.Hold)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }
            else if (rangeCheckresult == eCheckRangeAttackStateResult.Stop || _target == null)
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
            bool cancelPrepareAnimation = _owner.ActiveWeapon.Object_Type == (int)eObjectType.Thrown;

            Parallel.ForEach(_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
            {
                if (player == null)
                    return;

                // Special case for thrown weapons (bows and crossbows don't need this).
                // For some obscure reason, their 'BowShoot' animation doesn't cancel their 'BowPrepare', and 'BowPrepare' resumes after 'BowShoot'.
                if (cancelPrepareAnimation)
                    player.Out.SendInterruptAnimation(_owner);

                // The 'stance' parameter appears to be used to indicate the time it should take for the arrow's model to reach its target.
                // 0 doesn't display any arrow.
                // 1 means roughly 350ms (the lowest time possible), then each increment adds about 75ms (needs testing).
                // Using ticksToTarget, we can make the arrow take more time to reach its target the farther it is.
                player.Out.SendCombatAnimation(_owner, _target, (ushort)model, 0x00, player.Out.BowShoot, flightDuration, 0x00, ((GameLiving)_target).HealthPercent);
            });            

            switch (_owner.rangeAttackComponent.RangedAttackType)
            {
                case eRangedAttackType.Critical:
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

                    long rapidFireMaxDuration = _attackComponent.AttackSpeed(_weapon);
                    long elapsedTime = GameLoop.GameLoopTime - _owner.TempProperties.getProperty<long>(RangeAttackComponent.RANGED_ATTACK_START); // elapsed time before ready to fire

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

                if ((PALevel > 0) && (_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
                {
                    ECSGameSpellEffect bladeturn = livingTarget.effectListComponent.GetSpellEffects(eEffect.Bladeturn)?.FirstOrDefault();

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

            _attackData = _owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
        }

        protected virtual void PerformRangedAttack()
        {
            _attackComponent.weaponAction = new WeaponAction(_owner, _target, _weapon, null, _effectiveness, _interruptDuration, null);

            // Order is important. 'WeaponAction()' creates a snapshot of 'RangedAttackType' that will be used for damage calculation.
            // We then reset it for the next interval calculation.
            if (_owner.rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
                _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

            // A positive ticksToTarget means the effects of our attack will be delayed. Typically used for ranged attacks.
            if (_ticksToTarget > 0)
            {
                new ECSGameTimer(_owner, new ECSGameTimer.ECSTimerCallback(_attackComponent.weaponAction.Execute), _ticksToTarget);

                // This is done in weaponAction.Execute(), but we musn't wait for the attack to reach our target.
                _attackComponent.weaponAction.AttackFinished = true;
            }
            else
                _attackComponent.weaponAction.Execute();

            _attackData = _owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
        }

        protected virtual bool FinalizeMeleeAttack()
        {
            // Melee weapons tick every TICK_INTERVAL_FOR_NON_ATTACK if they didn't attack.
            if (_attackData != null &&
                _attackData.AttackResult is not eAttackResult.Missed
                and not eAttackResult.HitUnstyled
                and not eAttackResult.HitStyle
                and not eAttackResult.Evaded
                and not eAttackResult.Blocked
                and not eAttackResult.Parried)
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
            _owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;

            if (_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long)
            {
                _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

                if (EffectListService.GetAbilityEffectOnTarget(_owner, eEffect.SureShot) != null)
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                if (EffectListService.GetAbilityEffectOnTarget(_owner, eEffect.RapidFire) != null)
                {
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                    _interval = Math.Max(1500, _interval /= 2);
                }
                if (EffectListService.GetAbilityEffectOnTarget(_owner, eEffect.TrueShot) != null)
                    _owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;                            
            }

            Parallel.ForEach(_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
            {
                if (player == null)
                    return;

                // The 'stance' parameter appears to be used to tell whether or not the animation should be held, and doesn't seem to be related to the weapon speed.
                player.Out.SendCombatAnimation(_owner, null, (ushort)(_weapon != null ? _weapon.Model : 0), 0x00, player.Out.BowPrepare, 0x1A, 0x00, 0x00);
            });

            return true;
        }

        public virtual void CleanUp()
        {
            _owner.attackComponent.attackAction = null;
        }
    }
}
