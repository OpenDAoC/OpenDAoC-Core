using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_EmptyMind : NfRaTheEmptyMindHandler
	{
        public AtlasOF_EmptyMind(DbAbilities dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        protected override int GetDuration() { return 60000; }
        public override int CostForUpgrade(int currentLevel) { return OfRaHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }

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