using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Spells
{
	/// <summary>
	/// Reduce range needed to cast the spell
	/// </summary>
	[SpellHandler("Nearsight")]
	public class NearsightSpell : ImmunityEffectSpellHandler
	{
        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new NearsightEcsSpellEffect(initParams);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
			//Nearsight Immunity check
			if (target.HasAbility(Abilities.NSImmunity))
			{
				MessageToCaster(target.Name + " can't be nearsighted!", EChatType.CT_SpellResisted);
				SendEffectAnimation(target, 0, false, 0);
				return;
			}
			if (EffectListService.GetEffectOnTarget(target, EEffect.Nearsight) != null)
            {
				MessageToCaster(target.Name + " already has this effect!", EChatType.CT_SpellResisted);
				SendEffectAnimation(target, 0, false, 0);
				//target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
				return;
			}
			if (EffectListService.GetEffectOnTarget(target, EEffect.NearsightImmunity) != null)
			{
				MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
				SendEffectAnimation(target, 0, false, 0);
				
				return;
			}
			base.ApplyEffectOnTarget(target);
        }
        /// <summary>
        /// Calculates chance of spell getting resisted
        /// </summary>
        /// <param name="target">the target of the spell</param>
        /// <returns>chance that spell will be resisted for specific target</returns>
        public override int CalculateSpellResistChance(GameLiving target)
        {
            //Bonedancer rr5
            if (target.EffectList.GetOfType<NfRaAllureOfDeathEffect>() != null)
            {
                return NfRaAllureOfDeathEffect.nschance;
            }
            return base.CalculateSpellResistChance(target);

        }
		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			//GameSpellEffect mezz = SpellHandler.FindEffectOnTarget(effect.Owner, "Mesmerize");
 		//	if(mezz != null) mezz.Cancel(false);
			//// percent category
			//effect.Owner.DebuffCategory[(int)eProperty.ArcheryRange] += (int)Spell.Value;
			//effect.Owner.DebuffCategory[(int)eProperty.SpellRange] += (int)Spell.Value;
			//SendEffectAnimation(effect.Owner, 0, false, 1);
			//MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
			//Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_Spell, effect.Owner);
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//// percent category
			//effect.Owner.DebuffCategory[(int)eProperty.ArcheryRange] -= (int)Spell.Value;
			//effect.Owner.DebuffCategory[(int)eProperty.SpellRange] -= (int)Spell.Value;
			//if (!noMessages) {
			//	MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
			//	Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			//}
			return 60000;
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				// value should be in percents
				/*
				 * <Begin Info: Encrust Eyes>
				 * Function: nearsight
				 * Target's effective range of all their ranged attacks (archery and magic) reduced.
				 *  
				 * Value: 25%
				 * Target: Targetted
				 * Range: 2300
				 * Duration: 2:0 min
				 * Power cost: 5
				 * Casting time:      2.0 sec
				 * Damage: Matter
				 *  
				 * <End Info>
				 */

				var list = new List<string>();

                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "NearsightSpellHandler.DelveInfo.Function", (Spell.SpellType.ToString() == "" ? "(not implemented)" : Spell.SpellType.ToString())));
				list.Add(" "); //empty line
				list.Add(Spell.Description);
				list.Add(" "); //empty line
                if (Spell.Damage != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
                if (Spell.Value != 0)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Value", (int)Spell.Value)) + "%");
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration" + " Permanent."));
				else if(Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration/60000 + ":" + (Spell.Duration%60000/1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Frequency != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                if (Spell.RecastDelay > 60000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
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

		// constructor
		public NearsightSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	/// <summary>
	/// Reduce efficacity of nearsight effect
	/// </summary>
	[SpellHandler("NearsightReduction")]
	public class NearsightReductionSpellHandler : SpellHandler
	{
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}	
		// constructor
		public NearsightReductionSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}
