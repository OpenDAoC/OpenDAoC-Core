using System;
using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.Effects;
using DOL.Database;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.UnitTests.Spells
{
    /// <summary>
    /// Tests for crowd control mechanics including mez, stun, and root.
    /// Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control
    /// </summary>
    [TestFixture]
    public class CrowdControlTests
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
        public void CalculateEffectDuration_ShouldApplyResistReduction_WhenTargetHasResists()
        {
            // Test validates: DAoC Rule - CC duration reduced by target's resist percentage
            // Test scenario: 30 second mezz vs target with 25% heat resist
            // Expected: 30000 * (1 - 0.25) = 22500ms
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 30,
                DamageType = (int)eDamageType.Heat,
                Type = eSpellType.Mesmerize.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestMesmerizeSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.GetResistBase(eDamageType.Heat)).Returns(25);
            
            // Act
            int duration = handler.CalculateEffectDuration(_target.Object);
            
            // Assert
            duration.Should().Be(22500, "CC duration should be reduced by 25% due to resists");
        }
        
        [Test]
        public void CalculateEffectDuration_ShouldClampMinimumAt1ms_WhenHighResists()
        {
            // Test validates: DAoC Rule - CC duration minimum is 1ms regardless of resists
            // Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 10,
                DamageType = (int)eDamageType.Body,
                Type = eSpellType.Stun.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestStunSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.GetResistBase(eDamageType.Body)).Returns(99); // 99% resist
            
            // Act
            int duration = handler.CalculateEffectDuration(_target.Object);
            
            // Assert
            duration.Should().BeGreaterOrEqualTo(1, "CC duration should never go below 1ms");
        }
        
        [Test]
        public void CalculateEffectDuration_ShouldClampMaximumAt4xSpellDuration_WhenNegativeResists()
        {
            // Test validates: DAoC Rule - CC duration maximum is 4x spell duration
            // Test scenario: 10 second stun with -100% resists (vulnerability)
            // Expected: Maximum 40 seconds (10 * 4)
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 10,
                DamageType = (int)eDamageType.Spirit,
                Type = eSpellType.Stun.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestStunSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.GetResistBase(eDamageType.Spirit)).Returns(-100); // Vulnerability
            
            // Act
            int duration = handler.CalculateEffectDuration(_target.Object);
            
            // Assert
            duration.Should().BeLessOrEqualTo(40000, "CC duration should be capped at 4x spell duration");
        }
        
        [Test]
        public void MezDuration_ShouldBeReducedByMesmerizeDurationReduction_WhenTargetHasProperty()
        {
            // Test validates: DAoC Rule - MesmerizeDurationReduction property reduces mez duration
            // Test scenario: 30 second mez vs target with 50% mez duration reduction
            // Expected: 30000 * 0.5 = 15000ms
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 30,
                Type = eSpellType.Mesmerize.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestMesmerizeSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.GetModified(eProperty.MesmerizeDurationReduction)).Returns(50); // 50% reduction
            
            // Act
            int duration = handler.CalculateEffectDuration(_target.Object);
            
            // Assert
            duration.Should().Be(15000, "Mez duration should be reduced by 50%");
        }
        
        [Test]
        public void StunDuration_ShouldBeReducedByStunDurationReduction_WhenTargetHasProperty()
        {
            // Test validates: DAoC Rule - StunDurationReduction property reduces stun duration
            // Test scenario: 9 second stun vs target with 33% stun duration reduction (Stoicism)
            // Expected: 9000 * 0.67 = 6030ms
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 9,
                Type = eSpellType.Stun.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestStunSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.GetModified(eProperty.StunDurationReduction)).Returns(33); // 33% reduction
            
            // Act
            int duration = handler.CalculateEffectDuration(_target.Object);
            
            // Assert
            duration.Should().BeApproximately(6030, 10, "Stun duration should be reduced by 33%");
        }
        
        [Test]
        public void CheckSpellResist_ShouldReturnTrue_WhenTargetHasMezImmunity()
        {
            // Test validates: DAoC Rule - Mez immunity prevents new mezzes
            // Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control Immunity
            
            // Arrange
            var spell = new Spell(new DbSpell { Type = eSpellType.Mesmerize.ToString() }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestMesmerizeSpellHandler(_caster.Object, spell, spellLine);
            
            _target.Setup(x => x.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity))
                .Returns(true);
            
            // Act
            bool isResisted = handler.CheckSpellResist(_target.Object);
            
            // Assert
            isResisted.Should().BeTrue("Target with mez immunity should resist mezzes");
        }
        
        [Test]
        public void CheckSpellResist_ShouldReturnTrue_WhenNPCBelowHealthThreshold()
        {
            // Test validates: DAoC Rule - NPCs below 75% health resist mezzes
            // Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control
            
            // Arrange
            var spell = new Spell(new DbSpell { Type = eSpellType.Mesmerize.ToString() }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestMesmerizeSpellHandler(_caster.Object, spell, spellLine);
            
            var npcTarget = new Mock<GameNPC>();
            npcTarget.Setup(x => x.HealthPercent).Returns(70); // 70% health
            
            // Act
            bool isResisted = handler.CheckSpellResist(npcTarget.Object);
            
            // Assert
            isResisted.Should().BeTrue("NPCs below 75% health should resist mezzes");
        }
        
        [Test]
        public void StyleStun_ShouldHave5xDurationImmunity_WhenExpires()
        {
            // Test validates: DAoC Rule - Style stuns have 5x duration immunity
            // Test scenario: 3 second style stun
            // Expected immunity: 15 seconds (3 * 5)
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 3,
                Type = eSpellType.StyleStun.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new StyleStun(_caster.Object, spell, spellLine);
            var effect = new GameSpellEffect(handler, 3000, 0);
            
            // Act
            int immunityDuration = handler.OnEffectExpires(effect, false);
            
            // Assert
            immunityDuration.Should().Be(15000, "Style stun immunity should be 5x the stun duration");
        }
        
        [Test]
        public void StandardCC_ShouldHave60SecondImmunity_WhenExpires()
        {
            // Test validates: DAoC Rule - Standard CC has 60 second immunity
            // Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control Immunity
            
            // Arrange
            var spell = new Spell(new DbSpell 
            { 
                Duration = 30,
                Type = eSpellType.Mesmerize.ToString()
            }, 0);
            var spellLine = new SpellLine("Test", "Test", "Test", false);
            var handler = new TestMesmerizeSpellHandler(_caster.Object, spell, spellLine);
            var effect = new GameSpellEffect(handler, 30000, 0);
            
            // Act
            int immunityDuration = handler.OnEffectExpires(effect, false);
            
            // Assert
            immunityDuration.Should().Be(60000, "Standard CC should have 60 second immunity");
        }
        
        [Test]
        public void NPCStunImmunity_ShouldProvideDiminishingReturns_WhenReapplied()
        {
            // Test validates: DAoC Rule - NPCs get diminishing returns on CC
            // Test scenario: First stun 9s, second 4.5s, third 2.25s
            // Reference: Core_Systems_Game_Rules.md - Magic System - Crowd Control
            
            // Arrange
            var npc = new Mock<GameNPC>();
            var immunityEffect = new NpcStunImmunityEffect(new ECSGameEffectInitParams(npc.Object, 60000, 1.0, null));
            
            // Act & Assert
            immunityEffect.CanApplyNewEffect(9000).Should().BeTrue("First stun should be applicable");
            immunityEffect.OnApplyNewEffect(); // Apply first stun
            
            var secondDuration = immunityEffect.CalculateNewEffectDuration(9000);
            secondDuration.Should().Be(4500, "Second stun should be 50% duration");
            
            immunityEffect.OnApplyNewEffect(); // Apply second stun
            var thirdDuration = immunityEffect.CalculateNewEffectDuration(9000);
            thirdDuration.Should().Be(2250, "Third stun should be 25% duration");
        }
    }
    
    // Test helper classes to access protected methods
    public class TestMesmerizeSpellHandler : MesmerizeSpellHandler
    {
        public TestMesmerizeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) 
            : base(caster, spell, spellLine) { }
            
        public new int CalculateEffectDuration(GameLiving target) => base.CalculateEffectDuration(target);
        public new bool CheckSpellResist(GameLiving target) => base.CheckSpellResist(target);
    }
    
    public class TestStunSpellHandler : StunSpellHandler
    {
        public TestStunSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) 
            : base(caster, spell, spellLine) { }
            
        public new int CalculateEffectDuration(GameLiving target) => base.CalculateEffectDuration(target);
    }
} 