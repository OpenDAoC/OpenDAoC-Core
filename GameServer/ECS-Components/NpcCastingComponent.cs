using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    public class NpcCastingComponent : CastingComponent, ILosCheckListener
    {
        private readonly GameNPC _npcOwner;
        private readonly Dictionary<GameObject, List<SpellWaitingForLosCheck>> _spellsWaitingForLosCheck = new();
        private readonly Lock _spellsWaitingForLosCheckLock = new();
        private readonly QueuedCastLosCheckListener _queuedCastLosCheckListener;

        public override SpellHandler QueuedSpellHandler
        {
            get => base.QueuedSpellHandler;
            protected set
            {
                base.QueuedSpellHandler = value;

                if (base.QueuedSpellHandler != null)
                    StartQueuedCastLosCheck();
                else
                    _queuedCastLosCheckListener.StopAndClear();
            }
        }

        public GameLiving LastNegativeLosCheckTarget { get; private set; }
        private bool IsCasterGuard => _npcOwner is GuardCaster;

        public NpcCastingComponent(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
            _queuedCastLosCheckListener = new(this);
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

            // Consider the NPC is casting until we know it doesn't have LoS against this target.
            // This prevents it from moving while waiting for a LoS check.
            return LastNegativeLosCheckTarget != target;
        }

        protected override GamePlayer GetLosChecker(GameLiving target)
        {
            return _npcOwner.Brain.GetLosChecker(target);
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

            LastNegativeLosCheckTarget = null;
            base.ClearSpellHandlers();
        }

        public override void OnOutOfRangeOrNoLos(GameObject target)
        {
            if (QueuedSpellHandler?.Target == target)
                ClearQueuedSpellHandler();

            // Caster guards forget about the target.
            if (IsCasterGuard)
            {
                // Keep the target in the aggro list while the NPC is still casting.
                // This ensures that the NPC doesn't enter an idle state, potentially interfering with spell casting.
                if (!_npcOwner.IsCasting)
                    (_npcOwner.Brain as StandardMobBrain)?.RemoveFromAggroList(target as GameLiving);

                return;
            }

            if (_npcOwner.TargetObject == target)
                LastNegativeLosCheckTarget = target as GameLiving;
        }

        public override void OnSpellCast(Spell spell)
        {
            if (!spell.IsHarmful || !spell.IsInstantCast)
                return;

            _npcOwner.ApplyInstantHarmfulSpellDelay();
        }

        protected override void Stop()
        {
            base.Stop();
            _queuedCastLosCheckListener.StopAndClear();
            LastNegativeLosCheckTarget = null;
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
                }
                else
                {
                    OnOutOfRangeOrNoLos(target);

                    if (_npcOwner is NecromancerPet necromancerPet && necromancerPet.Owner is GamePlayer playerOwner)
                    {
                        string message = LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "AI.Brain.Necromancer.PetCantSeeTarget", _npcOwner.Name);
                        NecromancerPetBrain.MessageToOwner(message, eChatType.CT_SpellResisted, playerOwner);
                    }
                }

                list.Clear();
            }
        }

        private void StartQueuedCastLosCheck()
        {
            if (_queuedCastLosCheckListener.IsAlive)
            {
                if (_queuedCastLosCheckListener.QueuedSpellHandler == QueuedSpellHandler)
                    return;

                _queuedCastLosCheckListener.StopAndClear();
            }

            if (QueuedSpellHandler.LosChecker == null)
            {
                QueuedSpellHandler.HasLos = true;
                return;
            }

            GameLiving target = QueuedSpellHandler.Target;

            if (target == null || target == Owner)
            {
                QueuedSpellHandler.HasLos = true;
                return;
            }

            _queuedCastLosCheckListener.QueuedSpellHandler = QueuedSpellHandler;
            _queuedCastLosCheckListener.Start();
        }

        private class QueuedCastLosCheckListener : ECSGameTimerWrapperBase, ILosCheckListener
        {
            public SpellHandler QueuedSpellHandler { get; set; }

            public QueuedCastLosCheckListener(CastingComponent castingComponent) : base(castingComponent.Owner)
            {
                Interval = ServerProperties.Properties.CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL;
            }

            public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
            {
                if (QueuedSpellHandler == null)
                    return;

                QueuedSpellHandler.HasLos = response is LosCheckResponse.True;
            }

            public void StopAndClear()
            {
                Stop();
                QueuedSpellHandler = null;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (QueuedSpellHandler == null)
                    return 0;

                QueuedSpellHandler.LosChecker.Out.SendLosCheckRequest(Owner, QueuedSpellHandler.Target, this);
                return Interval;
            }
        }

        private readonly record struct SpellWaitingForLosCheck(Spell Spell, SpellLine SpellLine);
    }
}
