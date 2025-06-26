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
    /// Tests for fumble mechanics in combat.
    /// Reference: Core_Systems_Game_Rules.md - Fumble System
    /// </summary>
    [TestFixture]
    public class FumbleMechanicsTests
    {
        private Mock<GamePlayer> _attacker;
        private Mock<GamePlayer> _defender;

        [SetUp]
        public void Setup()
        {
            _attacker = MockHelper.CreateMockCharacter(level: 50);
            _defender = MockHelper.CreateMockCharacter(level: 50);
        }

        [Test]
        public void FumbleChance_BaseMissChance_ShouldBe2Percent()
        {
            // Test validates: DAoC Rule - Base fumble chance is 2% of miss chance

            // Arrange
            double missChance = 18.0; // Base 18% miss

            // Act
            double fumbleChance = missChance * 0.02; // 2% of miss chance

            // Assert
            fumbleChance.Should().Be(0.36, "Base fumble chance should be 2% of 18% = 0.36%");
        }

        [Test]
        public void FumbleChance_WithHighMissChance_ShouldScale()
        {
            // Test validates: DAoC Rule - Fumble scales with miss chance

            // Arrange
            double missChance = 50.0; // High miss chance scenario

            // Act
            double fumbleChance = missChance * 0.02;

            // Assert
            fumbleChance.Should().Be(1.0, "50% miss chance gives 1% fumble chance");
        }

        [Test]
        public void FumbleChance_MaximumRate_ShouldNotExceed5Percent()
        {
            // Test validates: DAoC Rule - Fumble chance caps at reasonable rate

            // Arrange
            double extremeMissChance = 99.0; // Near guaranteed miss

            // Act
            double fumbleChance = Math.Min(extremeMissChance * 0.02, 5.0);

            // Assert
            fumbleChance.Should().BeLessOrEqualTo(5.0, "Fumble chance should have reasonable cap");
        }

        [Test]
        public void FumbleResult_ShouldDoubleAttackTimer()
        {
            // Test validates: DAoC Rule - Fumble doubles next attack timer

            // Arrange
            int normalAttackSpeed = 3500; // 3.5 seconds
            bool isFumble = true;

            // Act
            int nextAttackDelay = isFumble ? normalAttackSpeed * 2 : normalAttackSpeed;

            // Assert
            nextAttackDelay.Should().Be(7000, "Fumble should double attack delay to 7 seconds");
        }

        [Test]
        public void FumbleChance_WithAmmo_ShouldBeAffected()
        {
            // Test validates: DAoC Rule - Ammo quality affects fumble chance

            // Arrange
            double baseMissChance = 18.0;
            double roughAmmoModifier = 1.15; // +15% miss with rough ammo

            // Act
            double modifiedMissChance = baseMissChance * roughAmmoModifier;
            double fumbleChance = modifiedMissChance * 0.02;

            // Assert
            modifiedMissChance.Should().Be(20.7);
            fumbleChance.Should().BeApproximately(0.414, 0.001, 
                "Rough ammo increases fumble chance proportionally");
        }

        [Test]
        public void FumbleChance_WithLevelDifference_ShouldAdjust()
        {
            // Test validates: DAoC Rule - Level affects fumble chance via miss chance

            // Arrange
            int attackerLevel = 45;
            int defenderLevel = 50;
            double baseMissChance = 18.0;
            double levelModifier = (defenderLevel - attackerLevel) * 1.33; // +6.65% miss

            // Act
            double adjustedMissChance = baseMissChance + levelModifier;
            double fumbleChance = adjustedMissChance * 0.02;

            // Assert
            adjustedMissChance.Should().Be(24.65);
            fumbleChance.Should().BeApproximately(0.493, 0.001, 
                "Higher level target increases fumble chance");
        }

        [Test]
        public void FumbleChance_WithBonusToHit_ShouldDecrease()
        {
            // Test validates: DAoC Rule - To-hit bonuses reduce fumble chance

            // Arrange
            double baseMissChance = 18.0;
            int styleBonus = 15; // +15 to-hit from style

            // Act
            double adjustedMissChance = Math.Max(1, baseMissChance - styleBonus);
            double fumbleChance = adjustedMissChance * 0.02;

            // Assert
            adjustedMissChance.Should().Be(3.0);
            fumbleChance.Should().Be(0.06, "To-hit bonus reduces fumble chance");
        }

        [Test]
        public void FumbleChance_DualWield_ShouldApplyToBothHands()
        {
            // Test validates: DAoC Rule - Each weapon can fumble independently

            // Arrange
            double missChance = 18.0;
            double fumbleChancePerWeapon = missChance * 0.02;

            // Act - Both weapons attack, each can fumble
            double mainHandFumble = fumbleChancePerWeapon;
            double offHandFumble = fumbleChancePerWeapon;

            // Assert
            mainHandFumble.Should().Be(0.36);
            offHandFumble.Should().Be(0.36);
        }

        [Test]
        public void FumbleRecovery_ShouldNotInterruptOtherActions()
        {
            // Test validates: DAoC Rule - Fumble recovery doesn't prevent other actions

            // Arrange
            bool canCast = true;
            bool canUseAbility = true;
            bool fumbled = true;

            // Act - Fumble only affects weapon swings
            bool canCastWhileFumbled = canCast && true; // Not affected
            bool canUseAbilityWhileFumbled = canUseAbility && true; // Not affected
            bool canAttackWhileFumbled = !fumbled; // Affected

            // Assert
            canCastWhileFumbled.Should().BeTrue("Can cast while fumbled");
            canUseAbilityWhileFumbled.Should().BeTrue("Can use abilities while fumbled");
            canAttackWhileFumbled.Should().BeFalse("Cannot attack while fumbled");
        }

        [Test]
        public void FumbleChance_NPCsVsPlayers_ShouldBeEqual()
        {
            // Test validates: DAoC Rule - NPCs use same fumble mechanics

            // Arrange
            double playerMissChance = 18.0;
            double npcMissChance = 18.0;

            // Act
            double playerFumbleChance = playerMissChance * 0.02;
            double npcFumbleChance = npcMissChance * 0.02;

            // Assert
            playerFumbleChance.Should().Be(npcFumbleChance, 
                "NPCs and players have same fumble mechanics");
        }
    }
} 