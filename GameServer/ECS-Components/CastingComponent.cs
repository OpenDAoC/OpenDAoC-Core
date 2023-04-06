using System;
using System.Linq;
using System.Threading;
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
        public ISpellHandler SpellHandler { get; private set; }
        public ISpellHandler QueuedSpellHandler { get; private set; }
        public GameLiving Owner { get; private set; }
        public int EntityManagerId { get; private set; } = EntityManager.UNSET_ID;

        private Spell _startCastSpellSpell;
        private SpellLine _startCastSpellSpellLine;
        private ISpellCastingAbilityHandler _startCastSpellSpellCastingAbilityHandler;
        private GameLiving _startCastSpellTarget;

        // Used as a boolean to tell if 'StartCastSpell' is currently being called.
        private long _startCastSpellRequested;

        private bool StartCastSpellRequested
        {
            get => Interlocked.Read(ref _startCastSpellRequested) == 1;
            set => Interlocked.Exchange(ref _startCastSpellRequested, Convert.ToInt64(value));
        }

        public CastingComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick(long time)
        {
            if (StartCastSpellRequested)
            {
                StartCastSpellRequested = false;
                StartCastSpell(_startCastSpellSpell, _startCastSpellSpellLine, _startCastSpellSpellCastingAbilityHandler, _startCastSpellTarget);
            }

            SpellHandler?.Tick(time);

            if (SpellHandler == null && QueuedSpellHandler == null)
                EntityManagerId = EntityManager.Remove(EntityManager.EntityType.CastingComponent, EntityManagerId);
        }

        public bool RequestStartCastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
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

            _startCastSpellSpell = spell;
            _startCastSpellSpellLine = line;
            _startCastSpellSpellCastingAbilityHandler = spellCastingAbilityHandler;
            _startCastSpellTarget = target;
            StartCastSpellRequested = true;

            if (EntityManagerId == -1)
                EntityManagerId = EntityManager.Add(EntityManager.EntityType.CastingComponent, this);

            return true;
        }

        private void StartCastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (Owner is GamePlayer playerOwner)
            {
                // Unstealth when we start casting (NS/Ranger/Hunter).
                if (playerOwner.IsStealthed)
                    playerOwner.Stealth(false);
            }

            ISpellHandler newSpellHandler = ScriptMgr.CreateSpellHandler(Owner, spell, line);

            // 'GameLiving.TargetObject' is used by 'SpellHandler.Tick()' but is likely to change during LoS checks or for queued spells (affects NPCs only).
            // So we pre-initialize 'SpellHandler.Target' with the passed down target, if there's any.
            if (target != null)
                newSpellHandler.Target = target;

            // Abilities that cast spells (i.e. Realm Abilities such as Volcanic Pillar) need to set this so the associated ability gets disabled if the cast is successful.
            newSpellHandler.Ability = spellCastingAbilityHandler;

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
                        if (spell.CastTime > 0 && SpellHandler is not ChamberSpellHandler && spell.SpellType != (byte)eSpellType.Chamber)
                        {
                            if (SpellHandler.Spell.InstrumentRequirement != 0)
                            {
                                if (spell.InstrumentRequirement != 0)
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

        public void CleanUpSpellHandler()
        {
            if (Owner is GamePlayer player)
            {
                if (SpellHandler?.Spell.CastTime > 0)
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
                    if (SpellHandler?.Spell.CastTime > 0)
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
