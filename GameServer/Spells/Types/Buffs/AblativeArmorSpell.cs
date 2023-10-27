using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS.Spells;

// Melee ablative.
[SpellHandler("AblativeArmor")]
public class AblativeArmorSpell : SpellHandler
{
	public AblativeArmorSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

	// Spell damage represent the absorb% of the ablative buff, but can't be superior to 100 and 0 must default to 25.
	public static int ValidateSpellDamage(int spellDamage)
	{
		return spellDamage > 100 ? 100 : spellDamage < 1 ? 25 : spellDamage;
	}

	public override void CreateECSEffect(EcsGameEffectInitParams initParams)
	{
		new AblativeArmorEcsSpellEffect(initParams);
	}

	public override void OnEffectStart(GameSpellEffect effect) { }

	public override void FinishSpellCast(GameLiving target)
	{
		m_caster.Mana -= PowerCost(target);
		base.FinishSpellCast(target);
	}
	
	/// <summary>
	/// Calculates the effect duration in milliseconds
	/// </summary>
	/// <param name="target">The effect target</param>
	/// <param name="effectiveness">The effect effectiveness</param>
	/// <returns>The effect duration in milliseconds</returns>
	protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
	{
		double duration = Spell.Duration;
		duration *= 1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01;
		return (int)duration;
	}

	public virtual bool MatchingDamageType(ref AttackData ad)
	{
		if (ad == null || (ad.AttackResult != EAttackResult.HitStyle && ad.AttackResult != EAttackResult.HitUnstyled))
			return false;

		if (!ad.IsMeleeAttack && ad.AttackType != EAttackType.Ranged)
			return false;
		
		return true;
	}

	public virtual void OnDamageAbsorbed(AttackData ad, int DamageAmount) { }
	
	public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
	{
		if (Spell.Pulse != 0 || Spell.Concentration != 0 || e.RemainingTime < 1)
			return null;

		DbPlayerXEffect eff = new DbPlayerXEffect();
		eff.Var1 = Spell.ID;
		eff.Duration = e.RemainingTime;
		eff.IsHandler = true;
		eff.Var2 = (int)Spell.Value;
		eff.SpellLine = SpellLine.KeyName;
		return eff;
	}

	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>
			{
				LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "AblativeArmor.DelveInfo.Function"),
				"",
				Spell.Description,
				""
			};

			if (Spell.Damage != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "AblativeArmor.DelveInfo.Absorption1", Spell.Damage));

			if (Spell.Damage > 100)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "AblativeArmor.DelveInfo.Absorption2"));

			if (Spell.Damage == 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "AblativeArmor.DelveInfo.Absorption3"));

			if (Spell.Value != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Value", Spell.Value));

			list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));

			if (Spell.Range != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));

			if (Spell.Duration >= ushort.MaxValue * 1000)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");
			else if (Spell.Duration > 60000)
				list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
			else if (Spell.Duration != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));

			if (Spell.Power != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
			if (Spell.CastTime < 0.1)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "AblativeArmor.DelveInfo.CastingTime"));
			else if (Spell.CastTime > 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));

			if (ServerProperty.SERV_LANGUAGE != "DE")
			{
				//SpellType
				list.Add(GetAblativeType());

				//Radius
				if (Spell.Radius != 0)
					list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Radius", Spell.Radius));

				//Frequency
				if (Spell.Frequency != 0)
					list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));

				//DamageType
				if (Spell.DamageType != 0)
					list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.DamageType", Spell.DamageType));
			}

			return list;
		}
	}

	// For delve info.
	protected virtual string GetAblativeType()
	{
		return "Type: Melee Absorption";
	}
}

// Magic Ablative.
[SpellHandler("MagicAblativeArmor")]
public class MagicAblativeArmorSpell : AblativeArmorSpell
{
	public MagicAblativeArmorSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	
	// Check if Melee
	public override bool MatchingDamageType(ref AttackData ad)
	{
		if (ad == null || (ad.AttackResult == EAttackResult.HitStyle && ad.AttackResult == EAttackResult.HitUnstyled))
			return false;

		if (ad.IsMeleeAttack && ad.AttackType == EAttackType.Ranged)
			return false;

		return true;
	}
	
	// For delve info.
	protected override string GetAblativeType()
	{
		return "Type: Magic Absorption";
	}
}

// Both magic and melee ablative.
[SpellHandler("BothAblativeArmor")]
public class BothAblativeArmorSpell : AblativeArmorSpell
{
	public BothAblativeArmorSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

	public override bool MatchingDamageType(ref AttackData ad)
	{
		return true;
	}

	// For delve info.
	protected override string GetAblativeType()
	{
		return "Type: Melee/Magic Absorption";
	}
}