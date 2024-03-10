using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace DOL.GS
{
    public class NpcAttackAction : AttackAction
    {
        private const int MIN_HEALTH_PERCENT_FOR_MELEE_SWITCH_ON_INTERRUPT = 70;
        private const int PET_LOS_CHECK_INTERVAL = 1000;

        private GameNPC _npcOwner;
        private bool _isGuardArcher;
        private GamePlayer _npcOwnerOwner;
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
                _interval = 0;
                return false;
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
                return base.FinalizeRangedAttack();
        }

        public override void CleanUp()
        {
            if (_npcOwner.Brain is NecromancerPetBrain necromancerPetBrain)
                necromancerPetBrain.ClearAttackSpellQueue();

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
                _npcOwnerOwner.Out.SendCheckLos(_npcOwner, _target, new CheckLosResponse(LosCheckCallback));
            else
                _hasLos = true;

            return PET_LOS_CHECK_INTERVAL;
        }

        private void LosCheckCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            _hasLos = response is eLosCheckResponse.TRUE;
        }
    }
}
