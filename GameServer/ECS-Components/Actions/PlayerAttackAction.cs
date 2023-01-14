﻿using System.Linq;
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

        public override bool CheckAimInterrupt()
        {
            if (_playerOwner.attackComponent.Attackers.Count <= 0)
                return false;

            // Don't interrupt aiming if we haven't received an interrupt timer.
            if (_playerOwner.InterruptTime <= GameLoop.GameLoopTime)
                return false;

            _playerOwner.attackComponent.StopAttack();
            OnAimInterrupt();
            return true;
        }

        public override void OnAimInterrupt()
        {
            string attackTypeMsg;
            GameObject attacker = _playerOwner.attackComponent.Attackers.Last();

            if (_playerOwner.attackComponent.AttackWeapon != null && _playerOwner.attackComponent.AttackWeapon.Object_Type == (int)eObjectType.Thrown)
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
            if (base.FinalizeRangedAttack())
            {
                _playerOwner.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, GameLoop.GameLoopTime);
                return true;
            }

            return false;
        }
    }
}
