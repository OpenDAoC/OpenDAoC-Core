using Core.GS.Calculators;
using Core.GS.Enums;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitResistCalculator
{
    [Test]
    public void CalcValue_50ConBuff_6()
    {
        var npc = NewNPC();
        npc.BaseBuffBonusCategory[EProperty.Constitution] = 50;

        int actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

        Assert.AreEqual(6, actual);
    }

    [Test]
    public void CalcValue_50ConDebuff_Minus6()
    {
        var npc = NewNPC();
        npc.DebuffCategory[EProperty.Constitution] = 50;

        int actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

        Assert.AreEqual(-6, actual);
    }

    private ResistsCalculator ResistCalculator => new ResistsCalculator();
    private UnitFakeNpc NewNPC() => new UnitFakeNpc();
    private EProperty SomeResistProperty => EProperty.Resist_First;
}