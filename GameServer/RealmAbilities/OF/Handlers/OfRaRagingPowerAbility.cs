using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaRagingPowerAbility : NfRaRagingPowerAbility
{
	public OfRaRagingPowerAbility(DbAbility dba, int level) : base(dba, level) { }

    public override int MaxLevel { get { return 1; } }
    public override int CostForUpgrade(int level) { return 10; }
    public override int GetReUseDelay(int level) { return 1800; } // 30 mins
    protected override int GetPowerHealAmount() { return 100; }

    // MCL 2 pre-req.
    public override bool CheckRequirement(GamePlayer player)
    {
        OfRaMysticCrystalLoreAbility MCL = player.GetAbility<OfRaMysticCrystalLoreAbility>();
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