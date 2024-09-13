using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Based on HealSpellHandler.cs
	/// Spell calculates a percentage of the caster's health.
	/// Heals target for the full amount, Caster loses half that amount in health.
	/// </summary>
	[SpellHandler(eSpellType.LifeTransfer)]
	public class LifeTransferSpellHandler : SpellHandler
	{
		// constructor
		public LifeTransferSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}

		/// <summary>
		/// Execute lifetransfer spell
		/// </summary>
		public override bool StartSpell(GameLiving target)
		{
			var targets = SelectTargets(target);
			if (targets.Count <= 0) return false;

			bool healed = false;
			int transferHeal;
			double spellValue = m_spell.Value;

			transferHeal = (int)(Caster.MaxHealth / 100 * Math.Abs(spellValue) * 2);

			//Needed to prevent divide by zero error
			if (transferHeal <= 0)
				transferHeal = 0;
			else
			{
				//Remaining health is used if caster does not have enough health, leaving caster at 1 hitpoint
				if ( (transferHeal >> 1) >= Caster.Health )
					transferHeal = ( (Caster.Health - 1) << 1);
			}

			foreach(GameLiving healTarget in targets)
			{
				if (target.IsDiseased)
				{
					MessageToCaster("Your target is diseased!", eChatType.CT_SpellResisted);
					healed |= HealTarget(healTarget, ( transferHeal >>= 1 ));
				}

				else healed |= HealTarget(healTarget, transferHeal);
			}

			if (!healed && Spell.Target == eSpellTarget.REALM)
			{
				m_caster.Mana -= PowerCost(target) >> 1;	// only 1/2 power if no heal
			}
			else
			{
				m_caster.Mana -= PowerCost(target);
				m_caster.Health -= transferHeal >> 1;
			}

			// send animation for non pulsing spells only
			if (Spell.Pulse == 0)
			{
				if (healed)
				{
					// send animation on all targets if healed
					foreach(GameLiving healTarget in targets)
						SendEffectAnimation(healTarget, 0, false, 1);
				}
				else
				{
					// show resisted effect if not healed
					SendEffectAnimation(Caster, 0, false, 0);
				}
			}

			return true;
		}

		/// <summary>
		/// Heals hit points of one target and sends needed messages, no spell effects
		/// </summary>
		/// <param name="target"></param>
		/// <param name="amount">amount of hit points to heal</param>
		/// <returns>true if heal was done</returns>
		public virtual bool HealTarget(GameLiving target, int amount)
		{
			if (target==null || target.ObjectState!=GameLiving.eObjectState.Active) return false;

			// we can't heal people we can attack
			if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
				return false;

			if (!target.IsAlive) 
			{
				MessageToCaster(target.GetName(0, true) + " is dead!", eChatType.CT_SpellResisted);
				return false;
			}

			if (m_caster == target)
			{
				MessageToCaster("You cannot transfer life to yourself.", eChatType.CT_SpellResisted);
				return false;
			}

			if (amount <= 0) //Player does not have enough health to transfer
			{
				MessageToCaster("You do not have enough health to transfer.", eChatType.CT_SpellResisted);
				return false;
			}

			int heal = target.ChangeHealth(Caster, eHealthChangeType.Spell, amount);

			if (heal == 0)
			{
				if (Spell.Pulse == 0)
				{
					MessageToCaster(target.GetName(0, true)+" is fully healed.", eChatType.CT_SpellResisted);
				}

				return false;
			}

			MessageToCaster("You heal " + target.GetName(0, false) + " for " + heal + " hit points!", eChatType.CT_Spell);
			MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + heal + " hit points.", eChatType.CT_Spell);

			if (heal < amount)
				MessageToCaster(target.GetName(0, true)+" is fully healed.", eChatType.CT_Spell);

			return true;
		}
	}
}
