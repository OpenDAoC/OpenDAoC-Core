using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The Concentration point calculator
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.MaxConcentration)]
public class MaxConcentrationCalculator : PropertyCalculator
{
	public MaxConcentrationCalculator() {}

	public override int CalcValue(GameLiving living, EProperty property) 
	{
		if (living is GamePlayer) 
		{
			GamePlayer player = living as GamePlayer;
			if (player.PlayerClass.ManaStat == EStat.UNDEFINED) 
                return 1000000;

			int concBase = (int)((player.Level * 4) * 2.2);
			int stat = player.GetModified((EProperty)player.PlayerClass.ManaStat);
			var statConc = (stat-50) * 2.8;
			//int factor = (stat > 50) ? (stat - 50) / 2 : (stat - 50);
			//int conc = (concBase + concBase * factor / 100) / 2;
			int conc = (concBase + (int)statConc) / 2;
			//Console.WriteLine($"New conc val {conc} oldConc {(concBase + concBase * ((stat - 50) / 2) / 100) / 2}");
			conc = (int)(player.Effectiveness * (double)conc);

			if (conc < 0)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat(living.Name+": concentration is less than zero (conc:{0} eff:{1:R} concBase:{2} stat:{3}})", conc, player.Effectiveness, concBase, stat);
				conc = 0;
			}

            if (player.GetSpellLine("Perfecter") != null
			   && player.MLLevel >= 4)
                conc += (20 * conc / 100);

			return conc;
		} 
		else 
		{
			return 1000000;	// default
		}
	}
}