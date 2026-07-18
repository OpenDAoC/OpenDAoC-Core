using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class NpcCastingComponent : CastingComponent, ILosCheckListener
    {
        private GameNPC _npcOwner;
        private Dictionary<GameObject, List<SpellWaitingForLosCheck>> _spellsWaitingForLosCheck = new();
        private Lock _spellsWaitingForLosCheckLock = new();

        private bool IsCasterGuardOrImmobile => _npcOwner is GuardCaster || _npcOwner.MaxSpeedBase == 0;

        public NpcCastingComponent(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
        }

        protected override bool RequestCastSpellInternal(
            Spell spell,
            SpellLine spellLine,
            ISpellCastingAbilityHandler spellCastingAbilityHandler,
            GameLiving target,
            GamePlayer losChecker)
        {
            if (losChecker == null)
                return base.RequestCastSpellInternal(spell, spellLine, spellCastingAbilityHandler, target, null);

            SpellWaitingForLosCheck spellWaitingForLosCheck = new(spell, spellLine);

            lock (_spellsWaitingForLosCheckLock)
            {
                if (_spellsWaitingForLosCheck.TryGetValue(target, out var list))
                    list.Add(spellWaitingForLosCheck);
                else
                    _spellsWaitingForLosCheck[target] = [spellWaitingForLosCheck];
            }

            losChecker.Out.SendLosCheckRequest(_npcOwner, target, this);
            return true; // Consider the NPC is casting while waiting for the reply to prevent it from moving.
        }

        protected override GamePlayer GetLosChecker(GameLiving target)
        {
            return _npcOwner.Brain.GetLosChecker(target);
        }

        public override void OnSpellCast(Spell spell)
        {
            if (!spell.IsHarmful || !spell.IsInstantCast)
                return;

            _npcOwner.ApplyInstantHarmfulSpellDelay();
        }

        public override void ClearSpellHandlers()
        {
            // Make sure NPCs don't start casting pending spells after being told to stop.
            lock (_spellsWaitingForLosCheckLock)
            {
                _spellsWaitingForLosCheck.Clear();
            }

            // Don't clear the attack spell queue here.
            if (_npcOwner.Brain is NecromancerPetBrain necromancerPetBrain)
                necromancerPetBrain.ClearSpellQueue();

            base.ClearSpellHandlers();
        }

        public override void OnOutOfRangeOrNoLos(GameObject target)
        {
            if (QueuedSpellHandler?.Target == target)
                ClearUpQueuedSpellHandler();

            // Immobile NPCs and caster guards forget about the target, other NPCs will try to move into range and line of sight.
            if (IsCasterGuardOrImmobile)
                (_npcOwner.Brain as StandardMobBrain)?.RemoveFromAggroList(target as GameLiving);
            else if (_npcOwner.TargetObject == target)
                _npcOwner.Follow(target, _npcOwner.StickMinimumRange, _npcOwner.StickMaximumRange);
        }

        public void HandleLosCheckResponse(GamePlayer losChecker, LosCheckResponse response, ushort targetId)
        {
            GameObject target = _npcOwner.CurrentRegion.GetObject(targetId);

            if (target == null)
                return;

            lock (_spellsWaitingForLosCheckLock)
            {
                if (!_spellsWaitingForLosCheck.TryGetValue(target, out var list))
                    return;

                if (response is LosCheckResponse.True)
                {
                    foreach (SpellWaitingForLosCheck spellWaitingForLosCheck in list)
                    {
                        Spell spell = spellWaitingForLosCheck.Spell;
                        SpellLine spellLine = spellWaitingForLosCheck.SpellLine;

                        if (spellLine != null && spell != null)
                            base.RequestCastSpellInternal(spell, spellLine, null, target as GameLiving, losChecker);
                    }

                    return;
                }

                list.Clear();
                OnOutOfRangeOrNoLos(target);

                if (_npcOwner is NecromancerPet necromancerPet && necromancerPet.Owner is GamePlayer playerOwner)
                {
                    string message = LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "AI.Brain.Necromancer.PetCantSeeTarget", _npcOwner.Name);
                    NecromancerPetBrain.MessageToOwner(message, eChatType.CT_SpellResisted, playerOwner);
                }
            }
        }

        private readonly record struct SpellWaitingForLosCheck(Spell Spell, SpellLine SpellLine);
    }
}
