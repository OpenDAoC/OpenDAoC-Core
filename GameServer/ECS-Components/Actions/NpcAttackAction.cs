using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const double TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH = 500; // NPCs will switch to ranged if further than melee range + (this * maxSpeed * 0.001).

        private readonly GameNPC _npcOwner;
        private CheckLosTimer _checkLosTimer;
        private PendingCheckLosTimer _pendingCheckLosTimer;
        private bool _wasMeleeWeaponSwitchForced; // Used to prevent NPCs from switching to their ranged weapon automatically if they explicitly switched to a melee weapon during combat.

        private static int LosCheckInterval => Properties.CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL;
        private bool IsArcherGuardOrImmobile => _npcOwner is GuardArcher || _npcOwner.MaxSpeedBase == 0;

        public NpcAttackAction(GameNPC owner) : base(owner)
        {
            _npcOwner = owner;
        }

        protected override void OnEveryTick()
        {
            if (_npcOwner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
            {
                StopPendingLosCheck();
                return;
            }

            RangeAttackComponent rangeAttackComponent = _npcOwner.rangeAttackComponent;

            if (rangeAttackComponent.RangedAttackState is eRangedAttackState.None)
                return;

            GameLiving desiredTarget = _npcOwner.TargetObject as GameLiving;

            if (desiredTarget != rangeAttackComponent.AutoFireTarget)
                UpdatePendingLosCheck(desiredTarget);
            else
                StopPendingLosCheck();
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
            if (!_npcOwner.IsInterruptedOrSelfInterrupted() &&
                _npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                !_npcOwner.IsWithinRadius(_target, meleeAttackRange) &&
                !_wasMeleeWeaponSwitchForced)
            {
                bool timerActive = _checkLosTimer != null && _checkLosTimer.IsAlive;
                bool targetChanged = _checkLosTimer?.ResolvedTarget != _target;

                if (timerActive && targetChanged)
                    _checkLosTimer.ChangeTarget(_target);

                if (!timerActive || targetChanged || _checkLosTimer.HasLos)
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
            RangeAttackComponent rangeAttackComponent = _npcOwner.rangeAttackComponent;
            bool isAiming = rangeAttackComponent.RangedAttackState is not eRangedAttackState.None;

            // Commit to the current target for the duration of the attack.
            // We use AutoFireTarget for this purpose since it's unused by NPCs.
            if (!isAiming)
                rangeAttackComponent.AutoFireTarget = _target;

            if (_checkLosTimer == null)
                _checkLosTimer = new(this, _target);
            else if (_checkLosTimer.Target != _target)
            {
                // If we were already pre-checking LoS on this target in the background while
                // finishing the previous cycle, reuse that result instead of waiting on a fresh check.
                bool hasPendingResult = _pendingCheckLosTimer != null && _pendingCheckLosTimer.ResolvedTarget == _target;
                _checkLosTimer.ChangeTarget(_target);

                if (hasPendingResult)
                {
                    _checkLosTimer.HasLos = _pendingCheckLosTimer.HasLos;
                    _checkLosTimer.ResolvedTarget = _target;
                }
                else
                {
                    StopPendingLosCheck();
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
            }

            bool shouldCheckLos = !isAiming || Properties.CHECK_LOS_DURING_NPC_RANGED_ATTACK;

            if (shouldCheckLos && !_checkLosTimer.HasLos)
            {
                if (isAiming && _checkLosTimer.ResolvedTarget == _target)
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
            _npcOwner.rangeAttackComponent.AutoFireTarget = null;
            bool lostLos = !_checkLosTimer.HasLos && _checkLosTimer.ResolvedTarget == _target;

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

        protected override void InterruptAim(GameLiving attacker)
        {
            // Lords can only be interrupted by their own target, and in melee range.
            if (_npcOwner is GuardLord &&
                _npcOwner.MaxSpeedBase == 0 &&
                (attacker != _npcOwner.TargetObject || !_npcOwner.IsWithinRadius(attacker, _npcOwner.MeleeAttackRange)))
            {
               return;
            }

            _npcOwner.StopAttack();
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

        public override void CleanUp()
        {
            if (_npcOwner.Brain is NecromancerPetBrain necroBrain)
                necroBrain.CheckSpellQueue();

            if (_checkLosTimer != null)
            {
                _checkLosTimer.ChangeTarget(null);
                _checkLosTimer = null;
            }

            if (_pendingCheckLosTimer != null)
            {
                _pendingCheckLosTimer.ChangeTarget(null);
                _pendingCheckLosTimer = null;
            }

            _wasMeleeWeaponSwitchForced = false;
            _npcOwner.rangeAttackComponent.AutoFireTarget = null;
            base.CleanUp();
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

        private void OnOutOfRangeOrNoLosRangedAttack()
        {
            // If we're a guard or an immobile NPC, let's forget about our target so that we can attack another one and not stare at the wall.
            if (IsArcherGuardOrImmobile)
            {
                GameLiving resolvedTarget = _checkLosTimer?.ResolvedTarget;

                if (resolvedTarget != null)
                    (_npcOwner.Brain as StandardMobBrain)?.RemoveFromAggroList(resolvedTarget);

                _npcOwner.rangeAttackComponent.AutoFireTarget = null;
                return;
            }

            if (AttackComponent.AttackState)
                _npcOwner.StopAttack();
        }

        private void UpdatePendingLosCheck(GameLiving target)
        {
            if (target == null)
            {
                StopPendingLosCheck();
                return;
            }

            if (_pendingCheckLosTimer == null)
                _pendingCheckLosTimer = new(this, target);
            else if (_pendingCheckLosTimer.Target != target)
                _pendingCheckLosTimer.ChangeTarget(target);
        }

        private void StopPendingLosCheck()
        {
            _pendingCheckLosTimer?.ChangeTarget(null);
        }

        private class CheckLosTimer : CheckLosTimerBase
        {
            public CheckLosTimer(NpcAttackAction attackAction, GameLiving target) : base(attackAction, target) { }

            protected override void OnLosEvaluated()
            {
                // Only react immediately if we aren't currently waiting for a bow draw completion.
                if (!HasLos && _attackAction._npcOwner.rangeAttackComponent.RangedAttackState is eRangedAttackState.None)
                    _attackAction.OnOutOfRangeOrNoLosRangedAttack();
            }
        }

        private class PendingCheckLosTimer : CheckLosTimerBase
        {
            public PendingCheckLosTimer(NpcAttackAction attackAction, GameLiving target) : base(attackAction, target) { }

            protected override void OnLosEvaluated()
            {
                // Immobile and guard NPCs drop the next target from their aggro list immediately.
                if (!HasLos && _attackAction.IsArcherGuardOrImmobile)
                    (_attackAction._npcOwner.Brain as StandardMobBrain)?.RemoveFromAggroList(ResolvedTarget);
            }
        }

        private abstract class CheckLosTimerBase : ECSGameTimerWrapperBase, ILosCheckListener
        {
            protected readonly NpcAttackAction _attackAction;
            private GamePlayer _losChecker;

            public GameLiving Target { get; private set; }
            public GameLiving ResolvedTarget { get; set; }
            public bool HasLos { get; set; }

            public CheckLosTimerBase(NpcAttackAction attackAction, GameLiving target) : base(attackAction._npcOwner)
            {
                _attackAction = attackAction;
                ChangeTarget(target);
            }

            public void ChangeTarget(GameLiving newTarget)
            {
                if (newTarget != Target)
                {
                    HasLos = false;
                    ResolvedTarget = null;
                }

                if (newTarget == null)
                {
                    Target = null;
                    _losChecker = null;
                    Stop();
                    return;
                }

                Target = newTarget;
                RefreshLosCheckerForCurrentTarget();
                Start(0);
            }

            public void RefreshLosCheckerForCurrentTarget()
            {
                _losChecker = _attackAction._npcOwner.Brain.GetLosChecker(Target);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (_losChecker == null || Owner.ObjectState is not eObjectState.Active)
                    return 0;

                _losChecker.Out.SendLosCheckRequest(Owner, Target, this);
                return LosCheckInterval;
            }

            public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
            {
                // The target may have changed. Don't act on an obsolete check.
                if (_attackAction._npcOwner.CurrentRegion.GetObject(targetId) is not GameLiving target || target != Target)
                    return;

                // Refresh the LoS checker if the current one stops responding, and wait for a reply.
                if (response is LosCheckResponse.Timeout && IsAlive)
                {
                    RefreshLosCheckerForCurrentTarget();
                    return;
                }

                ResolvedTarget = target;
                HasLos = response is LosCheckResponse.True;
                OnLosEvaluated();
            }

            protected abstract void OnLosEvaluated();
        }
    }
}
