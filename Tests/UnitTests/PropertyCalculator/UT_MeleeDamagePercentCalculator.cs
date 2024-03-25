using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            npc.BaseBuffBonusCategory[eProperty.Strength] = 50;

            int actual = MeleeDamageBonusCalculator.CalcValue(npc, MeleeDamageProperty);

            ClassicAssert.AreEqual(6, actual);
        }

        [Test]
        public void CalcValue_NPCWith50StrengthDebuff_Minus6()
        {
            var npc = NewNPC();
            npc.DebuffCategory[eProperty.Strength] = 50;

            int actual = MeleeDamageBonusCalculator.CalcValue(npc, MeleeDamageProperty);

            ClassicAssert.AreEqual(-6, actual);
        }

        private MeleeDamagePercentCalculator MeleeDamageBonusCalculator => new MeleeDamagePercentCalculator();
        private eProperty MeleeDamageProperty => eProperty.MeleeDamage;
        private FakeNPC NewNPC() => new FakeNPC();
    }
}
