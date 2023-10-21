using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Events;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	/// <summary>
	/// Increases the target's movement speed.
	/// </summary>
	[SpellHandler("SpeedEnhancement")]
	public class SpeedEnhancementSpell : SpellHandler
	{
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			Caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new StatBuffEcsSpellEffect(initParams);
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
		
		///// <summary>
		///// Start event listener for Speed Effect
		///// </summary>
		///// <param name="effect"></param>
		//public override void OnEffectAdd(GameSpellEffect effect)
		//{
		//	GamePlayer player = effect.Owner as GamePlayer;
			
		//	GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
		//	GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackFinished, new DOLEventHandler(OnAttack));
		//	GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.CastFinished, new DOLEventHandler(OnAttack));
		//	if (player != null)
		//		GameEventMgr.AddHandler(player, GamePlayerEvent.StealthStateChanged, new DOLEventHandler(OnStealthStateChanged));
			
		//	base.OnEffectAdd(effect);
		//}

		///// <summary>
		///// Remove event listener for Speed Effect
		///// </summary>
		///// <param name="effect"></param>
		///// <param name="overwrite"></param>
		//public override void OnEffectRemove(GameSpellEffect effect, bool overwrite)
		//{
		//	GamePlayer player = effect.Owner as GamePlayer;
		//	GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
		//	GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackFinished, new DOLEventHandler(OnAttack));
		//	if (player != null)
		//		GameEventMgr.RemoveHandler(player, GamePlayerEvent.StealthStateChanged, new DOLEventHandler(OnStealthStateChanged));
			
		//	base.OnEffectRemove(effect, overwrite);
		//}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target.EffectList.GetOfType<NfRaChargeEffect>() != null)
				return;

			if (target.TempProperties.GetProperty("Charging", false))
				return;

			if (target.EffectList.GetOfType<NfRaArmsLengthEffect>() != null)
				return;

			if (target.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(OfRaSpeedOfSoundEcsEffect)) != null)
				return;

			if (target is GamePlayer && (target as GamePlayer).IsRiding)
				return;

			if (target is Keeps.GameKeepGuard)
				return;

			// Graveen: archery speed shot
			if ((Spell.Pulse != 0 || Spell.CastTime != 0) && target.InCombat)
			{
				MessageToLiving(target, "You've been in combat recently, the spell has no effect on you!", EChatType.CT_SpellResisted);
				return;
			}
			base.ApplyEffectOnTarget(target);
		}

		///// <summary>
		///// When an applied effect starts
		///// duration spells only
		///// </summary>
		///// <param name="effect"></param>
		//public override void OnEffectStart(GameSpellEffect effect)
		//{
		//	base.OnEffectStart(effect);

		//	GamePlayer player = effect.Owner as GamePlayer;

		//	if (player == null || !player.IsStealthed)
		//	{
		//		effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, Spell.Value / 100.0);
		//		SendUpdates(effect.Owner);
		//	}
		//}

		///// <summary>
		///// When an applied effect expires.
		///// Duration spells only.
		///// </summary>
		///// <param name="effect">The expired effect</param>
		///// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		///// <returns>immunity duration in milliseconds</returns>
		//public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		//{
		//	effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);

		//	if (!noMessages)
		//	{
		//		SendUpdates(effect.Owner);
		//	}

		//	return base.OnEffectExpires(effect, noMessages);
		//}


		/// <summary>
		/// Sends updates on effect start/stop
		/// </summary>
		/// <param name="owner"></param>
		public virtual void SendUpdates(GameLiving owner)
		{
			if (owner.IsMezzed || owner.IsStunned)
				return;

			if (owner is GamePlayer)
			{
				((GamePlayer)owner).Out.SendUpdateMaxSpeed();
			}
			else if (owner is GameNpc)
			{
				GameNpc npc = (GameNpc)owner;
				short maxSpeed = npc.MaxSpeed;
				if (npc.CurrentSpeed > maxSpeed)
					npc.CurrentSpeed = maxSpeed;
			}
		}

		/// <summary>
		/// Handles attacks on player/by player
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
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
			else if (sp == null && (ad.AttackResult != EAttackResult.HitStyle && ad.AttackResult != EAttackResult.HitUnstyled))
			{
				return;
			}
			else if (sp != null && (sp.HasPositiveEffect || ad == null))
			{
				return;
			}

			if (living.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(OfRaSpeedOfSoundEcsEffect)) != null)
				return;
			
			//GameSpellEffect speed = SpellHandler.FindEffectOnTarget(living, this);
			EcsGameEffect speed = EffectListService.GetEffectOnTarget(living, EEffect.MovementSpeedBuff);
			if (speed != null)
				EffectService.RequestImmediateCancelEffect(speed);
				//speed.Cancel(false);
		}

		/// <summary>
		/// Handles stealth state changes
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private void OnStealthStateChanged(CoreEvent e, object sender, EventArgs arguments)
		{
			GamePlayer player = (GamePlayer)sender;
			if (player.IsStealthed)
				player.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
			else player.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, Spell.Value / 100.0);
			// max speed update is sent in setalth method
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
		public SpeedEnhancementSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
