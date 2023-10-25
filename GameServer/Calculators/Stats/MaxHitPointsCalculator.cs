using System;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.RealmAbilities;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS.Calculators;

/// <summary>
/// The Max HP calculator
///
/// BuffBonusCategory1 is used for absolute HP buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.MaxHealth)]
public class MaxHitPointsCalculator : PropertyCalculator
{
	private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public override int CalcValue(GameLiving living, EProperty property)
	{
		if (living is GamePlayer)
		{
			GamePlayer player = living as GamePlayer;
			int hpBase = player.CalculateMaxHealth(player.Level, player.GetModified(EProperty.Constitution));
			int buffBonus = living.BaseBuffBonusCategory[(int)property];

			if (buffBonus < 0)
				buffBonus = (int)((1 + (buffBonus / -100.0)) * hpBase) - hpBase;

			int itemBonus = living.ItemBonus[(int)property];
			int cap = Math.Max(player.Level * 4, 20) +
					  Math.Min(living.ItemBonus[(int)EProperty.MaxHealthCapBonus], player.Level * 4);
			itemBonus = Math.Min(itemBonus, cap);

			if (player.HasAbility(AbilityConstants.ScarsOfBattle) && player.Level >= 40)
			{
				int levelbonus = Math.Min(player.Level - 40, 10);
				hpBase = (int)(hpBase * (100 + levelbonus) * 0.01);
			}

			int abilityBonus = living.AbilityBonus[(int)property];
		
			OfRaToughnessAbility toughness = player.GetAbility<OfRaToughnessAbility>();
			double toughnessMod = toughness != null ? 1 + toughness.GetAmountForLevel(toughness.Level) * 0.01 : 1;

			return Math.Max((int)(hpBase * toughnessMod) + itemBonus + buffBonus + abilityBonus, 1);
		}
		else if (living is GameKeepComponent)
		{
			AGameKeep gameKeep = (living as GameKeepComponent)?.Keep;

			if (gameKeep != null)
			{
				int baseHealth = gameKeep.BaseLevel * ServerProperty.KEEP_COMPONENTS_BASE_HEALTH;
				baseHealth += (int)(baseHealth * (gameKeep.Level - 1) * ServerProperty.KEEP_COMPONENTS_HEALTH_UPGRADE_MODIFIER);
				return baseHealth;
			}

			return 0;
		}
		else if (living is GameKeepDoor)
		{
			AGameKeep gameKeep = (living as GameKeepDoor)?.Component?.Keep;

			if (gameKeep != null)
			{
				if (gameKeep.IsRelic)
					return ServerProperty.RELIC_DOORS_HEALTH;

				int baseHealth = gameKeep.BaseLevel * ServerProperty.KEEP_DOORS_BASE_HEALTH;
				baseHealth += (int)(baseHealth * (gameKeep.Level - 1) * ServerProperty.KEEP_DOORS_HEALTH_UPGRADE_MODIFIER);
				return baseHealth;
			}

			return 0;
		}
		else if (living is TheurgistPet theu)
		{
			/*
			int hp = 1;
			if (theu.Level < 2)
			{
				hp += theu.Constitution * (theu.Level + 1);
			} else
			{
				hp = theu.Constitution * theu.Level * 10 / 44;
			}

			if (theu.Name.Contains("air"))
			{
				//normal HP
			}
			else if (theu.Name.Contains("ice"))
			{
				hp = (int) Math.Ceiling(hp * 1.25);
			} else if (theu.Name.Contains("earth"))
			{
				hp = (int) Math.Ceiling(hp * 1.5);
			}
			return hp;
			*/

			int hp = 1;
			if (theu.Name.Contains("air"))
			{
				hp = 800;
			}
			else if (theu.Name.Contains("ice"))
			{
				hp = 500;
			} else if (theu.Name.Contains("earth"))
			{
				hp = 350;
			}
			
			hp = (int)((theu.Level / 44.0) * hp);
			if (hp < 10) hp = 10;
			return hp;
		}
		else if (living is TurretPet ani)
		{
			int hp = 1;
			/*
			if (ani.Level < 5)
			{
				hp += ani.Level * 2 + 50 + ani.Constitution;
			}
			else
			{
				hp = ani.Constitution * ani.Level;
			}
			*/

			if (living.Level < 10)
			{
				hp = living.Level * 20 + 20 + ani.Constitution + living.BaseBuffBonusCategory[(int)property];  // default
			}
			else
			{
				// approx to original formula, thx to mathematica :)
				hp = (int)(50 + 14 * living.Level + 0.548331 * living.Level) + ani.Constitution + living.BaseBuffBonusCategory[(int)property];
				if (living.Level < 25)
					hp += 20;
			}

			if (ani.Brain != null && ani.Brain is TurretFnfBrain)
				hp = (int)(hp * .8);

			return hp;
		}
        else if (living is GameSummonedPet pet)
        {
			int hp = 0;

			if (living.Level < 10)
			{
				hp = living.Level * 20 + 20 + pet.Constitution + living.BaseBuffBonusCategory[(int)property];  // default
			}
			else
			{
				// approx to original formula, thx to mathematica :)
				hp = (int)(50 + 15 * living.Level + 0.548331 * living.Level * living.Level) + pet.Constitution + living.BaseBuffBonusCategory[(int)property];
				if (living.Level < 25)
					hp += 20;
			}

			int basecon = (living as GameNpc).Constitution;
			int conmod = 20; // at level 50 +75 con ~= +300 hit points

			// first adjust hitpoints based on base CON

			if (basecon != ServerProperty.GAMENPC_BASE_CON)
			{
				hp = Math.Max(1, hp + ((basecon - ServerProperty.GAMENPC_BASE_CON) * ServerProperty.GAMENPC_HP_GAIN_PER_CON));
			}

			// Now adjust for buffs

			// adjust hit points based on constitution difference from base con
			// modified from http://www.btinternet.com/~challand/hp_calculator.htm
			int conhp = hp + (conmod * living.Level * (living.GetModified(EProperty.Constitution) - basecon) / 250);

			// 50% buff / debuff cap
			if (conhp > hp * 1.5)
				conhp = (int)(hp * 1.5);
			else if (conhp < hp / 2)
				conhp = hp / 2;

			conhp = (int)Math.Floor(0.6666 * (double)conhp);
			return hp;
			//return conhp;
		}
        else if (living is GameNpc)
		{
			int hp = 0;

			if (living.Level<20)
			{
				//14 hp per level
				//30 base
				//con * level HP, scaled by level
				hp = (int)((living.Level * 11) + 20 + (Math.Floor((double)((living as GameNpc).Constitution * living.Level) / (1 + (20-living.Level))))) /*living.BaseBuffBonusCategory[(int)property]*/;	// default
			}
			else
			{
				double levelScalar = .5;
				if (living.Level > 40)
				{
					//Console.WriteLine($"Scalar before {levelScalar} adding {(living.Level - 40) * .01} after {levelScalar + ((living.Level - 40) * .01)}");
					levelScalar += (living.Level - 40) * .0015;
				}
				// approx to original formula, thx to mathematica :)
				hp = (int)(50 + 12*living.Level + levelScalar * living.Level * (living.Level)) + (living as GameNpc).Constitution;
				if (living.Level < 25)
					hp += 20;
			}

			int basecon = (living as GameNpc).Constitution;
			int conmod = 25; // at level 50 +75 con ~= +300 hit points

			// first adjust hitpoints based on base CON

			if (basecon != ServerProperty.GAMENPC_BASE_CON && living.Level >= 10)
			{
				hp = Math.Max(1, hp + ((basecon - ServerProperty.GAMENPC_BASE_CON) * ServerProperty.GAMENPC_HP_GAIN_PER_CON));
			}

			// Now adjust for buffs
			if (living.Level > 10)
			{

				// adjust hit points based on constitution difference from base con
				// modified from http://www.btinternet.com/~challand/hp_calculator.htm
				int conhp = hp + (conmod * living.Level * (living.GetModified(EProperty.Constitution) - basecon) / 250);

				// 50% buff / debuff cap
				if (conhp > hp * 1.5)
					conhp = (int)(hp * 1.5);
				else if (conhp < hp / 2)
					conhp = hp / 2;
			}

			if(living is GameEpicBoss)
				hp = (int)(hp * 1.5); //epic bosses get 50% extra hp
			else if (living is GameEpicNpc)
				hp = (int)( hp * 1.25); //epic NPCs get 25% extra hp

			return hp;
			//return conhp;
		}
        else
        {
            if (living.Level < 10)
            {
                return living.Level * 20 + 20 + living.GetBaseStat(EStat.CON) /*living.BaseBuffBonusCategory[(int)property]*/;	// default
            }
            else
            {
                // approx to original formula, thx to mathematica :)
                int hp = (int)(50 + 11 * living.Level + 0.548331 * living.Level * living.Level) + living.GetBaseStat(EStat.CON)/*living.BaseBuffBonusCategory[(int)property]*/;
                if (living.Level < 25)
                {
                    hp += 20;
                }
                return hp;
            }
        }
	}

    /// <summary>
    /// Returns the hits cap for this living.
    /// </summary>
    /// <param name="living">The living the cap is to be determined for.</param>
    /// <returns></returns>
    public static int GetItemBonusCap(GameLiving living)
    {
        if (living == null) return 0;
        return living.Level * 4;
    }

    /// <summary>
    /// Returns the hits cap increase for the this living.
    /// </summary>
    /// <param name="living">The living the cap increase is to be determined for.</param>
    /// <returns></returns>
    public static int GetItemBonusCapIncrease(GameLiving living)
    {
        if (living == null) return 0;
        int itemBonusCapIncreaseCap = GetItemBonusCapIncreaseCap(living);
        int itemBonusCapIncrease = living.ItemBonus[(int)(EProperty.MaxHealthCapBonus)];
        return Math.Min(itemBonusCapIncrease, itemBonusCapIncreaseCap);
    }

    /// <summary>
    /// Returns the cap for hits cap increase for this living.
    /// </summary>
    /// <param name="living">The living the value is to be determined for.</param>
    /// <returns>The cap increase cap for this living.</returns>
    public static int GetItemBonusCapIncreaseCap(GameLiving living)
    {
        if (living == null) return 0;
        return living.Level * 4;
    }
}