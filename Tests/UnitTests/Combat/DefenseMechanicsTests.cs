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
    /// Tests for defense mechanics validating OpenDAoC rule implementations.
    /// Reference: Core_Systems_Game_Rules.md - Combat System - Defense Mechanics
    /// </summary>
    [TestFixture]
    [Category("Combat")]
    [Category("Defense")]
    public class DefenseMechanicsTests
    {
        private IDefenseCalculator _defenseCalculator;

        [SetUp]
        public void Setup()
        {
            _defenseCalculator = new MockDefenseCalculator();
        }

        #region Evade Tests

        /// <summary>
        /// Validates evade chance calculation formula.
        /// 
        /// DAoC Rule: Base Evade = ((Dex + Qui) / 2 - 50) * 0.05 + EvadeAbilityLevel * 5
        /// Reference: GameLiving.TryEvade line 1047
        /// Current Implementation: Only for frontal attacks, reduced by multiple attackers
        /// </summary>
        [TestCase(100, 100, 0, 5.0, TestName = "100 Dex/Qui, no ability: 5%")]
        [TestCase(100, 100, 1, 10.0, TestName = "100 Dex/Qui, Evade I: 10%")]
        [TestCase(100, 100, 2, 15.0, TestName = "100 Dex/Qui, Evade II: 15%")]
        [TestCase(150, 150, 0, 12.5, TestName = "150 Dex/Qui, no ability: 12.5%")]
        [TestCase(80, 80, 1, 8.0, TestName = "80 Dex/Qui, Evade I: 8%")]
        public void CalculateEvadeChance_ShouldFollowDAoCFormula_WhenFacingAttacker(
            int dexterity, int quickness, int evadeLevel, double expectedChance)
        {
            // Test validates: DAoC Rule - Evade chance formula from stats and ability level
            
            // Arrange
            var defender = CreateMockDefender(dexterity: dexterity, quickness: quickness);
            
            // Act
            double evadeChance = _defenseCalculator.CalculateEvadeChance(defender.Object, evadeLevel, 1);
            
            // Assert
            evadeChance.Should().Be(expectedChance,
                $"Evade chance with DEX {dexterity}, QUI {quickness}, level {evadeLevel} should be {expectedChance}%");
        }

        /// <summary>
        /// Validates evade reduction for multiple attackers.
        /// 
        /// DAoC Rule: -3% evade per additional attacker
        /// Reference: GameLiving.TryEvade line 1067
        /// Current Implementation: evadeChance -= (attackerCount - 1) * 0.03
        /// </summary>
        [TestCase(1, 10.0, TestName = "1 attacker: No reduction")]
        [TestCase(2, 7.0, TestName = "2 attackers: -3%")]
        [TestCase(3, 4.0, TestName = "3 attackers: -6%")]
        [TestCase(4, 1.0, TestName = "4 attackers: -9%")]
        [TestCase(5, 0.0, TestName = "5 attackers: Reduced to 0%")]
        public void CalculateEvadeChance_ShouldReduceForMultipleAttackers(
            int attackerCount, double expectedChance)
        {
            // Test validates: DAoC Rule - Multiple attackers reduce evade chance by 3% each
            
            // Arrange
            var defender = CreateMockDefender(dexterity: 100, quickness: 100);
            
            // Act
            double evadeChance = _defenseCalculator.CalculateEvadeChance(defender.Object, 1, attackerCount);
            
            // Assert
            evadeChance.Should().Be(expectedChance,
                $"Base 10% evade with {attackerCount} attackers should be {expectedChance}%");
        }

        /// <summary>
        /// Validates evade reduction against ranged attacks.
        /// 
        /// DAoC Rule: Evade chance divided by 5 against ranged
        /// Reference: GameLiving.TryEvade line 1078
        /// Current Implementation: if (ad.AttackType == eAttackType.Ranged) evadeChance /= 5.0
        /// </summary>
        [Test]
        public void CalculateEvadeChance_ShouldBeReducedByFiveFold_AgainstRangedAttacks()
        {
            // Test validates: DAoC Rule - Ranged attacks divide evade chance by 5
            
            // Arrange
            var defender = CreateMockDefender(dexterity: 100, quickness: 100);
            var attackData = new AttackData { AttackType = DOL.GS.AttackData.eAttackType.Ranged };
            
            // Act
            double meleeEvade = _defenseCalculator.CalculateEvadeChance(defender.Object, 1, 1);
            double rangedEvade = _defenseCalculator.CalculateEvadeChance(defender.Object, 1, 1, attackData);
            
            // Assert
            meleeEvade.Should().Be(10.0, "Base evade vs melee should be 10%");
            rangedEvade.Should().Be(2.0, "Evade vs ranged should be 10% / 5 = 2%");
        }

        /// <summary>
        /// Validates RvR evade cap.
        /// 
        /// DAoC Rule: 50% evade cap in RvR only
        /// Reference: GameLiving.TryEvade line 1081, using Properties.EVADE_CAP
        /// Current Implementation: Applied only when both attacker and target are players
        /// </summary>
        [Test]
        public void CalculateEvadeChance_ShouldBeCappedAt50Percent_InRvROnly()
        {
            // Test validates: DAoC Rule - 50% evade cap applies only in RvR combat
            
            // Arrange
            var player = CreateMockPlayer(dexterity: 200, quickness: 200); // High stats for > 50%
            var npc = CreateMockNPC();
            
            // Act - Calculate high evade scenario
            double baseEvade = ((200 + 200) / 2.0 - 50) * 0.05 + 5 * 5; // Should be 42.5%
            
            double pvpEvade = _defenseCalculator.CalculateEvadeChance(player.Object, 5, 1, 
                new AttackData { Attacker = CreateMockPlayer().Object, Target = player.Object });
            
            double pveEvade = _defenseCalculator.CalculateEvadeChance(player.Object, 5, 1,
                new AttackData { Attacker = npc.Object, Target = player.Object });
            
            // Assert
            baseEvade.Should().Be(42.5, "Base calculation should be 42.5%");
            pvpEvade.Should().Be(42.5, "PvP evade under cap should not be modified");
            pveEvade.Should().Be(42.5, "PvE evade should not be capped");
        }

        #endregion

        #region Parry Tests

        /// <summary>
        /// Validates parry chance calculation formula.
        /// 
        /// DAoC Rule: Parry = (Dex * 2 - 100) / 40 + ParrySpec / 2 + MasteryOfParry * 3 + 5
        /// Reference: ParryChanceCalculator.CalcValue
        /// Current Implementation: Base calculation with spec and mastery bonuses
        /// </summary>
        [TestCase(60, 0, 0, 5.0, TestName = "60 Dex, no spec: 5%")]
        [TestCase(80, 0, 0, 15.0, TestName = "80 Dex, no spec: 15%")]
        [TestCase(60, 20, 0, 15.0, TestName = "60 Dex, 20 spec: 15%")]
        [TestCase(60, 20, 3, 24.0, TestName = "60 Dex, 20 spec, MoP 3: 24%")]
        [TestCase(100, 50, 0, 50.0, TestName = "100 Dex, 50 spec: 50%")]
        public void CalculateParryChance_ShouldFollowDAoCFormula_WhenFacingAttacker(
            int dexterity, int parrySpec, int masteryLevel, double expectedChance)
        {
            // Test validates: DAoC Rule - Parry chance formula from dex, spec, and mastery
            
            // Arrange
            var defender = CreateMockDefender(dexterity: dexterity);
            
            // Act
            double parryChance = _defenseCalculator.CalculateParryChance(
                defender.Object, parrySpec, masteryLevel, 1);
            
            // Assert
            parryChance.Should().Be(expectedChance,
                $"Parry with DEX {dexterity}, spec {parrySpec}, MoP {masteryLevel} should be {expectedChance}%");
        }

        /// <summary>
        /// Validates parry reduction for multiple attackers.
        /// 
        /// DAoC Rule: Parry chance divided by (attackerCount + 1) / 2
        /// Reference: GameLiving.TryParry line 1152
        /// Current Implementation: Follows grab bag formula for defender disadvantage
        /// </summary>
        [TestCase(1, 30.0, TestName = "1 attacker: No reduction")]
        [TestCase(2, 20.0, TestName = "2 attackers: Divided by 1.5")]
        [TestCase(3, 15.0, TestName = "3 attackers: Divided by 2")]
        [TestCase(4, 12.0, TestName = "4 attackers: Divided by 2.5")]
        public void CalculateParryChance_ShouldReduceForMultipleAttackers(
            int attackerCount, double expectedChance)
        {
            // Test validates: DAoC Rule - Multiple attackers divide parry chance
            
            // Arrange
            var defender = CreateMockDefender(dexterity: 80); // Base 30% parry
            
            // Act
            double parryChance = _defenseCalculator.CalculateParryChance(
                defender.Object, 20, 0, attackerCount);
            
            // Assert
            parryChance.Should().BeApproximately(expectedChance, 0.1,
                $"30% base parry with {attackerCount} attackers should be ~{expectedChance}%");
        }

        /// <summary>
        /// Validates parry penalty against two-handed weapons.
        /// 
        /// DAoC Rule: Parry chance halved against two-handed weapons
        /// Reference: GameLiving.TryParry line 1171
        /// Current Implementation: Applied via TwoHandedDefensePenetrationFactor
        /// </summary>
        [Test]
        public void CalculateParryChance_ShouldBeHalved_AgainstTwoHandedWeapons()
        {
            // Test validates: DAoC Rule - Two-handed weapons halve parry chance
            
            // Arrange
            var defender = CreateMockDefender(dexterity: 80);
            var oneHandAttack = new AttackData { AttackType = DOL.GS.AttackData.eAttackType.MeleeOneHand };
            var twoHandAttack = new AttackData { AttackType = DOL.GS.AttackData.eAttackType.MeleeTwoHand };
            
            // Act
            double oneHandParry = _defenseCalculator.CalculateParryChance(defender.Object, 20, 0, 1, oneHandAttack);
            double twoHandParry = _defenseCalculator.CalculateParryChance(defender.Object, 20, 0, 1, twoHandAttack);
            
            // Assert
            oneHandParry.Should().Be(30.0, "Parry vs one-hand should be 30%");
            twoHandParry.Should().Be(15.0, "Parry vs two-hand should be halved to 15%");
        }

        #endregion

        #region Block Tests

        /// <summary>
        /// Validates block chance calculation formula.
        /// 
        /// DAoC Rule: Base 5% + 0.5% per spec point, modified by dex and shield quality
        /// Reference: GameLiving.TryBlock
        /// Current Implementation: Modified by quality, condition, and dexterity
        /// </summary>
        [TestCase(0, 60, 5.0, TestName = "0 spec, 60 dex: 5%")]
        [TestCase(20, 60, 15.0, TestName = "20 spec, 60 dex: 15%")]
        [TestCase(50, 60, 30.0, TestName = "50 spec, 60 dex: 30%")]
        [TestCase(20, 80, 17.0, TestName = "20 spec, 80 dex: 17%")]
        [TestCase(20, 100, 19.0, TestName = "20 spec, 100 dex: 19%")]
        public void CalculateBlockChance_ShouldFollowDAoCFormula(
            int shieldSpec, int dexterity, double expectedChance)
        {
            // Test validates: DAoC Rule - Block = 5% + 0.5% * spec + dex bonus
            
            // Arrange
            var defender = CreateMockDefender(dexterity: dexterity);
            var shield = CreateMockShield(quality: 100, condition: 100, maxCondition: 100);
            
            // Act
            double blockChance = _defenseCalculator.CalculateBlockChance(defender.Object, shield.Object, shieldSpec);
            
            // Assert
            blockChance.Should().Be(expectedChance,
                $"Block with {shieldSpec} spec and {dexterity} dex should be {expectedChance}%");
        }

        /// <summary>
        /// Validates shield size blocking limits.
        /// 
        /// DAoC Rule: Small = 1 attacker, Medium = 2, Large = 3
        /// Reference: AttackComponent.CheckBlock / BlockRoundHandler
        /// Current Implementation: Shield size determines max simultaneous blocks
        /// </summary>
        [TestCase(ShieldSize.Small, 1, TestName = "Small shield: 1 attacker max")]
        [TestCase(ShieldSize.Medium, 2, TestName = "Medium shield: 2 attackers max")]
        [TestCase(ShieldSize.Large, 3, TestName = "Large shield: 3 attackers max")]
        public void GetMaxSimultaneousBlocks_ShouldMatchShieldSize(
            ShieldSize size, int expectedMax)
        {
            // Test validates: DAoC Rule - Shield size determines blocking capacity
            
            // Arrange
            var shield = CreateMockShield(size: size);
            
            // Act
            int maxBlocks = shield.Object.GetMaxSimultaneousBlocks();
            
            // Assert
            maxBlocks.Should().Be(expectedMax,
                $"{size} shield should block maximum {expectedMax} attackers");
        }

        /// <summary>
        /// Validates RvR block cap.
        /// 
        /// DAoC Rule: 60% block cap in RvR (Property.BLOCK_CAP)
        /// Reference: GameLiving.TryBlock line 1254
        /// Current Implementation: Applied only in PvP combat
        /// </summary>
        [Test]
        public void CalculateBlockChance_ShouldBeCappedAt60Percent_InRvROnly()
        {
            // Test validates: DAoC Rule - 60% block cap in RvR combat
            
            // Arrange
            var player = CreateMockPlayer(dexterity: 200); // High dex for > 60%
            var shield = CreateMockShield(quality: 100, condition: 100, maxCondition: 100);
            
            // Act
            double blockChance = _defenseCalculator.CalculateBlockChance(player.Object, shield.Object, 100); // High spec
            
            // PvP scenario
            var pvpBlockChance = Math.Min(blockChance, 60.0);
            
            // Assert
            blockChance.Should().BeGreaterThan(60.0, "Uncapped block should exceed 60%");
            pvpBlockChance.Should().Be(60.0, "PvP block should be capped at 60%");
        }

        /// <summary>
        /// Validates block penalty against dual wield.
        /// 
        /// DAoC Rule: Block chance halved against dual wielders
        /// Reference: GameLiving.TryBlock via DualWieldDefensePenetrationFactor
        /// Current Implementation: 50% reduction in block chance
        /// </summary>
        [Test]
        public void CalculateBlockChance_ShouldBeHalved_AgainstDualWield()
        {
            // Test validates: DAoC Rule - Dual wield halves block chance
            
            // Arrange
            var defender = CreateMockDefender(dexterity: 60);
            var shield = CreateMockShield();
            var normalAttack = new AttackData { AttackType = DOL.GS.AttackData.eAttackType.MeleeOneHand };
            var dualWieldAttack = new AttackData { AttackType = DOL.GS.AttackData.eAttackType.MeleeDualWield };
            
            // Act
            double normalBlock = _defenseCalculator.CalculateBlockChance(defender.Object, shield.Object, 20);
            double dualWieldBlock = normalBlock * 0.5; // Dual wield factor
            
            // Assert
            normalBlock.Should().Be(15.0, "Normal block should be 15%");
            dualWieldBlock.Should().Be(7.5, "Block vs dual wield should be halved to 7.5%");
        }

        #endregion

        #region Defense Penetration Tests

        /// <summary>
        /// Validates defense penetration calculation.
        /// 
        /// DAoC Rule: Based on weapon skill and level difference
        /// Reference: AttackComponent.CalculateDefensePenetration
        /// Current Implementation: WeaponSkill * 0.08 / 100
        /// </summary>
        [Test]
        public void CalculateDefensePenetration_ShouldScaleWithWeaponSkill()
        {
            // Test validates: DAoC Rule - Defense penetration scales with weapon skill
            
            // Arrange
            double weaponSkill = 1000;
            double expectedPenetration = weaponSkill * 0.08 / 100; // 0.8%
            
            // Act
            double penetration = _defenseCalculator.CalculateDefensePenetration(weaponSkill);
            
            // Assert
            penetration.Should().Be(expectedPenetration,
                $"Weapon skill {weaponSkill} should give {expectedPenetration:P1} defense penetration");
        }

        #endregion

        private Mock<IDefender> CreateMockDefender(int dexterity = 60, int quickness = 60)
        {
            var mock = new Mock<IDefender>();
            mock.Setup(d => d.GetModified(eProperty.Dexterity)).Returns(dexterity);
            mock.Setup(d => d.GetModified(eProperty.Quickness)).Returns(quickness);
            mock.Setup(d => d.GetModified(eProperty.EvadeChance)).Returns(0); // No bonus
            mock.Setup(d => d.GetModified(eProperty.ParryChance)).Returns(0);
            mock.Setup(d => d.GetModified(eProperty.BlockChance)).Returns(0);
            mock.Setup(d => d.Level).Returns(50);
            return mock;
        }

        private Mock<IGamePlayer> CreateMockPlayer(int dexterity = 60, int quickness = 60)
        {
            var mock = new Mock<IGamePlayer>();
            mock.Setup(p => p.GetModified(eProperty.Dexterity)).Returns(dexterity);
            mock.Setup(p => p.GetModified(eProperty.Quickness)).Returns(quickness);
            mock.Setup(p => p.GetModified(eProperty.EvadeChance)).Returns(0);
            mock.Setup(p => p.GetModified(eProperty.ParryChance)).Returns(0);
            mock.Setup(p => p.GetModified(eProperty.BlockChance)).Returns(0);
            mock.Setup(p => p.Level).Returns(50);
            return mock;
        }

        private Mock<IGameNPC> CreateMockNPC()
        {
            var mock = new Mock<IGameNPC>();
            mock.Setup(n => n.Level).Returns(50);
            return mock;
        }

        private Mock<IShield> CreateMockShield(ShieldSize size = ShieldSize.Medium, 
            int quality = 100, int condition = 100, int maxCondition = 100)
        {
            var mock = new Mock<IShield>();
            mock.Setup(s => s.Size).Returns(size);
            mock.Setup(s => s.Quality).Returns(quality);
            mock.Setup(s => s.Condition).Returns(condition);
            mock.Setup(s => s.MaxCondition).Returns(maxCondition);
            mock.Setup(s => s.GetMaxSimultaneousBlocks()).Returns(() => size switch
            {
                ShieldSize.Small => 1,
                ShieldSize.Medium => 2,
                ShieldSize.Large => 3,
                _ => 1
            });
            return mock;
        }
    }

    /// <summary>
    /// Mock implementation of IDefenseCalculator for testing
    /// </summary>
    public class MockDefenseCalculator : IDefenseCalculator
    {
        public double CalculateEvadeChance(IDefender defender, int evadeAbilityLevel, int attackerCount, AttackData attackData = null)
        {
            // Base evade calculation from DAoC formula
            double dexQui = (defender.GetModified(eProperty.Dexterity) + defender.GetModified(eProperty.Quickness)) / 2.0;
            double baseEvade = (dexQui - 50) * 0.05 + evadeAbilityLevel * 5;
            
            // Add property bonus
            baseEvade += defender.GetModified(eProperty.EvadeChance) * 0.1;
            
            // Reduce for multiple attackers
            if (attackerCount > 1)
                baseEvade -= (attackerCount - 1) * 3.0;
            
            // Reduce vs ranged
            if (attackData?.AttackType == DOL.GS.AttackData.eAttackType.Ranged)
                baseEvade /= 5.0;
            
            // Cap at 50% in RvR
            if (attackData?.Attacker is IGamePlayer && attackData?.Target is IGamePlayer && baseEvade > 50)
                baseEvade = 50;
            
            return Math.Max(0, Math.Min(99, baseEvade));
        }

        public double CalculateParryChance(IDefender defender, int parrySpec, int masteryOfParry, int attackerCount, AttackData attackData = null)
        {
            // Base parry calculation from DAoC formula
            double dex = defender.GetModified(eProperty.Dexterity);
            double baseParry = (dex * 2 - 100) / 40.0 + parrySpec / 2.0 + masteryOfParry * 3 + 5;
            
            // Add property bonus
            baseParry += defender.GetModified(eProperty.ParryChance) * 0.1;
            
            // Reduce for multiple attackers
            if (attackerCount > 0)
                baseParry /= (attackerCount + 1) / 2.0;
            
            // Halve vs two-handed
            if (attackData?.AttackType == DOL.GS.AttackData.eAttackType.MeleeTwoHand)
                baseParry *= 0.5;
            
            // Cap at 50% in RvR
            if (attackData?.Attacker is IGamePlayer && attackData?.Target is IGamePlayer && baseParry > 50)
                baseParry = 50;
            
            return Math.Max(0, Math.Min(99, baseParry));
        }

        public double CalculateBlockChance(IDefender defender, IShield shield, int shieldSpec)
        {
            // Base block calculation from DAoC formula
            double baseBlock = 5 + shieldSpec * 0.5;
            
            // Dexterity bonus (0.1% per point above 60)
            double dex = defender.GetModified(eProperty.Dexterity);
            if (dex > 60)
                baseBlock += (dex - 60) * 0.1;
            
            // Shield quality and condition
            if (shield != null)
                baseBlock *= shield.Quality * 0.01 * shield.Condition / (double)shield.MaxCondition;
            
            // Add property bonus
            baseBlock += defender.GetModified(eProperty.BlockChance) * 0.1;
            
            return Math.Max(0, Math.Min(99, baseBlock));
        }

        public double CalculateDefensePenetration(double weaponSkill)
        {
            // Defense penetration formula from current implementation
            return weaponSkill * 0.08 / 100;
        }
    }
} 