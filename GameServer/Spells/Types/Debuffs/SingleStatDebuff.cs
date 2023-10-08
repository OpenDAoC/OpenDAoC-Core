/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Debuffs a single stat
	/// </summary>
	public abstract class SingleStatDebuff : SingleStatBuff
	{
		// bonus category
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }

        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
			new StatDebuffEcsSpellEffect(initParams);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
		{
			// var debuffs = target.effectListComponent.GetSpellEffects()
			// 					.Where(x => x.SpellHandler is SingleStatDebuff);

			// foreach (var debuff in debuffs)
            // {
			// 	var debuffSpell = debuff.SpellHandler as SingleStatDebuff;

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


			base.ApplyEffectOnTarget(target);
			
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
			//if(target is GameNPC) 
			//{
			//	IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
			//	if (aggroBrain != null)
			//		aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			//}
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
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			duration -= duration * target.GetResist(Spell.DamageType) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);
			return (int)duration;
		}
		
		/// <summary>
		/// Calculates chance of spell getting resisted
		/// </summary>
		/// <param name="target">the target of the spell</param>
		/// <returns>chance that spell will be resisted for specific target</returns>		
        public override int CalculateSpellResistChance(GameLiving target)
        {
        	int basechance =  base.CalculateSpellResistChance(target);      
            /*
 			GameSpellEffect rampage = SpellHandler.FindEffectOnTarget(target, "Rampage");
            if (rampage != null)
            {
            	basechance += (int)rampage.Spell.Value;
            }*/
            return Math.Min(100, basechance);
        }
		// constructor
		public SingleStatDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Str stat baseline debuff
	/// </summary>
	[SpellHandler("StrengthDebuff")]
	public class StrengthDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Strength; } }

		// constructor
		public StrengthDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Dex stat baseline debuff
	/// </summary>
	[SpellHandler("DexterityDebuff")]
	public class DexterityDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Dexterity; } }	

		// constructor
		public DexterityDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Con stat baseline debuff
	/// </summary>
	[SpellHandler("ConstitutionDebuff")]
	public class ConstitutionDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Constitution; } }	

		// constructor
		public ConstitutionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Armor factor debuff
	/// </summary>
	[SpellHandler("ArmorFactorDebuff")]
	public class ArmorFactorDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ArmorFactor; } }	

		// constructor
		public ArmorFactorDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Armor Absorption debuff
	/// </summary>
	[SpellHandler("ArmorAbsorptionDebuff")]
	public class ArmorAbsorptionDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public ArmorAbsorptionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Combat Speed debuff
	/// </summary>
	[SpellHandler("CombatSpeedDebuff")]
	public class CombatSpeedDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeSpeed; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public CombatSpeedDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Melee damage debuff
	/// </summary>
	[SpellHandler("MeleeDamageDebuff")]
	public class MeleeDamageDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.MeleeDamage; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public MeleeDamageDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Fatigue reduction debuff
	/// </summary>
	[SpellHandler("FatigueConsumptionDebuff")]
	public class FatigueConsumptionDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.FatigueConsumption; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public FatigueConsumptionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Fumble chance debuff
	/// </summary>
	[SpellHandler("FumbleChanceDebuff")]
	public class FumbleChanceDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.FumbleChance; } }      
		
		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		protected override void SendUpdates(GameLiving target)
		{
		}

		// constructor
		public FumbleChanceDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	/// <summary>
	/// DPS debuff
	/// </summary>
	[SpellHandler("DPSDebuff")]
	public class DPSDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.DPS; } }	

		// constructor
		public DPSDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Skills Debuff
	/// </summary>
	[SpellHandler("SkillsDebuff")]
	public class SkillsDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.AllSkills; } }	

		// constructor
		public SkillsDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Acuity stat baseline debuff
	/// </summary>
	[SpellHandler("AcuityDebuff")]
	public class AcuityDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Acuity; } }	

		// constructor
		public AcuityDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// Quickness stat baseline debuff
	/// </summary>
	[SpellHandler("QuicknessDebuff")]
	public class QuicknessDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.Quickness; } }	

		// constructor
		public QuicknessDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	/// <summary>
	/// ToHit Skill debuff
	/// </summary>
	[SpellHandler("ToHitDebuff")]
	public class ToHitSkillDebuff : SingleStatDebuff
	{
		public override EProperty Property1 { get { return EProperty.ToHitBonus; } }	

		// constructor
		public ToHitSkillDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
 }
