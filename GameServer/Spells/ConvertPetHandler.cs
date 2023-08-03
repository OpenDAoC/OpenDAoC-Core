using System;
using System.Collections;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Based on HealSpellHandler.cs
	/// Spell calculates a percentage of the caster's health.
	/// Heals target for the full amount, Caster loses half that amount in health.
	/// </summary>
	[SpellHandlerAttribute("PetConversion")]
	public class ConvertPetHandler : SpellHandler
	{
		// constructor
		public ConvertPetHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Execute pet conversion spell
		/// </summary>
		public override bool StartSpell(GameLiving target)
		{
			var targets = SelectTargets(target);
			if (targets.Count <= 0)
				if (m_caster.ControlledBrain != null)
					targets.Add(m_caster.ControlledBrain.Body);
				else
				{
					return false;
				}
			
			int mana = 0;

			foreach (GameLiving living in targets)
			{
				ApplyEffectOnTarget(living, 1.0);
				mana += (int)(living.Health * Spell.Value / 100);
			}

			int absorb = m_caster.ChangeMana(m_caster, EPowerChangeType.Spell, mana);

			if (m_caster is GamePlayer)
			{
				if (absorb > 0)
					MessageToCaster("You absorb " + absorb + " power points.", EChatType.CT_Spell);
				else
					MessageToCaster("Your power is already full!", EChatType.CT_SpellResisted);
				((GamePlayer)m_caster).CommandNpcRelease();
			}

			return true;
		}
	}
}