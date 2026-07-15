using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction, ILosCheckListener
    {
        private const double TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH = 500; // NPCs will switch to ranged if further than melee range + (this * maxSpeed * 0.001).

        private readonly GameNPC _npcOwner;
        private CheckLosTimer _checkLosTimer;
        private bool _hasLos;
        private GameObject _losCheckTarget;
        private bool _wasMeleeWeaponSwitchForced; // Used to prevent NPCs from switching to their ranged weapon automatically if they explicitly switched to a melee weapon during combat.

        private static int LosCheckInterval => Properties.CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL;
        private bool IsArcherGuardOrImmobile => _npcOwner is GuardArcher || _npcOwner.MaxSpeedBase == 0;

        public NpcAttackAction(GameNPC owner) : base(owner)
        {
            _npcOwner = owner;
        }

        public override void OnAimInterrupt(GameObject attacker)
        {
            // Use the follow target or current target (maybe redundant) instead of the interrupter.
            // We really don't want guards to move because a pet attacked them in melee.
            GameObject target = _npcOwner.TargetObject ?? _npcOwner.FollowTarget;

            if (target is not GameLiving livingFollowTarget)
                return;

            if (!_npcOwner.IsAllowedToFollow(livingFollowTarget))
            {
                _npcOwner.StopFollowing();
                return;
            }

            SwitchToMeleeAndTick();
        }

        public override void OnForcedWeaponSwitch()
        {
            switch (_npcOwner.ActiveWeaponSlot)
            {
                case eActiveWeaponSlot.Standard:
                case eActiveWeaponSlot.TwoHanded:
                {
                    _wasMeleeWeaponSwitchForced = true;
                    break;
                }
                case eActiveWeaponSlot.Distance:
                {
                    _wasMeleeWeaponSwitchForced = false;
                    break;
                }
            }
        }

        protected override bool PrepareMeleeAttack()
        {
            // Check spells before attacking to allow spell casting opportunity.
            // The NPC service's think cycles are not synchronized with attack cycles,
            // so without this, melee-attacking NPCs cannot reliably cast spells.
            if (_npcOwner.Brain is NecromancerPetBrain necroBrain)
            {
                if (necroBrain.CheckSpellQueue())
                    return false;
            }
            else if (_npcOwner.Brain is StandardMobBrain brain)
            {
                if (brain.CheckSpells(StandardMobBrain.eCheckSpellType.Offensive))
                {
                    _npcOwner.StopAttack();
                    return false;
                }
            }

            if (!_npcOwner.IsAttacking)
                return false;

            int meleeAttackRange = _npcOwner.MeleeAttackRange;
            int maxSpeed = _npcOwner.MaxSpeed;

            if (maxSpeed > 0)
                meleeAttackRange += (int) (TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH * maxSpeed * 0.001);

            // NPCs try to switch to their ranged weapon whenever possible.
            if (!_npcOwner.IsBeingInterrupted &&
                _npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                !_npcOwner.IsWithinRadius(_target, meleeAttackRange) &&
                !_wasMeleeWeaponSwitchForced)
            {
                bool timerInactive = _checkLosTimer == null || !_checkLosTimer.IsAlive;
                bool targetChanged = _losCheckTarget != _target;

                if (!timerInactive && targetChanged)
                {
                    _hasLos = false;
                    _checkLosTimer.ChangeTarget(_target);
                }

                if (timerInactive || targetChanged || _hasLos)
                {
                    SwitchToRangedAndTick();
                    return false;
                }
            }

            _combatStyle = StyleComponent.GetStyleToUse();

            if (!base.PrepareMeleeAttack())
                return false;

            // The target isn't in melee range yet. Check if another target is in range to attack on the way to the main target.
            if (!_npcOwner.IsWithinRadius(_target, meleeAttackRange) &&
                _npcOwner.Brain is not IControlledBrain &&
                _npcOwner.Brain is StandardMobBrain npcBrain)
            {
                GameLiving lastHighestThreatInAttackRange = npcBrain.LastHighestThreatInAttackRange;

                if (lastHighestThreatInAttackRange != null)
                    _target = lastHighestThreatInAttackRange;

                if (_target == null || !_npcOwner.IsWithinRadius(_target, meleeAttackRange))
                {
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
            }

            return true;
        }

        protected override bool PrepareRangedAttack()
        {
            if (_checkLosTimer == null)
                _checkLosTimer = new(_npcOwner, this, _target);
            else if (_losCheckTarget != _target)
            {
                _hasLos = false;
                _checkLosTimer.ChangeTarget(_target);
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }

            bool isAiming = _npcOwner.rangeAttackComponent.RangedAttackState is not eRangedAttackState.None;
            bool shouldCheckLos = !isAiming || Properties.CHECK_LOS_DURING_NPC_RANGED_ATTACK;

            if (shouldCheckLos && !_hasLos)
            {
                if (isAiming && _losCheckTarget == _target)
                    OnOutOfRangeOrNoLosRangedAttack();

                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }

            bool shouldCheckDistance = !isAiming || Properties.CHECK_RANGE_AT_NPC_RANGED_ATTACK_END;

            if (shouldCheckDistance && !_npcOwner.IsWithinRadius(_target, _npcOwner.attackComponent.AttackRange))
            {
                OnOutOfRangeOrNoLosRangedAttack();
                return false;
            }

            return base.PrepareRangedAttack();
        }

        protected override bool FinalizeRangedAttack()
        {
            bool lostLos = !_hasLos && _losCheckTarget == _target;

            // If we've lost LoS against our current target, or if we're out of attack range.
            if (lostLos || !_npcOwner.IsWithinRadius(_target, _npcOwner.attackComponent.AttackRange))
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;

                // Keep RangedAttackState as Aim for mobile NPCs so StopAttack applies the melee switch delay and resets state.
                if (IsArcherGuardOrImmobile)
                    _npcOwner.rangeAttackComponent.RangedAttackState = eRangedAttackState.None;

                OnOutOfRangeOrNoLosRangedAttack();
                return false;
            }

            return base.FinalizeRangedAttack();
        }

        public override void CleanUp()
        {
            if (_npcOwner.Brain is NecromancerPetBrain necroBrain)
                necroBrain.CheckSpellQueue();

            if (_checkLosTimer != null)
            {
                _checkLosTimer.Stop();
                _checkLosTimer = null;
            }

            _hasLos = false;
            _losCheckTarget = null;
            _wasMeleeWeaponSwitchForced = false;
            base.CleanUp();
        }

        public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
        {
            _losCheckTarget = _npcOwner.CurrentRegion.GetObject(targetId);

            // The target may have changed. Don't act on an obsolete check.
            if (_losCheckTarget == null || _losCheckTarget != _target)
            {
                _hasLos = false;
                return;
            }

            _hasLos = response is LosCheckResponse.True;

            // Only react immediately if we aren't currently waiting for a bow draw completion.
            if (!_hasLos && _npcOwner.rangeAttackComponent.RangedAttackState is eRangedAttackState.None)
                OnOutOfRangeOrNoLosRangedAttack();
        }

        private void SwitchToMeleeAndTick()
        {
            if (_npcOwner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                return;

            _npcOwner.StartAttackWithMeleeWeapon(_target);
        }

        private void SwitchToRangedAndTick()
        {
            if (_npcOwner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                return;

            _npcOwner.StartAttackWithRangedWeapon(_target);
        }

        private void ForceLos()
        {
            _hasLos = true;
            _npcOwner.TurnTo(_losCheckTarget);
        }

        private void OnOutOfRangeOrNoLosRangedAttack()
        {
            // If we're a guard or an immobile NPC, let's forget about our target so that we can attack another one and not stare at the wall.
            // Otherwise, switch to melee, but keep the timer alive.

            if (IsArcherGuardOrImmobile)
            {
                StandardMobBrain brain = _npcOwner.Brain as StandardMobBrain;

                if (_losCheckTarget is GameLiving livingLosCheckTarget)
                    brain.RemoveFromAggroList(livingLosCheckTarget);

                // We could not return here. This would force the NPC to draw its bow again.
                return;
            }

            if (AttackComponent.AttackState)
                _npcOwner.StopAttack();
        }

        private class CheckLosTimer : ECSGameTimerWrapperBase
        {
            private readonly GameNPC _npcOwner;
            private readonly NpcAttackAction _attackAction;
            private GameObject _target;
            private GamePlayer _losChecker;

            public CheckLosTimer(GameObject owner, NpcAttackAction attackAction, GameObject target) : base(owner)
            {
                _npcOwner = owner as GameNPC;
                _attackAction = attackAction;
                ChangeTarget(target);
            }

            public void ChangeTarget(GameObject newTarget)
            {
                if (newTarget == null)
                {
                    _target = null;
                    _losChecker = null;
                    Stop();
                    return;
                }

                if (_target != newTarget)
                {
                    _target = newTarget;

                    if (_npcOwner.Brain is IControlledBrain brain)
                        _losChecker = brain.GetPlayerOwner();
                    else if (_target is GamePlayer targetPlayer)
                        _losChecker = targetPlayer;
                    else if (_target is GameNPC npcTarget && npcTarget.Brain is IControlledBrain targetBrain)
                        _losChecker = targetBrain.GetPlayerOwner();
                    else
                        _losChecker = null;
                }

                // If there's no LoS checker, stop the timer and force LoS.
                if (_losChecker == null)
                {
                    _attackAction.ForceLos();
                    Stop();
                    return;
                }

                Start(0);
                Interval = LosCheckInterval;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (_losChecker == null || _npcOwner.ObjectState is not eObjectState.Active)
                    return 0;

                _losChecker.Out.SendLosCheckRequest(_npcOwner, _target, _attackAction);
                return LosCheckInterval;
            }
        }
    }
}
