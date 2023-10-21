using DOL.GS.RealmAbilities;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// The health regen rate calculator
	/// 
	/// BuffBonusCategory1 is used for all buffs
	/// BuffBonusCategory2 is used for all debuffs (positive values expected here)
	/// BuffBonusCategory3 unused
	/// BuffBonusCategory4 unused
	/// BuffBonusMultCategory1 unused
	/// </summary>
	[PropertyCalculator(EProperty.EnduranceRegenerationRate)]
	public class EnduranceRegenerationRateCalculator : PropertyCalculator
	{
		public EnduranceRegenerationRateCalculator() {}

		/// <summary>
		/// calculates the final property value
		/// </summary>
		/// <param name="living"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public override int CalcValue(GameLiving living, EProperty property)
		{
			int debuff = living.SpecBuffBonusCategory[(int)property];
			if (debuff < 0)
				debuff = -debuff;

			var p = living as GamePlayer;
			if (p != null)
				p.EnduDebuff = debuff;
			// buffs allow to regenerate endurance even in combat and while moving			
			double regenBuff =
				 living.BaseBuffBonusCategory[(int)property]
				+living.ItemBonus[(int)property];
			if (p != null)
				p.RegenBuff = regenBuff;
			double regen = regenBuff;
			if (regen == 0 && living is GamePlayer) //&& ((GamePlayer)living).HasAbility(Abilities.Tireless))
				regen++;
			// if (living is GamePlayer && living.HasAbility(Abilities.Tireless))
			// 	regen++;
			
			// --- [START] --- AtlasOF_Tireless ---------------------------------------------------------
			var raTireless = living.GetAbility<OfRAEndRegenEnhancerAbility>();

			if (raTireless != null)
			{
				regen++;
			}
			// --- [ END ] --- AtlasOF_Tireless ---------------------------------------------------------

			if (p != null)
				p.RegenAfterTireless = regen;
			/*    Patch 1.87 - COMBAT AND REGENERATION CHANGES
				- The bonus to regeneration while standing out of combat has been greatly increased. The amount of ticks 
					a player receives while standing has been doubled and it will now match the bonus to regeneration while sitting.
					Players will no longer need to sit to regenerate faster.
				- Fatigue now regenerates at the standing rate while moving.
			*/
			if (p != null)
			{
				p.NonCombatNonSprintRegen = 0;
				p.CombatRegen = 0;
			}
			if (!living.InCombat)
			{
				if (living is GamePlayer)
				{
					if (!((GamePlayer)living).IsSprinting)
					{
						regen += 4;
					}
				}
				if (p != null)
					p.NonCombatNonSprintRegen = regen;
			}
            else
            {
				regen -= 3;
				if (regen <= 0)
					regen = 0.1;
				if (regenBuff > 0)
					regen = regenBuff;
				if (p != null && raTireless != null)
					regen++;
				if (p != null)
					p.CombatRegen = regen;
			}
				

			regen -= debuff;

			if (regen < 0)
				regen = 0;

			if (regen != 0 && ServerProperties.Properties.ENDURANCE_REGEN_RATE != 1)
				regen *= ServerProperties.Properties.ENDURANCE_REGEN_RATE;

			double decimals = regen - (int)regen;
			if (Util.ChanceDouble(decimals))
			{
				regen += 1;	// compensate int rounding error
			}
			return (int)regen;
		}
	}
}
