﻿

using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// Calculator for Mythical Coin Bonus
    /// </summary>
    [PropertyCalculator(EProperty.MythicalCoin)]
    public class MythicalCoinCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, EProperty property)
        {
            if (living is GamePlayer)
            {
                return living.ItemBonus[(int)property];
            }
            return 0;
        }
    }
}