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
    /// Long Wind : Decreases the amount of endurance taken per tick when sprinting, by the number listed.
    /// </summary>
    public class AtlasOF_LongWindAbility : RAPropertyEnhancer
    {

        public AtlasOF_LongWindAbility(DbAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        protected override string ValueUnit { get { return "%"; } }

        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor5LevelsRA(currentLevel); }

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) { return 0; }
            switch (level)
            {
                case 1: return 20;
                case 2: return 40;
                case 3: return 60;
                case 4: return 80;
                case 5: return 100;
                default: return 0;
            }
        }

	}
}
