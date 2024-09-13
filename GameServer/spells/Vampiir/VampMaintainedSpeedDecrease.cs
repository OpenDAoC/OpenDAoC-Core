using System;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler for speed decreasing spells.  Special for vampiirs
    /// </summary>
    [SpellHandler(eSpellType.VampSpeedDecrease)]
    public class VampMaintainedSpeedDecrease : SpeedDecreaseSpellHandler
    {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected GameLiving m_originalTarget = null;
		protected bool m_isPulsing = false;

        public VampMaintainedSpeedDecrease(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Creates the corresponding spell effect for the spell
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        /// <returns></returns>
        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
			m_originalTarget = target;

			// this acts like a pulsing spell effect, but with 0 frequency.
            return new GameSpellEffect(this, CalculateEffectDuration(target), 0, effectiveness);
        }

		/// <summary>
		/// This spell is a pulsing spell, not a pulsing effect, so we check spell pulse
		/// </summary>
		/// <param name="effect"></param>
		public override void OnSpellPulse(PulsingSpellEffect effect)
		{
			if (m_originalTarget == null || Caster.ObjectState != GameObject.eObjectState.Active || m_originalTarget.ObjectState != GameObject.eObjectState.Active)
			{
				MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
				effect.Cancel(false);
				return;
			}

			if (!Caster.IsAlive ||
				!m_originalTarget.IsAlive ||
				Caster.IsMezzed ||
				Caster.IsStunned ||
				Caster.IsSitting ||
				(Caster.TargetObject is GameLiving ? m_originalTarget != Caster.TargetObject as GameLiving : true))
			{
				MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
				effect.Cancel(false);
				return;
			}

			if (!Caster.IsWithinRadius(m_originalTarget, CalculateSpellRange()))
			{
				MessageToCaster("Your target is no longer in range.", eChatType.CT_SpellExpires);
				effect.Cancel(false);
				return;
			}

			if (!Caster.TargetInView)
			{
				MessageToCaster("Your target is no longer in view.", eChatType.CT_SpellExpires);
				effect.Cancel(false);
				return;
			}

			base.OnSpellPulse(effect);
		}


        protected override void OnAttacked(DOLEvent e, object sender, EventArgs arguments)
        {
            // Spell can be used in combat, do nothing
        }
    }
}
