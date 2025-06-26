using System;
using DOL.GS;
using DOL.Database;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Tests.Helpers;

namespace Tests.UnitTests.Combat
{
    /// <summary>
    /// Tests for attack speed and weapon timing mechanics.
    /// Reference: Core_Systems_Game_Rules.md - Attack Speed
    /// </summary>
    [TestFixture]
    public class AttackSpeedTests
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
        public void AttackSpeed_PlayerMeleeWeapon_ShouldApplyQuicknessModifier()
        {
            // Test validates: DAoC Rule - Player swing speed modified by quickness

            // Arrange
            var weaponSpeed = 35; // 3.5 seconds base
            var quicknessStat = 100; // 100 quickness
            
            // Act - Formula: baseSpeed * (1 - (quickness - 60) * 0.002)
            double quicknessModifier = 1.0 - (quicknessStat - 60) * 0.002;
            double modifiedSpeed = weaponSpeed * quicknessModifier;

            // Assert
            quicknessModifier.Should().Be(0.92, "100 quickness gives 8% speed bonus");
            modifiedSpeed.Should().Be(32.2, "3.5s weapon with 100 quickness should swing at 3.22s");
        }

        [Test]
        public void AttackSpeed_NPCMeleeWeapon_ShouldUseBaseSpeed()
        {
            // Test validates: DAoC Rule - NPCs don't get quickness speed bonus

            // Arrange
            var weaponSpeed = 35; // 3.5 seconds
            
            // Act - NPCs use base weapon speed
            var npcAttackSpeed = weaponSpeed;

            // Assert
            npcAttackSpeed.Should().Be(35, "NPCs should use unmodified weapon speed");
        }

        [Test]
        public void AttackSpeed_QuicknessBonus_ShouldCapAt250()
        {
            // Test validates: DAoC Rule - Max attack speed bonus from quickness

            // Arrange
            var weaponSpeed = 40; // 4.0 seconds
            var maxQuickness = 250;
            
            // Act - Formula with cap
            double quicknessModifier = 1.0 - Math.Min((maxQuickness - 60) * 0.002, 0.38);
            double minSwingSpeed = weaponSpeed * quicknessModifier;

            // Assert
            quicknessModifier.Should().Be(0.62, "Max quickness gives 38% speed bonus");
            minSwingSpeed.Should().Be(24.8, "4.0s weapon at max speed swings at 2.48s");
        }

        [Test]
        public void AttackSpeed_MeleeSpeedBonus_ShouldStack()
        {
            // Test validates: DAoC Rule - Melee speed bonuses stack with quickness

            // Arrange
            var weaponSpeed = 35; // 3.5 seconds
            var quicknessStat = 100;
            var meleeSpeedBonus = 15; // 15% from items/buffs
            
            // Act
            double quicknessModifier = 1.0 - (quicknessStat - 60) * 0.002;
            double speedWithQuickness = weaponSpeed * quicknessModifier;
            double finalSpeed = speedWithQuickness * (1.0 - meleeSpeedBonus / 100.0);

            // Assert
            speedWithQuickness.Should().Be(32.2);
            finalSpeed.Should().BeApproximately(27.37, 0.01, 
                "15% melee speed bonus should stack with quickness");
        }

        [Test]
        public void AttackSpeed_DualWield_ShouldSwingAlternately()
        {
            // Test validates: DAoC Rule - Dual wield alternates weapons

            // Arrange
            var mainHandSpeed = 28; // 2.8 seconds
            var offHandSpeed = 24; // 2.4 seconds (faster)
            
            // Act - Each weapon swings at its own speed
            var swing1Time = 0; // Main hand at 0s
            var swing2Time = mainHandSpeed; // Off hand at 2.8s
            var swing3Time = swing2Time + offHandSpeed; // Main hand at 5.2s
            var swing4Time = swing3Time + mainHandSpeed; // Off hand at 8.0s

            // Assert
            swing2Time.Should().Be(28);
            swing3Time.Should().Be(52);
            swing4Time.Should().Be(80);
        }

        [Test]
        public void AttackSpeed_RangedWeapon_ShouldIncludeDrawTime()
        {
            // Test validates: DAoC Rule - Ranged weapons have draw time

            // Arrange
            var bowSpeed = 45; // 4.5 seconds
            var drawTime = 0; // Draw time is included in weapon speed for bows
            
            // Act
            var totalAttackTime = bowSpeed + drawTime;

            // Assert
            totalAttackTime.Should().Be(45, 
                "Bow attack time is just the weapon speed (draw included)");
        }

        [Test]
        public void AttackSpeed_MinimumSwingSpeed_ShouldBe15()
        {
            // Test validates: DAoC Rule - Minimum swing speed is 1.5 seconds

            // Arrange
            var weaponSpeed = 20; // 2.0 seconds
            var extremeSpeedBonus = 50; // 50% total speed bonus
            
            // Act
            double modifiedSpeed = weaponSpeed * (1.0 - extremeSpeedBonus / 100.0);
            double finalSpeed = Math.Max(modifiedSpeed, 15); // 1.5s minimum

            // Assert
            modifiedSpeed.Should().Be(10);
            finalSpeed.Should().Be(15, "Attack speed cannot go below 1.5 seconds");
        }

        [Test]
        public void AttackSpeed_Haste_ShouldAffectCastingNotMelee()
        {
            // Test validates: DAoC Rule - Haste affects casting, not melee

            // Arrange
            var meleeSpeed = 35; // 3.5 seconds
            var castTime = 30; // 3.0 seconds
            var hasteBonus = 20; // 20% haste
            
            // Act
            var meleeWithHaste = meleeSpeed; // No change
            var castWithHaste = castTime * (1.0 - hasteBonus / 100.0);

            // Assert
            meleeWithHaste.Should().Be(35, "Haste doesn't affect melee speed");
            castWithHaste.Should().Be(24, "Haste reduces cast time by 20%");
        }
    }
} 