using System.Text;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler for unbreakable speed decreasing spells
	/// </summary>
	[SpellHandler("UnbreakableSpeedDecrease")]
	public class UnbreakableSpeedDecreaseSpellHandler : ImmunityEffectSpellHandler
	{
		private const string EFFECT_PROPERTY = "UnbreakableSpeedDecreaseProperty";

		public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new StatDebuffECSEffect(initParams);
		}
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			var effect = EffectListService.GetSpellEffectOnTarget(target, eEffect.MovementSpeedDebuff);
			if (target.HasAbility(Abilities.CCImmunity)||target.HasAbility(Abilities.RootImmunity) || 
				EffectListService.GetEffectOnTarget(target, eEffect.SnareImmunity) != null ||
				EffectListService.GetEffectOnTarget(target, eEffect.SpeedOfSound) != null || 
				(effect != null && effect.SpellHandler.Spell.Value == 99)
				&& !Spell.Name.Equals("Prevent Flight"))
			{
				//EffectService.RequestCancelEffect(effect);
				MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
				OnSpellResisted(target);
				return;
			}
			if (target.EffectList.GetOfType<ChargeEffect>() != null)
			{
				MessageToCaster(target.Name + " is moving to fast for this spell to have any effect!", eChatType.CT_SpellResisted);
				return;
			}

			base.ApplyEffectOnTarget(target);
		}

		/// <summary>
		/// When an applied effect starts,
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0-Spell.Value*0.01);
			effect.Owner.OnMaxSpeedChange();

			MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
			Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), eChatType.CT_Spell, effect.Owner);

			RestoreSpeedTimer timer = new RestoreSpeedTimer(effect.Owner, effect);
			effect.Owner.TempProperties.SetProperty(EFFECT_PROPERTY, timer);
			timer.Interval = 650;
			timer.Start(1 + (effect.Duration >> 1));

			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
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
			base.OnEffectExpires(effect,noMessages);

			RestoreSpeedTimer timer = effect.Owner.TempProperties.GetProperty<RestoreSpeedTimer>(EFFECT_PROPERTY, null);
			effect.Owner.TempProperties.RemoveProperty(EFFECT_PROPERTY);
			if(timer!=null) timer.Stop();

			effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
			effect.Owner.OnMaxSpeedChange();

			MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
			Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), eChatType.CT_SpellExpires, effect.Owner);

			return 60000;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = base.CalculateEffectDuration(target, effectiveness);
			duration *= target.GetModified(eProperty.SpeedDecreaseDurationReduction) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);

			if (Spell.Name.Equals("Prevent Flight"))
				duration = Spell.Duration;
			return (int)duration;
		}

		/// <summary>
		/// Slowly restores the livings speed
		/// </summary>
		public sealed class RestoreSpeedTimer : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The speed changing effect
			/// </summary>
			private readonly GameSpellEffect m_effect;

			/// <summary>
			/// Constructs a new RestoreSpeedTimer
			/// </summary>
			/// <param name="effect">The speed changing effect</param>
			public RestoreSpeedTimer(GameObject target, GameSpellEffect effect) : base(target)
			{
				m_effect = effect;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GameSpellEffect effect = m_effect;

				double factor = 2.0 - (effect.Duration - effect.RemainingTime)/(double)(effect.Duration>>1);
				if (factor < 0) factor = 0;
				else if (factor > 1) factor = 1;

				effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - effect.Spell.Value*factor*0.01);
				effect.Owner.OnMaxSpeedChange();

				return factor <= 0 ? 0 : Interval;
			}


			/// <summary>
			/// Returns short information about the timer
			/// </summary>
			/// <returns>Short info about the timer</returns>
			public override string ToString()
			{
				return new StringBuilder(base.ToString())
					.Append(" SpeedDecreaseEffect: (").Append(m_effect.ToString()).Append(')')
					.ToString();
			}
		}

		/// <summary>
		/// Constructs a new UnbreakableSpeedDecreaseSpellHandler
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public UnbreakableSpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
