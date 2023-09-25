using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Effects;
using DOL.Events;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_EmptyMind : TheEmptyMindAbility
	{
        public AtlasOF_EmptyMind(DbAbility dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        protected override int GetDuration() { return 60000; }
        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }

        protected override int GetEffectiveness()
        {
            switch (Level)
            {
                case 1: return 5;
                case 2: return 10;
                case 3: return 15;
                default: return 0;
            }
        }
    }
}
