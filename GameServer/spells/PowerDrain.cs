using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Handles power drain (conversion of target health to caster
	/// power).
	/// </summary>
	/// <author>Aredhel</author>
	[SpellHandler(eSpellType.PowerDrain)]
	public class PowerDrain : DirectDamageSpellHandler
	{
		protected override bool IsDualComponentSpell => true;

		public PowerDrain(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// Calculate damage to the target.

			AttackData ad = CalculateDamageToTarget(target);
			SendDamageMessages(ad);
			DamageTarget(ad, true);
			DrainPower(ad);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

		/// <summary>
		/// Use a percentage of the damage to refill caster's power.
		/// </summary>
		/// <param name="ad">Attack data.</param>
		public virtual void DrainPower(AttackData ad)
		{
			if (ad == null || !m_caster.IsAlive)
				return;

			GameLiving owner = Owner();
			if (owner == null)
				return;

			int powerGain = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
			powerGain = owner.ChangeMana(m_caster, eManaChangeType.Spell, powerGain);

			if (powerGain > 0)
				MessageToOwner(String.Format("Your summon channels {0} power to you!", powerGain), eChatType.CT_Spell);
			else
				MessageToOwner("You cannot absorb any more power.", eChatType.CT_SpellResisted);
		}
		
		/// <summary>
		/// The target of the drain. Generally the caster, except for necropet
		/// </summary>
		/// <returns></returns>
		protected virtual GameLiving Owner()
		{
			return Caster;
		}
		

		/// <summary>
		/// Send message to owner.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="chatType"></param>
		protected virtual void MessageToOwner(String message, eChatType chatType)
		{
			base.MessageToCaster(message, chatType);
		}
	}
}
