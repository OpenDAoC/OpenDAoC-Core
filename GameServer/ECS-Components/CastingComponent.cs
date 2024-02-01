using System;
using System.Collections.Concurrent;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    // This component will hold all data related to casting spells.
    public class CastingComponent : IManagedEntity
    {
        private ConcurrentQueue<StartCastSpellRequest> _startCastSpellRequests = new(); // This isn't the actual spell queue.

        public GameLiving Owner { get; private set; }
        public SpellHandler SpellHandler { get; protected set; }
        public SpellHandler QueuedSpellHandler { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.CastingComponent, false);
        public bool IsCasting => SpellHandler != null;

        protected CastingComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public static CastingComponent Create(GameLiving owner)
        {
            if (owner is GamePlayer playerOwner)
                return new PlayerCastingComponent(playerOwner);
            else
                return new CastingComponent(owner);
        }

        public void Tick()
        {
            SpellHandler?.Tick();
            ProcessStartCastSpellRequests();

            if (SpellHandler == null && QueuedSpellHandler == null && _startCastSpellRequests.IsEmpty)
                EntityManager.Remove(this);
        }

        public virtual bool RequestStartCastSpell(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (RequestStartCastSpellInternal(new StartCastSpellRequest(spell, spellLine, spellCastingAbilityHandler, target)))
            {
                EntityManager.Add(this);
                return true;
            }

            return false;
        }

        protected bool RequestStartCastSpellInternal(StartCastSpellRequest startCastSpellRequest)
        {
            if (Owner.IsStunned || Owner.IsMezzed)
                Owner.Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));

            if (!CanCastSpell())
                return false;

            _startCastSpellRequests.Enqueue(startCastSpellRequest);
            return true;
        }

        protected virtual void ProcessStartCastSpellRequests()
        {
            while (_startCastSpellRequests.TryDequeue(out StartCastSpellRequest startCastSpellRequest))
                StartCastSpell(startCastSpellRequest);
        }

        protected SpellHandler CreateSpellHandler(StartCastSpellRequest startCastSpellRequest)
        {
            SpellHandler spellHandler = ScriptMgr.CreateSpellHandler(Owner, startCastSpellRequest.Spell, startCastSpellRequest.SpellLine) as SpellHandler;

            // 'GameLiving.TargetObject' is used by 'SpellHandler.Tick()' but is likely to change during LoS checks or for queued spells (affects NPCs only).
            // So we pre-initialize 'SpellHandler.Target' with the passed down target, if there's any.
            if (startCastSpellRequest.Target != null)
                spellHandler.Target = startCastSpellRequest.Target;

            // Abilities that cast spells (i.e. Realm Abilities such as Volcanic Pillar) need to set this so the associated ability gets disabled if the cast is successful.
            spellHandler.Ability = startCastSpellRequest.SpellCastingAbilityHandler;
            return spellHandler;
        }

        protected virtual void StartCastSpell(StartCastSpellRequest startCastSpellRequest)
        {
            SpellHandler newSpellHandler = CreateSpellHandler(startCastSpellRequest);

            if (SpellHandler != null)
            {
                if (SpellHandler.Spell?.IsFocus == true)
                {
                    if (newSpellHandler.Spell.IsInstantCast)
                        newSpellHandler.Tick();
                    else
                    {
                        SpellHandler = newSpellHandler;
                        SpellHandler.Tick();
                    }
                }
                else if (newSpellHandler.Spell.IsInstantCast)
                    newSpellHandler.Tick();
                else
                {
                    if (Owner is GamePlayer player)
                    {
                        if (newSpellHandler.Spell.CastTime > 0 && SpellHandler is not ChamberSpellHandler && newSpellHandler.Spell.SpellType != eSpellType.Chamber)
                        {
                            if (SpellHandler.Spell.InstrumentRequirement != 0)
                            {
                                if (newSpellHandler.Spell.InstrumentRequirement != 0)
                                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.CastSpell.AlreadyPlaySong"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                                else
                                    player.Out.SendMessage("You must wait " + ((SpellHandler.CastStartTick + SpellHandler.Spell.CastTime - GameLoop.GameLoopTime) / 1000 + 1).ToString() + " seconds to cast a spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);

                                return;
                            }
                        }

                        if (player.SpellQueue)
                        {
                            player.Out.SendMessage("You are already casting a spell! You prepare this spell as a follow up!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                            QueuedSpellHandler = newSpellHandler;
                        }
                        else
                            player.Out.SendMessage("You are already casting a spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                    else if (Owner is GameNPC npcOwner && npcOwner.Brain is IControlledBrain)
                        QueuedSpellHandler = newSpellHandler;
                }
            }
            else
            {
                if (newSpellHandler.Spell.IsInstantCast)
                    newSpellHandler.Tick();
                else
                {
                    SpellHandler = newSpellHandler;
                    SpellHandler.Tick();
                }
            }
        }

        public void InterruptCasting()
        {
            if (SpellHandler?.IsInCastingPhase == true)
            {
                foreach (GamePlayer player in Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendInterruptAnimation(Owner);
            }

            ClearUpSpellHandlers();
        }

        public void ClearUpSpellHandlers()
        {
            SpellHandler = null;
            QueuedSpellHandler = null;
        }

        public void OnSpellHandlerCleanUp(Spell currentSpell)
        {
            if (Owner is GamePlayer player)
            {
                if (currentSpell.CastTime > 0)
                {
                    if (QueuedSpellHandler != null && player.SpellQueue)
                    {
                        SpellHandler = QueuedSpellHandler;
                        QueuedSpellHandler = null;
                    }
                    else
                        SpellHandler = null;
                }
            }
            else if (Owner is NecromancerPet necroPet)
            {
                if (necroPet.Brain is NecromancerPetBrain necroBrain)
                {
                    if (currentSpell.CastTime > 0)
                    {
                        if (!Owner.attackComponent.AttackState)
                            necroBrain.CheckAttackSpellQueue();

                        if (QueuedSpellHandler != null)
                        {
                            SpellHandler = QueuedSpellHandler;
                            QueuedSpellHandler = null;
                        }
                        else
                            SpellHandler = null;

                        if (necroBrain.HasSpellsQueued())
                            necroBrain.CheckSpellQueue();
                    }
                }
            }
            else
            {
                if (QueuedSpellHandler != null)
                {
                    SpellHandler = QueuedSpellHandler;
                    QueuedSpellHandler = null;
                }
                else
                    SpellHandler = null;
            }
        }

        protected virtual bool CanCastSpell()
        {
            return !Owner.IsStunned && !Owner.IsMezzed && !Owner.IsSilenced;
        }

        public class StartCastSpellRequest
        {
            public Spell Spell { get; private set; }
            public SpellLine SpellLine { get; private set ; }
            public ISpellCastingAbilityHandler SpellCastingAbilityHandler { get; private set; }
            public GameLiving Target { get; private set; }

            public StartCastSpellRequest(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler, GameLiving target)
            {
                Spell = spell;
                SpellLine = spellLine;
                SpellCastingAbilityHandler = spellCastingAbilityHandler;
                Target = target;
            }
        }

        public class ChainedSpell : ChainedAction<Func<StartCastSpellRequest, bool>>
        {
            private StartCastSpellRequest _startCastSpellRequest;
            public override Skill Skill => Spell;
            public Spell Spell => _startCastSpellRequest.Spell;
            public SpellLine SpellLine => _startCastSpellRequest.SpellLine;

            public ChainedSpell(StartCastSpellRequest startCastSpellRequest, GamePlayer player) : base(player.castingComponent.RequestStartCastSpellInternal)
            {
                _startCastSpellRequest = startCastSpellRequest;
            }

            public override void Execute()
            {
                Handler.Invoke(_startCastSpellRequest);
            }
        }
    }
}
