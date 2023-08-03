using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Summary description for TauntSpellHandler.
	/// </summary>
	[SpellHandler("Taunt")]
	public class TauntSpellHandler : SpellHandler
	{
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			Caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		/// <summary>
		/// execute non duration spell effect on target
		/// </summary>
		/// <param name="target"></param>
		/// <param name="effectiveness"></param>
		public override void OnDirectEffect(GameLiving target, double effectiveness)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState!=GameLiving.eObjectState.Active) return;
			
			// no animation on stealthed players
			if (target is GamePlayer)
				if ( target.IsStealthed ) 
					return;
			
			SendEffectAnimation(target, 0, false, 1);

			// Create attack data.
			AttackData ad = CalculateDamageToTarget(target, effectiveness);
			DamageTarget(ad, false);

			// Interrupt only if target is actually casting
			if (target.IsCasting && Spell.Target.ToLower() != "cone")
				target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

		/// <summary>
		/// When spell was resisted
		/// </summary>
		/// <param name="target">the target that resisted the spell</param>
		protected override void OnSpellResisted(GameLiving target)
		{
			base.OnSpellResisted(target);

			// Interrupt only if target is actually casting
			if (target.IsCasting && Spell.Target.ToLower() != "cone")
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
		}

		/// <summary>
		/// Apply the extra aggression
		/// </summary>
		/// <param name="ad"></param>
		/// <param name="showEffectAnimation"></param>
		/// <param name="attackResult"></param>
		public override void DamageTarget(AttackData ad, bool showEffectAnimation, int attackResult)
		{
			base.DamageTarget(ad, showEffectAnimation, attackResult);

			if (ad.Target is GameNpc && Spell.Value > 0)
			{
				IOldAggressiveBrain aggroBrain = ((GameNpc)ad.Target).Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
				{
					// this amount is a wild guess - Tolakram
					aggroBrain.AddToAggroList(Caster, Math.Max(1, (int)(Spell.Value * Caster.Level * 0.1)));
					//log.DebugFormat("Damage: {0}, Taunt Value: {1}, Taunt Amount {2}", ad.Damage, Spell.Value, Math.Max(1, (int)(Spell.Value * Caster.Level * 0.1)));
				}
			}

			m_lastAttackData = ad;
		}


		public TauntSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}