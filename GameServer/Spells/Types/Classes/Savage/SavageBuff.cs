using System;
using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells
{
	
    // Main class for savage buffs
	public abstract class ASavageBuff : PropertyChangingSpell
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }

		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new SavageBuffEcsEffect(initParams);
		}

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			int cost = PowerCost(Caster);
			if (Caster.Health < cost)
			{
				MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SavageEnduranceHeal.CheckBeginCast.InsuffiscientHealth"), EChatType.CT_SpellResisted);
				return false;
			}
			return base.CheckBeginCast(selectedTarget);
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

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			SendUpdates(effect.Owner);
		}
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>(16);
                //list.Add("Function: " + (Spell.SpellType == "" ? "(not implemented)" : Spell.SpellType));
                //list.Add(" "); //empty line
                list.Add(Spell.Description);
                list.Add(" "); //empty line
                if (Spell.InstrumentRequirement != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));
                if (Spell.Damage != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
                if (Spell.LifeDrainReturn != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.HealthReturned", Spell.LifeDrainReturn));
                else if (Spell.Value != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Value", Spell.Value.ToString("0.###;0.###'%'")));
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Frequency != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.HealthCost", Spell.Power.ToString("0;0'%'")));
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
                if (Spell.IsFocus)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Focus"));

                return list;
            }
        }

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//base.OnEffectExpires(effect, noMessages);
			
			//if (m_spell.Power != 0)
			//{
			//	int cost = 0;
			//	if (m_spell.Power < 0)
			//		cost = (int)(m_caster.MaxHealth * Math.Abs(m_spell.Power) * 0.01);
			//	else
			//		cost = m_spell.Power;
			//	if (effect.Owner.Health > cost)
			//		effect.Owner.ChangeHealth(effect.Owner, eHealthChangeType.Spell, -cost);
			//}
			//SendUpdates(effect.Owner);
			return 0;
		}

		// constructor
		public ASavageBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}		
	}
	
	public abstract class ASavageStatBuff : ASavageBuff
	{
		/// <summary>
        /// Sends needed updates on start/stop
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				player.Out.SendCharStatsUpdate();
				player.Out.SendUpdateWeaponAndArmorStats();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
			}
		}
		// constructor
		public ASavageStatBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}				
	}
	public abstract class ASavageResistBuff : ASavageBuff
	{
		/// <summary>
        /// Sends needed updates on start/stop
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				player.Out.SendCharResistsUpdate();
				player.UpdatePlayerStatus();
			}
		}
		// constructor
		public ASavageResistBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}				
	}
	
	[SpellHandler("SavageParryBuff")]
	public class SavageParryBuffSpell : ASavageStatBuff
	{
		public override EProperty Property1 { get { return EProperty.ParryChance; } }

		// constructor
		public SavageParryBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	[SpellHandler("SavageEvadeBuff")]
	public class SavageEvadeBuffSpell : ASavageStatBuff
	{
		public override EProperty Property1 { get { return EProperty.EvadeChance; } }

		// constructor
		public SavageEvadeBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	[SpellHandler("SavageCombatSpeedBuff")]
	public class SavageCombatSpeedBuffSpell : ASavageStatBuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeSpeed; } }

		// constructor
		public SavageCombatSpeedBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	[SpellHandler("SavageDPSBuff")]
	public class SavageDpsBuffSpell : ASavageStatBuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeDamage; } }

		// constructor
		public SavageDpsBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}	
	[SpellHandler("SavageSlashResistanceBuff")]
	public class SavageSlashResistanceBuffSpell : ASavageResistBuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Slash; } }

		// constructor
		public SavageSlashResistanceBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	[SpellHandler("SavageThrustResistanceBuff")]
	public class SavageThrustResistanceBuffSpell : ASavageResistBuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Thrust; } }

		// constructor
		public SavageThrustResistanceBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
	[SpellHandler("SavageCrushResistanceBuff")]
	public class SavageCrushResistanceBuffSpell : ASavageResistBuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Crush; } }

		// constructor
		public SavageCrushResistanceBuffSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}


