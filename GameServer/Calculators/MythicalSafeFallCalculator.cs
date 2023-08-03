﻿

using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// Calculator for Mythical Safe Fall
    /// </summary>
    [PropertyCalculator(EProperty.MythicalSafeFall)]
    public class MythicalSafeFallCalculator : PropertyCalculator
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