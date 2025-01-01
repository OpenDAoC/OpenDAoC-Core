using System;
using System.Text;
using System.Threading;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Assists SpellHandler with pulsing spells 
	/// </summary>
	public sealed class PulsingSpellEffect : IConcentrationEffect
	{
		private readonly Lock _lockObject = new();

		/// <summary>
		/// The spell handler of this pulsing effect
		/// </summary>
		private readonly ISpellHandler m_spellHandler = null;

		/// <summary>
		/// The pulsing timer of this effect
		/// </summary>
		private SpellPulseAction m_spellPulseAction;

		/// <summary>
		/// Constructs new pulsing spell effect
		/// </summary>
		/// <param name="spellHandler">the spell handler doing the pulsing</param>
		public PulsingSpellEffect(ISpellHandler spellHandler)
		{
			if (spellHandler == null)
				throw new ArgumentNullException("spellHandler");
			m_spellHandler = spellHandler;
		}

		/// <summary>
		/// Returns the string representation of the PulsingSpellEffect
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return new StringBuilder(64)
				.Append("Name=").Append(Name)
				.Append(", OwnerName=").Append(OwnerName)
				.Append(", Icon=").Append(Icon)
				.Append("\nSpellHandler info: ").Append(SpellHandler.ToString())
				.ToString();
		}

		/// <summary>
		/// Starts the effect
		/// </summary>
		public void Start()
		{
			lock (_lockObject)
			{
				if (m_spellPulseAction != null)
					m_spellPulseAction.Stop();
				m_spellPulseAction = new SpellPulseAction(m_spellHandler.Caster, this);
				m_spellPulseAction.Interval = m_spellHandler.Spell.Frequency;
				m_spellPulseAction.Start(m_spellHandler.Spell.Frequency);
				//m_spellHandler.Caster.ConcentrationEffects.Add(this);
			}
		}

		/// <summary>
		/// Effect must be canceled
		/// </summary>
		/// <param name="playerCanceled">true if player decided to cancel that effect by shift + rightclick</param>
		public void Cancel(bool playerCanceled)
		{
			lock (_lockObject)
			{
				if (m_spellPulseAction != null)
				{
					m_spellPulseAction.Stop();
					m_spellPulseAction = null;
				}
				//m_spellHandler.Caster.ConcentrationEffects.Remove(this);
			}
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public string Name
		{
			get { return m_spellHandler.Spell.Name; }
		}

		/// <summary>
		/// The name of the owner
		/// </summary>
		public string OwnerName
		{
			get { return "Pulse: " + m_spellHandler.Spell.Target; }
		}

		/// <summary>
		/// Effect icon ID
		/// </summary>
		public ushort Icon
		{
			get { return m_spellHandler.Spell.Icon; }
		}

		/// <summary>
		/// Amount of concentration used by effect
		/// </summary>
		public byte Concentration
		{
			get { return m_spellHandler.Spell.Concentration; }
		}

		/// <summary>
		/// The spell handler
		/// </summary>
		public ISpellHandler SpellHandler
		{
			get { return m_spellHandler; }
		}

		/// <summary>
		/// The pulsing effect action
		/// </summary>
		private sealed class SpellPulseAction : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The pulsing effect
			/// </summary>
			private readonly PulsingSpellEffect m_effect;

			/// <summary>
			/// Constructs a new pulsing action
			/// </summary>
			/// <param name="actionSource"></param>
			/// <param name="effect"></param>
			public SpellPulseAction(GameObject actionSource, PulsingSpellEffect effect) : base(actionSource)
			{
				if (effect == null)
					throw new ArgumentNullException("effect");
				m_effect = effect;
			}

			/// <summary>
			/// Callback for spell pulses
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				PulsingSpellEffect effect = m_effect;
				effect.m_spellHandler.OnSpellPulse(effect);
				return 0;
			}
		}
	}
}
