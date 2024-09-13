using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	///
	/// </summary>
	[SpellHandler(eSpellType.EnduranceHeal)]
	public class EnduranceHealSpellHandler : SpellHandler
	{
		// constructor
		public EnduranceHealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Execute heal spell
		/// </summary>
		/// <param name="target"></param>
		public override bool StartSpell(GameLiving target)
		{
			var targets = SelectTargets(target);
			if (targets.Count <= 0) return false;

			bool healed = false;

			int spellValue = (int)Math.Round(Spell.Value);

			foreach (GameLiving healTarget in targets)
			{
				if (Spell.Value < 0 && healTarget != null)
					// Restore a percentage of the target's endurance
					spellValue = (int)Math.Round(Spell.Value * -0.01) * healTarget.MaxEndurance;

				healed |= HealTarget(healTarget, spellValue);
			}

			// group heals seem to use full power even if no heals
			if (!healed && Spell.Target == eSpellTarget.REALM)
				RemoveFromStat(PowerCost(target) >> 1); // only 1/2 power if no heal
			else
				RemoveFromStat(PowerCost(target));

			// send animation for non pulsing spells only
			if (Spell.Pulse == 0)
			{
				if (healed)
				{
					// send animation on all targets if healed
					foreach (GameLiving healTarget in targets)
						SendEffectAnimation(healTarget, 0, false, 1);
				}
				else
				{
					// show resisted effect if not healed
					SendEffectAnimation(Caster, 0, false, 0);
				}
			}

			if (!healed && Spell.CastTime == 0) m_startReuseTimer = false;

			return true;
		}

		protected virtual void RemoveFromStat(int value)
		{
			m_caster.Mana -= value;
		}

		/// <summary>
		/// Heals hit points of one target and sends needed messages, no spell effects
		/// </summary>
		/// <param name="target"></param>
		/// <param name="amount">amount of hit points to heal</param>
		/// <returns>true if heal was done</returns>
		public virtual bool HealTarget(GameLiving target, int amount)
		{
			if (target == null || target.ObjectState != GameLiving.eObjectState.Active) return false;

			// we can't heal people we can attack
			if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
				return false;

			if (!target.IsAlive)
			{
				//"You cannot heal the dead!" sshot550.tga
				MessageToCaster(target.GetName(0, true) + " is dead!", eChatType.CT_SpellResisted);
				return false;
			}

			int heal = target.ChangeEndurance(Caster, eEnduranceChangeType.Spell, amount);

			if (heal == 0)
			{
				if (Spell.Pulse == 0)
				{
					if (target == m_caster) MessageToCaster("Your endurance is full.", eChatType.CT_SpellResisted);
					else MessageToCaster(target.GetName(0, true) + " endurance is full.", eChatType.CT_SpellResisted);
				}
				return false;
			}

			if (m_caster == target)
			{
				MessageToCaster("You restore " + heal + " endurance points.", eChatType.CT_Spell);
				if (heal < amount)
					MessageToCaster("Your endurance is full.", eChatType.CT_Spell);
			}
			else
			{
				MessageToCaster("You restore " + target.GetName(0, false) + " for " + heal + " ednurance points!", eChatType.CT_Spell);
				MessageToLiving(target, "Your endurance was restored by " + m_caster.GetName(0, false) + " for " + heal + " points.", eChatType.CT_Spell);
				if (heal < amount)
					MessageToCaster(target.GetName(0, true) + " endurance is full.", eChatType.CT_Spell);
			}
			return true;
		}

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (selectedTarget != null && selectedTarget.EndurancePercent >= 90)
			{
				MessageToCaster("You cannot cast an endurance heal the target has above 90% endurance!", eChatType.CT_SpellResisted);
				return false;
			}
			return base.CheckBeginCast(selectedTarget);
		}
	}
}
