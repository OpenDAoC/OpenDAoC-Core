using System;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
///Handlers for the savage's special endurance heal that takes health instead of mana
/// </summary>
[SpellHandler("SavageEnduranceHeal")]
public class SavageEnduranceHealSpell : EnduranceHealSpell
{
	public SavageEnduranceHealSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

	protected override void RemoveFromStat(int value)
	{
		m_caster.Health -= value;
	}

	public override int PowerCost(GameLiving target)
	{
		int cost = 0;
		if (m_spell.Power < 0)
			cost = (int)(m_caster.MaxHealth * Math.Abs(m_spell.Power) * 0.01);
		else
			cost = m_spell.Power;
		return cost;
	}

	public override bool CheckBeginCast(GameLiving selectedTarget)
	{
		int cost = PowerCost(Caster);
		if (Caster.Health < cost)
		{
            MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SavageEnduranceHeal.CheckBeginCast.InsuffiscientHealth"), EChatType.CT_SpellResisted);
            return false;
		}
		return base.CheckBeginCast(Caster);
	}
}