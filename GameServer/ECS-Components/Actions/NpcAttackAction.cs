using System;
using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const double TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH = 500; // NPCs will switch to ranged if further than melee range + (this * maxSpeed * 0.001).

        private GameNPC _npcOwner;
        private bool _hasLos;
        private CheckLosTimer _checkLosTimer;
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

            if (IsArcherGuardOrImmobile &&
                (livingFollowTarget.ActiveWeaponSlot is eActiveWeaponSlot.Distance || !livingFollowTarget.IsWithinRadius(_npcOwner, livingFollowTarget.attackComponent.AttackRange)))
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

        public override bool OnOutOfRangeOrNoLosRangedAttack()
        {
            // If we're a guard or an immobile NPC, let's forget about our target so that we can attack another one and not stare at the wall.
            // Otherwise, switch to melee, but keep the timer alive.
            if (IsArcherGuardOrImmobile)
            {
                StandardMobBrain brain = _npcOwner.Brain as StandardMobBrain;

                if (_losCheckTarget is GameLiving livingLosCheckTarget)
                    brain.RemoveFromAggroList(livingLosCheckTarget);

                brain.AttackMostWanted(); // This won't immediately start the attack on the new target, but we can use `TargetObject` to start checking it.
                GameObject nextTarget = _npcOwner.TargetObject;

                if (nextTarget != _losCheckTarget)
                    _checkLosTimer?.ChangeTarget(nextTarget); // The timer might be already cleaned up if this was the last target.

                return true;
            }
            else if (AttackComponent.AttackState && !_hasLos)
            {
                SwitchToMeleeAndTick();
                return true;
            }

            return false;
        }

        protected override bool PrepareMeleeAttack()
        {
            int meleeAttackRange = _npcOwner.MeleeAttackRange;
            int maxSpeed = _npcOwner.MaxSpeed;

            if (maxSpeed > 0)
                meleeAttackRange = meleeAttackRange + (int) (TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH * maxSpeed * 0.001);

            // NPCs try to switch to their ranged weapon whenever possible.
            if (!_npcOwner.IsBeingInterrupted &&
                _npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                !_npcOwner.IsWithinRadius(_target, meleeAttackRange) &&
                !_wasMeleeWeaponSwitchForced)
            {
                // But only if there is no timer running or if it has LoS on its current target.
                // If the timer is running, it'll check for LoS continuously.
                if (!Properties.CHECK_LOS_BEFORE_NPC_RANGED_ATTACK || _checkLosTimer == null || !_checkLosTimer.IsAlive)
                {
                    SwitchToRangedAndTick();
                    return false;
                }

                if (_losCheckTarget != _target)
                {
                    _hasLos = false;
                    _checkLosTimer.ChangeTarget(_target);
                }
                else if (_hasLos)
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
                _target = npcBrain.LastHighestThreatInAttackRange;

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
            if (Properties.CHECK_LOS_BEFORE_NPC_RANGED_ATTACK)
            {
                if (_checkLosTimer == null)
                    _checkLosTimer = new CheckLosTimer(_npcOwner, _target, OnLosCheck, ForceLos);
                else if (_losCheckTarget != _target)
                {
                    _hasLos = false;
                    _checkLosTimer.ChangeTarget(_target);
                }

                if (!_hasLos)
                {
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
            }
            else
                _hasLos = true;

            return base.PrepareRangedAttack();
        }

        public override void CleanUp()
        {
            if (_checkLosTimer != null)
            {
                _checkLosTimer.Stop();
                _checkLosTimer = null;
            }

            _wasMeleeWeaponSwitchForced = false;
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

        private void OnLosCheck(GamePlayer player, LosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            _losCheckTarget = _npcOwner.CurrentRegion.GetObject(targetOID);

            if (_losCheckTarget == null || _losCheckTarget != _target)
                _hasLos = false;
            else
                _hasLos = response is LosCheckResponse.True;

            if (!_hasLos)
            {
                OnOutOfRangeOrNoLosRangedAttack();
                return;
            }

            _npcOwner.TurnTo(_losCheckTarget);
        }

        private void ForceLos()
        {
            _hasLos = true;
            _npcOwner.TurnTo(_losCheckTarget);
        }

        public class CheckLosTimer : ECSGameTimerWrapperBase
        {
            private GameNPC _npcOwner;
            private GameObject _target;
            private CheckLosResponse _callback;
            private Action _forceLos;
            private GamePlayer _losChecker;

            public CheckLosTimer(GameObject owner, GameObject target, CheckLosResponse callback, Action forceLos) : base(owner)
            {
                _npcOwner = owner as GameNPC;
                _callback = callback;
                _forceLos = forceLos;
                ChangeTarget(target);
            }

            public void ChangeTarget(GameObject newTarget)
            {
                if (newTarget == null)
                {
                    Stop();
                    return;
                }

                if (_target != newTarget)
                {
                    _target = newTarget;

                    if (_npcOwner.Brain is IControlledBrain brain)
                        _losChecker = brain.GetPlayerOwner();
                    if (_target is GamePlayer targetPlayer)
                        _losChecker = targetPlayer;
                    else if (_target is GameNPC npcTarget && npcTarget.Brain is IControlledBrain targetBrain)
                        _losChecker = targetBrain.GetPlayerOwner();
                }

                // Don't bother starting the timer if there's no one to perform the LoS check.
                if (_losChecker == null)
                {
                    _forceLos();
                    return;
                }

                if (!IsAlive && _losChecker != null)
                {
                    Start(1);
                    Interval = LosCheckInterval;
                }
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                // We normally rely on `AttackActon.CleanUp()` to stop this timer.
                if (!_npcOwner.attackComponent.AttackState || _npcOwner.ObjectState is not eObjectState.Active)
                    return 0;

                _losChecker.Out.SendCheckLos(_npcOwner, _target, new CheckLosResponse(_callback));
                return LosCheckInterval;
            }
        }
    }
}
