using DOL.GS.Effects;
using DOL.GS.Keeps;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Base class for spells with immunity like mez/root/stun/nearsight
	/// </summary>
	public abstract class ImmunityEffectSpellHandler : SpellHandler
	{
		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target == null || target.CurrentRegion == null)
				return;

			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			double duration = base.CalculateEffectDuration(target);

			if (target is not GamePlayer and not GameKeepGuard)
				return (int) duration;

			duration -= duration * target.GetResistBase(Spell.DamageType) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);
			return (int)duration;
		}

		/// <summary>
		/// Creates the corresponding spell effect for the spell
		/// </summary>
		/// <param name="target"></param>
		/// <param name="effectiveness"></param>
		/// <returns></returns>
		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellAndImmunityEffect(this, CalculateEffectDuration(target), 0, effectiveness);
		}

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="caster">The spell caster</param>
		/// <param name="spell">The spell being cast</param>
		/// <param name="spellLine">The spell's spellline</param>
		public ImmunityEffectSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}
