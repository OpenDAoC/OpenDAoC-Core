using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    public class PlayerAttackAction : AttackAction
    {
        private GamePlayer _playerOwner;

        public PlayerAttackAction(GamePlayer playerOwner) : base(playerOwner)
        {
            _playerOwner = playerOwner;
        }

        public override bool CheckInterruptTimer()
        {
            if (_playerOwner.attackComponent.Attackers.IsEmpty)
                return false;

            // Don't interrupt aiming if we haven't received an interrupt timer.
            if (!_playerOwner.IsBeingInterrupted)
                return false;

            _playerOwner.attackComponent.StopAttack();
            OnAimInterrupt(_playerOwner.LastInterrupter);
            return true;
        }

        public override void OnAimInterrupt(GameObject attacker)
        {
            string attackTypeMsg;

            if (_playerOwner.ActiveWeapon != null && _playerOwner.ActiveWeapon.Object_Type == (int)EObjectType.Thrown)
                attackTypeMsg = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Throw");
            else
                attackTypeMsg = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Shot");

            if (attacker is GameNpc npcAttacker)
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, _playerOwner.Client.Account.Language, npcAttacker), attackTypeMsg), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
            else
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), attackTypeMsg), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
        }

        protected override bool PrepareMeleeAttack()
        {
            _combatStyle = _styleComponent.GetStyleToUse();
            return base.PrepareMeleeAttack();
        }

        protected override bool PrepareRangedAttack()
        {
            if (base.PrepareRangedAttack())
            {
                // This is also done in weaponAction.Execute(), but we must unstealth immediately if the call is delayed.
                if (_ticksToTarget > 0)
                    _playerOwner.Stealth(false);

                return true;
            }

            return false;
        }

        protected override void PerformRangedAttack()
        {
            _playerOwner.rangeAttackComponent.RemoveEnduranceAndAmmoOnShot();
            base.PerformRangedAttack();
        }

        protected override bool FinalizeMeleeAttack()
        {
            if (base.FinalizeMeleeAttack())
            {
                if (_playerOwner.UseDetailedCombatLog)
                    _playerOwner.Out.SendMessage($"Attack Speed: {_interval / 1000.0}s", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

                return true;
            }

            return false;
        }

        protected override bool FinalizeRangedAttack()
        {
            bool stopAttack = false;

            if (_playerOwner.rangeAttackComponent.RangedAttackState != ERangedAttackState.AimFireReload)
                stopAttack = true;
            else if (_playerOwner.Endurance < RangeAttackComponent.DEFAULT_ENDURANCE_COST)
            {
                stopAttack = true;
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", _weapon.Name), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
            }

            if (stopAttack)
            {
                _attackComponent.StopAttack();
                _attackComponent.attackAction?.CleanUp();
                return false;
            }

            if (base.FinalizeRangedAttack())
            {
                _playerOwner.TempProperties.SetProperty(RangeAttackComponent.RANGED_ATTACK_START, GameLoop.GameLoopTime);
                return true;
            }

            return false;
        }
    }
}
