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

using System.Reflection;
using DOL.GS;
using DOL.GS.Spells;
using log4net;

namespace DOL.AI.Brain
{
    public class BomberBrain : ControlledNpcBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Spell _spell;
        private SpellLine _spellLine;
        private long _expireTime = GameLoop.GameLoopTime + 60 * 1000;

        public BomberBrain(GameLiving owner, Spell spell, SpellLine spellLine) : base(owner)
        {
            _spell = spell;
            _spellLine = spellLine;
        }

        public override int ThinkInterval => 300;

        protected override bool CheckDefensiveSpells(Spell spell)
        {
            return true;
        }

        protected override bool CheckOffensiveSpells(Spell spell)
        {
            return true;
        }

        public override void Think()
        {
            if (GameLoop.GameLoopTime >= _expireTime)
                Body.Delete();

            if (Body.IsWithinRadius(Body.TargetObject, 150))
                DeliverPayload();
        }

        private void DeliverPayload()
        {
            Spell subSpell = SkillBase.GetSpellByID(_spell.SubSpellID);

            if (subSpell == null)
            {
                if (log.IsErrorEnabled && subSpell == null)
                    log.Error("Bomber SubspellID for Bomber SpellID: " + _spell.ID + " is not implemented yet");

                Body.Health = 0;
                Body.Delete();
                return;
            }

            subSpell.Level = _spell.Level;

            if (Body.IsWithinRadius(Body.TargetObject, 350))
            {
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(m_owner, subSpell, SkillBase.GetSpellLine(_spellLine.KeyName));
                spellHandler.StartSpell(Body.TargetObject as GameLiving);
            }

            Body.Health = 0;
            Body.Delete();
        }

        public override void FollowOwner() { }
        public override void UpdatePetWindow() { }
    }
}
