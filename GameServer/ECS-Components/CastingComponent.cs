using System;
using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
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
        public virtual SpellHandler QueuedSpellHandler { get; protected set; }

        public ServiceObjectId ServiceObjectId { get; } = new(ServiceObjectType.CastingComponent);
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
                Stop();
                return;
            }

            // Only process up to count per tick to avoid infinite loops caused by some scripted NPCs able to call CastSpell recursively.
            int count = _startSkillRequests.Count;

            while (count-- > 0 && _startSkillRequests.TryDequeue(out StartSkillRequest startSkillRequest))
            {
                startSkillRequest.StartSkill();
                startSkillRequest.ResetAndReturn();
            }

            if (SpellHandler != null)
            {
                SpellHandler.Tick();

                if (SpellHandler?.CastState is eCastState.Casting or eCastState.CastingRetry)
                    StartDuringCastLosCheck();
            }

            if (SpellHandler == null && QueuedSpellHandler == null && _startSkillRequests.Count == 0)
                Stop();
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

        private void StartDuringCastLosCheck()
        {
            if (_duringCastLosCheckListener.IsAlive)
            {
                if (_duringCastLosCheckListener.SpellHandler == SpellHandler)
                    return;

                _duringCastLosCheckListener.StopAndClear();
            }

            bool checkLos = false;

            if (Owner is GameNPC)
                checkLos = Properties.CHECK_LOS_DURING_NPC_CAST;
            else if (Owner is GamePlayer)
                checkLos = Properties.CHECK_LOS_DURING_PLAYER_CAST;

            if (!checkLos || SpellHandler.LosChecker == null)
            {
                SpellHandler.HasLos = true;
                return;
            }

            GameLiving target = SpellHandler.Target;

            if (target == null || target == Owner)
            {
                SpellHandler.HasLos = true;
                return;
            }

            _duringCastLosCheckListener.SpellHandler = SpellHandler;
            _duringCastLosCheckListener.Start();
        }

        public bool StartEndOfCastLosCheck(GameLiving target, SpellHandler spellHandler)
        {
            if (SpellHandler.LosChecker == null || target == null || target == Owner)
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

        public virtual void OnOutOfRangeOrNoLos(GameObject target) { }

        public void ClearQueuedSpellHandler()
        {
            QueuedSpellHandler = null;
        }

        public virtual void OnSpellCast(Spell spell) { }

        public void PromoteQueuedSpellHandler()
        {
            if (Owner is NecromancerPet necroPet && necroPet.Brain is NecromancerPetBrain necroBrain)
                necroBrain.CheckAttackSpellQueue();

            _duringCastLosCheckListener.StopAndClear();

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
            return !Owner.IsCrowdControlled && !Owner.IsSilenced;
        }

        protected virtual void Stop()
        {
            ServiceObjectStore.Remove(this);
            _duringCastLosCheckListener.StopAndClear();
            SpellHandler = null;
            QueuedSpellHandler = null;
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

        public bool CheckCooldown(Spell spell)
        {
            int cooldown = Owner.GetSkillDisabledDuration(spell);

            if (cooldown <= 0)
                return true;

            // Live behavior as of 1.127:
            // Client side cooldown: "You must wait ... to recast this type of spell." (spell resisted, system window).
            // The duration is sent by SendDisableSkill. A X seconds cooldown will show exactly X on the first couple of recast attempts (probably rounded to the nearest).
            // More importantly, it ends about a second too early, at which point the server side cooldown takes relay.
            // This can causes mismatches during server lags, but this is how it's supposed to work (I suspect it helps against latency a bit).
            // Server side cooldown: "You must wait ... to recast this type of spell!" (spell resisted, system window).
            (Owner as GamePlayer)?.Out.SendMessage($"You must wait {FormatCooldown(cooldown)} to recast this type of spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
            return false;
        }

        public bool CheckCooldown(Ability ability)
        {
            int cooldown = Owner.GetSkillDisabledDuration(ability);

            if (cooldown <= 0)
                return true;

            // Live behavior as of 1.127:
            // No client side cooldown.
            // Server side cooldown: "You must wait ... to use this ability." (system, system window).
            (Owner as GamePlayer)?.Out.SendMessage($"You must wait {FormatCooldown(cooldown)} to use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        private static string FormatCooldown(int durationMs)
        {
            return Util.FormatSeconds(durationMs / 1000 + 1);
        }

        public class CastSpellRequest : StartSkillRequest
        {
            private Spell _spell;
            private SpellLine _spellLine;
            private ISpellCastingAbilityHandler _spellCastingAbilityHandler;
            private GameLiving _target;
            private GamePlayer _losChecker; // Only used by NPCs.

            public void Init(CastingComponent castingComponent, Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler, GameLiving target, GamePlayer losChecker)
            {
                Init(castingComponent);
                _spell = spell;
                _spellLine = spellLine;
                _spellCastingAbilityHandler = spellCastingAbilityHandler;
                _target = target;
                _losChecker = losChecker;
            }

            public override void ResetAndReturn()
            {
                _spell = null;
                _spellLine = null;
                _spellCastingAbilityHandler = null;
                _target = null;
                _losChecker = null;
                CastingComponent.ReturnToPool(this);
                base.ResetAndReturn();
            }

            public override void StartSkill()
            {
                // Cancel pulsing spell if already active.
                if (_spell.IsPulsing && CastingComponent.Owner.ActivePulseSpells.ContainsKey(_spell.SpellType))
                {
                    ECSPulseEffect effect = EffectListService.GetPulseEffectOnTarget(CastingComponent.Owner, _spell);

                    if (effect != null)
                    {
                        if (effect.End() && CastingComponent.Owner is GamePlayer player)
                        {
                            if (_spell.InstrumentRequirement == 0)
                                player.Out.SendMessage("You cancel your effect.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                            else
                                player.Out.SendMessage("You stop playing your song.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                        }

                        return;
                    }
                }

                SpellHandler newSpellHandler = ScriptMgr.CreateSpellHandler(CastingComponent.Owner, _spell, _spellLine) as SpellHandler;
                newSpellHandler.Ability = _spellCastingAbilityHandler;
                newSpellHandler.Target = _target;
                newSpellHandler.LosChecker = _losChecker;
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

                        // Handle songs.
                        if (newSpell.CastTime > 0 && currentSpell.InstrumentRequirement != 0)
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
                if (CastingComponent.Owner is not GamePlayer player || !CastingComponent.CheckCooldown(Ability))
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

        private class DuringCastLosCheckListener : ECSGameTimerWrapperBase, ILosCheckListener
        {
            public SpellHandler SpellHandler { get; set; }

            public DuringCastLosCheckListener(CastingComponent castingComponent) : base(castingComponent.Owner)
            {
                Interval = ServerProperties.Properties.CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL;
            }

            public void HandleLosCheckResponse(GamePlayer player, LosCheckResponse response, ushort targetId)
            {
                if (SpellHandler == null)
                    return;

                SpellHandler.HasLos = response is LosCheckResponse.True;
            }

            public void StopAndClear()
            {
                Stop();
                SpellHandler = null;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (SpellHandler == null)
                    return 0;

                SpellHandler.LosChecker.Out.SendLosCheckRequest(Owner, SpellHandler.Target, this);
                return Interval;
            }
        }

        // Currently unused, most likely outdated.
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
