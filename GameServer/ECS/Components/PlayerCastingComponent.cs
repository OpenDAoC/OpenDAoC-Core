using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    // This component will hold all data related to casting spells.
    public class PlayerCastingComponent : CastingComponent
    {
        private GamePlayer _playerOwner;

        public PlayerCastingComponent(GamePlayer playerOwner) : base(playerOwner)
        {
            _playerOwner = playerOwner;
        }

        public override bool RequestStartCastSpell(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (!_playerOwner.ChainedActions.CheckCommandInput(spell, spellLine))
                return false;

            if (_playerOwner.ChainedActions.Execute(spell))
            {
                EntityManager.Add<CastingComponent>(this);
                return true;
            }

            return base.RequestStartCastSpell(spell, spellLine, spellCastingAbilityHandler, target);
        }

        protected override void StartCastSpell(StartCastSpellRequest startCastSpellRequest)
        {
            // Unstealth when we start casting (NS/Ranger/Hunter).
            if (_playerOwner.IsStealthed)
                _playerOwner.Stealth(false);

            base.StartCastSpell(startCastSpellRequest);
        }

        protected override bool CanCastSpell()
        {
            if (_playerOwner.effectListComponent.ContainsEffectForEffectType(EEffect.Volley))
            {
                _playerOwner.Out.SendMessage("You can't cast spells while Volley is active!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsCrafting)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                _playerOwner.craftComponent.StopCraft();
                _playerOwner.CraftTimer = null;
                _playerOwner.Out.SendCloseTimerWindow();
            }

            if (_playerOwner.IsSalvagingOrRepairing)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                _playerOwner.CraftTimer.Stop();
                _playerOwner.CraftTimer = null;
                _playerOwner.Out.SendCloseTimerWindow();
            }

            if (_playerOwner.IsStunned)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsMezzed)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return false;
            }

            if (_playerOwner.IsSilenced)
            {
                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                return false;
            }

            return true;
        }
    }
}
