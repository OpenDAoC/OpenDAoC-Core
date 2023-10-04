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
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Heal Over Time spell handler
	/// </summary>
	[SpellHandler("HealOverTime")]
	public class HoTSpellHandler : SpellHandler
	{
		public override void CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			new HealOverTimeECSGameEffect(initParams);
		}

		/// <summary>
		/// Execute heal over time spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			// TODO: correct formula
			Effectiveness = 1.25;
			if(Caster is GamePlayer)
			{
				double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
				if (lineSpec < 1)
					lineSpec = 1;
				Effectiveness = 0.75;
				if (Spell.Level > 0)
				{
					Effectiveness += (lineSpec-1.0)/Spell.Level*0.5;
					if (Effectiveness > 1.25)
						Effectiveness = 1.25;
				}
			}
			base.ApplyEffectOnTarget(target);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{			
			//SendEffectAnimation(effect.Owner, 0, false, 1);
			////"{0} seems calm and healthy."
			//Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_Spell, effect.Owner);
		}

		public override void OnEffectPulse(GameSpellEffect effect)
		{
			base.OnEffectPulse(effect);
			OnDirectEffect(effect.Owner);
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;

			base.OnDirectEffect(target);
			double heal = Spell.Value * Effectiveness;
			
			if(target.Health < target.MaxHealth)
            {
				target.Health += (int)heal;
				if (target is NecromancerPet && Caster.Equals(target))
					MessageToLiving((target as NecromancerPet).Owner, "Your " + target.GetName(0, false) + " is healed for " + heal + " hit points!", eChatType.CT_Spell);
				else
					MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + heal + " hit points.", eChatType.CT_Spell);
			}
            else
            {
				MessageToLiving(target, "You are full health.", eChatType.CT_SpellResisted);
            }
			

            #region PVP DAMAGE

            if (target.DamageRvRMemory > 0 &&
                (target is NecromancerPet &&
                ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null
                || target is GamePlayer))
            {
                if (target.DamageRvRMemory > 0)
                    target.DamageRvRMemory -= (long)Math.Max(heal, 0);
            }

            #endregion PVP DAMAGE

			//"You feel calm and healthy."
			MessageToLiving(target, Spell.Message1, eChatType.CT_Spell);
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			if (!noMessages) {
				//"Your meditative state fades."
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				//"{0}'s meditative state fades."
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}


		// constructor
		public HoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
