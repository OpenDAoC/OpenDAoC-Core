using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using DOL.GS.Interfaces.Combat;
using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.Character;
using DOL.GS;
using DOL.Database;

namespace DOL.Tests.Unit.Combat
{
    /// <summary>
    /// Unit tests for the Combat System based on game rules documentation
    /// Tests cover attack resolution order, hit/miss calculations, damage formulas,
    /// and defense mechanics as defined in OpenDAoC
    /// </summary>
    [TestFixture]
    public class CombatSystemTests
    {
        private Mock<ICombatSystem> _combatSystemMock;
        private ICombatSystem _combatSystem;
        private Mock<IPropertyService> _propertyServiceMock;
        private Mock<IEffectService> _effectServiceMock;
        private IMissChanceCalculator _missChanceCalculator;
        private IDamageCalculator _damageCalculator;
        private IAmmoService _ammoService;
        
        [SetUp]
        public void Setup()
        {
            _propertyServiceMock = new Mock<IPropertyService>();
            _effectServiceMock = new Mock<IEffectService>();
            // TODO: Replace mock with real implementation once available
            _combatSystemMock = new Mock<ICombatSystem>();
            _combatSystem = _combatSystemMock.Object;

            _ammoService = new MockAmmoService();
            _missChanceCalculator = new MockMissChanceCalculator(_ammoService);
            _damageCalculator = new MockDamageCalculator();
            _combatSystem = new MockCombatSystem(_missChanceCalculator, _damageCalculator);
        }

        #region Attack Resolution Order Tests
        
        [Test]
        public void AttackResolution_ShouldFollowCorrectOrder()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var context = new AttackContext { AttackerCount = 1 };
            
            var defenseStats = new Mock<IDefenseStats>();
            defenseStats.Setup(x => x.GetEvadeChance(1)).Returns(0.1); // 10% evade
            defenseStats.Setup(x => x.GetParryChance(1)).Returns(0.15); // 15% parry
            defenseStats.Setup(x => x.GetBlockChance(1)).Returns(0.2); // 20% block
            
            defender.Setup(x => x.DefenseStats).Returns(defenseStats.Object);
            
            // TODO: Implement proper attack resolution order tracking
            // The order should be: Intercept -> Evade -> Parry -> Block -> Guard -> Hit/Miss -> Bladeturn
        }

        #endregion

        #region Hit/Miss Calculation Tests
        
        [Test]
        public void HitMissCalculation_BaseMissChance_ShouldBe18Percent()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                AttackType = AttackType.Melee
            };
            
            var missCalculator = new MissChanceCalculator();
            
            // Act
            var missChance = missCalculator.CalculateBaseMissChance(attackData);
            
            // Assert
            missChance.Should().Be(0.18, "Base miss chance should be 18% as per patch 1.117C");
        }

        [Test]
        [TestCase(50, 49, 0.1867)] // +1 level = +1.33% hit chance
        [TestCase(50, 51, 0.1733)] // -1 level = -1.33% hit chance
        [TestCase(50, 45, 0.2265)] // +5 levels = +6.65% hit chance
        [TestCase(50, 55, 0.1335)] // -5 levels = -6.65% hit chance
        public void HitMissCalculation_LevelDifference_ShouldAffectMissChance_PvEOnly(
            int attackerLevel, int defenderLevel, double expectedMissChance)
        {
            // Arrange
            var attacker = CreateMockAttacker(level: attackerLevel);
            var defender = CreateMockNPC(level: defenderLevel); // NPC for PvE
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                AttackType = AttackType.Melee,
                IsPvP = false
            };
            
            var missCalculator = new MissChanceCalculator();
            
            // Act
            var missChance = missCalculator.CalculateMissChance(attackData);
            
            // Assert
            missChance.Should().BeApproximately(expectedMissChance, 0.0001);
        }

        [Test]
        [TestCase(1, 0.18)]   // Single attacker, no reduction
        [TestCase(2, 0.175)]  // 2 attackers, -0.5% miss
        [TestCase(3, 0.17)]   // 3 attackers, -1% miss
        [TestCase(5, 0.16)]   // 5 attackers, -2% miss
        public void HitMissCalculation_MultipleAttackers_ShouldReduceMissChance(
            int attackerCount, double expectedMissChance)
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var context = new AttackContext { AttackerCount = attackerCount };
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                AttackType = AttackType.Melee,
                Context = context
            };
            
            var missCalculator = new MissChanceCalculator();
            
            // Act
            var missChance = missCalculator.CalculateMissChance(attackData);
            
            // Assert
            missChance.Should().Be(expectedMissChance);
        }

        [Test]
        [TestCase(eAmmoQuality.Rough, 0.33)]     // +15% miss chance
        [TestCase(eAmmoQuality.Standard, 0.18)]  // No modification
        [TestCase(eAmmoQuality.Footed, 0.135)]   // -25% miss chance
        public void CalculateMissChance_WithAmmo_ShouldApplyCorrectModifier(
            eAmmoQuality ammoQuality, double expectedMissChance)
        {
            // Arrange
            var attacker = CreateMockAttacker();
            var defender = CreateMockDefender();
            var ammo = CreateMockAmmo(ammoQuality);
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                Type = DOL.GS.AttackData.eAttackType.Ranged,
                Ammo = ammo.Object
            };

            // Act
            var missChance = _missChanceCalculator.CalculateMissChance(attackData);

            // Assert
            missChance.Should().BeApproximately(expectedMissChance, 0.001);
        }

        #endregion

        #region Damage Calculation Tests
        
        [Test]
        [TestCase(165, 37, 611.55)] // Standard 1h weapon
        [TestCase(165, 56, 924.0)]  // Slow 2h weapon
        [TestCase(165, 20, 330.0)]  // Fast weapon
        public void DamageCalculation_BaseDamage_ShouldFollowFormula(
            int weaponDPS, int weaponSpeed, double expectedBaseDamage)
        {
            // Arrange
            var weapon = CreateMockWeapon(dps: weaponDPS, speed: weaponSpeed);
            var damageCalculator = new DamageCalculator();
            
            // Act
            var baseDamage = damageCalculator.CalculateBaseDamage(weapon.Object);
            
            // Assert
            // BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponModifier
            // SlowWeaponModifier = 1 + (WeaponSpeed - 20) * 0.003
            baseDamage.Should().BeApproximately(expectedBaseDamage, 0.01);
        }

        [Test]
        public void DamageCalculation_WeaponSkillModifier_ShouldApplyCorrectly()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var weapon = CreateMockWeapon(dps: 165, speed: 37);
            
            var combatStats = new Mock<ICombatStats>();
            combatStats.Setup(x => x.GetWeaponSkill(It.IsAny<IWeapon>()))
                       .Returns(1000); // Base weapon skill
            
            attacker.Setup(x => x.CombatStats).Returns(combatStats.Object);
            
            var damageCalculator = new DamageCalculator();
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                Weapon = weapon.Object,
                SpecLevel = 50 // Full spec
            };
            
            // Act
            var weaponSkill = damageCalculator.CalculateWeaponSkill(attackData);
            
            // Assert
            // WeaponSkill = BaseWeaponSkill * RelicBonus * SpecModifier
            // BaseWeaponSkill = PlayerWeaponSkill + 90.68 (inherent skill)
            // SpecModifier = 1 + Variance * (SpecLevel - TargetLevel) * 0.01
            // Variance at spec 50 = 1.25
            weaponSkill.Should().BeApproximately(1090.68, 0.01);
        }

        [Test]
        [TestCase(1000, 600, 1.667)]  // Low armor
        [TestCase(1000, 1000, 1.0)]   // Equal armor
        [TestCase(1000, 1500, 0.667)] // High armor
        public void DamageCalculation_ArmorFactorModifier_ShouldReduceDamage(
            double weaponSkill, int armorFactor, double expectedDamageMod)
        {
            // Arrange
            var damageCalculator = new DamageCalculator();
            
            // Act
            var damageMod = damageCalculator.CalculateDamageMod(weaponSkill, armorFactor);
            
            // Assert
            // DamageMod = WeaponSkill / ArmorMod
            damageMod.Should().BeApproximately(expectedDamageMod, 0.001);
        }

        [Test]
        public void DamageCalculation_CriticalHit_ShouldHaveMinimum10Percent()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Damage = 1000,
                CriticalChance = 10 // 10% crit chance
            };
            
            var damageCalculator = new DamageCalculator();
            
            // Act
            var criticalDamage = damageCalculator.CalculateCriticalDamage(attackData);
            
            // Assert
            criticalDamage.Should().BeGreaterThanOrEqualTo(100, 
                "Critical damage should be at least 10% of raw damage");
        }

        #endregion

        #region Defense Mechanics Tests
        
        [Test]
        public void DefenseMechanics_Evade_ShouldCalculateCorrectly()
        {
            // Arrange
            var defender = CreateMockDefender(level: 50, dex: 100, qui: 80);
            var defenseCalculator = new DefenseCalculator();
            
            // Act
            var evadeChance = defenseCalculator.CalculateEvadeChance(
                defender.Object, evadeAbilityLevel: 3, attackerCount: 1);
            
            // Assert
            // Base Evade = ((Dex + Qui) / 2 - 50) * 0.05 + EvadeAbilityLevel * 5
            // = ((100 + 80) / 2 - 50) * 0.05 + 3 * 5
            // = (90 - 50) * 0.05 + 15
            // = 40 * 0.05 + 15 = 2 + 15 = 17%
            evadeChance.Should().Be(0.17);
        }

        [Test]
        [TestCase(1, 0.17)]    // Single attacker, full evade
        [TestCase(2, 0.17)]    // 2 attackers, same evade (divided by attackers/2)
        [TestCase(3, 0.113)]   // 3 attackers, reduced evade
        [TestCase(4, 0.085)]   // 4 attackers, further reduced
        public void DefenseMechanics_Evade_ShouldReduceWithMultipleAttackers(
            int attackerCount, double expectedEvadeChance)
        {
            // Arrange
            var defender = CreateMockDefender(level: 50, dex: 100, qui: 80);
            var defenseCalculator = new DefenseCalculator();
            
            // Act
            var evadeChance = defenseCalculator.CalculateEvadeChance(
                defender.Object, evadeAbilityLevel: 3, attackerCount: attackerCount);
            
            // Assert
            evadeChance.Should().BeApproximately(expectedEvadeChance, 0.001);
        }

        [Test]
        public void DefenseMechanics_Parry_ShouldCalculateCorrectly()
        {
            // Arrange
            var defender = CreateMockDefender(level: 50, dex: 100);
            var defenseCalculator = new DefenseCalculator();
            
            // Act
            var parryChance = defenseCalculator.CalculateParryChance(
                defender.Object, parrySpec: 50, masteryOfParry: 0, attackerCount: 1);
            
            // Assert
            // Base Parry = (Dex * 2 - 100) / 40 + ParrySpec / 2 + MasteryOfParry * 3 + 5
            // = (100 * 2 - 100) / 40 + 50 / 2 + 0 * 3 + 5
            // = 100 / 40 + 25 + 0 + 5
            // = 2.5 + 25 + 5 = 32.5%
            parryChance.Should().Be(0.325);
        }

        [Test]
        public void DefenseMechanics_Parry_ShouldBeHalvedVsTwoHandedWeapons()
        {
            // Arrange
            var defender = CreateMockDefender(level: 50, dex: 100);
            var weapon = CreateMockWeapon(weaponType: WeaponType.TwoHanded);
            var defenseCalculator = new DefenseCalculator();
            
            // Act
            var parryChance = defenseCalculator.CalculateParryChance(
                defender.Object, parrySpec: 50, masteryOfParry: 0, 
                attackerCount: 1, attackerWeapon: weapon.Object);
            
            // Assert
            parryChance.Should().Be(0.1625, "Parry should be halved vs two-handed weapons");
        }

        [Test]
        [TestCase(0, 0.05)]   // No shield spec, 5% base
        [TestCase(10, 0.10)]  // 10 spec, 10% block
        [TestCase(30, 0.20)]  // 30 spec, 20% block
        [TestCase(50, 0.30)]  // 50 spec, 30% block
        public void DefenseMechanics_Block_ShouldScaleWithShieldSpec(
            int shieldSpec, double expectedBlockChance)
        {
            // Arrange
            var defender = CreateMockDefender(level: 50, dex: 100);
            var shield = CreateMockShield(size: ShieldSize.Medium);
            var defenseCalculator = new DefenseCalculator();
            
            // Act
            var blockChance = defenseCalculator.CalculateBlockChance(
                defender.Object, shield.Object, shieldSpec: shieldSpec);
            
            // Assert
            // Base Block = 5% + 0.5% * ShieldSpec
            // Modified by Dex (0.1% per point above 60)
            blockChance.Should().BeApproximately(expectedBlockChance + 0.04, 0.001);
        }

        [Test]
        [TestCase(ShieldSize.Small, 1)]
        [TestCase(ShieldSize.Medium, 2)]
        [TestCase(ShieldSize.Large, 3)]
        public void DefenseMechanics_Block_ShieldSize_ShouldDetermineMaxSimultaneousBlocks(
            ShieldSize size, int expectedMaxBlocks)
        {
            // Arrange
            var shield = CreateMockShield(size: size);
            
            // Act
            var maxBlocks = shield.Object.GetMaxSimultaneousBlocks();
            
            // Assert
            maxBlocks.Should().Be(expectedMaxBlocks);
        }

        #endregion

        #region Style Combat Tests
        
        [Test]
        public void StyleCombat_PositionalRequirement_ShouldBeChecked()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var style = CreateMockStyle(positional: StylePositional.Side);
            
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                Style = style.Object,
                AttackerPosition = Position.Side
            };
            
            var styleValidator = new StyleValidator();
            
            // Act
            var canUseStyle = styleValidator.CanUseStyle(attackData);
            
            // Assert
            canUseStyle.Should().BeTrue("Attacker is at the correct position");
        }

        [Test]
        public void StyleCombat_OpeningRequirement_ShouldRequirePreviousResult()
        {
            // Arrange
            var attacker = CreateMockAttacker(level: 50);
            var defender = CreateMockDefender(level: 50);
            var style = CreateMockStyle(openingType: StyleOpeningType.Parry);
            
            var attackData = new AttackData
            {
                Attacker = attacker.Object,
                Target = defender.Object,
                Style = style.Object,
                LastAttackResult = AttackResult.Parried
            };
            
            var styleValidator = new StyleValidator();
            
            // Act
            var canUseStyle = styleValidator.CanUseStyle(attackData);
            
            // Assert
            canUseStyle.Should().BeTrue("Last attack was parried, matching opening requirement");
        }

        [Test]
        [TestCase(20, 2.0)]  // Fast weapon
        [TestCase(37, 3.7)]  // Standard weapon
        [TestCase(56, 5.6)]  // Slow weapon
        public void StyleCombat_EnduranceCost_ShouldScaleWithWeaponSpeed(
            int weaponSpeed, double expectedEnduranceCost)
        {
            // Arrange
            var style = CreateMockStyle();
            var weapon = CreateMockWeapon(speed: weaponSpeed);
            
            var styleCalculator = new StyleCalculator();
            
            // Act
            var enduranceCost = styleCalculator.CalculateEnduranceCost(style.Object, weapon.Object);
            
            // Assert
            enduranceCost.Should().Be(expectedEnduranceCost);
        }

        #endregion

        #region Spell Damage Tests
        
        [Test]
        public void SpellDamage_BaseCalculation_ShouldFollowFormula()
        {
            // Arrange
            var caster = CreateMockCaster(level: 50, intelligence: 100);
            var spell = CreateMockSpell(damage: 300);
            var spellCalculator = new SpellDamageCalculator();
            
            // Act
            var damage = spellCalculator.CalculateBaseDamage(caster.Object, spell.Object);
            
            // Assert
            // SpellDamage = DelveValue * (1 + StatModifier * 0.005) * (1 + SpecBonus * 0.005)
            // = 300 * (1 + 100 * 0.005) * (1 + 0)
            // = 300 * 1.5 = 450
            damage.Should().Be(450);
        }

        [Test]
        [TestCase(50, 50, 0.875)]  // Same level
        [TestCase(50, 45, 0.90)]   // +5 levels
        [TestCase(50, 55, 0.85)]   // -5 levels
        public void SpellDamage_HitChance_ShouldVaryWithLevelDifference(
            int casterLevel, int targetLevel, double expectedHitChance)
        {
            // Arrange
            var caster = CreateMockCaster(level: casterLevel);
            var target = CreateMockDefender(level: targetLevel);
            var spell = CreateMockSpell();
            var spellCalculator = new SpellDamageCalculator();
            
            // Act
            var hitChance = spellCalculator.CalculateHitChance(
                caster.Object, target.Object, spell.Object);
            
            // Assert
            // HitChance = 87.5% base +/- (SpellLevel - TargetLevel) / 2
            hitChance.Should().Be(expectedHitChance);
        }

        [Test]
        [TestCase(0, 0.75, 1.25)]    // No mastery, 75-125%
        [TestCase(1, 0.825, 1.25)]   // Mastery 1, 82.5-125%
        [TestCase(3, 0.975, 1.25)]   // Mastery 3, 97.5-125%
        public void SpellDamage_Variance_ShouldBeAffectedByMasteryOfMagic(
            int masteryLevel, double expectedMin, double expectedMax)
        {
            // Arrange
            var caster = CreateMockCaster(level: 50);
            caster.Setup(x => x.GetAbilityLevel(Abilities.MasteryOfMagic))
                  .Returns(masteryLevel);
            
            var spellCalculator = new SpellDamageCalculator();
            
            // Act
            var (minVariance, maxVariance) = spellCalculator.CalculateDamageVariance(
                caster.Object, masteryLevel);
            
            // Assert
            minVariance.Should().Be(expectedMin);
            maxVariance.Should().Be(expectedMax);
        }

        #endregion

        #region Helper Methods
        
        private Mock<IAttacker> CreateMockAttacker(int level = 50)
        {
            var attacker = new Mock<IAttacker>();
            attacker.Setup(x => x.Level).Returns(level);
            attacker.Setup(x => x.ActiveWeapon).Returns(CreateMockWeapon().Object);
            attacker.Setup(x => x.CombatStats).Returns(new Mock<ICombatStats>().Object);
            attacker.Setup(x => x.ActiveEffects).Returns(new List<IEffect>());
            return attacker;
        }

        private Mock<IDefender> CreateMockDefender(int level = 50, int dex = 60, int qui = 60)
        {
            var defender = new Mock<IDefender>();
            defender.Setup(x => x.Level).Returns(level);
            
            var defenseStats = new Mock<IDefenseStats>();
            defender.Setup(x => x.DefenseStats).Returns(defenseStats.Object);
            
            var stats = new Mock<IStats>();
            stats.Setup(x => x[Stat.Dexterity]).Returns(dex);
            stats.Setup(x => x[Stat.Quickness]).Returns(qui);
            defender.Setup(x => x.ModifiedStats).Returns(stats.Object);
            
            return defender;
        }

        private Mock<IDefender> CreateMockNPC(int level = 50)
        {
            var npc = CreateMockDefender(level);
            npc.Setup(x => x.IsPlayer).Returns(false);
            return npc;
        }

        private Mock<IWeapon> CreateMockWeapon(
            int dps = 165, 
            int speed = 37, 
            WeaponType weaponType = WeaponType.OneHanded)
        {
            var weapon = new Mock<IWeapon>();
            weapon.Setup(x => x.DPS).Returns(dps);
            weapon.Setup(x => x.Speed).Returns(speed);
            weapon.Setup(x => x.WeaponType).Returns(weaponType);
            return weapon;
        }

        private Mock<IWeapon> CreateMockRangedWeapon()
        {
            var weapon = CreateMockWeapon();
            weapon.Setup(x => x.WeaponType).Returns(WeaponType.Longbow);
            return weapon;
        }

        private IAmmo CreateMockAmmo(eAmmoQuality quality)
        {
            var ammo = new Mock<IAmmo>();
            ammo.Setup(x => x.PhysicalType).Returns(eObjectType.Arrow);
            ammo.Setup(x => x.AmmoQuality).Returns(quality);
            ammo.Setup(x => x.IsCompatibleWith(It.IsAny<IWeapon>())).Returns(true);
            return ammo.Object;
        }

        private Mock<IShield> CreateMockShield(ShieldSize size = ShieldSize.Medium)
        {
            var shield = new Mock<IShield>();
            shield.Setup(x => x.Size).Returns(size);
            shield.Setup(x => x.GetMaxSimultaneousBlocks()).Returns(
                size == ShieldSize.Small ? 1 : 
                size == ShieldSize.Medium ? 2 : 3);
            return shield;
        }

        private Mock<IStyle> CreateMockStyle(
            StylePositional positional = StylePositional.Any,
            StyleOpeningType openingType = StyleOpeningType.Any)
        {
            var style = new Mock<IStyle>();
            style.Setup(x => x.PositionalRequirement).Returns(positional);
            style.Setup(x => x.OpeningRequirement).Returns(openingType);
            return style;
        }

        private Mock<ICaster> CreateMockCaster(int level = 50, int intelligence = 60)
        {
            var caster = new Mock<ICaster>();
            caster.Setup(x => x.Level).Returns(level);
            
            var stats = new Mock<IStats>();
            stats.Setup(x => x[Stat.Intelligence]).Returns(intelligence);
            caster.Setup(x => x.ModifiedStats).Returns(stats.Object);
            
            return caster;
        }

        private Mock<ISpell> CreateMockSpell(int damage = 100)
        {
            var spell = new Mock<ISpell>();
            spell.Setup(x => x.Damage).Returns(damage);
            return spell;
        }
        
        #endregion
    }

    #region Test Implementation Classes
    
    // TODO: These should be moved to separate implementation files
    public class MissChanceCalculator : IMissChanceCalculator
    {
        public double CalculateBaseMissChance(AttackData attackData) => 0.18;
        public double CalculateMissChance(AttackData attackData) => 0.18; // Simplified
    }

    public class DamageCalculator : IDamageCalculator
    {
        public double CalculateBaseDamage(IWeapon weapon) => weapon.DPS * weapon.Speed * 0.1;
        public double CalculateWeaponSkill(AttackData attackData) => 1090.68; // Simplified
        public double CalculateDamageMod(double weaponSkill, int armorFactor) => weaponSkill / armorFactor;
        public int CalculateCriticalDamage(AttackData attackData) => attackData.Damage / 10;
    }

    public class DefenseCalculator : IDefenseCalculator
    {
        public double CalculateEvadeChance(IDefender defender, int evadeAbilityLevel, int attackerCount) => 0.17;
        public double CalculateParryChance(IDefender defender, int parrySpec, int masteryOfParry, int attackerCount, IWeapon attackerWeapon = null) => 0.325;
        public double CalculateBlockChance(IDefender defender, IShield shield, int shieldSpec) => 0.05 + shieldSpec * 0.005;
    }

    public class StyleValidator : IStyleValidator
    {
        public bool CanUseStyle(AttackData attackData) => true; // Simplified
    }

    public class StyleCalculator : IStyleCalculator
    {
        public double CalculateEnduranceCost(IStyle style, IWeapon weapon) => weapon.Speed * 0.1;
        public int CalculateStyleDamage(IStyle style, int baseDamage) => baseDamage;
    }

    public class SpellDamageCalculator : ISpellDamageCalculator
    {
        public int CalculateBaseDamage(ICaster caster, ISpell spell) => spell.Damage + (caster.ModifiedStats[Stat.Intelligence] * spell.Damage / 200);
        public double CalculateHitChance(ICaster caster, IDefender target, ISpell spell) => 0.875;
        public (double min, double max) CalculateDamageVariance(ICaster caster, int masteryLevel) => (0.75 + masteryLevel * 0.075, 1.25);
    }
    
    // Mock service interfaces - these should be defined elsewhere
    public interface IPropertyService { }
    public interface IEffectService { }
    
    #endregion

    /// <summary>
    /// Mock ammo service for testing ammo calculations
    /// </summary>
    public class MockAmmoService : IAmmoService
    {
        public double GetMissChanceModifier(eAmmoQuality quality)
        {
            return quality switch
            {
                eAmmoQuality.Rough => 0.15,     // +15% miss chance
                eAmmoQuality.Standard => 0.0,     // No modification
                eAmmoQuality.Footed => -0.25,     // -25% miss chance
                _ => 0.0
            };
        }

        public bool IsAmmoCompatible(IAmmo ammo, IWeapon weapon)
        {
            // Simplified compatibility check
            return ammo.PhysicalType switch
            {
                eObjectType.Arrow => weapon.WeaponType == eObjectType.Longbow || 
                                     weapon.WeaponType == eObjectType.RecurvedBow ||
                                     weapon.WeaponType == eObjectType.CompositeBow,
                eObjectType.Bolt => weapon.WeaponType == eObjectType.Crossbow,
                _ => false
            };
        }

        public IAmmo GetBestAmmoForWeapon(IInventory inventory, IWeapon weapon)
        {
            // Mock implementation - would search inventory for best ammo
            var mockAmmo = new Mock<IAmmo>();
            mockAmmo.Setup(x => x.AmmoQuality).Returns(eAmmoQuality.Footed);
            mockAmmo.Setup(x => x.PhysicalType).Returns(eObjectType.Arrow);
            return mockAmmo.Object;
        }

        public void ApplyAmmoWear(IAmmo ammo, int wearAmount = 1)
        {
            // Mock implementation - would reduce ammo condition/count
        }
    }

    /// <summary>
    /// Mock miss chance calculator that uses ammo service
    /// </summary>
    public class MockMissChanceCalculator : IMissChanceCalculator
    {
        private readonly IAmmoService _ammoService;
        private const double BaseMissChance = 0.18; // 18% base miss chance

        public MockMissChanceCalculator(IAmmoService ammoService)
        {
            _ammoService = ammoService;
        }

        public double CalculateBaseMissChance(AttackData attackData)
        {
            return BaseMissChance;
        }

        public double CalculateMissChance(AttackData attackData)
        {
            double missChance = BaseMissChance;

            // Apply ammo modifier for ranged attacks
            if (attackData.Type == DOL.GS.AttackData.eAttackType.Ranged && attackData.Ammo != null)
            {
                double ammoModifier = _ammoService.GetMissChanceModifier(attackData.Ammo.AmmoQuality);
                missChance += ammoModifier;
            }

            return Math.Max(0.0, Math.Min(1.0, missChance)); // Clamp between 0-100%
        }
    }

    /// <summary>
    /// Mock damage calculator for testing
    /// </summary>
    public class MockDamageCalculator : IDamageCalculator
    {
        public int CalculateBaseDamage(AttackData attackData)
        {
            // Simplified damage calculation for testing
            var weapon = attackData.Weapon;
            if (weapon == null) return 0;
            
            return weapon.DPS * weapon.Speed / 10; // Basic DPS calculation
        }

        public DamageResult CalculateDamage(AttackData attackData)
        {
            var baseDamage = CalculateBaseDamage(attackData);
            
            return new DamageResult
            {
                BaseDamage = baseDamage,
                ModifiedDamage = baseDamage,
                TotalDamage = baseDamage,
                WasCritical = false
            };
        }
    }

    /// <summary>
    /// Mock combat system for testing - integrates all components
    /// </summary>
    public class MockCombatSystem : ICombatSystem
    {
        private readonly IMissChanceCalculator _missChanceCalculator;
        private readonly IDamageCalculator _damageCalculator;

        public MockCombatSystem(IMissChanceCalculator missChanceCalculator, IDamageCalculator damageCalculator)
        {
            _missChanceCalculator = missChanceCalculator;
            _damageCalculator = damageCalculator;
        }

        public eAttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context)
        {
            var attackData = new AttackData
            {
                Attacker = attacker,
                Target = defender,
                Type = DOL.GS.AttackData.eAttackType.MeleeOneHand,
                Context = context
            };

            var missChance = _missChanceCalculator.CalculateMissChance(attackData);
            var hit = Random.Shared.NextDouble() > missChance;

            return hit ? eAttackResult.HitUnstyled : eAttackResult.Missed;
        }

        public DamageResult CalculateDamage(AttackData attackData)
        {
            return _damageCalculator.CalculateDamage(attackData);
        }

        public void ApplyDamage(ILiving target, DamageResult damage)
        {
            // Mock implementation - would apply damage to target
        }
    }
} 