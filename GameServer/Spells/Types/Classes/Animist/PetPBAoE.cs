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

namespace DOL.GS.Spells
{
    /// <summary>
    /// Summary description for TurretPBAoESpellHandler.
    /// </summary>
    [SpellHandler("TurretPBAoE")]
    public class PetPBAoE : DirectDamageSpellHandler
    {
        public PetPBAoE(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override bool HasPositiveEffect => false;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            // Allow the PBAoE to be casted on the main turret only.
            Target = Caster.ControlledBrain?.Body;
            return base.CheckBeginCast(Target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (Target.IsAlive)
                base.ApplyEffectOnTarget(target);
        }

        public override void DamageTarget(AttackData ad, bool showEffectAnimation)
        {
            // Set the turret as the attacker so that aggro is split properly (damage should already be calculated at this point).
            // This may cause some issues if something else relies on 'ad.Attacker', but is better than calculating aggro here.
            ad.Attacker = Target;
            base.DamageTarget(ad, showEffectAnimation);
        }
    }
}
