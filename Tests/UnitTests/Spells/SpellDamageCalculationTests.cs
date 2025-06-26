using System;
using DOL.GS;
using DOL.GS.Spells;
using DOL.Database;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.UnitTests.Spells
{
    /// <summary>
    /// Tests for spell damage calculation mechanics.
    /// Reference: Core_Systems_Game_Rules.md - Magic System
    /// </summary>
    [TestFixture]
    public class SpellDamageCalculationTests
    {
        private Mock<GamePlayer> _caster;
        private Mock<GamePlayer> _target;
        
        [SetUp]
        public void Setup()
        {
            _caster = MockHelper.CreateMockCharacter(level: 50);
            _target = MockHelper.CreateMockCharacter(level: 50);
        }
        
        [Test]
        public void CalculateToHitChance_ShouldReturn87Point5Percent_WhenBaseSpellCast()
        {
            // Test validates: DAoC Rule - Base spell hit chance is 87.5%
            // Reference: Core_Systems_Game_Rules.md - Magic System - Spell Hit Chance
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 50);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            // Act
            double hitChance = handler.CalculateToHitChance(_target.Object);
            
            // Assert
            hitChance.Should().Be(87.5, "Base spell hit chance should be 87.5%");
        }
        
        [Test]
        public void CalculateToHitChance_ShouldReduceBy2Point5Percent_WhenDualComponentSpell()
        {
            // Test validates: DAoC Rule - Dual component spells have -2.5% hit penalty
            // Reference: Core_Systems_Game_Rules.md - Magic System - Spell Hit Chance
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 50);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestDualComponentSpellHandler(_caster.Object, spell, spellLine);
            
            // Act
            double hitChance = handler.CalculateToHitChance(_target.Object);
            
            // Assert
            hitChance.Should().Be(85.0, "Dual component spells should have 2.5% lower hit chance");
        }
        
        [Test]
        public void CalculateToHitChance_ShouldModifyByLevelDifference_WhenCasterAndTargetDifferentLevels()
        {
            // Test validates: DAoC Rule - (spellLevel - targetLevel) / 2.0 modifier
            // Test scenario: Level 30 spell vs level 40 target
            // Expected: 87.5 + (30 - 40) / 2 = 87.5 - 5 = 82.5%
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 30);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            _target.Setup(x => x.Level).Returns((byte)40);
            
            // Act
            double hitChance = handler.CalculateToHitChance(_target.Object);
            
            // Assert
            hitChance.Should().Be(82.5, "Hit chance should be modified by spell/target level difference");
        }
        
        [Test]
        public void CalculateDamageVariance_ShouldReturnSpecBasedVariance_WhenStandardSpell()
        {
            // Test validates: DAoC Rule - min = (spec - 1) / targetLevel, max = 1.0
            // Test scenario: Spec 51 vs level 50 target
            // Expected: min = (51 - 1) / 50 = 1.0, max = 1.0
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 50);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            _caster.Setup(x => x.GetModifiedSpecLevel("Test_Spec")).Returns(51);
            
            // Act
            handler.CalculateDamageVariance(_target.Object, out double min, out double max);
            
            // Assert
            min.Should().BeApproximately(1.0, 0.01, "Min variance should be (51-1)/50 = 1.0");
            max.Should().Be(1.0, "Max variance should always be 1.0 for standard spells");
        }
        
        [Test]
        public void CalculateDamageVariance_ShouldReturn60To100Percent_WhenMobSpell()
        {
            // Test validates: DAoC Rule - Mob spells have fixed 60-100% variance
            // Reference: Core_Systems_Game_Rules.md - Magic System - Spell Damage Variance
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 50);
            var spellLine = new SpellLine(GlobalSpellsLines.Mob_Spells, "Mob", "Mob", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            // Act
            handler.CalculateDamageVariance(_target.Object, out double min, out double max);
            
            // Assert
            min.Should().Be(0.6, "Mob spells should have 60% min variance");
            max.Should().Be(1.0, "Mob spells should have 100% max variance");
        }
        
        [Test]
        public void CalculateDamageVariance_ShouldReturn75To125Percent_WhenItemEffect()
        {
            // Test validates: DAoC Rule - Item effects have 75-125% variance
            // Reference: Core_Systems_Game_Rules.md - Magic System - Spell Damage Variance
            
            // Arrange
            var spell = new Spell(new DbSpell { }, 50);
            var spellLine = new SpellLine(GlobalSpellsLines.Item_Effects, "Item", "Item", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            // Act
            handler.CalculateDamageVariance(_target.Object, out double min, out double max);
            
            // Assert
            min.Should().Be(0.75, "Item effects should have 75% min variance");
            max.Should().Be(1.25, "Item effects should have 125% max variance");
        }
        
        [Test]
        public void CalculateDamageBase_ShouldApplyStatAndSpecModifiers_WhenCasterHasIntAndSpec()
        {
            // Test validates: DAoC Rule - Damage * (1 + stat * 0.005) * (1 + spec * 0.005)
            // Test scenario: 100 base damage, 200 INT, 50 spec bonus
            // Expected: 100 * (1 + 200 * 0.005) * (1 + 50 * 0.005) = 100 * 2 * 1.25 = 250
            
            // Arrange
            var spell = new Spell(new DbSpell { Damage = 100 }, 50);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            _caster.Setup(x => x.GetModified(eProperty.Intelligence)).Returns(200);
            _caster.Setup(x => x.ItemBonus[It.IsAny<eProperty>()]).Returns(50); // Spec item bonus
            
            // Act
            double damage = handler.CalculateDamageBase(_target.Object);
            
            // Assert
            damage.Should().BeApproximately(250, 0.1, "Damage should be modified by INT and spec bonuses");
        }
        
        [Test]
        public void ModifyDamageWithTargetResist_ShouldApplyPrimaryAndSecondaryResists_WhenTargetHasResists()
        {
            // Test validates: DAoC Rule - damage * (1 - primary%) * (1 - secondary%)
            // Test scenario: 1000 damage, 26% primary resist, 20% secondary resist
            // Expected: 1000 * (1 - 0.26) * (1 - 0.20) = 1000 * 0.74 * 0.80 = 592
            
            // Arrange
            var spell = new Spell(new DbSpell { DamageType = (int)eDamageType.Heat }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            var attackData = new AttackData { Target = _target.Object };
            
            _target.Setup(x => x.GetResist(eDamageType.Heat)).Returns(26); // Primary resist
            _target.Setup(x => x.SpecBuffBonusCategory[eProperty.Resist_Heat]).Returns(20); // Secondary resist
            
            // Act
            double modifiedDamage = handler.ModifyDamageWithTargetResist(attackData, 1000);
            
            // Assert
            modifiedDamage.Should().BeApproximately(592, 1, "Damage should be reduced by both resist layers");
        }
        
        [Test]
        public void DamageCap_ShouldReturnTripleSpellDamage_WhenCalculatingCap()
        {
            // Test validates: DAoC Rule - Damage cap = Spell.Damage * 3.0 * effectiveness
            // Test scenario: 100 base damage, 1.5 effectiveness
            // Expected: 100 * 3.0 * 1.5 = 450
            
            // Arrange
            var spell = new Spell(new DbSpell { Damage = 100 }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            // Act
            double cap = handler.DamageCap(1.5);
            
            // Assert
            cap.Should().Be(450, "Damage cap should be 3x spell damage times effectiveness");
        }
        
        [Test]
        public void CalculateDamageToTarget_ShouldApplyCriticalDamage_WhenCriticalHit()
        {
            // Test validates: DAoC Rule - Spell criticals 10-100% vs NPCs, 10-50% vs players
            // Reference: Core_Systems_Game_Rules.md - Magic System - Critical Damage
            
            // Arrange
            var spell = new Spell(new DbSpell { Damage = 100 }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestSpellHandler(_caster.Object, spell, spellLine);
            
            _caster.Setup(x => x.SpellCriticalChance).Returns(100); // Guaranteed crit for test
            
            // Act
            var attackData = handler.CalculateDamageToTarget(_target.Object);
            
            // Assert
            attackData.CriticalDamage.Should().BeGreaterThan(0, "Critical damage should be applied");
            attackData.CriticalDamage.Should().BeLessOrEqualTo((int)(attackData.Damage * 0.5), 
                "Critical damage vs players should be capped at 50% of base damage");
        }
    }
    
    // Test helper class to access protected methods
    public class TestSpellHandler : SpellHandler
    {
        public TestSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) 
            : base(caster, spell, spellLine) { }
            
        public new double CalculateToHitChance(GameLiving target) => base.CalculateToHitChance(target);
        public new void CalculateDamageVariance(GameLiving target, out double min, out double max) 
            => base.CalculateDamageVariance(target, out min, out max);
        public new double CalculateDamageBase(GameLiving target) => base.CalculateDamageBase(target);
        public new double ModifyDamageWithTargetResist(AttackData ad, double damage) 
            => base.ModifyDamageWithTargetResist(ad, damage);
        public new double DamageCap(double effectiveness) => base.DamageCap(effectiveness);
        public new AttackData CalculateDamageToTarget(GameLiving target) => base.CalculateDamageToTarget(target);
    }
    
    public class TestDualComponentSpellHandler : TestSpellHandler
    {
        public TestDualComponentSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) 
            : base(caster, spell, spellLine) { }
            
        protected override bool IsDualComponentSpell => true;
    }
} 