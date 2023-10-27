﻿using Core.Database.Tables;
using Core.GS;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitPropertyChangingSpell
{
    [SetUp]
    public void Init()
    {
        GameLiving.LoadCalculators();
    }

    [Test]
    public void ApplyBuffOnTarget_5ResiPierceOnAnotherPlayer_Has5ResiPierceBaseBuffBonus()
    {
        var caster = new UnitFakePlayer();
        var target = new UnitFakePlayer();
        var spell = NewSpellWithValue(5);
        var spellLine = NewBasecSpellLine();
        var resiPierceBuff = new ResiPierceBuff(caster, spell, spellLine);

        resiPierceBuff.ApplyEffectOnTarget(target);

        var actual = target.BaseBuffBonusCategory[EProperty.ResistPierce];
        Assert.AreEqual(5, actual);
    }

    [Test]
    [Category("Unit")]
    [Category("Unreliable")]
    public void ApplyEffectOnTarget_50ConBuffOnL50NPC_51Constitution()
    {
        var caster = new UnitFakeNpc();
        var target = new UnitFakeNpc();
        target.Level = 50;
        var spell = NewSpellWithValue(50);
        var spellLine = NewBasecSpellLine();
        var constitutionBuff = new ConstitutionBuff(caster, spell, spellLine);

        constitutionBuff.ApplyEffectOnTarget(target);

        var actual = target.GetModified(EProperty.Constitution);
        Assert.AreEqual(51, actual);
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