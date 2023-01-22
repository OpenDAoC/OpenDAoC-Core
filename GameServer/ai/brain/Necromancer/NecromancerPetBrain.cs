/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
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

            FSM.Add(new NecromancerPetState_WAKING_UP(FSM, this));
            FSM.Add(new NecromancerPetState_DEFENSIVE(FSM, this));
            FSM.Add(new NecromancerPetState_AGGRO(FSM, this));
            FSM.Add(new NecromancerPetState_PASSIVE(FSM, this));
            FSM.Add(new StandardMobState_DEAD(FSM, this));

            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        }

        public override int ThinkInterval => 500;
        public override int CastInterval => 500;

        /// <summary>
        /// Brain main loop.
        /// </summary>
        public override void Think()
        {
            CheckTether();
            FSM.Think();
        }

        #region Events

        /// <summary>
        /// Process events.
        /// </summary>
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameNPCEvent.PetSpell)
            {
                PetSpellEventArgs petSpell = (PetSpellEventArgs)args;
                bool hadQueuedSpells = false;

                if (SpellsQueued)
                {
                    MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.CastSpellAfterAction", Body.Name), eChatType.CT_System, Owner as GamePlayer);
                    hadQueuedSpells = true;
                }

                if (Body.attackComponent.AttackState || Body.IsCasting)
                {
                    if (petSpell.Spell.IsInstantCast && !petSpell.Spell.IsHarmful)
                        CastSpell(petSpell.Spell, petSpell.SpellLine, petSpell.Target, true);
                    else if (!petSpell.Spell.IsInstantCast)
                        AddToSpellQueue(petSpell.Spell, petSpell.SpellLine, petSpell.Target);
                    else
                        AddToAttackSpellQueue(petSpell.Spell, petSpell.SpellLine, petSpell.Target);
                }
                else
                {
                    if (petSpell.Spell.IsInstantCast)
                        CastSpell(petSpell.Spell, petSpell.SpellLine, petSpell.Target, true);
                    else
                        AddToSpellQueue(petSpell.Spell, petSpell.SpellLine, petSpell.Target);
                }

                // Immediately cast if this was the first spell added.
                if (hadQueuedSpells == false && !Body.IsCasting)
                    CheckSpellQueue();
            }
            else if (e == GameLivingEvent.Dying)
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

                    if (SpellsQueued)
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
            else if (e == GameLivingEvent.CastSucceeded)
            {
                // The spell will cast.
                PetSpellEventArgs spellArgs = args as PetSpellEventArgs;
                SpellLine spellLine = spellArgs.SpellLine;

                if (spellArgs != null && spellArgs.Spell != null)
                    DebugMessageToOwner(string.Format("Now casting '{0}'", spellArgs.Spell.Name));

                // This message is for spells from the spell queue only, so suppress
                // it for insta cast buffs coming from the pet itself.
                if (spellLine.Name != NecromancerPet.PetInstaSpellLine)
                {
                    Owner.Notify(GameLivingEvent.CastStarting, Body, new CastingEventArgs(Body.CurrentSpellHandler));
                    MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, "AI.Brain.Necromancer.PetCastingSpell", Body.Name), eChatType.CT_System, Owner as GamePlayer);
                }
            }
            else if (e == GameNPCEvent.SwitchedTarget && sender == Body.TargetObject &&
                sender is GameNPC && !(sender as GameNPC).IsCrowdControlled)
            {
                // Target has started attacking someone else.
                if (Body.EffectList.GetOfType<TauntEffect>() != null)
                    (Body as NecromancerPet).Taunt();
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

                ArrayList effectList = new(1);
                effectList.Add(shadeEffect);

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
                if (!CastSpell(entry.Spell, entry.SpellLine, entry.Target, true))
                {
                    // If the spell can't be cast, remove it from the queue.
                    RemoveSpellFromQueue();
                }
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
                    if (!CastSpell(entry.Spell, entry.SpellLine, entry.Target, false))
                    {
                        // If the spell can't be cast, remove it from the queue.
                        RemoveSpellFromAttackQueue();
                    }
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
            if ((spellTarget != null && spellTarget.IsAlive) || spell.Target.ToLower() == "self" || spell.Range == 0)
            {
                if (spell.CastTime > 0)
                    Body.attackComponent.StopAttack();

                Body.TargetObject = spellTarget;

                return !Body.CastSpell(spell, line, checkLos);
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

        private Queue<SpellQueueEntry> m_spellQueue = new(2);
        private Queue<SpellQueueEntry> m_attackSpellQueue = new(2);

        public void ClearSpellQueue()
        {
            lock (m_spellQueue)
            {
                m_spellQueue.Clear();
            }
        }

        public void ClearAttackSpellQueue()
        {
            lock (m_attackSpellQueue)
            {
                m_attackSpellQueue.Clear();
            }
        }

        /// <summary>
        /// Fetches a spell from the queue without removing it; the spell is removed *after* the spell has finished casting.
        /// </summary>
        /// <returns>The next spell or null, if no spell is in the queue.</returns>
        private SpellQueueEntry GetSpellFromQueue()
        {
            lock (m_spellQueue)
            {
                if (m_spellQueue.Count > 0)
                {
                    DebugMessageToOwner(String.Format("Grabbing spell '{0}' from the start of the queue in order to cast it", m_spellQueue.Peek().Spell.Name));
                    return m_spellQueue.Peek();
                }
            }

            return null;
        }

        /// <summary>
        /// Fetches a spell from the queue without removing it; the spell is removed *after* the spell has finished casting.
        /// </summary>
        /// <returns>The next spell or null, if no spell is in the queue.</returns>
        private SpellQueueEntry GetSpellFromAttackQueue()
        {
            lock (m_attackSpellQueue)
            {
                if (m_attackSpellQueue.Count > 0)
                {
                    DebugMessageToOwner(String.Format("Grabbing spell '{0}' from the start of the queue in order to cast it", m_attackSpellQueue.Peek().Spell.Name));
                    return m_attackSpellQueue.Peek();
                }
            }

            return null;
        }

        /// <summary>
        /// Whether or not any spells are queued.
        /// </summary>
        public bool SpellsQueued
        {
            get
            {
                lock (m_spellQueue)
                {
                    return m_spellQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// Whether or not any spells are queued.
        /// </summary>
        public bool AttackSpellsQueued
        {
            get
            {
                lock (m_attackSpellQueue)
                {
                    return m_attackSpellQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// Removes the spell that is first in the queue.
        /// </summary>
        public void RemoveSpellFromQueue()
        {
            lock (m_spellQueue)
            {
                if (m_spellQueue.Count > 0)
                {
                    DebugMessageToOwner(string.Format("Removing spell '{0}' from the start of the queue", m_spellQueue.Peek().Spell.Name));

                    m_spellQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// Removes the spell that is first in the queue.
        /// </summary>
        public void RemoveSpellFromAttackQueue()
        {
            lock (m_attackSpellQueue)
            {
                if (m_attackSpellQueue.Count > 0)
                {
                    DebugMessageToOwner(string.Format("Removing spell '{0}' from the start of the queue", m_attackSpellQueue.Peek().Spell.Name));

                    m_attackSpellQueue.Dequeue();
                }
            }
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
            lock (m_spellQueue)
            {
                if (m_spellQueue.Count >= 2)
                    MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language, 
                        "AI.Brain.Necromancer.SpellNoLongerInQueue", 
                        m_spellQueue.Dequeue().Spell.Name, Body.Name), 
                        eChatType.CT_Spell, Owner as GamePlayer);

                DebugMessageToOwner(string.Format("Adding spell '{0}' to the end of the queue", spell.Name));
                m_spellQueue.Enqueue(new SpellQueueEntry(spell, spellLine, target));
            }
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
            lock (m_attackSpellQueue)
            {
                if (m_attackSpellQueue.Count >= 2)
                    MessageToOwner(LanguageMgr.GetTranslation((Owner as GamePlayer).Client.Account.Language,
                        "AI.Brain.Necromancer.SpellNoLongerInQueue",
                        m_attackSpellQueue.Dequeue().Spell.Name, Body.Name),
                        eChatType.CT_Spell, Owner as GamePlayer);

                DebugMessageToOwner(string.Format("Adding spell '{0}' to the end of the queue", spell.Name));
                m_attackSpellQueue.Enqueue(new SpellQueueEntry(spell, spellLine, target));
            }
        }

        #endregion

        #region Tether

        private const int m_softTether = 750;    // TODO: Check on Pendragon
        private const int m_hardTether = 2000;
        private TetherTimer m_tetherTimer = null;

        private void CheckTether()
        {
            // Check if pet is past hard tether range, if so, despawn it
            // right away.

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
                {
                    m_pet.Brain.Notify(GameNPCEvent.OutOfTetherRange, this, 
                        new TetherEventArgs(m_seconds));
                    m_seconds -= 1;
                }
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
            // Pet past its tether, update effect icon (remaining time) and send 
            // warnings to owner at t = 10 seconds and t = 5 seconds.
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
                long tick = GameTimer.GetTickCount();
                long seconds = tick / 1000;
                long minutes = seconds / 60;

                MessageToOwner(string.Format("[{0:00}:{1:00}.{2:000}] {3}", minutes % 60, seconds % 60, tick % 1000, message), eChatType.CT_Staff, Owner as GamePlayer);
            }
        }

        #endregion
    }
}
