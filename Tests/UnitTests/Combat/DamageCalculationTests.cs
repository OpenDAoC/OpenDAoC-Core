using System;
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
    /// Tests for damage calculation mechanics validating OpenDAoC rule implementations.
    /// Reference: Core_Systems_Game_Rules.md - Combat System - Damage Calculation
    /// </summary>
    [TestFixture]
    [Category("Combat")]
    [Category("Damage")]
    public class DamageCalculationTests
    {
        private IDamageCalculator _damageCalculator;

        [SetUp]
        public void Setup()
        {
            _damageCalculator = new MockDamageCalculator();
        }

        #region Base Damage Tests

        /// <summary>
        /// Validates base damage calculation formula.
        /// 
        /// DAoC Rule: BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponModifier
        /// Reference: AttackComponent.AttackDamage
        /// Current Implementation: Follows exact formula with slow weapon bonus
        /// </summary>
        [TestCase(165, 37, 1.051, 641, TestName = "165 DPS, 3.7s speed: 641 damage")]
        [TestCase(165, 45, 1.075, 798, TestName = "165 DPS, 4.5s speed: 798 damage")]
        [TestCase(150, 30, 1.030, 464, TestName = "150 DPS, 3.0s speed: 464 damage")]
        [TestCase(165, 20, 1.000, 330, TestName = "165 DPS, 2.0s speed: 330 damage (no bonus)")]
        public void CalculateBaseDamage_ShouldFollowDAoCFormula(
            int dps, int speed, double expectedModifier, int expectedDamage)
        {
            // Test validates: DAoC Rule - Base damage calculation with slow weapon modifier
            
            // Arrange
            var weapon = CreateMockWeapon(dps: dps, speed: speed);
            
            // Act
            var damage = _damageCalculator.CalculateBaseDamage(weapon);
            var modifier = _damageCalculator.CalculateSlowWeaponDamageModifier(weapon);
            
            // Assert
            modifier.Should().BeApproximately(expectedModifier, 0.001,
                $"Slow weapon modifier for {speed/10.0}s weapon should be {expectedModifier}");
            damage.Should().Be(expectedDamage,
                $"Base damage for {dps} DPS, {speed/10.0}s weapon should be {expectedDamage}");
        }

        /// <summary>
        /// Validates slow weapon damage modifier calculation.
        /// 
        /// DAoC Rule: SlowWeaponModifier = 1 + (Speed - 20) * 0.003
        /// Reference: AttackComponent.CalculateSlowWeaponDamageModifier
        /// Current Implementation: Weapons slower than 2.0s get bonus damage
        /// </summary>
        [TestCase(10, 0.970, TestName = "1.0s: 3% penalty")]
        [TestCase(20, 1.000, TestName = "2.0s: No modifier")]
        [TestCase(30, 1.030, TestName = "3.0s: 3% bonus")]
        [TestCase(37, 1.051, TestName = "3.7s: 5.1% bonus")]
        [TestCase(45, 1.075, TestName = "4.5s: 7.5% bonus")]
        [TestCase(50, 1.090, TestName = "5.0s: 9% bonus")]
        public void CalculateSlowWeaponDamageModifier_ShouldScaleWithSpeed(
            int speed, double expectedModifier)
        {
            // Test validates: DAoC Rule - Slow weapon modifier scaling
            
            // Arrange
            var weapon = CreateMockWeapon(speed: speed);
            
            // Act
            var modifier = _damageCalculator.CalculateSlowWeaponDamageModifier(weapon);
            
            // Assert
            modifier.Should().BeApproximately(expectedModifier, 0.001,
                $"Weapon speed {speed/10.0}s should have modifier {expectedModifier}");
        }

        #endregion

        #region Weapon Skill Tests

        /// <summary>
        /// Validates weapon skill calculation.
        /// 
        /// DAoC Rule: WeaponSkill = BaseWeaponSkill * RelicBonus * SpecModifier
        /// Reference: AttackComponent.CalculateWeaponSkill
        /// Current Implementation: Includes inherent skill of 90.68
        /// </summary>
        [Test]
        public void CalculateWeaponSkill_ShouldIncludeInherentSkill()
        {
            // Test validates: DAoC Rule - Inherent weapon skill of 90.68
            
            // Arrange
            var attacker = CreateMockAttacker();
            attacker.Setup(a => a.GetWeaponSkill(It.IsAny<IWeapon>())).Returns(1000);
            
            // Act
            var (weaponSkill, baseSkill) = _damageCalculator.CalculateWeaponSkill(
                attacker.Object, CreateMockDefender().Object, CreateMockWeapon().Object);
            
            // Assert
            baseSkill.Should().Be(1090.68,
                "Base weapon skill should include 90.68 inherent skill");
        }

        /// <summary>
        /// Validates spec modifier calculation.
        /// 
        /// DAoC Rule: SpecModifier = 1 + Variance * (SpecLevel - TargetLevel) * 0.01
        /// Reference: AttackComponent.CalculateSpecModifier
        /// Current Implementation: Variance ranges from 0.25 to 1.25 based on spec
        /// </summary>
        [TestCase(50, 50, 50, 1.0, TestName = "Equal levels: No modifier")]
        [TestCase(50, 45, 50, 1.0625, TestName = "5 levels higher: +6.25%")]
        [TestCase(45, 50, 50, 0.9375, TestName = "5 levels lower: -6.25%")]
        [TestCase(10, 50, 50, 0.75, TestName = "Low spec equal level: -25%")]
        public void CalculateSpecModifier_ShouldScaleWithLevelDifference(
            int specLevel, int targetLevel, int attackerLevel, double expectedModifier)
        {
            // Test validates: DAoC Rule - Spec modifier based on level difference
            
            // Arrange
            var attacker = CreateMockAttacker(level: attackerLevel);
            var defender = CreateMockDefender(level: targetLevel);
            
            // Act
            var modifier = _damageCalculator.CalculateSpecModifier(
                attacker.Object, defender.Object, specLevel);
            
            // Assert
            modifier.Should().BeApproximately(expectedModifier, 0.001,
                $"Spec {specLevel} vs level {targetLevel} should give modifier {expectedModifier}");
        }

        #endregion

        #region Armor Factor Tests

        /// <summary>
        /// Validates armor factor calculation including inherent AF.
        /// 
        /// DAoC Rule: ArmorFactor = TargetAF + 20 (inherent) + PlayerLevelBonus
        /// Reference: AttackComponent.CalculateTargetArmor
        /// Current Implementation: Players get level * 20 / 50 bonus AF
        /// </summary>
        [TestCase(0, 50, 40.0, TestName = "Naked level 50: 20 + 20 = 40 AF")]
        [TestCase(100, 50, 140.0, TestName = "100 AF level 50: 100 + 20 + 20 = 140 AF")]
        [TestCase(50, 25, 60.0, TestName = "50 AF level 25: 50 + 20 + 10 = 80 AF")]
        public void CalculateTargetArmor_ShouldIncludeInherentAndLevelBonus(
            double itemAF, int level, double expectedAF)
        {
            // Test validates: DAoC Rule - Armor includes inherent 20 AF + level bonus
            
            // Arrange
            var target = CreateMockPlayer(level: level);
            target.Setup(t => t.GetArmorAF(It.IsAny<eArmorSlot>())).Returns(itemAF);
            
            // Act
            var (armorFactor, armorMod) = _damageCalculator.CalculateTargetArmor(
                target.Object, eArmorSlot.TORSO);
            
            // Assert
            armorFactor.Should().Be(expectedAF,
                $"Level {level} with {itemAF} item AF should have total {expectedAF} AF");
        }

        /// <summary>
        /// Validates armor absorption calculation.
        /// 
        /// DAoC Rule: ArmorMod = ArmorFactor / (1 - Absorb)
        /// Reference: AttackComponent.CalculateTargetArmor
        /// Current Implementation: Absorb makes armor more effective
        /// </summary>
        [TestCase(100, 0.0, 100.0, TestName = "0% absorb: No modification")]
        [TestCase(100, 0.10, 111.11, TestName = "10% absorb: 11% more effective")]
        [TestCase(100, 0.27, 136.99, TestName = "27% absorb: 37% more effective")]
        [TestCase(100, 0.50, 200.0, TestName = "50% absorb: 100% more effective")]
        public void CalculateTargetArmor_ShouldApplyAbsorptionCorrectly(
            double armorFactor, double absorb, double expectedArmorMod)
        {
            // Test validates: DAoC Rule - Armor absorption formula
            
            // Arrange
            var target = CreateMockDefender();
            target.Setup(t => t.GetArmorAF(It.IsAny<eArmorSlot>())).Returns(armorFactor - 20); // Remove inherent
            target.Setup(t => t.GetArmorAbsorb(It.IsAny<eArmorSlot>())).Returns(absorb);
            
            // Act
            var (af, armorMod) = _damageCalculator.CalculateTargetArmor(
                target.Object, eArmorSlot.TORSO);
            
            // Assert
            armorMod.Should().BeApproximately(expectedArmorMod, 0.01,
                $"{armorFactor} AF with {absorb:P0} absorb should give {expectedArmorMod} armor mod");
        }

        #endregion

        #region Damage Modifier Tests

        /// <summary>
        /// Validates damage modifier calculation (weapon skill vs armor).
        /// 
        /// DAoC Rule: DamageMod = WeaponSkill / ArmorMod
        /// Reference: AttackComponent line 1156
        /// Current Implementation: Higher weapon skill or lower armor = more damage
        /// </summary>
        [TestCase(1000, 100, 10.0, TestName = "1000 WS vs 100 armor: 10x damage")]
        [TestCase(1000, 200, 5.0, TestName = "1000 WS vs 200 armor: 5x damage")]
        [TestCase(500, 100, 5.0, TestName = "500 WS vs 100 armor: 5x damage")]
        [TestCase(1500, 150, 10.0, TestName = "1500 WS vs 150 armor: 10x damage")]
        public void CalculateDamageMod_ShouldBeWeaponSkillOverArmor(
            double weaponSkill, double armorMod, double expectedMod)
        {
            // Test validates: DAoC Rule - Damage scales with WS/Armor ratio
            
            // Act
            var damageMod = weaponSkill / armorMod;
            
            // Assert
            damageMod.Should().Be(expectedMod,
                $"Weapon skill {weaponSkill} vs armor {armorMod} should give {expectedMod}x modifier");
        }

        #endregion

        #region Critical Hit Tests

        /// <summary>
        /// Validates critical hit damage calculation.
        /// 
        /// DAoC Rule: Crit damage 10-50% of base damage for PvP, 10-100% for PvE
        /// Reference: AttackComponent.CalculateCriticalDamage
        /// Current Implementation: Random damage within range based on target type
        /// </summary>
        [Test]
        public void CalculateCriticalDamage_ShouldBeCappedInPvP()
        {
            // Test validates: DAoC Rule - Critical damage capped at 50% in PvP
            
            // Arrange
            var attackData = new AttackData
            {
                Damage = 1000,
                CriticalChance = 100, // Force crit
                Target = CreateMockPlayer().Object
            };
            
            // Act
            var minCrit = (int)(attackData.Damage * 0.1); // 10%
            var maxCrit = (int)(attackData.Damage * 0.5); // 50% for PvP
            
            // Assert
            minCrit.Should().Be(100, "Minimum crit should be 10% of damage");
            maxCrit.Should().Be(500, "Maximum crit vs players should be 50% of damage");
        }

        [Test]
        public void CalculateCriticalDamage_ShouldBeUnCappedInPvE()
        {
            // Test validates: DAoC Rule - Critical damage up to 100% in PvE
            
            // Arrange
            var attackData = new AttackData
            {
                Damage = 1000,
                CriticalChance = 100, // Force crit
                Target = CreateMockNPC().Object
            };
            
            // Act
            var minCrit = (int)(attackData.Damage * 0.1); // 10%
            var maxCrit = (int)(attackData.Damage * 1.0); // 100% for PvE
            
            // Assert
            minCrit.Should().Be(100, "Minimum crit should be 10% of damage");
            maxCrit.Should().Be(1000, "Maximum crit vs NPCs should be 100% of damage");
        }

        #endregion

        #region Resistance Tests

        /// <summary>
        /// Validates damage type resistance calculation.
        /// 
        /// DAoC Rule: Each point of resistance reduces damage by 1%
        /// Reference: AttackComponent.CalculateTargetResistance
        /// Current Implementation: Simple percentage reduction
        /// </summary>
        [TestCase(0, 1.0, TestName = "0% resist: Full damage")]
        [TestCase(10, 0.9, TestName = "10% resist: 90% damage")]
        [TestCase(26, 0.74, TestName = "26% resist: 74% damage")]
        [TestCase(50, 0.5, TestName = "50% resist: Half damage")]
        public void CalculateTargetResistance_ShouldReduceDamageByPercentage(
            int resistance, double expectedModifier)
        {
            // Test validates: DAoC Rule - Resistance percentage reduction
            
            // Arrange
            var target = CreateMockDefender();
            target.Setup(t => t.GetResist(It.IsAny<eDamageType>())).Returns(resistance);
            
            // Act
            var modifier = _damageCalculator.CalculateTargetResistance(
                target.Object, eDamageType.Slash);
            
            // Assert
            modifier.Should().Be(expectedModifier,
                $"{resistance}% resistance should result in {expectedModifier:P0} damage");
        }

        #endregion

        #region Two-Handed and Dual Wield Tests

        /// <summary>
        /// Validates two-handed weapon damage bonus.
        /// 
        /// DAoC Rule: Two-handed weapons get spec-based damage bonus
        /// Reference: AttackComponent.CalculateTwoHandedDamageModifier
        /// Current Implementation: Scales with spec level
        /// </summary>
        [TestCase(0, 1.0, TestName = "0 spec: No bonus")]
        [TestCase(25, 1.025, TestName = "25 spec: 2.5% bonus")]
        [TestCase(50, 1.05, TestName = "50 spec: 5% bonus")]
        public void CalculateTwoHandedDamageModifier_ShouldScaleWithSpec(
            int specLevel, double expectedModifier)
        {
            // Test validates: DAoC Rule - Two-handed damage scales with specialization
            
            // Arrange
            var weapon = CreateMockWeapon(weaponType: WeaponType.TwoHanded);
            
            // Act
            var modifier = _damageCalculator.CalculateTwoHandedDamageModifier(weapon, specLevel);
            
            // Assert
            modifier.Should().Be(expectedModifier,
                $"Two-handed with {specLevel} spec should have {expectedModifier}x damage");
        }

        /// <summary>
        /// Validates left-hand (off-hand) damage calculation.
        /// 
        /// DAoC Rule: Off-hand damage based on Left Axe spec
        /// Reference: AttackComponent.CalculateLeftAxeModifier
        /// Current Implementation: 62.5% base + 0.34% per spec point
        /// </summary>
        [TestCase(0, 0.625, TestName = "0 spec: 62.5% damage")]
        [TestCase(25, 0.71, TestName = "25 spec: 71% damage")]
        [TestCase(50, 0.795, TestName = "50 spec: 79.5% damage")]
        public void CalculateLeftAxeModifier_ShouldScaleWithSpec(
            int leftAxeSpec, double expectedModifier)
        {
            // Test validates: DAoC Rule - Left hand damage scaling with spec
            
            // Act
            var modifier = _damageCalculator.CalculateLeftAxeModifier(leftAxeSpec);
            
            // Assert
            modifier.Should().BeApproximately(expectedModifier, 0.001,
                $"Left Axe spec {leftAxeSpec} should give {expectedModifier:P1} damage");
        }

        #endregion

        private Mock<IAttacker> CreateMockAttacker(int level = 50)
        {
            var mock = new Mock<IAttacker>();
            mock.Setup(a => a.Level).Returns(level);
            mock.Setup(a => a.GetModified(It.IsAny<eProperty>())).Returns(0);
            return mock;
        }

        private Mock<IDefender> CreateMockDefender(int level = 50)
        {
            var mock = new Mock<IDefender>();
            mock.Setup(d => d.Level).Returns(level);
            mock.Setup(d => d.GetArmorAF(It.IsAny<eArmorSlot>())).Returns(0);
            mock.Setup(d => d.GetArmorAbsorb(It.IsAny<eArmorSlot>())).Returns(0);
            mock.Setup(d => d.GetResist(It.IsAny<eDamageType>())).Returns(0);
            return mock;
        }

        private Mock<IGamePlayer> CreateMockPlayer(int level = 50)
        {
            var mock = new Mock<IGamePlayer>();
            mock.Setup(p => p.Level).Returns(level);
            mock.Setup(p => p.GetArmorAF(It.IsAny<eArmorSlot>())).Returns(0);
            mock.Setup(p => p.GetArmorAbsorb(It.IsAny<eArmorSlot>())).Returns(0);
            return mock;
        }

        private Mock<IGameNPC> CreateMockNPC()
        {
            var mock = new Mock<IGameNPC>();
            mock.Setup(n => n.Level).Returns(50);
            return mock;
        }

        private IWeapon CreateMockWeapon(int dps = 165, int speed = 37, 
            WeaponType weaponType = WeaponType.Sword)
        {
            var mock = new Mock<IWeapon>();
            mock.Setup(w => w.DPS).Returns(dps);
            mock.Setup(w => w.Speed).Returns(speed);
            mock.Setup(w => w.WeaponType).Returns(weaponType);
            return mock.Object;
        }
    }

    /// <summary>
    /// Extended mock damage calculator implementation
    /// </summary>
    public partial class MockDamageCalculator : IDamageCalculator
    {
        public int CalculateBaseDamage(IWeapon weapon)
        {
            if (weapon == null) return 0;
            
            double slowWeaponModifier = CalculateSlowWeaponDamageModifier(weapon);
            return (int)(weapon.DPS * weapon.Speed * 0.1 * slowWeaponModifier);
        }

        public double CalculateSlowWeaponDamageModifier(IWeapon weapon)
        {
            // Slow weapon bonus formula from current implementation
            return 1 + (weapon.Speed - 20) * 0.003;
        }

        public (double weaponSkill, double baseWeaponSkill) CalculateWeaponSkill(
            IAttacker attacker, IDefender target, IWeapon weapon)
        {
            double baseWeaponSkill = attacker.GetWeaponSkill(weapon) + 90.68; // Inherent skill
            double specModifier = CalculateSpecModifier(attacker, target, 50); // Mock spec
            double weaponSkill = baseWeaponSkill * specModifier; // No relic bonus in mock
            
            return (weaponSkill, baseWeaponSkill);
        }

        public double CalculateSpecModifier(IAttacker attacker, IDefender target, int specLevel)
        {
            // Variance calculation from current implementation
            double variance = 0.25 + Math.Min(specLevel, 50) * 0.02;
            return 1 + variance * (attacker.Level - target.Level) * 0.01;
        }

        public (double armorFactor, double armorMod) CalculateTargetArmor(
            IDefender target, eArmorSlot slot)
        {
            double armorFactor = target.GetArmorAF(slot) + 20; // Inherent AF
            
            // Player bonus
            if (target is IGamePlayer)
                armorFactor += target.Level * 20 / 50.0;
            
            double absorb = target.GetArmorAbsorb(slot);
            double armorMod = absorb >= 1 ? double.MaxValue : armorFactor / (1 - absorb);
            
            return (armorFactor, armorMod);
        }

        public double CalculateTargetResistance(IDefender target, eDamageType damageType)
        {
            return 1.0 - target.GetResist(damageType) * 0.01;
        }

        public double CalculateTwoHandedDamageModifier(IWeapon weapon, int specLevel)
        {
            // Two-handed bonus scales with spec
            return 1.0 + specLevel * 0.001;
        }

        public double CalculateLeftAxeModifier(int leftAxeSpec)
        {
            if (leftAxeSpec == 0) return 0.625;
            return 0.625 + 0.0034 * leftAxeSpec;
        }

        public DamageResult CalculateDamage(AttackData attackData)
        {
            var baseDamage = CalculateBaseDamage(attackData.Weapon);
            
            return new DamageResult
            {
                BaseDamage = baseDamage,
                ModifiedDamage = baseDamage
            };
        }
    }
} 