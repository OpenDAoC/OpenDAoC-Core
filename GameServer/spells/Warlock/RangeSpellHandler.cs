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
using System.Reflection;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler(eSpellType.Range)]
	public class RangeSpellHandler : PrimerSpellHandler
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
            GameSpellEffect UninterruptableSpell = SpellHandler.FindEffectOnTarget(Caster, "Uninterruptable");
  			if(UninterruptableSpell != null) { MessageToCaster("You already preparing a Uninterruptable spell", eChatType.CT_System); return false; }
            GameSpellEffect PowerlessSpell = SpellHandler.FindEffectOnTarget(Caster, "Powerless");
  			if(PowerlessSpell != null) { MessageToCaster("You already preparing	a Powerless spell", eChatType.CT_System); return false; }
            GameSpellEffect RangeSpell = SpellHandler.FindEffectOnTarget(Caster, "Range");
            if (RangeSpell != null) { MessageToCaster("You must finish casting Range before you can cast it again", eChatType.CT_System); return false; }
            return true;
		}
		/// <summary>
		/// Calculates the power to cast the spell
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public override int PowerCost(GameLiving target)
		{
            double basepower = m_spell.Power; //<== defined a basevar first then modified this base-var to tell %-costs from absolut-costs

            // percent of maxPower if less than zero
			if (basepower < 0)
			{
				if (Caster is GamePlayer && ((GamePlayer)Caster).CharacterClass.ManaStat != eStat.UNDEFINED)
				{
					GamePlayer player = Caster as GamePlayer;
					basepower = player.CalculateMaxMana(player.Level, player.GetBaseStat(player.CharacterClass.ManaStat)) * basepower * -0.01;
				}
				else
				{
					basepower = Caster.MaxMana * basepower * -0.01;
				}
			}
            return (int)basepower;
		}

//		public override bool CasterIsAttacked(GameLiving attacker)
//		{
//			return false;
//		}

		// constructor
		public RangeSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
