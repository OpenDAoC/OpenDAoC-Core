using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("ArcheryDoT")]
    public class ArcheryDotSpellHandler : DamageOverTimeSpell
    {
		public ArcheryDotSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override double CalculateDamageBase(GameLiving target)
        {
            return Spell.Damage;
        }

		/// <summary>
		/// Calculates min damage variance %
		/// </summary>
		/// <param name="target">spell target</param>
		/// <param name="min">returns min variance</param>
		/// <param name="max">returns max variance</param>
		public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			int speclevel = 1;
			if (m_caster is GamePlayer)
			{
				speclevel = ((GamePlayer)m_caster).GetModifiedSpecLevel(SpecConstants.Archery);
			}

			min = 1.25;
			max = 1.25;

			if (target.Level > 0)
			{
				min = 0.75 + (speclevel - 1) / (double)target.Level * 0.5;
			}

			if (speclevel - 1 > target.Level)
			{
				double overspecBonus = (speclevel - 1 - target.Level) * 0.005;
				min += overspecBonus;
				max += overspecBonus;
			}

			if (min > max) min = max;
			if (min < 0) min = 0;
		}

    }
}
