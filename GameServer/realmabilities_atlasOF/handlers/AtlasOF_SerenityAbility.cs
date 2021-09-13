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
    /// Serenity : Your spell points regenerate faster than normal.
    /// Pre-Requisits : Augmented Acuity lvl 2
    /// </summary>
    public class AtlasOF_SerenityAbility : RAPropertyEnhancer
    {

        public AtlasOF_SerenityAbility(DBAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugAcuityLevel(player, 2); }

        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) { return 0; }
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
