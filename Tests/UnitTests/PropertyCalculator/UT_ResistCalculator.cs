using DOL.GS;
using DOL.GS.PropertyCalc;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Unit.Gameserver.PropertyCalc
{
    [TestFixture]
    class UT_ResistCalculator
    {
        [Test]
        public void CalcValue_50ConBuff_6()
        {
            var npc = NewNPC();
            npc.BaseBuffBonusCategory[eProperty.Constitution] = 50;

            int actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

            ClassicAssert.AreEqual(6, actual);
        }

        [Test]
        public void CalcValue_50ConDebuff_Minus6()
        {
            var npc = NewNPC();
            npc.DebuffCategory[eProperty.Constitution] = 50;

            int actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

            ClassicAssert.AreEqual(-6, actual);
        }

        private ResistCalculator ResistCalculator => new ResistCalculator();
        private FakeNPC NewNPC() => new FakeNPC();
        private eProperty SomeResistProperty => eProperty.Resist_First;
    }
}
