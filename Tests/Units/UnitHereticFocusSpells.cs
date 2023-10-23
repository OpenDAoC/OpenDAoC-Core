using Core.Database.Tables;
using Core.GS;
using Core.GS.GameUtils;
using Core.GS.Players;
using Core.GS.Skills;
using Core.GS.Spells;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitHereticFocusSpells
{
    [SetUp]
    public void SetUp()
    {
        GameServer.LoadTestDouble(new FakeServer());
    }

    [Test]
    public void OnDirectEffect_100InitialDamage_NoTick_FirstTickDoes100Damage()
    {
        double initialDamage = 100;
        int growthPercent = 25;
        var spell = NewHereticFocusDamageSpell(initialDamage, growthPercent);
        var source = NewL50Player();
        var target = NewFakeNPC();
        var spellLine = NewGenericSpellLine();
        var damageFocus = new RampingDamageFocusSpell(source, spell, spellLine);

        Util.LoadTestDouble(new ChanceAlwaysHundredPercent());
        damageFocus.OnDirectEffect(target);

        var actual = source.LastDamageDealt;
        var expected = 100;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void OnDirectEffect_100InitialDamageAnd25PercentGrowth_TickTwice_NextTickDoes150Damage()
    {
        double initialDamage = 100;
        int growthPercent = 25;
        var spell = NewHereticFocusDamageSpell(initialDamage, growthPercent);
        var source = NewL50Player();
        var target = NewFakeNPC();
        var spellLine = NewGenericSpellLine();
        var damageFocus = new RampingDamageFocusSpell(source, spell, spellLine);

        Util.LoadTestDouble(new ChanceAlwaysHundredPercent());
        damageFocus.OnSpellPulse(null);
        damageFocus.OnSpellPulse(null);
        damageFocus.OnDirectEffect(target);

        var actual = source.LastDamageDealt;
        var expected = 150;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void OnDirectEffect_100InitialDamageAnd50PercentGrowth_OneTick_NextTickDoes150Damage()
    {
        double initialDamage = 100;
        int growthPercent = 50;
        var spell = NewHereticFocusDamageSpell(initialDamage, growthPercent);
        var source = NewL50Player();
        var target = NewFakeNPC();
        var spellLine = NewGenericSpellLine();
        var damageFocus = new RampingDamageFocusSpell(source, spell, spellLine);

        Util.LoadTestDouble(new ChanceAlwaysHundredPercent());
        damageFocus.OnSpellPulse(null);
        damageFocus.OnDirectEffect(target);

        var actual = source.LastDamageDealt;
        var expected = 150;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void OnDirectEffect_100InitialDamageAnd50PercentGrowthAnd70PercentGrowthCap_TickTwice_NextTickDoes170Damage()
    {
        double initialDamage = 100;
        int growthPercent = 50;
        int growthCapPercent = 70;
        var spell = NewHereticFocusDamageSpell(initialDamage, growthPercent, growthCapPercent);
        var source = NewL50Player();
        var target = NewFakeNPC();
        var spellLine = NewGenericSpellLine();
        var damageFocus = new RampingDamageFocusSpell(source, spell, spellLine);

        Util.LoadTestDouble(new ChanceAlwaysHundredPercent());
        damageFocus.OnSpellPulse(null);
        damageFocus.OnSpellPulse(null);
        damageFocus.OnDirectEffect(target);

        var actual = source.LastDamageDealt;
        var expected = 170;
        Assert.AreEqual(expected, actual);
    }

    private Spell NewHereticFocusDamageSpell(double initialDamage, int growthPercent)
    {
        int noCap = int.MaxValue;
        return NewHereticFocusDamageSpell(initialDamage, growthPercent, noCap);
    }

    private Spell NewHereticFocusDamageSpell(double initialDamage, int growthPercent, int growthCapPercent)
    {
        var dbspell = new DbSpell();
        dbspell.LifeDrainReturn = growthPercent;
        dbspell.AmnesiaChance = growthCapPercent;
        dbspell.Target = "Enemy";
        var spell = new Spell(dbspell, 1);
        spell.Damage = initialDamage;
        spell.Level = 50;
        return spell;
    }

    private SpellLine NewGenericSpellLine()
    {
        return new SpellLine("keyname", "lineName", "specName", true);
    }

    private UnitFakePlayer NewL50Player()
    {
        var player = new UnitFakePlayer();
        player.FakePlayerClass = new DefaultPlayerClass();
        player.Level = 50;
        return player;
    }

    private class ChanceAlwaysHundredPercent : Util
    {
        protected override int RandomImpl(int min, int max)
        {
            return 100;
        }
    }

    private static UnitFakeNpc NewFakeNPC() => new UnitFakeNpc();
}