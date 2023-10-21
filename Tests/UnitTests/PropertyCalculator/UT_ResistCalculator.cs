using Core.GS;
using Core.GS.Calculators;
using Core.GS.Enums;
using NUnit.Framework;

namespace Core.Tests.Unit.Gameserver.PropertyCalc
{
    [TestFixture]
    class UT_ResistCalculator
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
        private FakeNPC NewNPC() => new FakeNPC();
        private EProperty SomeResistProperty => EProperty.Resist_First;
    }
}
