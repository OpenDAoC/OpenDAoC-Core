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
using DOL.GS;

namespace DOL.AI.Brain
{
    public class TurretMainPetTankBrain : TurretBrain
    {
        public TurretMainPetTankBrain(GameLiving owner) : base(owner) { }

        protected override bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target)
        {
            // Tank turrets don't check for spells if their target is close, but attack in melee instead.
            if (Body.IsWithinRadius(target, Body.attackComponent.AttackRange))
            {
                Body.StopCurrentSpellcast();
                Body.StartAttack(target);
                return true;
            }
            else
            {
                Body.StopAttack();
                return base.TrustCast(spell, type, target);
            }
        }
    }
}