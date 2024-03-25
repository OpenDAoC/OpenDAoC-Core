using DOL.Database;
using DOL.GS;
using DOL.GS.Spells;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Unit.Gameserver
{
    [TestFixture]
    class UT_PropertyChangingSpell
    {
        [SetUp]
        public void Init()
        {
            GameLiving.LoadCalculators();
        }

        [Test]
        public void ApplyBuffOnTarget_5ResiPierceOnAnotherPlayer_Has5ResiPierceBaseBuffBonus()
        {
            var caster = new FakePlayer();
            var target = new FakePlayer();
            var spell = NewSpellWithValue(5);
            var spellLine = NewBasecSpellLine();
            var resiPierceBuff = new ResiPierceBuff(caster, spell, spellLine);

            resiPierceBuff.ApplyEffectOnTarget(target);

            var actual = target.BaseBuffBonusCategory[eProperty.ResistPierce];
            ClassicAssert.AreEqual(5, actual);
        }

        [Test]
        [Category("Unit")]
        [Category("Unreliable")]
        public void ApplyEffectOnTarget_50ConBuffOnL50NPC_51Constitution()
        {
            var caster = new FakeNPC();
            var target = new FakeNPC();
            target.Level = 50;
            var spell = NewSpellWithValue(50);
            var spellLine = NewBasecSpellLine();
            var constitutionBuff = new ConstitutionBuff(caster, spell, spellLine);

            constitutionBuff.ApplyEffectOnTarget(target);

            var actual = target.GetModified(eProperty.Constitution);
            ClassicAssert.AreEqual(51, actual);
        }

        private Spell NewSpellWithValue(int value)
        {
            var dbSpell = new DbSpell();
            dbSpell.Value = value;
            dbSpell.Target = "Realm";
            dbSpell.Duration = 10;
            var spell = new Spell(dbSpell, 0);
            return spell;
        }

        private SpellLine NewBasecSpellLine() => new SpellLine("", "", "", true);
    }
}
