using System.Text;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	/// <summary>
	/// Spell handler for unbreakable speed decreasing spells
	/// </summary>
	[SpellHandler("UnbreakableSpeedDecrease")]
	public class UnbreakableSpeedDecreaseSpell : ImmunityEffectSpellHandler
	{
		private const string EFFECT_PROPERTY = "UnbreakableSpeedDecreaseProperty";

		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new StatDebuffEcsSpellEffect(initParams);
		}
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			var effect = EffectListService.GetSpellEffectOnTarget(target, EEffect.MovementSpeedDebuff);
			if (target.HasAbility(Abilities.CCImmunity)||target.HasAbility(Abilities.RootImmunity) || 
				EffectListService.GetEffectOnTarget(target, EEffect.SnareImmunity) != null ||
				EffectListService.GetEffectOnTarget(target, EEffect.SpeedOfSound) != null || 
				(effect != null && effect.SpellHandler.Spell.Value == 99)
				&& !Spell.Name.Equals("Prevent Flight"))
			{
				//EffectService.RequestCancelEffect(effect);
				MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
				OnSpellResisted(target);
				return;
			}
			if (target.EffectList.GetOfType<NfRaChargeEffect>() != null)
			{
				MessageToCaster(target.Name + " is moving to fast for this spell to have any effect!", EChatType.CT_SpellResisted);
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
			effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect, 1.0-Spell.Value*0.01);

			SendUpdates(effect.Owner);

			MessageToLiving(effect.Owner, Spell.Message1, EChatType.CT_Spell);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), EChatType.CT_Spell, effect.Owner);

			RestoreSpeedTimer timer = new RestoreSpeedTimer(effect.Owner, effect);
			effect.Owner.TempProperties.SetProperty(EFFECT_PROPERTY, timer);
			timer.Interval = 650;
			timer.Start(1 + (effect.Duration >> 1));

			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
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

			effect.Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, effect);

			SendUpdates(effect.Owner);

			MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), EChatType.CT_SpellExpires, effect.Owner);

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
			duration *= target.GetModified(EProperty.SpeedDecreaseDurationReduction) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);

			if (Spell.Name.Equals("Prevent Flight"))
				duration = Spell.Duration;
			return (int)duration;
		}

		/// <summary>
		/// Sends updates on effect start/stop
		/// </summary>
		/// <param name="owner"></param>
		public static void SendUpdates(GameLiving owner)
		{
			if (owner.IsMezzed || owner.IsStunned)
				return;

			GamePlayer player = owner as GamePlayer;
			if (player != null)
				player.Out.SendUpdateMaxSpeed();

			GameNpc npc = owner as GameNpc;
			if (npc != null)
			{
				short maxSpeed = npc.MaxSpeed;
				if (npc.CurrentSpeed > maxSpeed)
					npc.CurrentSpeed = maxSpeed;
			}
		}

		/// <summary>
		/// Slowly restores the livings speed
		/// </summary>
		public sealed class RestoreSpeedTimer : EcsGameTimerWrapperBase
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
			protected override int OnTick(EcsGameTimer timer)
			{
				GameSpellEffect effect = m_effect;

				double factor = 2.0 - (effect.Duration - effect.RemainingTime)/(double)(effect.Duration>>1);
				if (factor < 0) factor = 0;
				else if (factor > 1) factor = 1;

				effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect, 1.0 - effect.Spell.Value*factor*0.01);

				SendUpdates(effect.Owner);

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
		public UnbreakableSpeedDecreaseSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
