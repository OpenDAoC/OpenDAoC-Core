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
 *///made by DeMAN

using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler("WaterBreathing")]
	public class WaterBreathingSpellHandler : SpellHandler
	{
		public WaterBreathingSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			return (int)duration;
		}
		
		public override void OnEffectStart(GameSpellEffect effect)
		{

			GamePlayer player = effect.Owner as GamePlayer;
            
			if (player != null)
			{
                player.CanBreathUnderWater = true;
				player.BaseBuffBonusCategory[(int)EProperty.WaterSpeed] += (int)Spell.Value;
				player.Out.SendUpdateMaxSpeed();
			}

			EChatType toLiving = (Spell.Pulse == 0) ? EChatType.CT_Spell : EChatType.CT_SpellPulse;
			EChatType toOther = (Spell.Pulse == 0) ? EChatType.CT_System : EChatType.CT_SpellPulse;
			if (Spell.Message2 != "")
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
			MessageToLiving(effect.Owner, Spell.Message1 == "" ? "You find yourself able to move freely and breathe water like air!" : Spell.Message1, toLiving);
			base.OnEffectStart(effect);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			GamePlayer player = effect.Owner as GamePlayer;
			
            if (player != null)
			{
                //Check for Mythirian of Ektaktos on effect expiration to prevent unneccessary removal of Water Breathing Effect
                DbInventoryItem item = player.Inventory.GetItem((EInventorySlot)37);
                if (item == null || !item.Name.ToLower().Contains("ektaktos"))
                {
                    player.CanBreathUnderWater = false;
                }
				player.BaseBuffBonusCategory[(int)EProperty.WaterSpeed] -= (int)Spell.Value;
				player.Out.SendUpdateMaxSpeed();
				if (player.IsDiving & player.CanBreathUnderWater == false)
					MessageToLiving(effect.Owner, "With a gulp and a gasp you realize that you are unable to breathe underwater any longer!", EChatType.CT_SpellExpires);
			}
			return base.OnEffectExpires(effect, noMessages);
		}
	}
}
