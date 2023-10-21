using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler("Range")]
	public class RangeSpell : PrimerSpell
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
            GameSpellEffect UninterruptableSpell = SpellHandler.FindEffectOnTarget(Caster, "Uninterruptable");
  			if(UninterruptableSpell != null) { MessageToCaster("You already preparing a Uninterruptable spell", EChatType.CT_System); return false; }
            GameSpellEffect PowerlessSpell = SpellHandler.FindEffectOnTarget(Caster, "Powerless");
  			if(PowerlessSpell != null) { MessageToCaster("You already preparing	a Powerless spell", EChatType.CT_System); return false; }
            GameSpellEffect RangeSpell = SpellHandler.FindEffectOnTarget(Caster, "Range");
            if (RangeSpell != null) { MessageToCaster("You must finish casting Range before you can cast it again", EChatType.CT_System); return false; }
            return true;
		}
		/// <summary>
		/// Calculates the power to cast the spell
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public override int PowerCost(GameLiving target)
		{
            double basepower = m_spell.Power; //<== defined a basevar first then modified this base-var to tell %-costs from absolut-costs

            // percent of maxPower if less than zero
			if (basepower < 0)
			{
				if (Caster is GamePlayer && ((GamePlayer)Caster).PlayerClass.ManaStat != EStat.UNDEFINED)
				{
					GamePlayer player = Caster as GamePlayer;
					basepower = player.CalculateMaxMana(player.Level, player.GetBaseStat(player.PlayerClass.ManaStat)) * basepower * -0.01;
				}
				else
				{
					basepower = Caster.MaxMana * basepower * -0.01;
				}
			}
            return (int)basepower;
		}

//		public override bool CasterIsAttacked(GameLiving attacker)
//		{
//			return false;
//		}

		// constructor
		public RangeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
