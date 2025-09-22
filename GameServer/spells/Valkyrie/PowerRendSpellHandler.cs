using System;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.spells
{
	/// <summary>
	/// Power Rend is a style effect unique to the Valkyrie's sword specialization line.
	/// </summary>
	[SpellHandler(eSpellType.PowerRend)]
	public class PowerRendSpellHandler : SpellHandler
	{
		private Random m_rng = new Random();

		public PowerRendSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

				
		public override void OnDirectEffect(GameLiving target)
		{
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
				return;

			SendEffectAnimation(target, m_spell.ClientEffect, boltDuration: 0, noSound: false, success: 1);
						
			/*var mesmerizeEffect = target.FindEffectOnTarget("Mesmerize");
			if (mesmerizeEffect != null)
				mesmerizeEffect.Cancel(false);

			var speedDecreaseEffect = target.FindEffectOnTarget("SpeedDecrease");
			if (speedDecreaseEffect != null)
				speedDecreaseEffect.Cancel(false);*/

						
			bool targetIsGameplayer = target is GamePlayer;
			var necroPet = target as NecromancerPet;
			
			if (targetIsGameplayer || necroPet != null)
			{
				int powerRendValue;

				if (targetIsGameplayer)
				{
					powerRendValue = (int)(target.MaxMana * Spell.Value * GetVariance());
					if (powerRendValue > target.Mana)
						powerRendValue = target.Mana;
					target.Mana -= powerRendValue;
					target.MessageToSelf(string.Format(m_spell.Message2, powerRendValue), eChatType.CT_Spell);
				}
				else
				{
					powerRendValue = (int)(necroPet.Owner.MaxMana * Spell.Value * GetVariance());
					if (powerRendValue > necroPet.Owner.Mana)
						powerRendValue = necroPet.Owner.Mana;
					necroPet.Owner.Mana -= powerRendValue;
					necroPet.Owner.MessageToSelf(string.Format(m_spell.Message2, powerRendValue), eChatType.CT_Spell);
				}

				MessageToCaster(string.Format(m_spell.Message1, powerRendValue), eChatType.CT_Spell);
			}
		}
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target == null || target.CurrentRegion == null)
				return;

			base.ApplyEffectOnTarget(target);

			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		public override double CalculateSpellResistChance(GameLiving target) => 100 - CalculateToHitChance(target);

		private double GetVariance()
		{
			int intRandom = m_rng.Next(0, 37);
			double factor = 1 + (double)intRandom / 100;
			return factor;
		}
	}
}
