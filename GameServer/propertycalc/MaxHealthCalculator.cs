using System;
using DOL.GS.Keeps;
using DOL.GS.RealmAbilities;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// The Max HP calculator
	///
	/// BuffBonusCategory1 is used for absolute HP buffs
	/// BuffBonusCategory2 unused
	/// BuffBonusCategory3 unused
	/// BuffBonusCategory4 unused
	/// BuffBonusMultCategory1 unused
	/// </summary>
	[PropertyCalculator(eProperty.MaxHealth)]
	public class MaxHealthCalculator : PropertyCalculator
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override int CalcValue(GameLiving living, eProperty property)
		{
			if (living is GamePlayer)
			{
				GamePlayer player = living as GamePlayer;
				int hpBase = player.CalculateMaxHealth(player.Level, player.GetModified(eProperty.Constitution));
				int buffBonus = living.BaseBuffBonusCategory[(int)property];
				if (buffBonus < 0) buffBonus = (int)((1 + (buffBonus / -100.0)) * hpBase) - hpBase;
				int itemBonus = living.ItemBonus[(int)property];
				int cap = Math.Max(player.Level * 4, 20) + // at least 20
						  Math.Min(living.ItemBonus[(int)eProperty.MaxHealthCapBonus], player.Level * 4);
				itemBonus = Math.Min(itemBonus, cap);
				if (player.HasAbility(Abilities.ScarsOfBattle) && player.Level >= 40)
				{
					int levelbonus = Math.Min(player.Level - 40, 10);
					hpBase = (int)(hpBase * (100 + levelbonus) * 0.01);
				}
				int abilityBonus = living.AbilityBonus[(int)property];

				#region Calculation : AtlasOF_Thoughness
				// --- [START] --- AtlasOF_Thoughness ---------------------------------------------------------
				int raToughnessAmount = 0;
				AtlasOF_ToughnessAbility raToughness = living.GetAbility<AtlasOF_ToughnessAbility>();

				if (raToughness != null)
				{
					if (raToughness.Level > 0)
					{
						raToughnessAmount += (hpBase * raToughness.Level * 3) / 100;
					}
				}
				// --- [ END ] --- AtlasOF_Thoughness ---------------------------------------------------------
				#endregion

				return Math.Max(hpBase + itemBonus + buffBonus + abilityBonus + raToughnessAmount, 1); // at least 1
			}
			else if (living is GameKeepComponent)
			{
				GameKeepComponent keepComp = living as GameKeepComponent;

				if (keepComp.Keep != null)
					return (keepComp.Keep.EffectiveLevel(keepComp.Keep.Level) + 1) * keepComp.Keep.BaseLevel * 200;

				return 0;
			}
			else if (living is GameKeepDoor)
			{
				GameKeepDoor keepdoor = living as GameKeepDoor;

				if (keepdoor.Component != null && keepdoor.Component.Keep != null)
				{
					return (keepdoor.Component.Keep.EffectiveLevel(keepdoor.Component.Keep.Level) + 1) * keepdoor.Component.Keep.BaseLevel * 200;
				}

				return 0;

				//todo : use material too to calculate maxhealth
			}
			else if (living is TheurgistPet theu)
			{
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
					hp = living.Level * 20 + 20 + ani.Constitution;  // default
				}
				else
				{
					// approx to original formula, thx to mathematica :)
					hp = (int)(50 + 11 * living.Level + 0.548331 * living.Level) + ani.Constitution /*living.BaseBuffBonusCategory[(int)property]*/;
					if (living.Level < 25)
						hp += 20;
				}

				if (ani.Brain != null && ani.Brain is DOL.AI.Brain.TurretFNFBrain)
					hp = (int)(hp * .8);

				return hp;
			}
            else if (living is GamePet pet)
            {
				int hp = 0;

				if (living.Level < 10)
				{
					hp = living.Level * 20 + 20 + pet.Constitution/*living.BaseBuffBonusCategory[(int)property]*/;  // default
				}
				else
				{
					// approx to original formula, thx to mathematica :)
					hp = (int)(50 + 11 * living.Level + 0.548331 * living.Level * living.Level) + pet.Constitution /*living.BaseBuffBonusCategory[(int)property]*/;
					if (living.Level < 25)
						hp += 20;
				}

				int basecon = (living as GameNPC).Constitution;
				int conmod = 20; // at level 50 +75 con ~= +300 hit points

				// first adjust hitpoints based on base CON

				if (basecon != ServerProperties.Properties.GAMENPC_BASE_CON)
				{
					hp = Math.Max(1, hp + ((basecon - ServerProperties.Properties.GAMENPC_BASE_CON) * ServerProperties.Properties.GAMENPC_HP_GAIN_PER_CON));
				}

				// Now adjust for buffs

				// adjust hit points based on constitution difference from base con
				// modified from http://www.btinternet.com/~challand/hp_calculator.htm
				int conhp = hp + (conmod * living.Level * (living.GetModified(eProperty.Constitution) - basecon) / 250);

				// 50% buff / debuff cap
				if (conhp > hp * 1.5)
					conhp = (int)(hp * 1.5);
				else if (conhp < hp / 2)
					conhp = hp / 2;

				conhp = (int)Math.Floor(0.6666 * (double)conhp);
				return hp;
				//return conhp;
			}
            else if (living is GameNPC)
			{
				int hp = 0;

				if (living.Level<20)
				{
					//14 hp per level
					//30 base
					//con * level HP, scaled by level
					hp = (int)((living.Level * 14) + 30 + (Math.Floor((double)((living as GameNPC).Constitution * living.Level) / (1 + (20-living.Level))))) /*living.BaseBuffBonusCategory[(int)property]*/;	// default
				}
				else
				{
					// approx to original formula, thx to mathematica :)
					hp = (int)(50 + 11*living.Level + 0.448331 * living.Level * (living.Level)) + (living as GameNPC).Constitution;
					if (living.Level < 25)
						hp += 20;
				}

				int basecon = (living as GameNPC).Constitution;
				int conmod = 20; // at level 50 +75 con ~= +300 hit points

				// first adjust hitpoints based on base CON

				if (basecon != ServerProperties.Properties.GAMENPC_BASE_CON && living.Level >= 10)
				{
					hp = Math.Max(1, hp + ((basecon - ServerProperties.Properties.GAMENPC_BASE_CON) * ServerProperties.Properties.GAMENPC_HP_GAIN_PER_CON));
				}

				// Now adjust for buffs
				if (living.Level > 10)
				{

					// adjust hit points based on constitution difference from base con
					// modified from http://www.btinternet.com/~challand/hp_calculator.htm
					int conhp = hp + (conmod * living.Level * (living.GetModified(eProperty.Constitution) - basecon) / 250);

					// 50% buff / debuff cap
					if (conhp > hp * 1.5)
						conhp = (int)(hp * 1.5);
					else if (conhp < hp / 2)
						conhp = hp / 2;
				}

				if(living is GameEpicBoss)
					hp = (int)(hp * 2); //epic bosses get 100% extra hp
				else if (living is GameEpicNPC)
					hp = (int)( hp * 1.5); //epic NPCs get 50% extra hp

				return hp;
				//return conhp;
			}
            else
            {
                if (living.Level < 10)
                {
                    return living.Level * 20 + 20 + living.GetBaseStat(eStat.CON) /*living.BaseBuffBonusCategory[(int)property]*/;	// default
                }
                else
                {
                    // approx to original formula, thx to mathematica :)
                    int hp = (int)(50 + 11 * living.Level + 0.548331 * living.Level * living.Level) + living.GetBaseStat(eStat.CON)/*living.BaseBuffBonusCategory[(int)property]*/;
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
            int itemBonusCapIncrease = living.ItemBonus[(int)(eProperty.MaxHealthCapBonus)];
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
}
