using DOL.GS.Effects;

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

			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
			}

			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			// http://support.darkageofcamelot.com/kb/article.php?id=423
			// Patch Notes: Version 1.52
			// The duration is 100% at the middle of the area, and it tails off to 50%
			// duration at the edges. This does NOT change the way area effect spells
			// work against monsters, only realm enemies (i.e. enemy players and enemy realm guards).
			double duration = base.CalculateEffectDuration(target);

			if (!(target is GamePlayer) && !(target is Keeps.GameKeepGuard))
				return (int)duration;

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
