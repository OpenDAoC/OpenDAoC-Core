using DOL.AI.Brain;
using DOL.GS.Keeps;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const int MIN_HEALTH_PERCENT_FOR_MELEE_SWITCH_ON_INTERRUPT = 70;
        // Check interval (upper bound) in ms of entities around this NPC when its main target is out of range. Used to attack other entities on its path.
        private const int NPC_VICINITY_CHECK_DELAY = 1000;
        private GameNPC _npcOwner;
        private bool _isGuardArcher;
        // Next check for NPCs in attack range to hit while on the way to main target.
        private long _nextVicinityCheck = 0;

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
                _npcOwner.SwitchToRanged(_target);
                _interval = _attackComponent.AttackSpeed(_weapon);
                return false;
            }

            _combatStyle = _styleComponent.NPCGetStyleToUse();

            if (!base.PrepareMeleeAttack())
                return false;

            // The target isn't in melee range yet. Check if another target is in range to attack on the way to the main target.
            if (_target != null &&
                _npcOwner.Brain is not IControlledBrain &&
                _npcOwner.Brain is StandardMobBrain npcBrain &&
                npcBrain.AggroTable.Count > 0 &&
                !_npcOwner.IsWithinRadius(_target, _attackComponent.AttackRange + _rangeBonus))
            {
                GameLiving possibleTarget = null;
                long maxaggro = 0;
                long aggro;

                foreach (GamePlayer playerInRadius in _npcOwner.GetPlayersInRadius((ushort)_attackComponent.AttackRange))
                {
                    if (npcBrain.AggroTable.ContainsKey(playerInRadius))
                    {
                        aggro = npcBrain.GetAggroAmountForLiving(playerInRadius);

                        if (aggro <= 0)
                            continue;

                        if (aggro > maxaggro)
                        {
                            possibleTarget = playerInRadius;
                            maxaggro = aggro;
                        }
                    }
                }

                // Check for NPCs in attack range. Only check if the NPCNextNPCVicinityCheck is less than the current GameLoop Time.
                if (_nextVicinityCheck < GameLoop.GameLoopTime)
                {
                    // Set the next check for NPCs. Will be in a range from 100ms -> NPC_VICINITY_CHECK_DELAY.
                    _nextVicinityCheck = GameLoop.GameLoopTime + Util.Random(100,NPC_VICINITY_CHECK_DELAY);

                    foreach (GameNPC npcInRadius in _npcOwner.GetNPCsInRadius((ushort)_attackComponent.AttackRange))
                    {
                        if (npcBrain.AggroTable.ContainsKey(npcInRadius))
                        {
                            aggro = npcBrain.GetAggroAmountForLiving(npcInRadius);

                            if (aggro <= 0)
                                continue;

                            if (aggro > maxaggro)
                            {
                                possibleTarget = npcInRadius;
                                maxaggro = aggro;
                            }
                        }
                    }
                }

                if (possibleTarget == null)
                {
                    _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return false;
                }
                else
                    _target = possibleTarget;
            }

            return true;
        }

        protected override bool FinalizeRangedAttack()
        {
            // Switch to melee if range to target is less than 200.
            if (_npcOwner != null &&
                _npcOwner.TargetObject != null &&
                _npcOwner.IsWithinRadius(_target, 200))
            {
                _npcOwner.SwitchToMelee(_target);
                _interval = 1;
                return false;
            }
            else
            {
                // Mobs always shoot and reload.
                _npcOwner.rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFireReload;
                return base.FinalizeRangedAttack();
            }            
        }
    }
}
