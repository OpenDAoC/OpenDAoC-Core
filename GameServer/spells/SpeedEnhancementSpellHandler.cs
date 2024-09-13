using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Increases the target's movement speed.
	/// </summary>
	[SpellHandler(eSpellType.SpeedEnhancement)]
	public class SpeedEnhancementSpellHandler : SpellHandler
	{
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			Caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new StatBuffECSEffect(initParams);
        }

		protected override int CalculateEffectDuration(GameLiving target)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
			if (Spell.InstrumentRequirement != 0)
			{
				DbInventoryItem instrument = Caster.ActiveWeapon;
				if (instrument != null)
				{
					duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level); // up to 200% duration for songs
					duration *= instrument.Condition / (double)instrument.MaxCondition * instrument.Quality / 100;
				}
			}
			
			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);
			return (int)duration;
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target.EffectList.GetOfType<ChargeEffect>() != null)
				return;

			if (target.TempProperties.GetProperty<bool>("Charging"))
				return;

			if (target.EffectList.GetOfType<ArmsLengthEffect>() != null)
				return;

			if (target.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) != null)
				return;

			if (target is GamePlayer && (target as GamePlayer).IsRiding)
				return;

			if (target is Keeps.GameKeepGuard)
				return;

			// Graveen: archery speed shot
			if ((Spell.Pulse != 0 || Spell.CastTime != 0) && target.InCombat)
			{
				MessageToLiving(target, "You've been in combat recently, the spell has no effect on you!", eChatType.CT_SpellResisted);
				return;
			}
			base.ApplyEffectOnTarget(target);
		}

		/// <summary>
		/// Handles attacks on player/by player
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private void OnAttack(DOLEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
			AttackFinishedEventArgs attackFinished = arguments as AttackFinishedEventArgs;
			CastingEventArgs castFinished = arguments as CastingEventArgs;
			AttackData ad = null;
			ISpellHandler sp = null;

			if (attackedByEnemy != null)
			{
				ad = attackedByEnemy.AttackData;
			}
			else if (attackFinished != null)
			{
				ad = attackFinished.AttackData;
			}
			else if (castFinished != null)
			{
				sp = castFinished.SpellHandler;
				ad = castFinished.LastAttackData;
			}

			// Speed should drop if the player casts an offensive spell
			if (sp == null && ad == null)
			{
				return;
			}
			else if (sp == null && (ad.AttackResult != eAttackResult.HitStyle && ad.AttackResult != eAttackResult.HitUnstyled))
			{
				return;
			}
			else if (sp != null && (sp.HasPositiveEffect || ad == null))
			{
				return;
			}

			if (living.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) != null)
				return;
			
			//GameSpellEffect speed = SpellHandler.FindEffectOnTarget(living, this);
			ECSGameEffect speed = EffectListService.GetEffectOnTarget(living, eEffect.MovementSpeedBuff);
			if (speed != null)
				EffectService.RequestImmediateCancelEffect(speed);
				//speed.Cancel(false);
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				/*
				<Begin Info: Motivation Sng>
 
				The movement speed of the target is increased.
 
				Target: Group
				Range: 2000
				Duration: 30 sec
				Frequency: 6 sec
				Casting time:      3.0 sec
				
				This spell's effect will not take hold while the target is in combat.
				<End Info>
				*/
				IList<string> list = base.DelveInfo;

				list.Add(" "); //empty line
				list.Add("This spell's effect will not take hold while the target is in combat.");

				return list;
			}
		}

		/// <summary>
		/// The spell handler constructor
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public SpeedEnhancementSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
