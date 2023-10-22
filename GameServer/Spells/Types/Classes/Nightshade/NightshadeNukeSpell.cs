using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("NightshadeNuke")]
public class NightshadeNukeSpell : DirectDamageSpell
{
	/// <summary>
	/// Calculates min damage variance %
	/// </summary>
	/// <param name="target">spell target</param>
	/// <param name="min">returns min variance</param>
	/// <param name="max">returns max variance</param>
	public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
	{
		int speclevel = 1;
		if (Caster is GamePlayer)
		{
			speclevel = ((GamePlayer)Caster).GetModifiedSpecLevel(SpecConstants.Stealth);
			if (speclevel > ((GamePlayer)Caster).Level)
				speclevel = ((GamePlayer)Caster).Level;
		}

		min = 0.5;
		max = 1.0;

		if (target.Level > 0)
		{
			min = 0.75 + (speclevel - 1) / (double)target.Level * 0.5;
		}

		/*
		if (speclevel - 1 > target.Level)
		{
			double overspecBonus = (speclevel - 1 - target.Level) * 0.005;
			min += overspecBonus;
			max += overspecBonus;
		}*/

		if (min > max) min = max;
		if (min < 0) min = 0;
	}
	/// <summary>
	/// Calculates the base 100% spell damage which is then modified by damage variance factors
	/// </summary>
	/// <returns></returns>
	public override double CalculateDamageBase(GameLiving target)
	{
		double spellDamage = Spell.Damage;
		GamePlayer player = Caster as GamePlayer;

		if (player != null)
		{
			int strValue = player.GetModified((EProperty)player.Strength);
			spellDamage *= (strValue -player.Level) / 200.0 + 1;
		}

		if (spellDamage < 0)
			spellDamage = 0;

		return spellDamage;
	}
	public NightshadeNukeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}