using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    //this component will hold all data related to casting spells
    public class CastingComponent
    {
        public ISpellHandler SpellHandler;
        public ISpellHandler InstantSpellHandler;
        public ISpellHandler QueuedSpellHandler;
        public GameLiving Owner { get; private set; }
        public int EntityManagerId { get; set; } = EntityManager.UNSET_ID;
        public bool IsCasting => SpellHandler != null && SpellHandler.IsCasting;

        public CastingComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick(long time)
        {
            SpellHandler?.Tick(time);

            // No 'InstantSpellHandler' check because those aren't always cleaned up.
            if (SpellHandler == null && QueuedSpellHandler == null)
                EntityManagerId = EntityManager.Remove(EntityManager.EntityType.CastingComponent, EntityManagerId);
        }

        public bool StartCastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            if (EntityManagerId == -1)
                EntityManagerId = EntityManager.Add(EntityManager.EntityType.CastingComponent, this);

            if (Owner is GamePlayer playerOwner)
            {
                if (!CanCastSpell(playerOwner))
                    return false; 

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

            // Performing the first tick here since 'SpellHandler' relies on the owner's target, which may get cleared before 'Tick()' is called by the casting service.
            if (SpellHandler != null)
            {
                if (SpellHandler.Spell != null && SpellHandler.Spell.IsFocus)
                {
                    if (newSpellHandler.Spell.IsInstantCast)
                        newSpellHandler.Tick(GameLoop.GameLoopTime);
                    else
                        TickThenReplaceSpellHandler(ref SpellHandler, newSpellHandler);
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

                                return false;
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
                    TickThenReplaceSpellHandler(ref SpellHandler, newSpellHandler);

                // Why?
                if (SpellHandler is SummonNecromancerPet necroPetHandler)
                    necroPetHandler.SetConAndHitsBonus();
            }

            return true;
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

        private static void TickThenReplaceSpellHandler(ref ISpellHandler oldSpellHandler, ISpellHandler newSpellHandler)
        {
            newSpellHandler.Tick(GameLoop.GameLoopTime);
            oldSpellHandler = newSpellHandler;
        }
    }
}
