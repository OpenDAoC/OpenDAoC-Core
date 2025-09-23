using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class PlayerCastingComponent : CastingComponent
    {
        private GamePlayer _playerOwner;

        public PlayerCastingComponent(GamePlayer playerOwner) : base(playerOwner)
        {
            _playerOwner = playerOwner;
        }

        protected override GamePlayer GetLosChecker(GameLiving target)
        {
            return _playerOwner;
        }

        protected override bool CanCastSpell()
        {
            if (_playerOwner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
            {
                _playerOwner.Out.SendMessage("You can't cast spells while Volley is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsCrafting)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                _playerOwner.craftComponent.StopCraft();
                _playerOwner.CraftTimer = null;
                _playerOwner.Out.SendCloseTimerWindow();
            }

            if (_playerOwner.IsSalvagingOrRepairing)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                _playerOwner.CraftTimer.Stop();
                _playerOwner.CraftTimer = null;
                _playerOwner.Out.SendCloseTimerWindow();
            }

            if (_playerOwner.IsStunned)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsMezzed)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsSilenced)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                return false;
            }

            return true;
        }

        protected override void SendSpellCancelMessage(bool moving, bool focusSpell)
        {
            if (focusSpell)
            {
                if (moving)
                    _playerOwner.Out.SendMessage("You move and interrupt your focus!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                else
                    _playerOwner.Out.SendMessage($"You lose your focus on your spell.", eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
            }
            else if (moving)
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client, "SpellHandler.CasterMove"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
}
