using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells;

/// <summary>
/// Base class for all resist debuffs, needed to set effectiveness and duration
/// </summary>
public abstract class AResistDebuff : PropertyChangingSpell
{
	/// <summary>
	/// Gets debuff type name for delve info
	/// </summary>
	public abstract string DebuffTypeName { get; }

    public override void CreateECSEffect(EcsGameEffectInitParams initParams)
    {
		new StatDebuffEcsSpellEffect(initParams);
    }

    /// <summary>
    /// Debuff category is 3 for debuffs
    /// </summary>
    public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }

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
		duration -= duration * target.GetResist(m_spell.DamageType) * 0.01;
		
		if (duration < 1)
			duration = 1;
		else if (duration > (Spell.Duration * 4))
			duration = (Spell.Duration * 4);
		return (int)duration;
	}

	public override void ApplyEffectOnTarget(GameLiving target)
	{
		//check for existing effect
		// var debuffs = target.effectListComponent.GetSpellEffects()
		// 					.Where(x => x.SpellHandler is AbstractResistDebuff);

		// foreach (var debuff in debuffs)
		// {
		// 	var debuffSpell = debuff.SpellHandler as AbstractResistDebuff;

		// 	if (debuffSpell.Property1 == this.Property1 && debuffSpell.Spell.Value >= Spell.Value)
		// 	{
		// 		// Old Spell is Better than new one
		// 		SendSpellResistAnimation(target);
		// 		this.MessageToCaster(eChatType.CT_SpellResisted, "{0} already has that effect.", target.GetName(0, true));
		// 		MessageToCaster("Wait until it expires. Spell Failed.", eChatType.CT_SpellResisted);
		// 		// Prevent Adding.
		// 		return;
		// 	}
		// }


		//TODO: correct effectiveness formula
		// invoke direct effect if not resisted for DD w/ debuff spells
		if (Caster is GamePlayer && Spell.Level > 0)
        {
            if (((GamePlayer)Caster).PlayerClass.ClassType == EPlayerClassType.ListCaster)
			{
				int specLevel = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
				Effectiveness = 0.75;
                Effectiveness += (specLevel - 1.0) * 0.5 / Spell.Level;
                Effectiveness = Math.Max(0.75, Effectiveness);
                Effectiveness = Math.Min(1.25, Effectiveness);
                Effectiveness *= (1.0 + m_caster.GetModified(EProperty.BuffEffectiveness) * 0.01);
            }
			else
			{
				Effectiveness = 1.0; 
				Effectiveness *= (1.0 + m_caster.GetModified(EProperty.DebuffEffectivness) * 0.01);
			}

			Effectiveness *= GetCritBonus();
		}
        
		
		base.ApplyEffectOnTarget(target);

		if (target.Realm == 0 || Caster.Realm == 0)
		{
			target.LastAttackedByEnemyTickPvE = GameLoopMgr.GameLoopTime;
			Caster.LastAttackTickPvE = GameLoopMgr.GameLoopTime;
		}
		else
		{
			target.LastAttackedByEnemyTickPvP = GameLoopMgr.GameLoopTime;
			Caster.LastAttackTickPvP = GameLoopMgr.GameLoopTime;
		}
		if(target is GameNpc)
		{
			IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
			if (aggroBrain != null)
				aggroBrain.AddToAggroList(Caster, 1);
		}
		if(Spell.CastTime>0) target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
	}

    private double GetCritBonus()
    {
        double critMod = 1.0;
        int critChance = Caster.DotCriticalChance;

        if (critChance <= 0)
            return critMod;

        GamePlayer playerCaster = Caster as GamePlayer;

        if (playerCaster?.UseDetailedCombatLog == true && critChance > 0)
            playerCaster.Out.SendMessage($"Debuff crit chance: {Caster.DotCriticalChance}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

        if (Util.Chance(critChance))
        {                    
            critMod *= 1 + Util.Random(1, 10) * 0.1;
            playerCaster?.Out.SendMessage($"Your {Spell.Name} critically debuffs the enemy for {Math.Round(critMod - 1,3) * 100}% additional effect!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
        }

        return critMod;
    }

	/// <summary>
	/// Calculates chance of spell getting resisted
	/// </summary>
	/// <param name="target">the target of the spell</param>
	/// <returns>chance that spell will be resisted for specific target</returns>
	public override int CalculateSpellResistChance(GameLiving target)
	{
		int basechance = base.CalculateSpellResistChance(target);
		/*
		GameSpellEffect rampage = SpellHandler.FindEffectOnTarget(target, "Rampage");
		if (rampage != null)
		{
			basechance += (int)rampage.Spell.Value;
		}*/
		return Math.Min(100, basechance);
	}
	/// <summary>
	/// Updates changes properties to living
	/// </summary>
	/// <param name="target"></param>
	protected override void SendUpdates(GameLiving target)
	{
		base.SendUpdates(target);
		if (target is GamePlayer)
		{
			GamePlayer player = (GamePlayer)target;
			player.Out.SendCharResistsUpdate();
		}
	}

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			/*
			<Begin Info: Nullify Dissipation>
			Function: resistance decrease

			Decreases the target's resistance to the listed damage type.

			Resist decrease Energy: 15
			Target: Targetted
			Range: 1500
			Duration: 15 sec
			Power cost: 13
			Casting time:      2.0 sec
			Damage: Cold

			<End Info>
			 */

			var list = new List<string>();
			list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "ResistDebuff.DelveInfo.Function"));
			list.Add(" "); //empty line
			list.Add(Spell.Description);
			list.Add(" "); //empty line
			list.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "ResistDebuff.DelveInfo.Decrease", DebuffTypeName, m_spell.Value)));
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
			list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
			if (Spell.RecastDelay > 60000)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + Spell.RecastDelay / 60000 + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
			else if (Spell.RecastDelay > 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 1000).ToString() + " sec");
			if (Spell.Concentration != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.ConcentrationCost", Spell.Concentration));
			if (Spell.Radius != 0)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Radius", Spell.Radius));
			if (Spell.DamageType != EDamageType.Natural)
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));

			return list;
		}
	}

	//constructor
	public AResistDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}