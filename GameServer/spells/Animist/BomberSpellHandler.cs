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
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("Bomber")]
    public class BomberSpellHandler : SummonSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string BOMBER_TARGET = "bombertarget";
        private const string BOMBER_SPAWN_TICK = "bomberspawntick";

        public BomberSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_isSilent = true;
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Spell.SubSpellID == 0)
            {
                MessageToCaster("SPELL NOT IMPLEMENTED: CONTACT GM", eChatType.CT_Important);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);

            if (m_pet is not null)
            {
                m_pet.Level = m_pet.Owner?.Level ?? 1; // No bomber class to override SetPetLevel() in, so set level here.
                m_pet.TempProperties.setProperty(BOMBER_TARGET, target);
                m_pet.TempProperties.setProperty(BOMBER_SPAWN_TICK, GameLoop.GameLoopTime);
                m_pet.Name = Spell.Name;
                m_pet.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                m_pet.Flags ^= GameNPC.eFlags.PEACE;
                m_pet.FixedSpeed = true;
                m_pet.MaxSpeedBase = 350;
                m_pet.Follow(target, 5, Spell.Range * 5);
            }
        }

        protected override void AddHandlers()
        {
            GameEventMgr.AddHandler(m_pet, GameNPCEvent.ArriveAtTarget, BomberArriveAtTarget);
        }

        protected override void RemoveHandlers()
        {
            GameEventMgr.RemoveHandler(m_pet, GameNPCEvent.ArriveAtTarget, BomberArriveAtTarget);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new BomberBrain(owner);
        }

        protected override void SetBrainToOwner(IControlledBrain brain) { }

        protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments) { }

        private void BomberArriveAtTarget(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC bomber = sender as GameNPC;

            if (bomber == null || m_pet == null || bomber != m_pet)
                return;

            Spell subSpell = SkillBase.GetSpellByID(m_spell.SubSpellID);
            GameLiving living = m_pet.TempProperties.getProperty<object>(BOMBER_TARGET, null) as GameLiving;

            if (subSpell == null || living == null)
            {
                if (log.IsErrorEnabled && subSpell == null)
                    log.Error("Bomber SubspellID for Bomber SpellID: " + m_spell.ID + " is not implemented yet");

                bomber.Health = 0;
                bomber.Delete();
                return;
            }

            subSpell.Level = m_spell.Level;

            if (living.IsWithinRadius(bomber, 350))
            {
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(Caster, subSpell, SkillBase.GetSpellLine(SpellLine.KeyName));
                spellHandler.StartSpell(living);
            }

            bomber.Health = 0;
            bomber.Delete();
        }

        public override void CastSubSpells(GameLiving target) { }
    }
}