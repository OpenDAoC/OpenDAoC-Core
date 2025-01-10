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

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The evade chance calculator. Returns 0 .. 1000 chance.
    /// 
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.EvadeChance)]
    public class EvadeChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int chance = 0;

            if (living is GamePlayer player)
            {
                if (player.HasAbility(Abilities.Evade))
                    chance += (int) ((((player.Dexterity + player.Quickness) / 2 - 50) * 0.05 + player.GetAbilityLevel(Abilities.Evade) * 5) * 10);

                chance += player.BaseBuffBonusCategory[(int) property] * 10;
                chance += player.SpecBuffBonusCategory[(int) property] * 10;
                chance -= player.DebuffCategory[(int) property] * 10;
                chance += player.OtherBonus[(int) property] * 10;
                chance += player.AbilityBonus[(int) property] * 10;
            }
            else if (living is GameNPC npc)
                chance += npc.AbilityBonus[(int)property] * 10 + npc.EvadeChance * 10;

            return chance;
        }
    }
}
