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
    public class CastingComponent : IManagedEntity
    {
        protected ConcurrentQueue<StartSkillRequest> _startSkillRequests = new(); // This isn't the actual spell queue. Also contains abilities.

        public GameLiving Owner { get; }
        public SpellHandler SpellHandler { get; protected set; }
        public SpellHandler QueuedSpellHandler { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.CastingComponent, false);
        public bool IsCasting => SpellHandler != null; // May not be actually casting yet.

        protected CastingComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public static CastingComponent Create(GameLiving living)
        {
            if (living is GameNPC npc)
                return new NpcCastingComponent(npc);
            else if (living is GamePlayer player)
                return new PlayerCastingComponent(player);
            else
                return new CastingComponent(living);
        }

        public void Tick()
        {
            SpellHandler?.Tick();
            ProcessStartSkillRequests();

            if (SpellHandler == null && QueuedSpellHandler == null && _startSkillRequests.IsEmpty)
                EntityManager.Remove(this);
        }

        public virtual bool RequestStartCastSpell(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (RequestStartCastSpellInternal(new StartCastSpellRequest(this, spell, spellLine, spellCastingAbilityHandler, target)))
            {
                EntityManager.Add(this);
                return true;
            }

            return false;
        }

        protected bool RequestStartCastSpellInternal(StartCastSpellRequest startCastSpellRequest)
        {
            if (Owner.IsIncapacitated)
                Owner.Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));

            if (!CanCastSpell())
                return false;

            _startSkillRequests.Enqueue(startCastSpellRequest);
            return true;
        }

        public virtual void RequestStartUseAbility(Ability ability)
        {
            // Always allowed. The handler will check if the ability can be used or not.
            _startSkillRequests.Enqueue(new StartUseAbilityRequest(this, ability));
            EntityManager.Add(this);
        }

        protected virtual void ProcessStartSkillRequests()
        {
            while (_startSkillRequests.TryDequeue(out StartSkillRequest startSkillRequest))
                startSkillRequest.StartSkill();
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

        public void CancelFocusSpells(bool moving)
        {
            SpellHandler?.CancelFocusSpells(moving);
        }

        public virtual void ClearUpSpellHandlers()
        {
            QueuedSpellHandler = null;
            SpellHandler = null;
        }

        public void ClearUpQueuedSpellHandler()
        {
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

        public class StartCastSpellRequest : StartSkillRequest
        {
            public Spell Spell { get; }
            public SpellLine SpellLine { get; private set ; }
            public ISpellCastingAbilityHandler SpellCastingAbilityHandler { get; }
            public GameLiving Target { get; }

            public StartCastSpellRequest(CastingComponent castingComponent, Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler, GameLiving target) : base(castingComponent)
            {
                Spell = spell;
                SpellLine = spellLine;
                SpellCastingAbilityHandler = spellCastingAbilityHandler;
                Target = target;
            }

            public override void StartSkill()
            {
                SpellHandler newSpellHandler = CreateSpellHandler();
                Spell newSpell = newSpellHandler.Spell;

                SpellHandler currentSpellHandler = CastingComponent.SpellHandler;
                Spell currentSpell = currentSpellHandler?.Spell;

                if (currentSpellHandler != null)
                {
                    if (currentSpell?.IsFocus == true)
                    {
                        if (newSpell.IsInstantCast)
                            newSpellHandler.Tick();
                        else
                        {
                            CastingComponent.SpellHandler = newSpellHandler;
                            newSpellHandler.Tick();
                        }
                    }
                    else if (newSpell.IsInstantCast)
                        newSpellHandler.Tick();
                    else
                    {
                        GamePlayer player = CastingComponent.Owner as GamePlayer;

                        if (newSpell.CastTime > 0 && currentSpell.InstrumentRequirement != 0)
                        {
                            HandleSong(player);
                            return;
                        }

                        if (player == null)
                            CastingComponent.QueuedSpellHandler = newSpellHandler;
                        else if (player.SpellQueue)
                        {
                            player.Out.SendMessage("You are already casting a spell! You prepare this spell as a follow up!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                            CastingComponent.QueuedSpellHandler = newSpellHandler;
                        }
                        else
                            player.Out.SendMessage("You are already casting a spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }
                }
                else
                {
                    if (newSpell.IsInstantCast)
                        newSpellHandler.Tick();
                    else
                    {
                        CastingComponent.SpellHandler = newSpellHandler;
                        newSpellHandler.Tick();
                    }
                }

                SpellHandler CreateSpellHandler()
                {
                    SpellHandler spellHandler = ScriptMgr.CreateSpellHandler(CastingComponent.Owner, Spell, SpellLine) as SpellHandler;

                    // 'GameLiving.TargetObject' is used by 'SpellHandler.Tick()' but is likely to change during LoS checks or for queued spells (affects NPCs only).
                    // So we pre-initialize 'SpellHandler.Target' with the passed down target, if there's any.
                    if (Target != null)
                        spellHandler.Target = Target;

                    // Abilities that cast spells (i.e. Realm Abilities such as Volcanic Pillar) need to set this so the associated ability gets disabled if the cast is successful.
                    spellHandler.Ability = SpellCastingAbilityHandler;
                    return spellHandler;
                }

                void HandleSong(GamePlayer player)
                {
                    // Since flute mez is allowed to effectively stay in a casting state even after losing LoS for example, we allow the player to cast other songs here.
                    // Otherwise the only way to cancel an out of LoS / range flute mez is to swap weapons.
                    if (currentSpellHandler.CastState is eCastState.CastingRetry)
                    {
                        currentSpellHandler.InterruptCasting();

                        if (newSpell.SpellType is eSpellType.Mesmerize && newSpell.InstrumentRequirement != 0)
                        {
                            currentSpellHandler.MessageToCaster("You stop playing your song.", eChatType.CT_Spell);
                            return;
                        }

                        // Not very elegant, but we need to do something with our new spell now that we've cancelled the flute mez.
                        if (CastingComponent.SpellHandler == null)
                            StartSkill();

                        return;
                    }

                    if (player != null)
                    {
                        if (newSpell.InstrumentRequirement != 0)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.CastSpell.AlreadyPlaySong"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                        else
                            player.Out.SendMessage($"You must wait {(currentSpellHandler.CastStartTick + currentSpell.CastTime - GameLoop.GameLoopTime) / 1000 + 1} seconds to cast a spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }

                    return;
                }
            }
        }

        public class StartUseAbilityRequest : StartSkillRequest
        {
            public Ability Ability { get; }

            public StartUseAbilityRequest(CastingComponent castingComponent, Ability ability) : base(castingComponent)
            {
                Ability = ability;
            }

            public override void StartSkill()
            {
                // Only players are currently supported.
                if (CastingComponent.Owner is not GamePlayer player)
                    return;

                IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(Ability.KeyName);

                if (handler != null)
                    handler.Execute(Ability, player);
                else
                    Ability.Execute(player);
            }
        }

        public abstract class StartSkillRequest
        {
            protected CastingComponent CastingComponent { get; }

            public StartSkillRequest(CastingComponent castingComponent)
            {
                CastingComponent = castingComponent;
            }

            public virtual void StartSkill() { }
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
