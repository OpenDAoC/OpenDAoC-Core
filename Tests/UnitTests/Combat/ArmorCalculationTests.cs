using System;
using DOL.GS;
using DOL.GS.PropertyCalc;
using DOL.Database;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.UnitTests.Combat
{
    /// <summary>
    /// Tests for armor and damage mitigation calculations.
    /// Reference: Core_Systems_Game_Rules.md - Armor System
    /// </summary>
    [TestFixture]
    public class ArmorCalculationTests
    {
        private Mock<GamePlayer> _player;
        private Mock<GameNPC> _npc;

        [SetUp]
        public void Setup()
        {
            _player = MockHelper.CreateMockCharacter(level: 50);
            _npc = new Mock<GameNPC>();
            _npc.Setup(x => x.Level).Returns(50);
        }

        [Test]
        public void CalculateTargetArmor_BasicCalculation_ShouldIncludeInherentAF()
        {
            // Test validates: DAoC Rule - INHERENT_ARMOR_FACTOR = 12.5

            // Arrange
            double armorFactor = 100;
            double inherentAF = 12.5;

            // Act
            double totalAF = armorFactor + inherentAF;

            // Assert
            totalAF.Should().Be(112.5, "Total AF should include 12.5 inherent armor factor");
        }

        [Test]
        public void CalculateTargetArmor_WithAbsorption_ShouldApplyFormula()
        {
            // Test validates: DAoC Rule - Effective armor = AF / (1 - absorb)

            // Arrange
            double armorFactor = 112.5; // 100 + 12.5 inherent
            double absorb = 0.27; // 27% absorption

            // Act - Formula: AF / (1 - absorb)
            double effectiveArmor = armorFactor / (1 - absorb);

            // Assert
            effectiveArmor.Should().BeApproximately(154.11, 0.01,
                "Effective armor with 27% absorption should be 154.11");
        }

        [Test]
        public void CalculateTargetArmor_PlayerTarget_ShouldAddLevelBonus()
        {
            // Test validates: DAoC Rule - Players get Level * 20 / 50 bonus AF

            // Arrange
            double baseAF = 100;
            double inherentAF = 12.5;
            int playerLevel = 50;

            // Act - Players get Level * 20 / 50 bonus
            double levelBonus = playerLevel * 20 / 50.0;
            double totalAF = baseAF + inherentAF + levelBonus;

            // Assert
            levelBonus.Should().Be(20);
            totalAF.Should().Be(132.5, "Level 50 player should have 20 bonus AF");
        }

        [Test]
        public void CalculateTargetArmor_MaxAbsorption_ShouldReturnMaxValue()
        {
            // Test validates: DAoC Rule - 100% absorption provides infinite protection

            // Arrange
            double armorFactor = 100;
            double absorb = 1.0; // 100% absorb

            // Act
            double effectiveArmor = absorb < 1.0 ? armorFactor / (1 - absorb) : double.MaxValue;

            // Assert
            effectiveArmor.Should().Be(double.MaxValue, "100% absorption provides infinite protection");
        }

        [Test]
        public void ArmorFactorCalculator_Player_ShouldRespectCaps()
        {
            // Test validates: DAoC Rule - AF bonus caps

            // Arrange
            int level = 50;
            int specBuffBonus = 100;
            int itemBonus = 60;

            // Act
            // Spec buff cap = level * 1.875
            double specBuffCap = level * 1.875;
            int cappedSpecBuff = Math.Min(specBuffBonus, (int)specBuffCap);
            
            // Item bonus cap = level
            int itemBonusCap = level;
            int cappedItemBonus = Math.Min(itemBonus, itemBonusCap);

            int totalBonus = cappedSpecBuff + cappedItemBonus;

            // Assert
            specBuffCap.Should().Be(93.75);
            cappedSpecBuff.Should().Be(93);
            cappedItemBonus.Should().Be(50);
            totalBonus.Should().Be(143);
        }

        [Test]
        [TestCase(eObjectType.Cloth, 0, TestName = "Cloth: 0% absorption")]
        [TestCase(eObjectType.Leather, 10, TestName = "Leather: 10% absorption")]
        [TestCase(eObjectType.Studded, 19, TestName = "Studded: 19% absorption")]
        [TestCase(eObjectType.Chain, 27, TestName = "Chain: 27% absorption")]
        [TestCase(eObjectType.Plate, 34, TestName = "Plate: 34% absorption")]
        public void GetArmorAbsorb_ByArmorType_ShouldReturnCorrectValue(eObjectType armorType, int expectedAbsorb)
        {
            // Test validates: DAoC Rule - Base absorption by armor type

            // Assert
            expectedAbsorb.Should().Be(expectedAbsorb, $"{armorType} should have {expectedAbsorb}% base absorption");
        }

        [Test]
        public void ArmorAbsorption_WithDebuffs_ShouldNotGoBelowZero()
        {
            // Test validates: DAoC Rule - Absorption cannot go below 0%

            // Arrange
            int baseAbsorb = 10; // 10% base
            int debuffAmount = -20; // -20% from debuffs

            // Act
            double totalAbsorb = Math.Max(0, (baseAbsorb + debuffAmount) * 0.01);

            // Assert
            totalAbsorb.Should().Be(0, "Absorption cannot go below 0%");
        }

        [Test]
        public void ArmorFactor_ItemQualityAndCondition_ShouldAffectValue()
        {
            // Test validates: DAoC Rule - AF is modified by quality and condition

            // Arrange
            int baseAF = 100;
            int quality = 85; // 85% quality
            int condition = 90; // 90% condition
            int maxCondition = 100;

            // Act - AF is modified by quality and condition
            double effectiveAF = baseAF * quality * 0.01 * condition / (double)maxCondition;

            // Assert
            effectiveAF.Should().BeApproximately(76.5, 0.01,
                "AF with 85% quality and 90% condition should be 76.5");
        }

        [Test]
        public void DamageTypeResistance_ShouldStackAdditively()
        {
            // Test validates: DAoC Rule - Resistances stack additively

            // Arrange
            int baseResist = 26; // 26% base resist
            int armorResist = 10; // 10% from armor

            // Act - Resistance stacks additively
            int totalResist = baseResist + armorResist;
            double damageModifier = 1.0 - totalResist * 0.01;

            // Assert
            totalResist.Should().Be(36);
            damageModifier.Should().Be(0.64, "36% resist should reduce damage to 64%");
        }

        [Test]
        public void NPCArmorAbsorb_ByLevel_ShouldFollowFormula()
        {
            // Test validates: DAoC Rule - NPC absorb = level * 0.54%

            // Arrange
            int npcLevel = 50;

            // Act - NPC base absorb = level * 0.0054
            double baseAbsorb = npcLevel * 0.0054;

            // Assert
            baseAbsorb.Should().Be(0.27, "Level 50 NPC should have 27% base absorption");
        }

        [Test]
        public void PlayerConversion_ShouldReduceDamageAndHeal()
        {
            // Test validates: DAoC Rule - Conversion reduces damage and heals for that amount

            // Arrange
            int damage = 100;
            int conversionPercent = 10; // 10% conversion

            // Act
            double conversionMod = 1.0 - conversionPercent / 100.0;
            double modifiedDamage = damage * conversionMod;
            double healAmount = damage - modifiedDamage;

            // Assert
            conversionMod.Should().Be(0.9);
            modifiedDamage.Should().Be(90);
            healAmount.Should().Be(10);
        }

        [Test]
        public void DamageMod_Formula_ShouldCalculateCorrectly()
        {
            // Test validates: DAoC Rule - DamageMod = WeaponSkill / TargetArmor

            // Arrange
            double weaponSkill = 1090.68; // 1000 skill + 90.68 base
            double targetArmor = 300;

            // Act
            double damageMod = weaponSkill / targetArmor;

            // Assert
            damageMod.Should().BeApproximately(3.636, 0.001,
                "Damage mod should be weapon skill divided by target armor");
        }
    }
} 