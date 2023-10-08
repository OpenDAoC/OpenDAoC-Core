using NUnit.Framework;
using DOL.GS;
using DOL.GS.PropertyCalc;

namespace DOL.Tests.Unit.Gameserver.PropertyCalc
{
    [TestFixture]
    class UT_MeleeDamagePercentCalculator
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
        private FakeNPC NewNPC() => new FakeNPC();
    }
}
