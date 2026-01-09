namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.DirectDamage)]
	public class DirectDamageSpellHandler : SpellHandler
	{
		public override string ShortDescription => $"Inflicts {Spell.Damage} {Spell.DamageTypeToString()} damage to the target.";

		public DirectDamageSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Execute direct damage spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		/// <returns></returns>
		public override double CalculateDamageBase(GameLiving target)
		{
			GamePlayer player = Caster as GamePlayer;

			// % damage procs
			if (Spell.Damage < 0)
			{
				double spellDamage = 0;

				if (player != null)
				{
					// This equation is used to simulate live values - Tolakram
					spellDamage = (target.MaxHealth * -Spell.Damage * .01) / 2.5;
				}

				if (spellDamage < 0)
					spellDamage = 0;

				return spellDamage;
			}

			return base.CalculateDamageBase(target);
		}

		public override double DamageCap(double effectiveness)
		{
			if (Spell.Damage < 0)
			{
				return (Target.MaxHealth * -Spell.Damage * .01) * 3.0 * effectiveness;
			}

			return base.DamageCap(effectiveness);
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null)
				return;

			// 1.65 compliance. No LoS check on PBAoE or AoE spells.
			if (Spell.Target is eSpellTarget.CONE)
			{
				if (!Caster.castingComponent.StartEndOfCastLosCheck(target, this))
					DealDamage(target);
			}
			else
				DealDamage(target);
		}

		public override void OnEndOfCastLosCheck(GameLiving target, LosCheckResponse response)
		{
			if (response is LosCheckResponse.True)
				DealDamage(target);
		}

		protected virtual void DealDamage(GameLiving target)
		{
			if (!target.IsAlive || target.ObjectState is not GameObject.eObjectState.Active)
				return;

			AttackData ad = CalculateDamageToTarget(target);
			SendDamageMessages(ad);
			DamageTarget(ad, true);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}
	}
}
