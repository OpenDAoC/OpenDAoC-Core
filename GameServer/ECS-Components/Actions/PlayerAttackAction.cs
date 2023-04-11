using System.Linq;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
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
            if (_playerOwner.attackComponent.Attackers.Count <= 0)
                return false;

            GameObject attacker = _playerOwner.attackComponent.Attackers.Last();

            // Don't interrupt aiming if we haven't received an interrupt timer.
            if (!_playerOwner.IsBeingInterrupted)
                return false;

            _playerOwner.attackComponent.StopAttack();
            OnAimInterrupt(attacker);
            return true;
        }

        public override void OnAimInterrupt(GameObject attacker)
        {
            string attackTypeMsg;

            if (_playerOwner.ActiveWeapon != null && _playerOwner.ActiveWeapon.Object_Type == (int)eObjectType.Thrown)
                attackTypeMsg = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Throw");
            else
                attackTypeMsg = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Shot");

            if (attacker is GameNPC npcAttacker)
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, _playerOwner.Client.Account.Language, npcAttacker), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            else
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
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
                    _playerOwner.Out.SendMessage($"Attack Speed: {_interval / 1000.0}s", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                return true;
            }

            return false;
        }

        protected override bool FinalizeRangedAttack()
        {
            bool stopAttack = false;

            if (_playerOwner.rangeAttackComponent.RangedAttackState != eRangedAttackState.AimFireReload)
                stopAttack = true;
            else if (_playerOwner.Endurance < RangeAttackComponent.DEFAULT_ENDURANCE_COST)
            {
                stopAttack = true;
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", _weapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }

            if (stopAttack)
            {
                _attackComponent.StopAttack();
                _attackComponent.attackAction?.CleanUp();
                return false;
            }

            if (base.FinalizeRangedAttack())
            {
                _playerOwner.TempProperties.setProperty(RangeAttackComponent.RANGED_ATTACK_START, GameLoop.GameLoopTime);
                return true;
            }

            return false;
        }
    }
}
