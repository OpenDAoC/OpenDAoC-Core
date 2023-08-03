using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{

    public class OfRaSoldiersBarricadeHandler : NfRaSoldiersBarricadeHandler
    {
        public OfRaSoldiersBarricadeHandler(DbAbilities dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 10; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        protected override int GetArmorFactorAmount() { return 330; }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add(string.Format("Value: {0} AF", GetArmorFactorAmount()));
        }
    }
}