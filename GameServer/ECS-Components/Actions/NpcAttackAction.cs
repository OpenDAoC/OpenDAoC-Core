using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const int HEALTH_PERCENT_THRESHOLD_FOR_MELEE_SWITCH_ON_INTERRUPT = 80;
        private const double TIME_TO_TARGET_THRESHOLD_BEFORE_MELEE_SWITCH = 250; // NPCs will switch to melee if within melee range + (this * maxSpeed * 0.001).
        private const double TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH = 1000; // NPCs will switch to ranged if further than melee range + (this * maxSpeed * 0.001).

        private GameNPC _npcOwner;
        private bool _hasLos;
        private CheckLosTimer _checkLosTimer;
        private GameObject _losCheckTarget;

        private static int LosCheckInterval => Properties.CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL;
        private bool IsGuardArcherOrImmobile => _npcOwner is GuardArcher || _npcOwner.MaxSpeedBase == 0;

        public NpcAttackAction(GameNPC owner) : base(owner)
        {
            _npcOwner = owner;
        }

        public override void OnAimInterrupt(GameObject attacker)
        {
            // If the NPC is interrupted, we need to tell it to stop following its target if we want the following code to work.
            _npcOwner.StopFollowing();

            // Guard archers and immobile NPCs should ignore the health threshold.
            // They will still switch to melee if their target gets in melee range.
            if ((!IsGuardArcherOrImmobile && _npcOwner.HealthPercent < HEALTH_PERCENT_THRESHOLD_FOR_MELEE_SWITCH_ON_INTERRUPT) ||
                (attacker is GameLiving livingAttacker && livingAttacker.ActiveWeaponSlot is not eActiveWeaponSlot.Distance && livingAttacker.IsWithinRadius(_npcOwner, livingAttacker.attackComponent.AttackRange)))
                SwitchToMeleeAndTick();
        }

        public override bool OnOutOfRangeOrNoLosRangedAttack()
        {
            // If we're a guard or an immobile NPC, let's forget about our target so that we can attack another one and not stare at the wall.
            // Otherwise, switch to melee, but keep the timer alive.
            if (IsGuardArcherOrImmobile)
            {
                GameObject oldTarget = _target;
                StandardMobBrain brain = _npcOwner.Brain as StandardMobBrain;
                brain.RemoveFromAggroList(_losCheckTarget as GameLiving);
                brain.AttackMostWanted(); // This won't immediately start the attack on the new target, but we can use `TargetObject` to start checking it.
                GameObject newTarget = _npcOwner.TargetObject;

                if (newTarget != oldTarget)
                    _checkLosTimer?.ChangeTarget(newTarget); // The timer might be already cleaned up if this was the last target.

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
            int offsetMeleeAttackRange = -1; // Makes `IsWithinRadius` return false.

            if (_npcOwner.IsMoving)
            {
                int maxSpeed = _npcOwner.MaxSpeed;

                if (maxSpeed > 0)
                    offsetMeleeAttackRange = meleeAttackRange + (int) (TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH * maxSpeed * 0.001);
            }

            // NPCs try to switch to their ranged weapon whenever possible.
            if (!_npcOwner.IsBeingInterrupted &&
                _npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                !_npcOwner.IsWithinRadius(_target, offsetMeleeAttackRange))
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
                    _checkLosTimer = new CheckLosTimer(_npcOwner, _target, LosCheckCallback);
                else if (_losCheckTarget != _target)
                {
                    _hasLos = false;
                    _checkLosTimer.ChangeTarget(_target);
                }
                else if (!_hasLos)
                {
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
            }
            else
                _hasLos = true;

            return base.PrepareRangedAttack();
        }

        protected override void CleanUp()
        {
            if (_npcOwner.Brain is NecromancerPetBrain necromancerPetBrain)
                necromancerPetBrain.ClearAttackSpellQueue();

            if (_checkLosTimer != null)
            {
                _checkLosTimer.Stop();
                _checkLosTimer = null;
            }

            base.CleanUp();
        }

        private void SwitchToMeleeAndTick()
        {
            if (_npcOwner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                return;

            _npcOwner.SwitchToMelee(_target);
        }

        private void SwitchToRangedAndTick()
        {
            if (_npcOwner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                return;

            _npcOwner.SwitchToRanged(_target);
        }

        private void LosCheckCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            _hasLos = response is eLosCheckResponse.TRUE;
            _losCheckTarget = _npcOwner.CurrentRegion.GetObject(targetOID);

            if (_losCheckTarget == null)
                return;

            if (_hasLos)
            {
                _npcOwner.TurnTo(_losCheckTarget);
                return;
            }

            OnOutOfRangeOrNoLosRangedAttack();
        }

        public class CheckLosTimer : ECSGameTimerWrapperBase
        {
            private GameNPC _npcOwner;
            private GameObject _target;
            private CheckLosResponse _callback;
            private GamePlayer _losChecker;

            public CheckLosTimer(GameObject owner, GameObject target, CheckLosResponse callback) : base(owner)
            {
                _npcOwner = owner as GameNPC;
                _callback = callback;
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
                    _callback(null, eLosCheckResponse.TRUE, 0, 0);
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
