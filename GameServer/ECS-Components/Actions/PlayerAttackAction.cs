using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class PlayerAttackAction : AttackAction
    {
        private GamePlayer _playerOwner;

        public PlayerAttackAction(GamePlayer owner) : base(owner)
        {
            _playerOwner = owner;
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
            _combatStyle = StyleComponent.GetStyleToUse();
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

            if (_playerOwner.rangeAttackComponent.Ammo.Count == 0)
                _playerOwner.rangeAttackComponent.UpdateAmmo(_playerOwner.ActiveWeapon);
        }

        protected override bool FinalizeMeleeAttack()
        {
            if (base.FinalizeMeleeAttack())
            {
                if (_playerOwner.UseDetailedCombatLog)
                    _playerOwner.Out.SendMessage($"Attack Speed: {_interval / 1000.0}s", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                StyleComponent.NextCombatStyle = null;
                StyleComponent.NextCombatBackupStyle = null;
                return true;
            }

            return false;
        }

        protected override bool FinalizeRangedAttack()
        {
            bool stopAttack = false;

            if (_playerOwner.rangeAttackComponent.RangedAttackState is not eRangedAttackState.AimFireReload)
                stopAttack = true;
            else if (_playerOwner.Endurance < RangeAttackComponent.DEFAULT_ENDURANCE_COST)
            {
                stopAttack = true;
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", _weapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }
            else
            {
                DbInventoryItem ammo = _playerOwner.rangeAttackComponent.Ammo;

                if (ammo == null || ammo.Count == 0)
                    stopAttack = true;
            }

            if (stopAttack)
            {
                AttackComponent.StopAttack();
                CleanUp();
                return false;
            }

            return base.FinalizeRangedAttack();
        }
    }
}
