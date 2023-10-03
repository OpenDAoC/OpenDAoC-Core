﻿/*
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
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
	[SpellHandler("RampingDamageFocus")]
	public class RampingDamageFocus : SpellHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private int pulseCount = 0;
		private ISpellHandler snareSubSpell;

		public RampingDamageFocus(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, new FocusSpell(spell), spellLine) 
		{
			snareSubSpell = Spell.Value > 0 ? CreateSnare() : null;
		}

		public override void FinishSpellCast(GameLiving target)
		{
			Target = target;
			Caster.Mana -= (PowerCost(target) + Spell.PulsePower);
			base.FinishSpellCast(target);
			OnDirectEffect(target);
		}

		public override bool StartSpell(GameLiving target)
		{
			if (Target == null)
				Target = target;

			if (Target == null) return false;

			ApplyEffectOnTarget(target);
			return true;
		}

		public override void OnSpellPulse(PulsingSpellEffect effect)
		{
			if (Caster.ObjectState != GameObject.eObjectState.Active)
				return;
			if (Caster.IsStunned || Caster.IsMezzed)
				return;

			//(Caster as GamePlayer).Out.SendCheckLOS(Caster, m_spellTarget, CheckLOSPlayerToTarget);

			if (Caster.Mana >= Spell.PulsePower)
			{
				Caster.Mana -= Spell.PulsePower;
				SendEffectAnimation(Caster, 0, true, 1); // pulsing auras or songs
				pulseCount += 1;
				OnDirectEffect(Target);
			}
			else
			{
				MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
				FocusSpellAction(/*null, Caster, null*/);
				effect.Cancel(false);
			}
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellEffect(this, CalculateEffectDuration(target, effectiveness), 0, effectiveness);
		}

		public override bool CasterIsAttacked(GameLiving attacker)
		{
			if (Spell.Uninterruptible && Caster.GetDistanceTo(attacker) > 200)
				return false;

            if (Caster.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration)
                || Caster.effectListComponent.ContainsEffectForEffectType(eEffect.FacilitatePainworking)
                || Caster.effectListComponent.ContainsEffectForEffectType(eEffect.QuickCast))
                return false;
           
			if (IsInCastingPhase && Stage < 2)
			{
				Caster.LastInterruptMessage = attacker.GetName(0, true) + " attacks you and your spell is interrupted!";
				MessageToLiving(Caster, Caster.LastInterruptMessage, eChatType.CT_SpellResisted);
				InterruptCasting(); // always interrupt at the moment
				return true;
			}
			return false;
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;

			var targets = SelectTargets(target);

			foreach (GameLiving t in targets)
			{
				if (Util.Chance(CalculateSpellResistChance(t)))
				{
					OnSpellResisted(t);
					continue;
				}
				
				DealDamage(t);

				if (Spell.Value > 0)
				{
					snareSubSpell.StartSpell(t);
				}
			}
		}

		protected virtual void DealDamage(GameLiving target)
		{
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			double growthPercent = Spell.LifeDrainReturn * 0.01;
			double growthCapPercent = Spell.AmnesiaChance * 0.01;
			double damageIncreaseInPercent = Math.Min(pulseCount * growthPercent, growthCapPercent);

			AttackData ad = CalculateDamageToTarget(target);
			ad.Damage += (int)(ad.Damage * damageIncreaseInPercent);
			SendDamageMessages(ad);
			DamageTarget(ad, true);			
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>(32);
				GamePlayer p = null;

				if (Spell.Damage != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
				list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Target", Spell.Target) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Target", Spell.Target));
				if (Spell.Range != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Range", Spell.Range) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Range", Spell.Range));
				if (Spell.Duration != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Duration") + " " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'") : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " (Focus) " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
				if (Spell.Frequency != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
				if (Spell.Power != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
				list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
				if (Spell.Radius != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Radius", Spell.Radius) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Radius", Spell.Radius));
				if (Spell.DamageType != eDamageType.Natural)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));
				list.Add(" "); //empty line
				list.Add("Repeating direct damage spell that starts at " + Spell.Damage + " " + Spell.DamageType + " damage and increase by " + Spell.LifeDrainReturn + "% every tick up to a maximum of " + Spell.AmnesiaChance + "%.");
				list.Add(" "); //empty line
				if (Spell.Value > 0)
					list.Add("The target is slowed by " + Spell.Value + "%.");

				return list;
			}
		}

		private ISpellHandler CreateSnare()
		{
			DbSpell dbSpell = new DbSpell();
			dbSpell.ClientEffect = Spell.ClientEffect;
			dbSpell.Icon = Spell.Icon;
			dbSpell.Type = eSpellType.SpeedDecrease.ToString();
			dbSpell.Duration = (Spell.Radius == 0) ? 10 : 3;
			dbSpell.Target = "Enemy";
			dbSpell.Range = 1500;
			dbSpell.Value = Spell.Value;
			dbSpell.Name = Spell.Name + " Snare";
			dbSpell.DamageType = (int)Spell.DamageType;
			Spell subSpell = new Spell(dbSpell, Spell.Level);
			ISpellHandler subSpellHandler = new SnareWithoutImmunity(Caster, subSpell, SpellLine);
			return subSpellHandler;
		}
	}

	public class SnareWithoutImmunity : SpeedDecreaseSpellHandler
	{
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			return 0;
		}

		public SnareWithoutImmunity(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	public class FocusSpell : Spell
	{
		public FocusSpell(Spell spell) : base(spell, spell.SpellType) 
		{
			if (spell.Frequency == 0) Frequency = 5000;
			else Frequency = spell.Frequency;
		}

		public override bool IsFocus => true;
		public override int Pulse => 1;
		public override int Frequency { get; }
	}
}
