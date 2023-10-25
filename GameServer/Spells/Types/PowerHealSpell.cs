using System;
using Core.GS.Enums;
using Core.GS.Players;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("PowerHeal")]
public class PowerHealSpell : SpellHandler
{
	// constructor
	public PowerHealSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	/// <summary>
	/// Execute heal spell
	/// </summary>
	/// <param name="target"></param>
	public override bool StartSpell(GameLiving target)
	{
		var targets = SelectTargets(target);
		if (targets.Count <= 0) return false;

		bool healed = false;

		if (Spell.Value < 0 && m_caster is GamePlayer player)
			Spell.Value = (Spell.Value * -0.01) * player.MaxMana;

		int spellValue = (int)Math.Round(Spell.Value);

		foreach (GameLiving healTarget in targets)
		{
			if (healTarget is GamePlayer 
			    && (
			    ((GamePlayer)healTarget).PlayerClass is ClassVampiir
				|| ((GamePlayer)healTarget).PlayerClass is ClassMaulerAlb
				|| ((GamePlayer)healTarget).PlayerClass is ClassMaulerHib
				|| ((GamePlayer)healTarget).PlayerClass is ClassMaulerMid))
				continue;

			if (Spell.Value < 0)
				// Restore a percentage of the target's mana
				spellValue = (int)Math.Round((Spell.Value * -0.01) * healTarget.MaxMana);

			healed |= HealTarget(healTarget, spellValue);
		}

		// group heals seem to use full power even if no heals
		if (!healed && Spell.Target == ESpellTarget.REALM)
			m_caster.Mana -= PowerCost(target) >> 1; // only 1/2 power if no heal
		else
			m_caster.Mana -= PowerCost(target);

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
			MessageToCaster(target.GetName(0, true) + " is dead!", EChatType.CT_SpellResisted);
			return false;
		}

		int heal = target.ChangeMana(Caster, EPowerChangeType.Spell, amount);

		if (heal == 0)
		{
			if (Spell.Pulse == 0)
			{
				if (target == m_caster) MessageToCaster("Your power is full.", EChatType.CT_SpellResisted);
				else MessageToCaster(target.GetName(0, true) + " power is full.", EChatType.CT_SpellResisted);
			}
			return false;
		}

		if (m_caster == target)
		{
			MessageToCaster("You restore " + heal + " power points.", EChatType.CT_Spell);
			if (heal < amount)
				MessageToCaster("Your power is full.", EChatType.CT_Spell);
		}
		else
		{
			MessageToCaster("You restore " + target.GetName(0, false) + " for " + heal + " power points!", EChatType.CT_Spell);
			MessageToLiving(target, "Your power was restored by " + m_caster.GetName(0, false) + " for " + heal + " points.", EChatType.CT_Spell);
			if (heal < amount)
				MessageToCaster(target.GetName(0, true) + " mana is full.", EChatType.CT_Spell);
		}
		return true;
	}
}