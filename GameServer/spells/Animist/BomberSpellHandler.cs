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

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            if (m_pet is not null)
            {
                m_pet.Level = m_pet.Owner?.Level ?? 1; // No bomber class to override SetPetLevel() in, so set level here.
                m_pet.Name = Spell.Name;
                m_pet.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                m_pet.Flags ^= GameNPC.eFlags.PEACE;
                m_pet.FixedSpeed = true;
                m_pet.MaxSpeedBase = 350;
                m_pet.TargetObject = target;
                m_pet.Follow(target, 5, Spell.Range * 5);
            }
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new BomberBrain(owner, Spell, SpellLine);
        }

        protected override void SetBrainToOwner(IControlledBrain brain) { }

        protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments) { }

        public override void CastSubSpells(GameLiving target) { }
    }
}
