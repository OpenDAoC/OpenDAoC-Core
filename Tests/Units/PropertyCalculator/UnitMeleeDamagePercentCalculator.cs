using Core.GS.Calculators;
using Core.GS.Enums;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitMeleeDamagePercentCalculator
{
    [Test]
    public void CalcValue_50StrengthBuff_6()
    {
        var npc = NewNPC();
        npc.BaseBuffBonusCategory[EProperty.Strength] = 50;

        int actual = MeleeDamageBonusCalculator.CalcValue(npc, MeleeDamageProperty);

        Assert.AreEqual(6, actual);
    }

    [Test]
    public void CalcValue_NPCWith50StrengthDebuff_Minus6()
    {
        var npc = NewNPC();
        npc.DebuffCategory[EProperty.Strength] = 50;

        int actual = MeleeDamageBonusCalculator.CalcValue(npc, MeleeDamageProperty);

        Assert.AreEqual(-6, actual);
    }

    private MeleeDamagePercentCalculator MeleeDamageBonusCalculator => new MeleeDamagePercentCalculator();
    private EProperty MeleeDamageProperty => EProperty.MeleeDamage;
    private UnitFakeNpc NewNPC() => new UnitFakeNpc();
}