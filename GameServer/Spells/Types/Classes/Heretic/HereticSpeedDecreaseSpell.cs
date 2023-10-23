using System;
using System.Text;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Styles;

namespace Core.GS.Spells;

public abstract class HereticImmunityEffectSpell : HereticPiercingMagicSpell
{
	/// <summary>
	/// called when spell effect has to be started and applied to targets
	/// </summary>
	public override void FinishSpellCast(GameLiving target)
	{
		m_caster.Mana -= PowerCost(target);
		base.FinishSpellCast(target);
	}

	/// <summary>
    /// Determines wether this spell is better than given one
	/// </summary>
	/// <param name="oldeffect"></param>
	/// <param name="neweffect"></param>
	/// <returns></returns>
	public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
	{
		if (oldeffect.Owner is GamePlayer) return false; //no overwrite for players
		return base.IsNewEffectBetter(oldeffect, neweffect);
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
        if (target.HasAbility(AbilityConstants.CCImmunity))
        {
            MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
            return;
        }
        if (target.TempProperties.GetProperty("Charging", false))
        {
            MessageToCaster(target.Name + " is moving to fast for this spell to have any effect!", EChatType.CT_SpellResisted);
            return;
        }
		base.ApplyEffectOnTarget(target);

		if (Spell.CastTime > 0) 
		{
			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
		}
		if(target is GameNpc) 
		{
			GameNpc npc = (GameNpc)target;
			IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
			if (aggroBrain != null)
				aggroBrain.AddToAggroList(Caster, 1);
		}
	}

//		/// <summary>
//		/// Calculates effect duration in ticks
//		/// </summary>
//		/// <param name="target"></param>
//		/// <param name="effectiveness"></param>
//		/// <returns></returns>
//		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
//		{
//			// http://support.darkageofcamelot.com/kb/article.php?id=423
//			// Patch Notes: Version 1.52
//			// The duration is 100% at the middle of the area, and it tails off to 50%
//			// duration at the edges. This does NOT change the way area effect spells
//			// work against monsters, only realm enemies (i.e. enemy players and enemy realm guards).
//			int duration = base.CalculateEffectDuration(target, effectiveness);
//			if (target is GamePlayer == false)
//				return duration;
//			duration *= (int)(0.5 + 0.5*effectiveness);
//			duration -= (int)(duration * target.GetResist(Spell.DamageType) * 0.01);
//
//			if (duration < 1) duration = 1;
//			else if (duration > (Spell.Duration << 5)) duration = (Spell.Duration << 5); // duration is in seconds, mult by 32
//			return duration;
//		}

	protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
	{
		double duration = base.CalculateEffectDuration(target, effectiveness);
		duration *= target.GetModified(EProperty.SpeedDecreaseDurationReduction) * 0.01;

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
		return new GameSpellAndImmunityEffect(this, (int)CalculateEffectDuration(target, effectiveness), 0, effectiveness);
	}

	// constructor
	public HereticImmunityEffectSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
}

[SpellHandler("HereticSpeedDecrease")]
public class HereticSpeedDecreaseSpell : HereticImmunityEffectSpell
{
	private readonly object TIMER_PROPERTY;
	private const string EFFECT_PROPERTY = "HereticSpeedDecreaseProperty";

	public override void OnEffectStart(GameSpellEffect effect)
	{
		base.OnEffectStart(effect);
		effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect, 1.0-Spell.Value*0.01);

		SendUpdates(effect.Owner);

		MessageToLiving(effect.Owner, Spell.Message1, EChatType.CT_Spell);
		MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), EChatType.CT_Spell, effect.Owner);

		RestoreSpeedTimer timer = new(null, effect);
		effect.Owner.TempProperties.SetProperty(EFFECT_PROPERTY, timer);

		//REVOIR
		timer.Interval = 650;
		timer.Start(1 + (effect.Duration >> 1));

		effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
	}


	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
		base.OnEffectExpires(effect,noMessages);

		EcsGameTimer timer = effect.Owner.TempProperties.GetProperty<EcsGameTimer>(EFFECT_PROPERTY, null);
		effect.Owner.TempProperties.RemoveProperty(EFFECT_PROPERTY);
		timer.Stop();

		effect.Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, effect);

		SendUpdates(effect.Owner);

		MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
		MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), EChatType.CT_SpellExpires, effect.Owner);

		return 60000;
	}


	protected virtual void OnAttacked(CoreEvent e, object sender, EventArgs arguments)
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
        if (attackArgs.AttackData.SpellHandler is StyleBleedingEffect || attackArgs.AttackData.SpellHandler is DamageOverTimeSpell || attackArgs.AttackData.SpellHandler is HereticDotSpell)
        {
            GameSpellEffect affect = FindEffectOnTarget(living, this);
            if (affect != null)
                affect.Cancel(false);
        }
	}


	/// <summary>
	/// Sends updates on effect start/stop
	/// </summary>
	/// <param name="owner"></param>
	protected static void SendUpdates(GameLiving owner)
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


	public HereticSpeedDecreaseSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
	{
		TIMER_PROPERTY = this;
	}

	/// <summary>
	/// Slowly restores the livings speed
	/// </summary>
	private sealed class RestoreSpeedTimer : EcsGameTimerWrapperBase
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