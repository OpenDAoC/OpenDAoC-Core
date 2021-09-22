using System.Reflection;
using System.Collections;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{

    public class AtlasOF_SoldiersBarricade : SoldiersBarricadeAbility
    {
        public AtlasOF_SoldiersBarricade(DBAbility dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 10; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        protected override int GetArmorFactorAmount() { return 500; }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Level 1: Value: 500 AF");
        }
    }
}