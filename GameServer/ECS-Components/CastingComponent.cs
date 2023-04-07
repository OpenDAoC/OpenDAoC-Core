using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    // This component will hold all data related to casting spells.
    public class CastingComponent
    {
        private class StartCastSpellRequest
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

        public ISpellHandler SpellHandler { get; private set; }
        public ISpellHandler QueuedSpellHandler { get; private set; }
        public GameLiving Owner { get; private set; }
        public int EntityManagerId { get; private set; } = EntityManager.UNSET_ID;
        private ConcurrentQueue<StartCastSpellRequest> _startCastSpellRequests = new(); // This isn't the actual spell queue.

        public CastingComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick(long time)
        {
            StartCastSpellRequest startCastSpellRequest = null;

            // Retrieve the first spell that was requested if we don't have a currently active spell handler, otherwise take the last one and discard the others.
            if (SpellHandler == null)
                _startCastSpellRequests.TryDequeue(out startCastSpellRequest);
            else
            {
                while (_startCastSpellRequests.TryDequeue(out StartCastSpellRequest result))
                {
                    if (result != null)
                        startCastSpellRequest = result;
                }
            }

            if (startCastSpellRequest != null)
                StartCastSpell(startCastSpellRequest);

            SpellHandler?.Tick(time);

            if (SpellHandler == null && QueuedSpellHandler == null)
            {
                _startCastSpellRequests.Clear();
                EntityManagerId = EntityManager.Remove(EntityManager.EntityType.CastingComponent, EntityManagerId);
            }
        }

        public bool RequestStartCastSpell(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (Owner.IsStunned || Owner.IsMezzed)
            {
                Owner.Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));
                return false;
            }

            if (Owner is GamePlayer playerOwner)
            {
                if (!CanCastSpell(playerOwner))
                    return false;
            }

            _startCastSpellRequests.Enqueue(new StartCastSpellRequest(spell, spellLine, spellCastingAbilityHandler, target));

            if (EntityManagerId == -1)
                EntityManagerId = EntityManager.Add(EntityManager.EntityType.CastingComponent, this);

            return true;
        }

        private void StartCastSpell(StartCastSpellRequest startCastSpellRequest)
        {
            if (Owner is GamePlayer playerOwner)
            {
                // Unstealth when we start casting (NS/Ranger/Hunter).
                if (playerOwner.IsStealthed)
                    playerOwner.Stealth(false);
            }

            ISpellHandler newSpellHandler = ScriptMgr.CreateSpellHandler(Owner, startCastSpellRequest.Spell, startCastSpellRequest.SpellLine);

            // 'GameLiving.TargetObject' is used by 'SpellHandler.Tick()' but is likely to change during LoS checks or for queued spells (affects NPCs only).
            // So we pre-initialize 'SpellHandler.Target' with the passed down target, if there's any.
            if (startCastSpellRequest.Target != null)
                newSpellHandler.Target = startCastSpellRequest.Target;

            // Abilities that cast spells (i.e. Realm Abilities such as Volcanic Pillar) need to set this so the associated ability gets disabled if the cast is successful.
            newSpellHandler.Ability = startCastSpellRequest.SpellCastingAbilityHandler;

            // Performing the first tick here since 'SpellHandler' relies on 'GameLiving.TargetObject' (when 'target' is null), which may get cleared before 'Tick()' is called by the casting service.
            // It should also make casting very slightly more responsive.
            if (SpellHandler != null)
            {
                if (SpellHandler.Spell?.IsFocus == true)
                {
                    if (newSpellHandler.Spell.IsInstantCast)
                        newSpellHandler.Tick(GameLoop.GameLoopTime);
                    else
                        TickThenReplaceSpellHandler(newSpellHandler);
                }
                else if (newSpellHandler.Spell.IsInstantCast)
                    newSpellHandler.Tick(GameLoop.GameLoopTime);
                else
                {
                    if (Owner is GamePlayer player)
                    {
                        if (startCastSpellRequest.Spell.CastTime > 0 && SpellHandler is not ChamberSpellHandler && startCastSpellRequest.Spell.SpellType != (byte)eSpellType.Chamber)
                        {
                            if (SpellHandler.Spell.InstrumentRequirement != 0)
                            {
                                if (startCastSpellRequest.Spell.InstrumentRequirement != 0)
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
                    newSpellHandler.Tick(GameLoop.GameLoopTime);
                else
                    TickThenReplaceSpellHandler(newSpellHandler);

                // Why?
                if (SpellHandler is SummonNecromancerPet necroPetHandler)
                    necroPetHandler.SetConAndHitsBonus();
            }
        }

        public void InterruptCasting()
        {
            if (SpellHandler?.IsCasting == true)
            {
                Parallel.ForEach(Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>(), player =>
                {
                    player.Out.SendInterruptAnimation(Owner);
                });
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
                        necroBrain.RemoveSpellFromQueue();

                        if (!Owner.attackComponent.AttackState)
                            necroBrain.CheckAttackSpellQueue();

                        if (QueuedSpellHandler != null)
                        {
                            SpellHandler = QueuedSpellHandler;
                            QueuedSpellHandler = null;
                        }
                        else
                            SpellHandler = null;

                        if (necroBrain.SpellsQueued)
                            necroBrain.CheckSpellQueue();
                    }
                    else
                    {
                        if (necroPet.attackComponent.AttackState)
                            necroBrain.RemoveSpellFromAttackQueue();
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

        private void TickThenReplaceSpellHandler(ISpellHandler newSpellHandler)
        {
            newSpellHandler.Tick(GameLoop.GameLoopTime);
            SpellHandler = newSpellHandler;
        }

        private static bool CanCastSpell(GameLiving living)
        {
            GamePlayer player = living as GamePlayer;

            if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
            {
                player.Out.SendMessage("You can't cast spells while Volley is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player != null && player.IsCrafting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.craftComponent.StopCraft();
                player.CraftTimer = null;
                player.Out.SendCloseTimerWindow();
            }

            if (player != null && player.IsSalvagingOrRepairing)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.CraftTimer.Stop();
                player.CraftTimer = null;
                player.Out.SendCloseTimerWindow();
            }

            if (living != null)
            {
                if (living.IsStunned)
                {
                    player?.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (living.IsMezzed)
                {
                    player?.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (living.IsSilenced)
                {
                    player?.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            return true;
        }
    }
}
