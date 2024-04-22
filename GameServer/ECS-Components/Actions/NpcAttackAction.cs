using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const int MIN_HEALTH_PERCENT_FOR_MELEE_SWITCH_ON_INTERRUPT = 70;

        private GameNPC _npcOwner;
        private bool _isGuardArcher;
        private bool _hasLos;
        private CheckLosTimer _checkLosTimer;
        private GameObject _losCheckTarget;

        private static int LosCheckInterval => Properties.CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL;
        private bool HasLosOnCurrentTarget => _losCheckTarget == _target && _hasLos;

        public NpcAttackAction(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
            _isGuardArcher = _npcOwner is GuardArcher;
        }

        public override void OnAimInterrupt(GameObject attacker)
        {
            // Guard archers shouldn't switch to melee when interrupted, otherwise they fall from the wall.
            // They will still switch to melee if their target is in melee range.
            if (!_isGuardArcher && _npcOwner.HealthPercent < MIN_HEALTH_PERCENT_FOR_MELEE_SWITCH_ON_INTERRUPT)
                _npcOwner.SwitchToMelee(_target);
        }

        protected override bool PrepareMeleeAttack()
        {
            // NPCs try to switch to their ranged weapon whenever possible.
            if (!_npcOwner.IsBeingInterrupted &&
                _npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                !_npcOwner.IsWithinRadius(_target, 500))
            {
                // But only if there is no timer running or if it has LoS.
                // If the timer is running, it'll check for LoS continuously.
                if (_checkLosTimer == null || !_checkLosTimer.IsAlive || !HasLosOnCurrentTarget)
                {
                    _npcOwner.SwitchToRanged(_target);
                    _interval = 0;
                    return false;
                }
            }

            _combatStyle = StyleComponent.NPCGetStyleToUse();

            if (!base.PrepareMeleeAttack())
                return false;

            int attackRange = AttackComponent.AttackRange;

            // The target isn't in melee range yet. Check if another target is in range to attack on the way to the main target.
            if (!_npcOwner.IsWithinRadius(_target, attackRange) &&
                _npcOwner.Brain is not IControlledBrain &&
                _npcOwner.Brain is StandardMobBrain npcBrain)
            {
                _target = npcBrain.LastHighestThreatInAttackRange;

                if (_target == null || !_npcOwner.IsWithinRadius(_target, attackRange))
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
                else
                    _checkLosTimer.ChangeTarget(_target);

                if (!HasLosOnCurrentTarget)
                {
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
            }
            else
                _hasLos = true;

            return base.PrepareRangedAttack();
        }

        protected override bool FinalizeRangedAttack()
        {
            // Switch to melee if range to target is less than 200.
            if (_npcOwner != null &&
                _npcOwner.TargetObject != null &&
                _npcOwner.IsWithinRadius(_target, 200))
            {
                _npcOwner.SwitchToMelee(_target);
                _interval = 0;
                return false;
            }

            return base.FinalizeRangedAttack();
        }

        public override void CleanUp()
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

            // If we're a guard, let's forget about our target so that we can attack another one and not stare at the wall.
            // Otherwise, switch to melee, but keep the timer alive.
            if (_isGuardArcher)
            {
                GameObject oldTarget = _target;
                StandardMobBrain brain = _npcOwner.Brain as StandardMobBrain;
                brain.RemoveFromAggroList(_losCheckTarget as GameLiving);
                brain.AttackMostWanted(); // This won't immediately start the attack on the new target, but we can use `TargetObject` to start checking it.
                GameObject newTarget = _npcOwner.TargetObject;

                if (newTarget != oldTarget)
                    _checkLosTimer?.ChangeTarget(newTarget); // The timer might be already cleaned up if this was the last target.
            }
            else if (_npcOwner.attackComponent.AttackState)
            {
                _npcOwner.SwitchToMelee(_target);
                _interval = 0;
            }
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
