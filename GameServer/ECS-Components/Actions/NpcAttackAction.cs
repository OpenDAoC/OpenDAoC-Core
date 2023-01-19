using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const int MIN_HEALTH_PERCENT_FOR_MELEE_SWITCH_ON_INTERRUPT = 70;
        // Check interval (upper bound) in ms of entities around this NPC when its main target is out of range. Used to attack other entities on its path.
        private const int NPC_VICINITY_CHECK_INTERVAL = 1000;
        private const int PET_LOS_CHECK_INTERVAL = 1000;

        private GameNPC _npcOwner;
        private bool _isGuardArcher;
        // Next check for NPCs in attack range to hit while on the way to main target.
        private long _nextVicinityCheck = 0;
        private GamePlayer _npcOwnerOwner;
        private int _petLosCheckInterval = PET_LOS_CHECK_INTERVAL;
        private bool _hasLos;

        public NpcAttackAction(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
            _isGuardArcher = _npcOwner is GuardArcher;

            if (Properties.ALWAYS_CHECK_PET_LOS && npcOwner.Brain is IControlledBrain npcOwnerBrain)
            {
                _npcOwnerOwner = npcOwnerBrain.GetPlayerOwner();
                new ECSGameTimer(_npcOwner, new ECSGameTimer.ECSTimerCallback(CheckLos), 1);
            }
            else
                _hasLos = true;
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
            if (!_hasLos)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }

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
                    _nextVicinityCheck = GameLoop.GameLoopTime + Util.Random(100, NPC_VICINITY_CHECK_INTERVAL);

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

        protected override bool PrepareRangedAttack()
        {
            if (!_hasLos)
            {
                _interval = TICK_INTERVAL_FOR_NON_ATTACK;
                return false;
            }

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

        public override void CleanUp()
        {
            _petLosCheckInterval = 0;
            base.CleanUp();
        }

        private int CheckLos(ECSGameTimer timer)
        {
            if (_target == null)
                _hasLos = false;
            else if (_npcOwner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                _hasLos = true;
            else if (_target is GamePlayer || (_target is GameNPC _targetNpc &&
                                              _targetNpc.Brain is IControlledBrain _targetNpcBrain &&
                                              _targetNpcBrain.GetPlayerOwner() != null))
                // Target is either a player or a pet owned by a player.
                _npcOwnerOwner.Out.SendCheckLOS(_npcOwner, _target, new CheckLOSResponse(LosCheckCallback));
            else
                _hasLos = true;

            return _petLosCheckInterval;
        }

        private void LosCheckCallback(GamePlayer player, ushort response, ushort targetOID)
        {
            if (targetOID == 0)
                return;

            _hasLos = (response & 0x100) == 0x100;
        }
    }
}
