using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    // This component will hold all data related to casting spells.
    public class PlayerCastingComponent : CastingComponent
    {
        private GamePlayer _playerOwner;
        private PairedSpell _currentPairedSpell;
        private Spell _pairedSpellCommandFirstSpell;
        private Dictionary<Spell, PairedSpell> _pairedSpells = new();

        public override PairedSpellInputStep PairedSpellCommandInputStep { get; set; }

        public PlayerCastingComponent(GamePlayer playerOwner) : base(playerOwner)
        {
            _playerOwner = playerOwner;
        }

        protected override SpellHandler CreateSpellHandler(StartCastSpellRequest startCastSpellRequest)
        {
            SpellHandler spellHandler = base.CreateSpellHandler(startCastSpellRequest);

            // Check if the paired spell, if there is any, can still be cast by that character.
            if (Properties.ALLOW_PAIRED_SPELLS && _pairedSpells.TryGetValue(startCastSpellRequest.Spell, out _currentPairedSpell))
            {
                spellHandler.HasPairedSpell = true;
                Spell pairedSpell = _currentPairedSpell.Spell;
                SpellLine pairedSpellLine = _currentPairedSpell.SpellLine;
                bool isValidSpell = pairedSpellLine.IsBaseLine ? _playerOwner.Level >= pairedSpell.Level : _playerOwner.GetBaseSpecLevel(pairedSpellLine.Spec) >= pairedSpell.Level;

                if (!isValidSpell)
                {
                    _playerOwner.Out.SendMessage($"{pairedSpell.Name} is no longer a valid paired spell for your {(!pairedSpellLine.IsBaseLine? "spec " : "")}level and has been cleared.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    _pairedSpells.Remove(startCastSpellRequest.Spell);
                    _currentPairedSpell = null;
                }
            }

            return spellHandler;
        }

        protected override void StartCastSpell(SpellHandler newSpellHandler)
        {
            // Unstealth when we start casting (NS/Ranger/Hunter).
            if (_playerOwner.IsStealthed)
                _playerOwner.Stealth(false);

            base.StartCastSpell(newSpellHandler);
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

        public override bool PairedSpellInputCheck(Spell spell, SpellLine spellLine)
        {
            switch (PairedSpellCommandInputStep)
            {
                case PairedSpellInputStep.FIRST:
                {
                    _pairedSpells.Remove(spell);
                    _pairedSpellCommandFirstSpell = spell;
                    _playerOwner.Out.SendMessage($"Select a second spell to pair {_pairedSpellCommandFirstSpell.Name} with.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    PairedSpellCommandInputStep = PairedSpellInputStep.SECOND;
                    return false;
                }
                case PairedSpellInputStep.SECOND:
                {
                    _pairedSpells[_pairedSpellCommandFirstSpell] = new(spell, spellLine);
                    _playerOwner.Out.SendMessage($"{_pairedSpellCommandFirstSpell.Name} is now paired with {spell.Name}.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    _pairedSpellCommandFirstSpell = null;
                    PairedSpellCommandInputStep = PairedSpellInputStep.NONE;
                    return false;
                }
                case PairedSpellInputStep.CLEAR:
                {
                    if (_pairedSpells.Remove(spell, out PairedSpell pairedSpell))
                        _playerOwner.Out.SendMessage($"{spell.Name} is no longer paired with with {pairedSpell.Spell.Name}.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    else
                        _playerOwner.Out.SendMessage($"{spell.Name} is not currently paired with another spell.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);

                    PairedSpellCommandInputStep = PairedSpellInputStep.NONE;
                    return false;
                }
                case PairedSpellInputStep.NONE:
                default:
                    return true;
            }
        }

        public override void StartCastPairedSpell()
        {
            if (_currentPairedSpell == null)
                return;

            // Allow queuing paired spells.
            if (SpellHandler?.IsPairedSpell != true)
                SpellHandler = null;

            SpellHandler newSpellHandler = CreateSpellHandler(new StartCastSpellRequest(_currentPairedSpell.Spell, _currentPairedSpell.SpellLine, null, null));
            newSpellHandler.IsPairedSpell = true;
            StartCastSpell(newSpellHandler);

            // Tick immediately. Instant spells already tick from `StartCastSpell`.
            if (!SpellHandler.Spell.IsInstantCast)
                SpellHandler.Tick(GameLoop.GameLoopTime);
        }
    }
}
