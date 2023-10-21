using System.Collections;
using System.Collections.Specialized;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.GS.PropertyCalc;

namespace Core.GS.Spells
{
	/// <summary>
	/// Health regeneration rate buff
	/// </summary>
	[SpellHandler("HealthRegenBuff")]
	public class HealthRegenSpell : PropertyChangingSpell
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
		public override EProperty Property1 { get { return EProperty.HealthRegenerationRate; } }

		public HealthRegenSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	/// <summary>
	/// Power regeneration rate buff
	/// </summary>
	[SpellHandler("PowerRegenBuff")]
	public class PowerRegenSpellHandler : PropertyChangingSpell
	{
		public override void ApplyEffectOnTarget(GameLiving target)
		{
            if (target is GamePlayer && (((GamePlayer)target).PlayerClass.ID == (int)EPlayerClass.Vampiir
                || ((GamePlayer)target).PlayerClass.ID == (int)EPlayerClass.MaulerAlb
                || ((GamePlayer)target).PlayerClass.ID == (int)EPlayerClass.MaulerMid
                || ((GamePlayer)target).PlayerClass.ID == (int)EPlayerClass.MaulerHib)) { MessageToCaster("This spell has no effect on this class!", EChatType.CT_Spell); return; }
			base.ApplyEffectOnTarget(target);
		}
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
		public override EProperty Property1 { get { return EProperty.PowerRegenerationRate; } }

		public PowerRegenSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}

	/// <summary>
	/// Endurance regeneration rate buff
	/// </summary>
	[SpellHandler("EnduranceRegenBuff")]
	public class EnduranceRegenSpell : PropertyChangingSpell
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
		public override EProperty Property1 { get { return EProperty.EnduranceRegenerationRate; } }

		/// <summary>
		/// The max range from caster to owner for all conc buffs
		/// </summary>
		private const int CONC_MAX_RANGE = 1500;

		/// <summary>
		/// The interval for range checks, in milliseconds
		/// </summary>
		private const int RANGE_CHECK_INTERVAL = 5000;

		/// <summary>
		/// Holds all owners of conc buffs
		/// </summary>
		private ListDictionary m_concEffects;

		/// <summary>
		/// Execute property changing spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			if (Spell.Concentration > 0)
			{
				m_concEffects = new ListDictionary();
				RangeCheckAction rangeCheck = new RangeCheckAction(Caster, this);
				rangeCheck.Interval = RANGE_CHECK_INTERVAL;
				rangeCheck.Start(RANGE_CHECK_INTERVAL);
			}
			base.FinishSpellCast(target);
		}

		/// <summary>
		/// start changing effect on target
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if (Spell.Concentration > 0) {
				lock (m_concEffects) {
					m_concEffects.Add(effect, effect); // effects are enabled at start
				}
			}
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
			if (Spell.Concentration > 0) {
				lock (m_concEffects) {
					EnableEffect(effect); // restore disabled effect before it is completely canceled
					m_concEffects.Remove(effect);
				}
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		/// <summary>
		/// Disables spell effect
		/// </summary>
		/// <param name="effect"></param>
		private void EnableEffect(GameSpellEffect effect)
		{
			if (m_concEffects[effect] != null) return; // already enabled
			m_concEffects[effect] = effect;
			IPropertyIndexer bonuscat = GetBonusCategory(effect.Owner, BonusCategory1);
			bonuscat[(int)Property1] += (int)(Spell.Value * effect.Effectiveness);
		}

		/// <summary>
		/// Enables spell effect
		/// </summary>
		/// <param name="effect"></param>
		private void DisableEffect(GameSpellEffect effect)
		{
			if (m_concEffects[effect] == null) return; // already disabled
			m_concEffects[effect] = null;
			IPropertyIndexer bonuscat = GetBonusCategory(effect.Owner, BonusCategory1);
			bonuscat[(int)Property1] -= (int)(Spell.Value * effect.Effectiveness);
		}

		/// <summary>
		/// Checks effect owner distance and cancels the effect if too far
		/// </summary>
		private sealed class RangeCheckAction : EcsGameTimerWrapperBase
		{
			/// <summary>
			/// The list of effects
			/// </summary>
			private readonly EnduranceRegenSpell m_handler;

			/// <summary>
			/// Constructs a new RangeCheckAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="handler">The spell handler</param>
			public RangeCheckAction(GameLiving actionSource, EnduranceRegenSpell handler) : base(actionSource)
			{
				m_handler = handler;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				IDictionary effects = m_handler.m_concEffects;
				GameLiving caster = (GameLiving) timer.Owner;

				lock (effects)
				{
					if (effects.Count <= 0)
					{
						Stop(); // all effects were canceled, stop the timer
						return 0;
					}

					ArrayList disableEffects = null;
					ArrayList enableEffects = null;
					foreach (DictionaryEntry de in effects)
					{
						GameSpellEffect effect = (GameSpellEffect)de.Key;
						GameLiving effectOwner = effect.Owner;
						if (caster == effectOwner) continue;
//						DOLConsole.WriteLine(Caster.Name+" to "+effectOwner.Name+" range="+range);

						if (!caster.IsWithinRadius(effectOwner, CONC_MAX_RANGE))
						{
							if (de.Value != null)
							{
								// owner is out of range and effect is still active, disable it
								if (disableEffects == null)
									disableEffects = new ArrayList(1);
								disableEffects.Add(effect);
							}
						}
						else if (de.Value == null)
						{
							// owner entered the range and effect was disabled, enable it now
							if (enableEffects == null)
								enableEffects = new ArrayList(1);
							enableEffects.Add(effect);
						}
					}

					if (enableEffects != null)
						foreach (GameSpellEffect fx in enableEffects)
							m_handler.EnableEffect(fx);

					if (disableEffects != null)
						foreach (GameSpellEffect fx in disableEffects)
							m_handler.DisableEffect(fx);
				}

				return Interval;
			}
		}

		/// <summary>
		/// Constructs a new EnduranceRegenSpellHandler
		/// </summary>
		/// <param name="caster">The spell caster</param>
		/// <param name="spell">The spell used</param>
		/// <param name="line">The spell line used</param>
		public EnduranceRegenSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
