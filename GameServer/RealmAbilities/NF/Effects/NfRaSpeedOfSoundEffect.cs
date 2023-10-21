using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Calculators;

namespace Core.GS.Effects
{
	public class NfRaSpeedOfSoundEffect : TimedEffect, IGameEffect
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public NfRaSpeedOfSoundEffect(int duration)
			: base(duration)
		{ }

		CoreEventHandler m_attackFinished = new CoreEventHandler(AttackFinished);


		/// <summary>
		/// Called when effect is to be started
		/// </summary>
		/// <param name="living">The living to start the effect for</param>
		public override void Start(GameLiving living)
		{
			// log.InfoFormat("Starting SpeedOfSoundEffect for player {0} giving speed of {1}", living.Name, PropertyCalc.MaxSpeedCalculator.SPEED4);

			base.Start(living);
			living.TempProperties.SetProperty("Charging", true);
			GameEventMgr.AddHandler(living, GameLivingEvent.AttackFinished, m_attackFinished);
			GameEventMgr.AddHandler(living, GameLivingEvent.CastFinished, m_attackFinished);
			living.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, MaxMovementSpeedCalculator.SPEED4);		
			if (living is GamePlayer)
				(living as GamePlayer).Out.SendUpdateMaxSpeed();
		}

		/// <summary>
		/// Called when the effectowner attacked an enemy
		/// </summary>
		/// <param name="e">The event which was raised</param>
		/// <param name="sender">Sender of the event</param>
		/// <param name="args">EventArgs associated with the event</param>
		private static void AttackFinished(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = (GamePlayer)sender;
			if (e == GameLivingEvent.CastFinished)
			{
				CastingEventArgs cfea = args as CastingEventArgs;

				if (cfea.SpellHandler.Caster != player)
					return;

				//cancel if the effectowner casts a non-positive spell
				if (!cfea.SpellHandler.HasPositiveEffect)
				{
					NfRaSpeedOfSoundEffect effect = player.EffectList.GetOfType<NfRaSpeedOfSoundEffect>();
					if (effect != null)
						effect.Cancel(false);
				}
			}
			else if (e == GameLivingEvent.AttackFinished)
			{
				AttackFinishedEventArgs afargs = args as AttackFinishedEventArgs;
				if (afargs == null)
					return;

				if (afargs.AttackData.Attacker != player)
					return;

				switch (afargs.AttackData.AttackResult)
				{
					case EAttackResult.HitStyle:
					case EAttackResult.HitUnstyled:
					case EAttackResult.Blocked:
					case EAttackResult.Evaded:
					case EAttackResult.Fumbled:
					case EAttackResult.Missed:
					case EAttackResult.Parried:
						NfRaSpeedOfSoundEffect effect = player.EffectList.GetOfType<NfRaSpeedOfSoundEffect>();
						if (effect != null)
							effect.Cancel(false);
						break;
				}
			}
		}

		public override void Stop()
		{
			
			base.Stop();
			m_owner.TempProperties.RemoveProperty("Charging");
			m_owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
			if (m_owner is GamePlayer)
            {
				//log.InfoFormat("Stop SpeedOfSoundEffect for player {0}", m_owner.Name);
				(m_owner as GamePlayer).Out.SendUpdateMaxSpeed();
			}
				
			GameEventMgr.RemoveHandler(m_owner, GameLivingEvent.AttackFinished, m_attackFinished);
			GameEventMgr.RemoveHandler(m_owner, GameLivingEvent.CastFinished, m_attackFinished);
		}


		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name
		{
			get
			{
				return "Speed of Sound";
			}
		}

		/// <summary>
		/// Icon ID
		/// </summary>
		public override UInt16 Icon
		{
			get
			{
				return 3020;
			}
		}

		/// <summary>
		/// Delve information
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delveInfoList = new List<string>();
				delveInfoList.Add("Gives immunity to stun/snare/root and mesmerize spells and provides unbreakeable speed.");
				delveInfoList.Add(" ");

				int seconds = (int)(RemainingTime / 1000);
				if (seconds > 0)
				{
					delveInfoList.Add(" ");
					delveInfoList.Add("- " + seconds + " seconds remaining.");
				}

				return delveInfoList;
			}
		}
	}
}
