using System;
using System.Text;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    public abstract class HereticImmunityEffectSpellHandler : HereticPiercingMagic
	{
		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
				Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
				Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
			}
            if (target.HasAbility(Abilities.CCImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }
            if (target.TempProperties.GetProperty<bool>("Charging"))
            {
                MessageToCaster(target.Name + " is moving to fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }
			base.ApplyEffectOnTarget(target);

			if (Spell.CastTime > 0) 
			{
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
			}
			if(target is GameNPC) 
			{
				GameNPC npc = (GameNPC)target;
				IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, 1);
			}
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			double duration = base.CalculateEffectDuration(target);
			duration *= target.GetModified(eProperty.SpeedDecreaseDurationReduction) * 0.01;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);

			return (int)duration;
		}

		/// <summary>
		/// Creates the corresponding spell effect for the spell
		/// </summary>
		/// <param name="target"></param>
		/// <param name="effectiveness"></param>
		/// <returns></returns>
		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellAndImmunityEffect(this, (int)CalculateEffectDuration(target), 0, effectiveness);
		}

		// constructor
		public HereticImmunityEffectSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}

	[SpellHandler(eSpellType.HereticSpeedDecrease)]
	public class HereticSpeedDecreaseSpellHandler : HereticImmunityEffectSpellHandler
	{
		private readonly object TIMER_PROPERTY;
		private const string EFFECT_PROPERTY = "HereticSpeedDecreaseProperty";

		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0-Spell.Value*0.01);
			effect.Owner.OnMaxSpeedChange();

			MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
			Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), eChatType.CT_Spell, effect.Owner);

			RestoreSpeedTimer timer = new(null, effect);
			effect.Owner.TempProperties.SetProperty(EFFECT_PROPERTY, timer);

			//REVOIR
			timer.Interval = 650;
			timer.Start(1 + (effect.Duration >> 1));

			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}


		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect,noMessages);

			ECSGameTimer timer = effect.Owner.TempProperties.GetProperty<ECSGameTimer>(EFFECT_PROPERTY);
			effect.Owner.TempProperties.RemoveProperty(EFFECT_PROPERTY);
			timer.Stop();

			effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
			effect.Owner.OnMaxSpeedChange();

			MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
			Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), eChatType.CT_SpellExpires, effect.Owner);

			return 60000;
		}


		protected virtual void OnAttacked(DOLEvent e, object sender, EventArgs arguments)
		{
			AttackedByEnemyEventArgs attackArgs = arguments as AttackedByEnemyEventArgs;
			GameLiving living = sender as GameLiving;
			if (attackArgs == null) return;
			if (living == null) return;
			GameSpellEffect effect = FindEffectOnTarget(living, this);
			if (attackArgs.AttackData.Damage > 0)
			{
							if (effect != null)
								effect.Cancel(false);
			}
            if (attackArgs.AttackData.SpellHandler is StyleBleeding || attackArgs.AttackData.SpellHandler is DoTSpellHandler || attackArgs.AttackData.SpellHandler is HereticDoTSpellHandler)
            {
                GameSpellEffect affect = FindEffectOnTarget(living, this);
                if (affect != null)
                    affect.Cancel(false);
            }
		}

		public HereticSpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
			TIMER_PROPERTY = this;
		}

		/// <summary>
		/// Slowly restores the livings speed
		/// </summary>
		private sealed class RestoreSpeedTimer : ECSGameTimerWrapperBase
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

				if (factor <= 0)
					return 0;

				return Interval;
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
	}
}
