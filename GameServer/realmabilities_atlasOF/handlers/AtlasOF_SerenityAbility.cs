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

        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }

        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }


    }
}
