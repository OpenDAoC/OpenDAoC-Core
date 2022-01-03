using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Serenity : Your hit points regenerate faster than normal.
    /// </summary>
    public class AtlasOF_Regeneration : RAPropertyEnhancer
    {

        public AtlasOF_Regeneration(DBAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) { return 0; }
            
            // return values in milliseconds for tick
            switch (level)
            {
                case 1: return 500;
                case 2: return 1000;
                case 3: return 1500;
                case 4: return 2000;
                case 5: return 2500;
                default: return 0;
            }
        }

    }
}
