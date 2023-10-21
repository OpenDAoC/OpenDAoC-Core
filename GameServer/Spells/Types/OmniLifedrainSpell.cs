using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	[SpellHandler("OmniLifedrain")]
	public class OmniLifedrainSpell : DirectDamageSpell
	{
		/// <summary>
		/// execute direct effect
		/// </summary>
		/// <param>target that gets the damage</param>
		/// <param>factor from 0..1 (0%-100%)</param>
		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// calc damage and healing
			AttackData ad = CalculateDamageToTarget(target);
			SendDamageMessages(ad);
			DamageTarget(ad, true);
			StealLife(ad);
			StealEndo(ad);
			StealPower(ad);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

		/// <summary>
		/// Uses percent of damage to heal the caster
		/// </summary>
		public virtual void StealLife(AttackData ad)
		{
			if (ad == null) return;
			if (!m_caster.IsAlive) return;

			int heal = (ad.Damage + ad.CriticalDamage)* Spell.LifeDrainReturn / 100; // % factor on all drains
			if (m_caster.IsDiseased)
			{
				MessageToCaster("You are diseased!", EChatType.CT_SpellResisted);
				heal >>= 1;
			}

            heal = m_caster.ChangeHealth(m_caster, EHealthChangeType.Spell, heal);

			if (heal > 0)
			{
				MessageToCaster("You steal " + heal + " hit point" + (heal == 1 ? "." : "s."), EChatType.CT_Spell);


                #region PVP DAMAGE

                if (m_caster is NecromancerPet && ((m_caster as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || m_caster is GamePlayer)
                {
                    if (m_caster.DamageRvRMemory > 0)
                        m_caster.DamageRvRMemory -= (long)Math.Max(heal, 0);
                }

                #endregion PVP DAMAGE

			}
			else
			{
				MessageToCaster("You cannot absorb any more life.", EChatType.CT_SpellResisted);

                #region PVP DAMAGE

                if (m_caster is NecromancerPet && ((m_caster as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || m_caster is GamePlayer)
                {
                    if (m_caster.DamageRvRMemory > 0)
                        m_caster.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                }
                #endregion PVP DAMAGE
			}
		}
		/// <summary>
		/// Uses percent of damage to renew endurance
		/// </summary>
		public virtual void StealEndo(AttackData ad)
		{
			if (ad == null) return;
			if (!m_caster.IsAlive) return;

			int renew = ((ad.Damage + ad.CriticalDamage) * Spell.ResurrectHealth / 100) * Spell.LifeDrainReturn / 100; // %endo returned
            renew = m_caster.ChangeEndurance(m_caster, EEnduranceChangeType.Spell, renew);
			if (renew > 0)
			{
				MessageToCaster("You steal " + renew + " endurance.", EChatType.CT_Spell);
			}
			else
			{
				MessageToCaster("You cannot steal any more endurance.", EChatType.CT_SpellResisted);
			}
		}
		/// <summary>
		/// Uses percent of damage to replenish power
		/// </summary>
		public virtual void StealPower(AttackData ad)
		{
			if (ad == null) return;
			if (!m_caster.IsAlive) return;

			int replenish = ((ad.Damage + ad.CriticalDamage) * Spell.ResurrectMana  / 100) * Spell.LifeDrainReturn / 100; // %mana returned
            replenish = m_caster.ChangeMana(m_caster, EPowerChangeType.Spell, replenish);
			if (replenish > 0)
			{
				MessageToCaster("You steal " + replenish + " power.", EChatType.CT_Spell);
			}
			else
			{
				MessageToCaster("Your power is already full.", EChatType.CT_SpellResisted);
			}
		}

		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		/// <returns></returns>
		public override double CalculateDamageBase(GameLiving target)
		{
			double spellDamage = Spell.Damage;
			return spellDamage;
		}

		// constructor
		public OmniLifedrainSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				//Name
				list.Add("omni-lifedrain \n");
				//Description
				list.Add("Damages the target. A portion of damage is returned to the caster as health, power, and endurance.\n");
				list.Add("Damage: " + Spell.Damage);
                list.Add("Health returned: " + Spell.LifeDrainReturn + "% of damage dealt \n Power returned: " + Spell.ResurrectMana  + "% of damage dealt \n Endurance returned: "+ Spell.ResurrectHealth  +"% of damage dealt");
				list.Add("Target: " + Spell.Target);
				if (Spell.Range != 0) list.Add("Range: " + Spell.Range);
				list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
				if (Spell.DamageType != EDamageType.Natural)
					list.Add("Damage: " + GlobalConstants.DamageTypeToName(Spell.DamageType));
				return list;
			}
		}
	}
}
