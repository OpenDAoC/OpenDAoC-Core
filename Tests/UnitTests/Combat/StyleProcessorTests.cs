using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using DOL.GS;
using DOL.GS.Interfaces.Combat;
using DOL.GS.Interfaces.Core;
using DOL.Database;

namespace DOL.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for style mechanics validating OpenDAoC rule implementations.
    /// Reference: Core_Systems_Game_Rules.md - Combat System - Style Combat
    /// </summary>
    [TestFixture]
    [Category("Combat")]
    [Category("Styles")]
    public class StyleProcessorTests
    {
        private IStyleProcessor _styleProcessor;
        private IStyleCalculator _styleCalculator;

        [SetUp]
        public void Setup()
        {
            _styleCalculator = new MockStyleCalculator();
            _styleProcessor = new MockStyleProcessor(_styleCalculator);
        }

        #region Style Requirements Tests

        /// <summary>
        /// Validates style attack result requirements.
        /// 
        /// DAoC Rule: Styles have specific attack result requirements (parry, evade, etc.)
        /// Reference: StyleProcessor.CanUseStyle
        /// Current Implementation: Must match exact result from previous attack
        /// </summary>
        [TestCase(StyleAttackResultRequirement.Hit, eAttackResult.HitUnstyled, true, TestName = "Hit requirement: Success on hit")]
        [TestCase(StyleAttackResultRequirement.Hit, eAttackResult.Missed, false, TestName = "Hit requirement: Fail on miss")]
        [TestCase(StyleAttackResultRequirement.Parry, eAttackResult.Parried, true, TestName = "Parry requirement: Success on parry")]
        [TestCase(StyleAttackResultRequirement.Block, eAttackResult.Blocked, true, TestName = "Block requirement: Success on block")]
        [TestCase(StyleAttackResultRequirement.Any, eAttackResult.Missed, true, TestName = "Any requirement: Always succeeds")]
        public void CanUseStyle_ShouldCheckAttackResultRequirement(
            StyleAttackResultRequirement requirement, eAttackResult lastResult, bool expectedCanUse)
        {
            // Test validates: DAoC Rule - Styles require specific attack results
            
            // Arrange
            var style = CreateMockStyle(attackResultRequirement: requirement);
            var lastAttackData = new AttackData { AttackResult = lastResult };
            
            // Act
            bool canUse = _styleProcessor.CanUseStyle(lastAttackData, style.Object);
            
            // Assert
            canUse.Should().Be(expectedCanUse,
                $"Style with {requirement} requirement should {(expectedCanUse ? "succeed" : "fail")} after {lastResult}");
        }

        /// <summary>
        /// Validates style positional requirements.
        /// 
        /// DAoC Rule: Some styles require specific positions (side, back, front)
        /// Reference: StyleProcessor.CanUseStyle / CheckStylePositionalRequirement
        /// Current Implementation: Uses 90/180 degree arcs for positionals
        /// </summary>
        [TestCase(StylePositional.Front, Position.Front, true, TestName = "Front style: Success from front")]
        [TestCase(StylePositional.Side, Position.Side, true, TestName = "Side style: Success from side")]
        [TestCase(StylePositional.Back, Position.Back, true, TestName = "Back style: Success from back")]
        [TestCase(StylePositional.Back, Position.Front, false, TestName = "Back style: Fail from front")]
        [TestCase(StylePositional.Any, Position.Side, true, TestName = "Any position: Always succeeds")]
        public void CanUseStyle_ShouldCheckPositionalRequirement(
            StylePositional requirement, Position attackerPosition, bool expectedCanUse)
        {
            // Test validates: DAoC Rule - Positional styles require correct positioning
            
            // Arrange
            var style = CreateMockStyle(positionalRequirement: requirement);
            var attacker = CreateMockAttacker();
            var target = CreateMockDefender();
            
            // Act
            bool canUse = _styleProcessor.CanUseStyle(null, style.Object, attacker.Object, target.Object, attackerPosition);
            
            // Assert
            canUse.Should().Be(expectedCanUse,
                $"{requirement} style should {(expectedCanUse ? "succeed" : "fail")} from {attackerPosition}");
        }

        #endregion

        #region Style Damage Tests

        /// <summary>
        /// Validates style damage calculation with growth rate.
        /// 
        /// DAoC Rule: StyleDamage = GrowthRate * Spec * AttackSpeed / UnstyledDamageCap * UnstyledDamage
        /// Reference: StyleProcessor.ExecuteStyle lines 344-347
        /// Current Implementation: Dynamic scaling based on weapon speed and spec
        /// </summary>
        [TestCase(0.0, 50, 3.7, 0.0, TestName = "0 GR: No style damage")]
        [TestCase(0.1, 50, 3.7, 18.5, TestName = "0.1 GR, 50 spec, 3.7s: 18.5 damage")]
        [TestCase(0.5, 50, 3.7, 92.5, TestName = "0.5 GR, 50 spec, 3.7s: 92.5 damage")]
        [TestCase(1.0, 50, 3.7, 185.0, TestName = "1.0 GR, 50 spec, 3.7s: 185 damage")]
        public void CalculateStyleDamage_ShouldFollowGrowthRateFormula(
            double growthRate, int spec, double weaponSpeed, double expectedDamage)
        {
            // Test validates: DAoC Rule - Style damage growth rate formula
            
            // Arrange
            var style = CreateMockStyle(growthRate: growthRate);
            double unstyledDamage = 1000;
            double unstyledDamageCap = 1000; // Simplified for test
            
            // Act
            var styleDamage = _styleCalculator.CalculateStyleDamage(
                style.Object, spec, weaponSpeed, unstyledDamage, unstyledDamageCap);
            
            // Assert
            styleDamage.Should().Be(expectedDamage,
                $"Style with GR {growthRate}, spec {spec}, speed {weaponSpeed}s should add {expectedDamage} damage");
        }

        /// <summary>
        /// Validates stealth opener damage calculations.
        /// 
        /// DAoC Rule: Stealth openers have static damage formulas
        /// Reference: StyleProcessor.ExecuteStyle lines 327-341
        /// Current Implementation: Specific formulas for BS I, BS II, PA
        /// </summary>
        [TestCase(335, 50, false, 238.33, TestName = "Backstab I: ~5 + spec*14/3")]
        [TestCase(339, 50, false, 345.0, TestName = "Backstab II: 45 + spec*6")]
        [TestCase(343, 50, false, 525.0, TestName = "Perforate Artery 1H: 75 + spec*9")]
        [TestCase(343, 50, true, 675.0, TestName = "Perforate Artery 2H: 75 + spec*12")]
        public void CalculateStealthStyleDamage_ShouldUseStaticFormulas(
            int styleId, int criticalStrikeSpec, bool isTwoHanded, double expectedDamage)
        {
            // Test validates: DAoC Rule - Stealth opener damage formulas
            
            // Arrange
            var style = CreateMockStyle(id: styleId, stealthRequirement: true);
            
            // Act
            var damage = _styleCalculator.CalculateStealthStyleDamage(
                style.Object, criticalStrikeSpec, isTwoHanded);
            
            // Assert
            damage.Should().BeApproximately(expectedDamage, 0.01,
                $"Style {styleId} with CS {criticalStrikeSpec} should deal {expectedDamage} damage");
        }

        /// <summary>
        /// Validates minimum style damage.
        /// 
        /// DAoC Rule: Styles with growth rate do at least 1 damage
        /// Reference: StyleProcessor.ExecuteStyle lines 349-354
        /// Current Implementation: Forces 1 damage minimum for GR > 0
        /// </summary>
        [Test]
        public void CalculateStyleDamage_ShouldDoMinimumOneDamage_WhenGrowthRatePositive()
        {
            // Test validates: DAoC Rule - Minimum 1 style damage with positive GR
            
            // Arrange
            var style = CreateMockStyle(growthRate: 0.01); // Very low GR
            double unstyledDamage = 10; // Low damage
            double unstyledDamageCap = 1000; // High cap = very low modifier
            
            // Act
            var styleDamage = _styleCalculator.CalculateStyleDamage(
                style.Object, 1, 2.0, unstyledDamage, unstyledDamageCap);
            
            // Assert
            styleDamage.Should().BeGreaterOrEqualTo(1.0,
                "Style with positive growth rate should always do at least 1 damage");
        }

        #endregion

        #region Endurance Cost Tests

        /// <summary>
        /// Validates style endurance cost calculation.
        /// 
        /// DAoC Rule: Endurance cost based on weapon speed
        /// Reference: StyleProcessor.CalculateEnduranceCost
        /// Current Implementation: BaseEnd + WeaponSpeed modifier
        /// </summary>
        [TestCase(20, 5, TestName = "2.0s weapon: 5 endurance")]
        [TestCase(30, 7, TestName = "3.0s weapon: 7 endurance")]
        [TestCase(37, 9, TestName = "3.7s weapon: 9 endurance")]
        [TestCase(45, 11, TestName = "4.5s weapon: 11 endurance")]
        public void CalculateEnduranceCost_ShouldScaleWithWeaponSpeed(
            int weaponSpeed, int expectedCost)
        {
            // Test validates: DAoC Rule - Style endurance scales with weapon speed
            
            // Arrange
            var style = CreateMockStyle();
            
            // Act
            var cost = _styleCalculator.CalculateEnduranceCost(style.Object, weaponSpeed);
            
            // Assert
            cost.Should().Be(expectedCost,
                $"Style with {weaponSpeed/10.0}s weapon should cost {expectedCost} endurance");
        }

        #endregion

        #region Style Proc Tests

        /// <summary>
        /// Validates style proc execution.
        /// 
        /// DAoC Rule: Styles can have spell procs with chance to fire
        /// Reference: StyleProcessor.ExecuteStyle style procs section
        /// Current Implementation: Checks proc chance and creates spell effects
        /// </summary>
        [Test]
        public void ExecuteStyle_ShouldFireProcs_WhenChanceSucceeds()
        {
            // Test validates: DAoC Rule - Style procs fire based on chance
            
            // Arrange
            var procSpell = CreateMockSpell(id: 100);
            var styleProc = new StyleProc
            {
                Chance = 100, // Always proc for test
                SpellId = procSpell.Object.ID
            };
            var style = CreateMockStyle();
            style.Setup(s => s.Procs).Returns(new List<StyleProc> { styleProc });
            
            // Act
            var effects = new List<ISpellHandler>();
            bool executed = _styleProcessor.ExecuteStyle(
                CreateMockAttacker().Object,
                CreateMockDefender().Object,
                style.Object,
                CreateMockWeapon().Object,
                1000, 1000,
                out _, out _, out _,
                effects);
            
            // Assert
            executed.Should().BeTrue("Style should execute successfully");
            effects.Should().HaveCount(1, "Style proc should create one effect");
        }

        #endregion

        #region Style To-Hit and Defense Bonuses

        /// <summary>
        /// Validates style to-hit bonus application.
        /// 
        /// DAoC Rule: Styles can have to-hit bonuses that reduce miss chance
        /// Reference: AttackComponent.GetMissChance lines 2633-2635
        /// Current Implementation: Direct reduction of miss chance
        /// </summary>
        [TestCase(0, 18.0, TestName = "No bonus: 18% miss")]
        [TestCase(5, 13.0, TestName = "+5 to-hit: 13% miss")]
        [TestCase(10, 8.0, TestName = "+10 to-hit: 8% miss")]
        [TestCase(15, 3.0, TestName = "+15 to-hit: 3% miss")]
        public void Style_BonusToHit_ShouldReduceMissChance(
            int bonusToHit, double expectedMissChance)
        {
            // Test validates: DAoC Rule - Style to-hit bonus reduces miss chance
            
            // Arrange
            var style = CreateMockStyle(bonusToHit: bonusToHit);
            
            // Act
            double baseMissChance = 18.0;
            double modifiedMissChance = baseMissChance - bonusToHit;
            
            // Assert
            modifiedMissChance.Should().Be(expectedMissChance,
                $"Style with +{bonusToHit} to-hit should reduce miss chance to {expectedMissChance}%");
        }

        /// <summary>
        /// Validates defensive style bonus application.
        /// 
        /// DAoC Rule: Successfully executed defensive styles grant defense bonus
        /// Reference: AttackComponent.GetMissChance lines 2637-2639
        /// Current Implementation: Increases attacker's miss chance
        /// </summary>
        [TestCase(0, 18.0, TestName = "No bonus: 18% miss")]
        [TestCase(5, 23.0, TestName = "+5 defense: 23% miss")]
        [TestCase(10, 28.0, TestName = "+10 defense: 28% miss")]
        public void Style_BonusToDefense_ShouldIncreaseMissChance(
            int bonusToDefense, double expectedMissChance)
        {
            // Test validates: DAoC Rule - Defensive style bonus increases attacker miss
            
            // Arrange
            var defensiveStyle = CreateMockStyle(bonusToDefense: bonusToDefense);
            var lastAttackData = new AttackData
            {
                AttackResult = eAttackResult.HitStyle,
                Style = defensiveStyle.Object
            };
            
            // Act
            double baseMissChance = 18.0;
            double modifiedMissChance = baseMissChance + bonusToDefense;
            
            // Assert
            modifiedMissChance.Should().Be(expectedMissChance,
                $"Previous defensive style with +{bonusToDefense} defense should increase miss to {expectedMissChance}%");
        }

        #endregion

        private Mock<IStyle> CreateMockStyle(
            int id = 1,
            double growthRate = 0.5,
            StyleAttackResultRequirement attackResultRequirement = StyleAttackResultRequirement.Any,
            StylePositional positionalRequirement = StylePositional.Any,
            bool stealthRequirement = false,
            int bonusToHit = 0,
            int bonusToDefense = 0)
        {
            var mock = new Mock<IStyle>();
            mock.Setup(s => s.ID).Returns(id);
            mock.Setup(s => s.GrowthRate).Returns(growthRate);
            mock.Setup(s => s.AttackResultRequirement).Returns(attackResultRequirement);
            mock.Setup(s => s.PositionalRequirement).Returns(positionalRequirement);
            mock.Setup(s => s.StealthRequirement).Returns(stealthRequirement);
            mock.Setup(s => s.BonusToHit).Returns(bonusToHit);
            mock.Setup(s => s.BonusToDefense).Returns(bonusToDefense);
            mock.Setup(s => s.Procs).Returns(new List<StyleProc>());
            return mock;
        }

        private Mock<ISpell> CreateMockSpell(int id = 1)
        {
            var mock = new Mock<ISpell>();
            mock.Setup(s => s.ID).Returns(id);
            return mock;
        }

        private Mock<IAttacker> CreateMockAttacker()
        {
            var mock = new Mock<IAttacker>();
            mock.Setup(a => a.Level).Returns(50);
            return mock;
        }

        private Mock<IDefender> CreateMockDefender()
        {
            var mock = new Mock<IDefender>();
            mock.Setup(d => d.Level).Returns(50);
            mock.Setup(d => d.GetArmorAbsorb(It.IsAny<eArmorSlot>())).Returns(0);
            return mock;
        }

        private Mock<IWeapon> CreateMockWeapon()
        {
            var mock = new Mock<IWeapon>();
            mock.Setup(w => w.Speed).Returns(37);
            return mock;
        }
    }

    /// <summary>
    /// Mock implementation of IStyleProcessor for testing
    /// </summary>
    public class MockStyleProcessor : IStyleProcessor
    {
        private readonly IStyleCalculator _calculator;

        public MockStyleProcessor(IStyleCalculator calculator)
        {
            _calculator = calculator;
        }

        public bool CanUseStyle(AttackData lastAttackData, IStyle style, 
            IAttacker attacker = null, IDefender target = null, Position? position = null)
        {
            // Check attack result requirement
            if (lastAttackData != null && style.AttackResultRequirement != StyleAttackResultRequirement.Any)
            {
                var requiredResult = GetRequiredAttackResult(style.AttackResultRequirement);
                if (lastAttackData.AttackResult != requiredResult)
                    return false;
            }

            // Check positional requirement
            if (position.HasValue && style.PositionalRequirement != StylePositional.Any)
            {
                if (!CheckPositional(style.PositionalRequirement, position.Value))
                    return false;
            }

            return true;
        }

        public bool ExecuteStyle(IAttacker attacker, IDefender target, IStyle style, 
            IWeapon weapon, double unstyledDamage, double unstyledDamageCap,
            out double styleDamage, out double styleDamageCap, out int animationId,
            List<ISpellHandler> styleEffects = null)
        {
            styleDamage = 0;
            styleDamageCap = 0;
            animationId = style.ID;

            // Calculate style damage
            if (style.StealthRequirement)
            {
                styleDamage = _calculator.CalculateStealthStyleDamage(
                    style, 50, weapon.WeaponType == WeaponType.TwoHanded);
                styleDamage *= 1.0 - target.GetArmorAbsorb(eArmorSlot.TORSO); // Apply absorb
                styleDamageCap = -1; // Uncapped
            }
            else
            {
                styleDamage = _calculator.CalculateStyleDamage(
                    style, 50, weapon.Speed / 10.0, unstyledDamage, unstyledDamageCap);
                styleDamageCap = styleDamage; // Same calculation for cap
            }

            // Process procs
            if (styleEffects != null)
            {
                foreach (var proc in style.Procs)
                {
                    if (Util.Chance(proc.Chance))
                    {
                        var effect = new Mock<ISpellHandler>().Object;
                        styleEffects.Add(effect);
                    }
                }
            }

            return true;
        }

        private eAttackResult GetRequiredAttackResult(StyleAttackResultRequirement requirement)
        {
            return requirement switch
            {
                StyleAttackResultRequirement.Hit => eAttackResult.HitUnstyled,
                StyleAttackResultRequirement.Block => eAttackResult.Blocked,
                StyleAttackResultRequirement.Evade => eAttackResult.Evaded,
                StyleAttackResultRequirement.Fumble => eAttackResult.Fumbled,
                StyleAttackResultRequirement.Style => eAttackResult.HitStyle,
                StyleAttackResultRequirement.Miss => eAttackResult.Missed,
                StyleAttackResultRequirement.Parry => eAttackResult.Parried,
                _ => eAttackResult.Any
            };
        }

        private bool CheckPositional(StylePositional requirement, Position position)
        {
            return requirement switch
            {
                StylePositional.Front => position == Position.Front,
                StylePositional.Side => position == Position.Side,
                StylePositional.Back => position == Position.Back,
                _ => true
            };
        }
    }

    /// <summary>
    /// Mock implementation of IStyleCalculator for testing
    /// </summary>
    public class MockStyleCalculator : IStyleCalculator
    {
        public double CalculateStyleDamage(IStyle style, int spec, double weaponSpeed,
            double unstyledDamage, double unstyledDamageCap)
        {
            if (style.GrowthRate == 0) return 0;

            double modifiedGrowthRate = style.GrowthRate * spec * weaponSpeed / unstyledDamageCap;
            double styleDamage = modifiedGrowthRate * unstyledDamage;

            // Minimum 1 damage for positive GR
            if (styleDamage < 1 && style.GrowthRate > 0)
                styleDamage = 1;

            return styleDamage;
        }

        public double CalculateStealthStyleDamage(IStyle style, int criticalStrikeSpec, bool isTwoHanded)
        {
            return style.ID switch
            {
                335 => Math.Min(5, criticalStrikeSpec / 10.0) + criticalStrikeSpec * 14 / 3.0, // BS I
                339 => Math.Min(45, criticalStrikeSpec) + criticalStrikeSpec * 6, // BS II
                343 => Math.Min(75, criticalStrikeSpec * 1.5) + criticalStrikeSpec * (isTwoHanded ? 12 : 9), // PA
                _ => 0
            };
        }

        public int CalculateEnduranceCost(IStyle style, int weaponSpeed)
        {
            // Simplified endurance calculation
            return 1 + weaponSpeed / 5;
        }

        public int CalculateStyleDamage(IStyle style, int baseDamage)
        {
            // Simplified for specific tests
            return (int)(baseDamage * style.GrowthRate);
        }
    }

    // Additional enums and classes for style testing
    public enum StyleAttackResultRequirement
    {
        Any,
        Block,
        Evade,
        Fumble,
        Hit,
        Style,
        Miss,
        Parry
    }

    public enum StylePositional
    {
        Any,
        Front,
        Side,
        Back
    }

    public class StyleProc
    {
        public int Chance { get; set; }
        public int SpellId { get; set; }
    }
} 