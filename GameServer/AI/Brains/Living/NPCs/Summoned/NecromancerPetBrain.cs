using System;
using System.Collections;
using System.Collections.Concurrent;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain for the necromancer pets.
    /// </summary>
    /// <author>Aredhel</author>
    public class NecromancerPetBrain : ControlledNpcBrain
    {
        public NecromancerPetBrain(GameLiving owner) : base(owner)
        {
            FSM.ClearStates();

            FSM.Add(new NecromancerPetState_WAKING_UP(this));
            FSM.Add(new NecromancerPetState_DEFENSIVE(this));
            FSM.Add(new NecromancerPetState_AGGRO(this));
            FSM.Add(new NecromancerPetState_PASSIVE(this));
            FSM.Add(new StandardMobState_DEAD(this));

            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        }

        public override int ThinkInterval => 500;

        /// <summary>
        /// Brain main loop.
        /// </summary>
        public override void Think()
        {
            CheckTether();
            FSM.Think();
        }

        #region Events

        public void OnOwnerFinishPetSpellCast(Spell spell, SpellLine spellLine, GameLiving target)
        {
            bool hadQueuedSpells = false;

            if (!m_spellQueue.IsEmpty)
            {
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.CastSpellAfterAction", Body.Name), eChatType.CT_System, Owner as GamePlayer);
                hadQueuedSpells = true;
            }

            if (Body.attackComponent.AttackState || Body.IsCasting)
            {
                if (spell.IsInstantCast && !spell.IsHarmful)
                    CastSpell(spell, spellLine, target, true);
                else if (!spell.IsInstantCast)
                    AddToSpellQueue(spell, spellLine, target);
                else
                    AddToAttackSpellQueue(spell, spellLine, target);
            }
            else
            {
                if (spell.IsInstantCast)
                    CastSpell(spell, spellLine, target, true);
                else
                    AddToSpellQueue(spell, spellLine, target);
            }

            // Immediately cast if this was the first spell added.
            if (hadQueuedSpells == false && !Body.IsCasting)
                CheckSpellQueue();
        }

        public void OnPetBeginCast(Spell spell, SpellLine spellLine)
        {
            DebugMessageToOwner($"Now casting '{spell}'");

            // This message is for spells from the spell queue only, so suppress it for insta cast buffs coming from the pet itself.
            if (spellLine.Name != NecromancerPet.PetInstaSpellLine)
            {
                Owner.Notify(GameLivingEvent.CastStarting, Body, new CastingEventArgs(Body.CurrentSpellHandler));
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.PetCastingSpell", Body.Name), eChatType.CT_System, Owner as GamePlayer);
            }
        }

        /// <summary>
        /// Process events.
        /// </summary>
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameLivingEvent.Dying)
            {
                // At necropet Die, we check DamageRvRMemory for transfer it to owner if necessary.
                GamePlayer playerowner = GetPlayerOwner();

                if (playerowner != null && Body.DamageRvRMemory > 0)
                    playerowner.DamageRvRMemory = Body.DamageRvRMemory;

                return;
            }
            else if (e == GameLivingEvent.CastFinished)
            {
                // Instant cast spells bypass the queue.
                if (args is CastingEventArgs cArgs && !cArgs.SpellHandler.Spell.IsInstantCast)
                {
                    // Remove the spell that has finished casting from the queue, if there are more, keep casting.
                    RemoveSpellFromQueue();
                    AttackMostWanted();

                    if (!m_spellQueue.IsEmpty)
                    {
                        DebugMessageToOwner("+ Cast finished, more spells to cast");
                        CheckSpellQueue();
                    }
                    else
                        DebugMessageToOwner("- Cast finished, no more spells to cast");
                }
                else
                {
                    RemoveSpellFromAttackQueue();
                    AttackMostWanted();
                }

                Owner.Notify(GameLivingEvent.CastFinished, Owner, args);
            }
            else if (e == GameLivingEvent.CastFailed)
            {
                // Tell owner why cast has failed.
                switch ((args as CastFailedEventArgs).Reason)
                {
                    case CastFailedEventArgs.Reasons.TargetTooFarAway:
                        MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, 
                            "AI.Brain.Necromancer.ServantFarAwayToCast"), eChatType.CT_SpellResisted, Owner as GamePlayer);
                        break;

                    case CastFailedEventArgs.Reasons.TargetNotInView:
                        MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, 
                            "AI.Brain.Necromancer.PetCantSeeTarget", Body.Name), eChatType.CT_SpellResisted, Owner as GamePlayer);
                        break;

                    case CastFailedEventArgs.Reasons.NotEnoughPower:
                        RemoveSpellFromQueue();
                        MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language,
                            "AI.Brain.Necromancer.NoPower", Body.Name), eChatType.CT_SpellResisted, Owner as GamePlayer);
                        break;
                }
            }
            else if (e == GameLivingEvent.AttackFinished)
                Owner.Notify(GameLivingEvent.AttackFinished, Owner, args);
        }

        /// <summary>
        /// Set the tether timer if pet gets out of range or comes back into range.
        /// </summary>
        /// <param name="seconds"></param>
        public void SetTetherTimer(int seconds)
        {
            NecromancerShadeEffect shadeEffect = Owner.EffectList.GetOfType<NecromancerShadeEffect>();

            if (shadeEffect != null)
            {
                lock (shadeEffect)
                {
                    shadeEffect.SetTetherTimer(seconds);
                }

                ArrayList effectList = new(1)
                {
                    shadeEffect
                };

                int effectsCount = 1;

                (Owner as GamePlayer)?.Out.SendUpdateIcons(effectList, ref effectsCount);
            }
        }

        #endregion

        #region Spell Queue

        /// <summary>
        /// See if there are any spells queued up and if so, get the first one
        /// and cast it.
        /// </summary>
        public void CheckSpellQueue()
        {
            SpellQueueEntry entry = GetSpellFromQueue();

            if (entry != null)
            {
                // If the spell can be cast, remove it from the queue.
                if (CastSpell(entry.Spell, entry.SpellLine, entry.Target, true))
                    RemoveSpellFromQueue();
            }
        }

        public GameLiving GetSpellTarget()
        {
            SpellQueueEntry entry = GetSpellFromQueue();

            if (entry != null)
                return entry.Target;
            else
                return null;
        }

        /// <summary>
        /// See if there are any spells queued up and if so, get the first one and cast it.
        /// </summary>
        public void CheckAttackSpellQueue()
        {
            int spellsQueued = m_attackSpellQueue.Count;

            for (int i = 0; i < spellsQueued; i++)
            {
                SpellQueueEntry entry = GetSpellFromAttackQueue();

                if (entry != null)
                {
                    // If the spell can be cast, remove it from the queue.
                    if (CastSpell(entry.Spell, entry.SpellLine, entry.Target, false))
                        RemoveSpellFromAttackQueue();
                }
            }
        }

        /// <summary>
        /// Try to cast a spell, returning true if the spell started to cast.
        /// </summary>
        /// <returns>Whether or not the spell started to cast.</returns>
        private bool CastSpell(Spell spell, SpellLine line, GameObject target, bool checkLos)
        {
            GameLiving spellTarget = target as GameLiving;

            // Target must be alive, or this is a self spell, or this is a pbaoe spell.
            if ((spellTarget != null && spellTarget.IsAlive) || spell.Target == eSpellTarget.SELF || spell.Range == 0)
            {
                if (spell.CastTime > 0)
                    Body.attackComponent.StopAttack();

                Body.TargetObject = spellTarget;
                Body.CastSpell(spell, line, checkLos);

                // Assume that the spell can always be casted, otherwise the same spell will be queued in the casting component if LoS checks are enabled.
                return true;
            }
            else
            {
                DebugMessageToOwner("Invalid target for spell '" + spell.Name + "'");
                return false;
            }
        }

        /// <summary>
        /// This class holds a single entry for the spell queue.
        /// </summary>
        private class SpellQueueEntry
        {
            public Spell Spell { get; private set; }
            public SpellLine SpellLine { get; private set; }
            public GameLiving Target { get; private set; }

            public SpellQueueEntry(Spell spell, SpellLine spellLine, GameLiving target)
            {
                Spell = spell;
                SpellLine = spellLine;
                Target = target;
            }

            public SpellQueueEntry(SpellQueueEntry entry) : this(entry.Spell, entry.SpellLine, entry.Target) { }
        }

        private ConcurrentQueue<SpellQueueEntry> m_spellQueue = new();
        private ConcurrentQueue<SpellQueueEntry> m_attackSpellQueue = new();

        public void ClearSpellQueue()
        {
            m_spellQueue.Clear();
        }

        public void ClearAttackSpellQueue()
        {
            m_attackSpellQueue.Clear();
        }

        /// <summary>
        /// Whether or not any spells are queued.
        /// </summary>
        public bool HasSpellsQueued()
        {
            return !m_spellQueue.IsEmpty;
        }

        /// <summary>
        /// Whether or not any spells are queued.
        /// </summary>
        public bool HasAttackSpellsQueued()
        {
            return !m_attackSpellQueue.IsEmpty;
        }

        /// <summary>
        /// Fetches a spell from the queue without removing it; the spell is removed *after* the spell has finished casting.
        /// </summary>
        /// <returns>The next spell or null, if no spell is in the queue.</returns>
        private SpellQueueEntry GetSpellFromQueue()
        {
            m_spellQueue.TryPeek(out SpellQueueEntry spellQueueEntry);

            if (!m_spellQueue.IsEmpty)
                DebugMessageToOwner(string.Format("Grabbing spell '{0}' from the start of the queue in order to cast it", spellQueueEntry.Spell.Name));

            return spellQueueEntry;
        }

        /// <summary>
        /// Fetches a spell from the queue without removing it; the spell is removed *after* the spell has finished casting.
        /// </summary>
        /// <returns>The next spell or null, if no spell is in the queue.</returns>
        private SpellQueueEntry GetSpellFromAttackQueue()
        {
            m_attackSpellQueue.TryPeek(out SpellQueueEntry spellQueueEntry);

            if (spellQueueEntry != null)
                DebugMessageToOwner(string.Format("Grabbing spell '{0}' from the start of the queue in order to cast it", spellQueueEntry.Spell.Name));

            return spellQueueEntry;
        }

        /// <summary>
        /// Removes the spell that is first in the queue.
        /// </summary>
        public void RemoveSpellFromQueue()
        {
            m_spellQueue.TryDequeue(out SpellQueueEntry spellQueueEntry);

            if (spellQueueEntry != null)
                DebugMessageToOwner(string.Format("Removing spell '{0}' from the start of the queue", spellQueueEntry.Spell.Name));
        }

        /// <summary>
        /// Removes the spell that is first in the queue.
        /// </summary>
        public void RemoveSpellFromAttackQueue()
        {
            m_attackSpellQueue.TryDequeue(out SpellQueueEntry spellQueueEntry);

            if (spellQueueEntry != null)
                DebugMessageToOwner(string.Format("Removing spell '{0}' from the start of the queue", spellQueueEntry.Spell.Name));
        }

        /// <summary>
        /// Add a spell to the queue. If there are already 2 spells in the
        /// queue, remove the spell that the pet would cast next.
        /// </summary>
        /// <param name="spell">The spell to add.</param>
        /// <param name="spellLine">The spell line the spell is in.</param>
        /// <param name="target">The target to cast the spell on.</param>
        private void AddToSpellQueue(Spell spell, SpellLine spellLine, GameLiving target)
        {
            SpellQueueEntry spellQueueEntry = null;

            while (m_spellQueue.Count >= 2)
                m_spellQueue.TryDequeue(out spellQueueEntry);

            if (spellQueueEntry != null)
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.SpellNoLongerInQueue", spellQueueEntry.Spell.Name, Body.Name), eChatType.CT_Spell, Owner as GamePlayer);

            DebugMessageToOwner(string.Format("Adding spell '{0}' to the end of the queue", spell.Name));
            m_spellQueue.Enqueue(new SpellQueueEntry(spell, spellLine, target));
        }

        /// <summary>
        /// Add a spell to the queue. If there are already 2 spells in the
        /// queue, remove the spell that the pet would cast next.
        /// </summary>
        /// <param name="spell">The spell to add.</param>
        /// <param name="spellLine">The spell line the spell is in.</param>
        /// <param name="target">The target to cast the spell on.</param>
        private void AddToAttackSpellQueue(Spell spell, SpellLine spellLine, GameLiving target)
        {
            SpellQueueEntry spellQueueEntry = null;

            while (m_attackSpellQueue.Count >= 2)
                m_attackSpellQueue.TryDequeue(out spellQueueEntry);

            if (spellQueueEntry != null)
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.SpellNoLongerInQueue", spellQueueEntry.Spell.Name, Body.Name), eChatType.CT_Spell, Owner as GamePlayer);

            DebugMessageToOwner(string.Format("Adding spell '{0}' to the end of the queue", spell.Name));
            m_attackSpellQueue.Enqueue(new SpellQueueEntry(spell, spellLine, target));
        }

        #endregion

        #region Tether

        private const int m_softTether = 750; // TODO: Check on Pendragon
        private const int m_hardTether = 2000;
        private TetherTimer m_tetherTimer = null;

        private void CheckTether()
        {
            // Check if pet is past hard tether range, if so, despawn it right away.
            if (!Body.IsWithinRadius(Owner, m_hardTether))
            {
                m_tetherTimer?.Stop();
                (Body as NecromancerPet).CutTether();
                return;
            }

            // Check if pet is out of soft tether range.
            if (!Body.IsWithinRadius(Owner, m_softTether))
            {
                if (m_tetherTimer == null)
                {
                    // Pet just went out of range, start the timer.
                    m_tetherTimer = new TetherTimer(Body as NecromancerPet);
                    m_tetherTimer.Callback = new ECSGameTimer.ECSTimerCallback(FollowCallback);
                    m_tetherTimer.Start(1);
                    followSeconds = 10;
                }
            }
            else
            {
                if (m_tetherTimer != null)
                {
                    // Pet is back in range, stop the timer.
                    m_tetherTimer.Stop();
                    m_tetherTimer = null;
                    SetTetherTimer(-1);
                }
            }
        }

        /// <summary>
        /// Timer for pet out of tether range.
        /// </summary>
        private class TetherTimer : ECSGameTimer
        {
            private NecromancerPet m_pet;
            private int m_seconds = 10;

            public TetherTimer(NecromancerPet pet) : base(pet) 
            {
                m_pet = pet;
            }

            protected void OnTick()
            {
                Interval = 1000;

                if (m_seconds > 0)
                    m_seconds -= 1;
                else
                {
                    Stop();
                    m_pet.CutTether();
                }
            }
        }

        private int followSeconds = 10;

        private int FollowCallback(ECSGameTimer timer)
        {
            if (followSeconds > 0)
            {
                OutOfTetherCheck(followSeconds);
                followSeconds -= 1;
            }
            else
            {
                Stop();
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language,
                    "AI.Brain.Necromancer.HaveLostBondToPet"), eChatType.CT_System, Owner as GamePlayer);
                (Body as NecromancerPet)?.CutTether();
                return 0;
            }

            return 1000;
        }

        private void OutOfTetherCheck(int secondsRemaining)
        {
            // Pet past its tether, update effect icon (remaining time) and send warnings to owner at t = 10 seconds and t = 5 seconds.
            SetTetherTimer(secondsRemaining);

            if (secondsRemaining == 10)
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language,
                    "AI.Brain.Necromancer.PetTooFarBeLostSecIm", secondsRemaining), eChatType.CT_System, Owner as GamePlayer);
            else if (secondsRemaining == 5)
                MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language,
                    "AI.Brain.Necromancer.PetTooFarBeLostSec", secondsRemaining), eChatType.CT_System, Owner as GamePlayer);
        }

        /// <summary>
        /// Send a message to the shade.
        /// </summary>
        public static void MessageToOwner(string message, eChatType chatType, GamePlayer owner)
        {
            if ((owner != null) && (message.Length > 0))
                owner.Out.SendMessage(message, chatType, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// For debugging purposes only.
        /// </summary>
        private void DebugMessageToOwner(string message)
        {
            if (GS.ServerProperties.Properties.ENABLE_DEBUG)
            {
                long tick = GameLoop.GetCurrentTime();
                long seconds = tick / 1000;
                long minutes = seconds / 60;

                MessageToOwner(string.Format("[{0:00}:{1:00}.{2:000}] {3}", minutes % 60, seconds % 60, tick % 1000, message), eChatType.CT_Staff, Owner as GamePlayer);
            }
        }

        #endregion
    }
}
