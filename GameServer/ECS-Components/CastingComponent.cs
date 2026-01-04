using System;
using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using DOL.Logging;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class CastingComponent : IServiceObject
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string ALREADY_CASTING_MESSAGE = "You are already casting a spell!";
        private const int NO_QUEUE_INPUT_BUFFER = 250; // 250ms is roughly equivalent to the delay between inputs imposed by the client.

        private readonly Queue<StartSkillRequest> _startSkillRequests = new(); // This isn't the actual spell queue. Also contains abilities.
        private readonly Queue<CastSpellRequest> _castSpellRequestPool = new();
        private readonly Queue<UseAbilityRequest> _useAbilityRequestPool = new();
        private readonly Lock _startSkillRequestsLock = new();
        private readonly DuringCastLosCheckListener _duringCastLosCheckListener;
        private readonly EndOfCastLosCheckListener _endOfCastLosCheckListener;

        public GameLiving Owner { get; }
        public SpellHandler SpellHandler { get; protected set; }
        public SpellHandler QueuedSpellHandler { get; private set; }
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.CastingComponent);
        public bool IsCasting => SpellHandler != null; // May not be actually casting yet.

        protected CastingComponent(GameLiving owner)
        {
            Owner = owner;
            _duringCastLosCheckListener = new(this);
            _endOfCastLosCheckListener = new(this);
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
            if (Owner.ObjectState is not eObjectState.Active)
            {
                ServiceObjectStore.Remove(this);
                return;
            }

            SpellHandler?.Tick();

            // Only process up to count per tick to avoid infinite loops caused by some scripted NPCs able to call CastSpell recursively.
            int count = _startSkillRequests.Count;

            while (count-- > 0 && _startSkillRequests.TryDequeue(out StartSkillRequest startSkillRequest))
            {
                startSkillRequest.StartSkill();
                startSkillRequest.ResetAndReturn();
            }

            if (SpellHandler == null && QueuedSpellHandler == null && _startSkillRequests.Count == 0)
                ServiceObjectStore.Remove(this);
        }

        public bool RequestCastSpell(
            Spell spell,
            SpellLine spellLine,
            ISpellCastingAbilityHandler spellCastingAbilityHandler = null,
            GameLiving target = null, // Always null for players.
            bool checkLos = true)
        {
            GamePlayer losChecker = checkLos && spell.RequiresLosCheck() ? GetLosChecker(target) : null;
            return RequestCastSpellInternal(spell, spellLine, spellCastingAbilityHandler, target, losChecker);
        }

        protected virtual bool RequestCastSpellInternal(
            Spell spell,
            SpellLine spellLine,
            ISpellCastingAbilityHandler spellCastingAbilityHandler,
            GameLiving target,
            GamePlayer losChecker)
        {
            if (Owner.IsIncapacitated)
                Owner.Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));

            if (!CanCastSpell())
                return false;

            lock (_startSkillRequestsLock)
            {
                if (!_castSpellRequestPool.TryDequeue(out CastSpellRequest request))
                    request = new();

                request.Init(this, spell, spellLine, spellCastingAbilityHandler, target, losChecker);
                _startSkillRequests.Enqueue(request);
            }

            ServiceObjectStore.Add(this);
            return true;
        }

        protected virtual GamePlayer GetLosChecker(GameLiving target)
        {
            return null;
        }

        public bool StartDuringCastLosCheck(GameLiving target)
        {
            // Target check may appear redundant since CastingComponent is supposed to set LosChecker to null when no LoS check is required.
            // But for player casted spells, the target is determined by SpellHandler.
            if (SpellHandler.LosChecker == null || target == Owner)
                return false;

            _duringCastLosCheckListener.SpellHandler = SpellHandler;
            SpellHandler.LosChecker.Out.SendLosCheckRequest(Owner, target, _duringCastLosCheckListener);
            return true;
        }

        public bool StartEndOfCastLosCheck(GameLiving target, SpellHandler spellHandler)
        {
            if (SpellHandler.LosChecker == null || target == Owner)
                return false;

            _endOfCastLosCheckListener.AddPendingLosCheck(target, spellHandler);
            SpellHandler.LosChecker.Out.SendLosCheckRequest(Owner, target, _endOfCastLosCheckListener);
            return true;
        }

        public void RequestUseAbility(Ability ability)
        {
            // Always allowed. The handler will check if the ability can be used or not.
            lock (_startSkillRequestsLock)
            {
                if (!_useAbilityRequestPool.TryDequeue(out UseAbilityRequest startUseAbilityRequest))
                    startUseAbilityRequest = new();

                startUseAbilityRequest.Init(this, ability);
                _startSkillRequests.Enqueue(startUseAbilityRequest);
            }

            ServiceObjectStore.Add(this);
        }

        public int CalculateSpellRange(Spell spell)
        {
            const int minRange = 32;
            return spell == null ? minRange : Math.Max(minRange, (int) (spell.Range * Owner.GetModified(eProperty.SpellRange) * 0.01));
        }

        public void InterruptCasting(bool moving)
        {
            // A race condition can happen here.
            SpellHandler spellHandler = SpellHandler;

            if (spellHandler != null)
            {
                if (spellHandler.IsInCastingPhase)
                {
                    foreach (GamePlayer player in Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendInterruptAnimation(Owner);
                }

                // Only send a spell cancel message if we're not cancelling a focus spell (already handled by `CancelFocusSpells`).
                if (!CancelFocusSpells(moving))
                    SendSpellCancelMessage(moving, false);
            }

            ClearSpellHandlers();
        }

        public bool CancelFocusSpells(bool moving)
        {
            SpellHandler spellHandler = SpellHandler;

            if (spellHandler == null || !spellHandler.Spell.IsFocus)
                return false;

            spellHandler.CancelFocusSpells();
            SendSpellCancelMessage(moving, true);
            return true;
        }

        protected virtual void SendSpellCancelMessage(bool moving, bool focusSpell) { }

        public virtual void ClearSpellHandlers()
        {
            QueuedSpellHandler = null;
            SpellHandler = null;
        }

        public void ClearUpQueuedSpellHandler()
        {
            QueuedSpellHandler = null;
        }

        public virtual void OnSpellCast(Spell spell) { }

        public void PromoteQueuedSpellHandler()
        {
            if (Owner is NecromancerPet necroPet && necroPet.Brain is NecromancerPetBrain necroBrain)
                necroBrain.CheckAttackSpellQueue();

            if (QueuedSpellHandler != null)
            {
                SpellHandler = QueuedSpellHandler;
                QueuedSpellHandler = null;
            }
            else
                SpellHandler = null;
        }

        protected virtual bool CanCastSpell()
        {
            return !Owner.IsStunned && !Owner.IsMezzed && !Owner.IsSilenced;
        }

        public void ReturnToPool(CastSpellRequest request)
        {
            // A few classes can cast many instant spells simultaneously.
            if (_castSpellRequestPool.Count > 10)
                return;

            _castSpellRequestPool.Enqueue(request);
        }

        public void ReturnToPool(UseAbilityRequest request)
        {
            if (_useAbilityRequestPool.Count > 2)
                return;

            _useAbilityRequestPool.Enqueue(request);
        }

        public class CastSpellRequest : StartSkillRequest
        {
            public Spell Spell { get; private set; }
            public SpellLine SpellLine { get; private set ; }
            public ISpellCastingAbilityHandler SpellCastingAbilityHandler { get; private set; }
            public GameLiving Target { get; private set; }
            public GamePlayer LosChecker { get; private set; } // Only used by NPCs.

            public void Init(CastingComponent castingComponent, Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler, GameLiving target, GamePlayer losChecker)
            {
                Init(castingComponent);
                Spell = spell;
                SpellLine = spellLine;
                SpellCastingAbilityHandler = spellCastingAbilityHandler;
                Target = target;
                LosChecker = losChecker;
            }

            public override void ResetAndReturn()
            {
                Spell = null;
                SpellLine = null;
                SpellCastingAbilityHandler = null;
                Target = null;
                LosChecker = null;
                CastingComponent.ReturnToPool(this);
                base.ResetAndReturn();
            }

            public override void StartSkill()
            {
                SpellHandler newSpellHandler = CreateSpellHandler();
                Spell newSpell = newSpellHandler.Spell;

                SpellHandler currentSpellHandler = CastingComponent.SpellHandler;
                Spell currentSpell = currentSpellHandler?.Spell;

                if (currentSpellHandler != null)
                {
                    if (newSpell.IsInstantCast)
                        newSpellHandler.Tick();
                    else if (currentSpell != null)
                    {
                        if (CastingComponent.Owner is not GamePlayer player)
                        {
                            CastingComponent.QueuedSpellHandler = newSpellHandler;
                            return;
                        }

                        if (newSpell.CastTime > 0 && currentSpell.InstrumentRequirement != 0)
                        {
                            HandleSong(player);
                            return;
                        }

                        // Focus spells aren't allowed to have any spell be queued after them.
                        if (currentSpell.IsFocus)
                        {
                            if (currentSpellHandler.CastState is eCastState.Focusing)
                                CastingComponent.SpellHandler = newSpellHandler;
                            else
                                player.Out.SendMessage(ALREADY_CASTING_MESSAGE, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);

                            return;
                        }

                        if (player.SpellQueue)
                        {
                            player.Out.SendMessage($"{ALREADY_CASTING_MESSAGE} You prepare this spell as a follow up!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                            CastingComponent.QueuedSpellHandler = newSpellHandler;
                        }
                        else if (currentSpellHandler.IsInCastingPhase && currentSpellHandler.IsCastEndingSoon(NO_QUEUE_INPUT_BUFFER))
                            CastingComponent.QueuedSpellHandler = newSpellHandler; // Spell queue is disabled. Silently queue the spell.
                        else
                            player.Out.SendMessage(ALREADY_CASTING_MESSAGE, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
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
                    spellHandler.Target = Target;
                    spellHandler.LosChecker = LosChecker;
                    spellHandler.Ability = SpellCastingAbilityHandler;
                    return spellHandler;
                }

                void HandleSong(GamePlayer player)
                {
                    // Since flute mez is allowed to effectively stay in a casting state even after losing LoS for example, we allow the player to cast other songs here.
                    // Otherwise the only way to cancel an out of LoS / range flute mez is to swap weapons.
                    if (currentSpellHandler.CastState is eCastState.CastingRetry)
                    {
                        CastingComponent.InterruptCasting(false);

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

        public class UseAbilityRequest : StartSkillRequest
        {
            public Ability Ability { get; private set; }

            public void Init(CastingComponent castingComponent, Ability ability)
            {
                Init(castingComponent);
                Ability = ability;
            }

            public override void ResetAndReturn()
            {
                Ability = null;
                CastingComponent.ReturnToPool(this);
                base.ResetAndReturn();
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
            protected CastingComponent CastingComponent { get; private set; }

            public void Init(CastingComponent castingComponent)
            {
                CastingComponent = castingComponent;
            }

            public virtual void ResetAndReturn()
            {
                CastingComponent = null;
            }

            public virtual void StartSkill() { }
        }

        private class DuringCastLosCheckListener : ILosCheckListener
        {
            private CastingComponent _castingComponent;

            public SpellHandler SpellHandler { get; set; }

            public DuringCastLosCheckListener(CastingComponent castingComponent)
            {
                _castingComponent = castingComponent;
            }

            public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
            {
                // Ensure the spell handler is still relevant.
                if (SpellHandler == null || SpellHandler != _castingComponent.SpellHandler)
                    return;

                // Let the spell handler handle the response on its next tick.
                SpellHandler.HasLos = response is LosCheckResponse.True;
            }
        }

        private class EndOfCastLosCheckListener : ILosCheckListener
        {
            private CastingComponent _castingComponent;
            private Dictionary<ushort, List<SpellHandler>> _pendingLosChecks;

            public EndOfCastLosCheckListener(CastingComponent castingComponent)
            {
                _castingComponent = castingComponent;
            }

            public void AddPendingLosCheck(GameLiving target, SpellHandler spellHandler)
            {
                _pendingLosChecks ??= new();

                if (_pendingLosChecks.TryGetValue(target.ObjectID, out var list))
                    list.Add(spellHandler);
                else
                    _pendingLosChecks[target.ObjectID] = [spellHandler]; // Consider pooling if end of cast LoS checks become common.
            }

            public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
            {
                if (_pendingLosChecks == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(EndOfCastLosCheckListener)} encountered null {nameof(_pendingLosChecks)}");

                    return;
                }

                if (!_pendingLosChecks.Remove(targetId, out var spellHandlers))
                    return;

                if (_castingComponent.Owner.CurrentRegion.GetObject(targetId) is not GameLiving target)
                    return;

                foreach (SpellHandler spellHandler in spellHandlers)
                {
                    if (spellHandler.CastState is not eCastState.Finished)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"{nameof(EndOfCastLosCheckListener)} received LoS response for spell handler not in {nameof(eCastState.Finished)} state. (Spell handler: {spellHandler})");

                        continue;
                    }

                    spellHandler.OnEndOfCastLosCheck(target, response);
                }
            }
        }
    }
}
