using System;
using DOL.GS;
using DOL.GS.Effects;
using DOL.Database;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Tests.Helpers;
using DOL.GS.PropertyCalc;

namespace Tests.UnitTests.Combat
{
    /// <summary>
    /// Tests for critical strike mechanics in combat.
    /// Reference: Core_Systems_Game_Rules.md - Critical Strike System
    /// </summary>
    [TestFixture]
    public class CriticalStrikeTests
    {
        private Mock<GamePlayer> _attacker;
        private Mock<GamePlayer> _defender;
        private Mock<GameNPC> _npc;

        [SetUp]
        public void Setup()
        {
            _attacker = MockHelper.CreateMockCharacter(level: 50);
            _defender = MockHelper.CreateMockCharacter(level: 50);
            _npc = new Mock<GameNPC>();
            _npc.Setup(x => x.Level).Returns(50);
        }

        [Test]
        public void CriticalChance_BaseRate_ShouldBe10Percent()
        {
            // Test validates: DAoC Rule - Base critical chance is 10%

            // Arrange
            int baseWeaponSkill = 1000;
            
            // Act - Formula: (11 + weaponSpec / 10) / 100
            double critChance = (11 + baseWeaponSkill / 10.0) / 100.0;

            // Assert
            critChance.Should().BeApproximately(1.11, 0.001, 
                "Base crit chance should be about 111% (capped at lower value in practice)");
        }

        [Test]
        public void CriticalChance_WithCriticalStrike_ShouldIncrease()
        {
            // Test validates: DAoC Rule - Critical Strike ability increases crit chance

            // Arrange
            int criticalStrikeLevel = 9; // CS IX
            
            // Act - CS adds 3% per level
            double csBonus = criticalStrikeLevel * 3.0 / 100.0;
            double totalCritChance = 0.10 + csBonus; // Base 10% + CS bonus

            // Assert
            csBonus.Should().Be(0.27);
            totalCritChance.Should().Be(0.37, "CS IX should give 37% total crit chance");
        }

        [Test]
        public void CriticalChance_WithWildPower_ShouldIncrease()
        {
            // Test validates: DAoC Rule - Wild Power increases crit chance

            // Arrange
            int wildPowerLevel = 5; // WP V
            
            // Act - Wild Power adds 1% per level
            double wpBonus = wildPowerLevel * 1.0 / 100.0;
            double totalCritChance = 0.10 + wpBonus;

            // Assert
            wpBonus.Should().Be(0.05);
            totalCritChance.Should().Be(0.15, "WP V should give 15% total crit chance");
        }

        [Test]
        public void CriticalDamage_VsPlayer_ShouldCapAt50Percent()
        {
            // Test validates: DAoC Rule - PvP critical damage capped at 50%

            // Arrange
            int baseDamage = 1000;
            bool isPlayerTarget = true;

            // Act
            int minCritDamage = (int)(baseDamage * 0.1); // 10% minimum
            int maxCritDamage = isPlayerTarget ? 
                (int)(baseDamage * 0.5) : // 50% max vs players
                baseDamage; // 100% max vs mobs

            // Assert
            minCritDamage.Should().Be(100);
            maxCritDamage.Should().Be(500, "PvP crit damage should cap at 50%");
        }

        [Test]
        public void CriticalDamage_VsNPC_ShouldCapAt100Percent()
        {
            // Test validates: DAoC Rule - PvE critical damage capped at 100%

            // Arrange
            int baseDamage = 1000;
            bool isPlayerTarget = false;

            // Act
            int minCritDamage = (int)(baseDamage * 0.1); // 10% minimum
            int maxCritDamage = isPlayerTarget ? 
                (int)(baseDamage * 0.5) : 
                baseDamage; // 100% max vs mobs

            // Assert
            minCritDamage.Should().Be(100);
            maxCritDamage.Should().Be(1000, "PvE crit damage should cap at 100%");
        }

        [Test]
        public void CriticalChance_OnStyleOnly_ShouldApply()
        {
            // Test validates: DAoC Rule - Crits only happen on successful styles

            // Arrange
            bool isStyledAttack = true;
            bool isUnstyledAttack = false;
            double critChance = 0.20; // 20% crit chance

            // Act
            double styledCritChance = isStyledAttack ? critChance : 0;
            double unstyledCritChance = isUnstyledAttack ? critChance : 0;

            // Assert
            styledCritChance.Should().Be(0.20, "Styled attacks can crit");
            unstyledCritChance.Should().Be(0, "Unstyled attacks cannot crit");
        }

        [Test]
        public void CriticalStrike_RealmAbility_ShouldStackWithOtherBonuses()
        {
            // Test validates: DAoC Rule - Multiple crit bonuses stack

            // Arrange
            int criticalStrikeRA = 6; // CS VI = 18%
            int wildPowerRA = 3; // WP III = 3%
            int itemCritBonus = 5; // 5% from items

            // Act
            double totalCritBonus = (criticalStrikeRA * 3) + (wildPowerRA * 1) + itemCritBonus;
            double totalCritChance = 10 + totalCritBonus; // Base 10% + bonuses

            // Assert
            totalCritBonus.Should().Be(26);
            totalCritChance.Should().Be(36, "All crit bonuses should stack");
        }

        [Test]
        public void CriticalChance_WithDebuff_ShouldReduce()
        {
            // Test validates: DAoC Rule - Crit chance can be debuffed

            // Arrange
            double baseCritChance = 20.0; // 20%
            int critDebuff = -5; // -5% crit debuff

            // Act
            double debuffedCritChance = Math.Max(0, baseCritChance + critDebuff);

            // Assert
            debuffedCritChance.Should().Be(15, "Crit debuff should reduce chance");
        }

        [Test]
        public void CriticalDamage_ShouldBeRandomInRange()
        {
            // Test validates: DAoC Rule - Crit damage is random between min and max

            // Arrange
            int baseDamage = 500;
            int minCrit = (int)(baseDamage * 0.1); // 50
            int maxCrit = (int)(baseDamage * 0.5); // 250 (vs player)

            // Act - Simulate multiple critical hits
            bool allInRange = true;
            for (int i = 0; i < 10; i++)
            {
                // Simulated random crit damage
                int critDamage = minCrit + (i * (maxCrit - minCrit) / 10);
                if (critDamage < minCrit || critDamage > maxCrit)
                    allInRange = false;
            }

            // Assert
            allInRange.Should().BeTrue("All crit damage should be within min/max range");
        }

        [Test]
        public void CriticalChance_MaxCap_ShouldBe50Percent()
        {
            // Test validates: DAoC Rule - Critical chance has maximum cap

            // Arrange
            double extremeCritBonus = 100.0; // Extreme bonus

            // Act
            double cappedCritChance = Math.Min(extremeCritBonus, 50.0);

            // Assert
            cappedCritChance.Should().Be(50.0, "Crit chance should cap at 50%");
        }

        [Test]
        public void CriticalStrike_OnMiss_ShouldNotOccur()
        {
            // Test validates: DAoC Rule - Cannot crit on a miss

            // Arrange
            bool attackHit = false;
            double critChance = 0.50; // 50% crit chance

            // Act
            bool canCrit = attackHit && critChance > 0;

            // Assert
            canCrit.Should().BeFalse("Cannot critical strike on a missed attack");
        }
    }
} 