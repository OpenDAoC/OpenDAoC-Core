using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Damage Over Time spell handler
	/// </summary>
	[SpellHandler("HealthToEndurance")]
	public class HealthToEnduranceSpell : SpellHandler
	{

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (m_caster.Endurance == m_caster.MaxEndurance)
			{
				MessageToCaster("You already have full endurance!", EChatType.CT_Spell);
				return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		/// <summary>
		/// Execute damage over time spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			base.FinishSpellCast(target);

			GiveEndurance(m_caster, (int)m_spell.Value);
			OnEffectExpires(null, true);
		}

		public override int CalculateEnduranceCost()
		{
			return 0;
		}

		protected virtual void GiveEndurance(GameLiving target, int amount)
		{
			if (target.Endurance >= amount)
				amount = target.MaxEndurance - target.Endurance;

			target.ChangeEndurance(target, EEnduranceChangeType.Spell, amount);
			MessageToCaster("You transfer " + amount + " life to Endurance!", EChatType.CT_Spell);
		}

		// constructor
		public HealthToEnduranceSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
