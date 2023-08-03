using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class OfRaRagingPowerHandler : NfRaRagingPowerHandler
	{
		public OfRaRagingPowerHandler(DbAbilities dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 10; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        protected override int GetPowerHealAmount() { return 100; }

        // MCL 2 pre-req.
        public override bool CheckRequirement(GamePlayer player)
        {
            OfRaMysticCrystalLoreHandler MCL = player.GetAbility<OfRaMysticCrystalLoreHandler>();
            if (MCL == null)
                return false;

            return player.CalculateSkillLevel(MCL) > 1;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Value: 100%");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Casting time: instant");
        }
    }
}