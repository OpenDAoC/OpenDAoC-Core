# OpenDAoC Test Review Guide

This guide provides comprehensive criteria for reviewing tests in the OpenDAoC (Dark Age of Camelot) server emulator project. Every test must validate both functionality and authentic DAoC game mechanics, with clear documentation of what specific rule or behavior is being tested.

## Quick Navigation

1. **Test Quality Standards** - Structure, naming, and clarity
2. **Game Rule Validation Testing** - DAoC mechanics accuracy
3. **Test Documentation Requirements** - What each test validates
4. **Test Structure & Organization** - AAA pattern and categorization
5. **Mock Usage Guidelines** - When and how to mock
6. **Performance Testing Standards** - Critical path validation
7. **Test Data Management** - Builders and realistic scenarios
8. **Integration Testing** - Cross-system validation
9. **Test Coverage Requirements** - Coverage targets and priorities
10. **Common Test Smells** - Anti-patterns to avoid

## Project Testing Context

**Primary Goal**: Validate authentic DAoC gameplay mechanics
**Framework**: NUnit with FluentAssertions and Moq
**Architecture**: Interface-driven with comprehensive mocking
**Critical Requirement**: Every test must document the specific game rule being validated
**Performance Targets**: Combat tests < 1ms, Property tests < 0.5ms

---

## 1. Test Quality Standards

### Test Naming Convention (STRICTLY ENFORCED)
```csharp
// ✅ GOOD: Descriptive test names following pattern
[TestFixture]
public class CombatHitChanceCalculatorTests
{
    [Test]
    public void CalculateHitChance_ShouldReturn82Percent_WhenBaseAttackWith18PercentBaseMiss()
    {
        // Test validates: DAoC Rule - 18% base miss chance (patch 1.117C)
    }
    
    [Test]
    public void CalculateHitChance_ShouldReduceBy1Point33Percent_WhenAttackerLevel1Higher()
    {
        // Test validates: DAoC Rule - Level difference modifier ±1.33% per level (PvE only)
    }
    
    [Test]
    public void CalculateEvadeChance_ShouldHalveChance_WhenTwoAttackers()
    {
        // Test validates: DAoC Rule - Defense chances divided by (attackers / 2)
    }
}

// ❌ BAD: Vague or incomplete test names
public void TestHitChance() { } // What aspect? What rule?
public void Combat_Test1() { }  // No indication of what's being tested
public void ShouldWork() { }    // Completely unhelpful
```

### Required Test Documentation
```csharp
/// <summary>
/// Validates DAoC damage calculation formula for slow weapons.
/// 
/// DAoC Rule: BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponModifier
/// SlowWeaponModifier = 1 + (WeaponSpeed - 20) * 0.003
/// 
/// Reference: Core_Systems_Game_Rules.md - Combat System - Damage Calculation
/// Test Scenario: 2H sword with 45 speed should get slow weapon bonus
/// Expected Result: Damage = 165 * 45 * 0.1 * 1.075 = 798.1875 ≈ 798
/// </summary>
[Test]
public void CalculateBaseDamage_ShouldApplySlowWeaponBonus_WhenWeaponSpeedAbove20()
{
    // Arrange - Exact test scenario documented above
    var weapon = new WeaponBuilder()
        .WithDPS(165)
        .WithSpeed(45) // Slow weapon - triggers bonus
        .Build();
    
    // Act
    var damage = _damageCalculator.CalculateBaseDamage(weapon);
    
    // Assert - Verify exact DAoC formula
    Assert.That(damage, Is.EqualTo(798), 
        "Slow weapon bonus calculation must match DAoC formula exactly");
}
```

### Test Structure Requirements (AAA Pattern)
```csharp
// ✅ GOOD: Clear AAA structure with game rule validation
[Test]
public void CalculateSpecializationPoints_ShouldReturn20Points_WhenLevel10WarriorWithMultiplier20()
{
    // Test validates: DAoC Rule - Spec points = Level * ClassSpecMultiplier / 10
    
    // Arrange
    var warrior = new CharacterBuilder()
        .WithLevel(10)
        .WithClass(new CharacterClassBuilder()
            .WithSpecializationMultiplier(20) // Warrior spec multiplier
            .Build())
        .Build();
    
    // Act
    var specPoints = _specCalculator.CalculateSpecPoints(warrior, 10);
    
    // Assert
    Assert.That(specPoints, Is.EqualTo(20), 
        "Warrior at level 10 should have exactly 20 spec points (10 * 20 / 10)");
}

// ❌ BAD: Unclear structure, missing game rule validation
[Test]
public void TestSpecPoints()
{
    var character = CreateCharacter(); // What kind? What level?
    var result = Calculator.Calculate(character); // What should it return?
    Assert.IsTrue(result > 0); // Weak assertion - doesn't validate rule
}
```

---

## 2. Game Rule Validation Testing

### Combat Mechanics Testing
```csharp
[TestFixture]
public class CombatAttackResolutionTests
{
    /// <summary>
    /// Validates correct DAoC attack resolution order.
    /// 
    /// DAoC Rule: Attack resolution order is:
    /// 1. Intercept, 2. Evade, 3. Parry, 4. Block, 5. Guard, 6. Miss, 7. Bladeturn
    /// 
    /// Reference: Core_Systems_Game_Rules.md - Combat System - Attack Resolution Order
    /// </summary>
    [Test]
    public void ProcessAttack_ShouldCheckEvadeBeforeParry_WhenBothAreAvailable()
    {
        // Arrange
        var defender = new DefenderBuilder()
            .WithEvadeChance(1.0) // 100% evade chance
            .WithParryChance(1.0) // 100% parry chance - should not be reached
            .Build();
        
        var attacker = CreateStandardAttacker();
        
        // Act
        var result = _combatSystem.ProcessAttack(attacker, defender, new AttackContext());
        
        // Assert
        Assert.That(result.Type, Is.EqualTo(AttackResult.Evaded), 
            "Evade check must occur before parry check in DAoC resolution order");
        
        // Verify parry was never checked due to evade success
        Mock.Get(defender).Verify(d => d.TryParry(It.IsAny<AttackData>()), Times.Never,
            "Parry should not be checked when evade succeeds");
    }
}
```

### Character Progression Testing
```csharp
[TestFixture]
public class CharacterStatProgressionTests
{
    /// <summary>
    /// Validates DAoC stat progression rules for primary/secondary/tertiary stats.
    /// 
    /// DAoC Rules:
    /// - Primary stat: +1 per level starting at level 6
    /// - Secondary stat: +1 every 2 levels starting at level 6  
    /// - Tertiary stat: +1 every 3 levels starting at level 6
    /// 
    /// Reference: Core_Systems_Game_Rules.md - Character Progression - Stat Progression
    /// </summary>
    [TestCase(1, 0, 0, 0, TestName = "Level 1-5: No stat gains")]
    [TestCase(6, 1, 1, 1, TestName = "Level 6: All stats gain 1 point")]
    [TestCase(7, 2, 1, 1, TestName = "Level 7: Only primary stat gains")]
    [TestCase(8, 3, 2, 1, TestName = "Level 8: Primary and secondary gain")]
    [TestCase(9, 4, 2, 2, TestName = "Level 9: Primary and tertiary gain")]
    [TestCase(10, 5, 3, 2, TestName = "Level 10: Primary and secondary gain")]
    public void CalculateStatGains_ShouldFollowDAoCProgression_WhenLevelingFromLevel1(
        int targetLevel, int expectedPrimary, int expectedSecondary, int expectedTertiary)
    {
        // Arrange
        var characterClass = new CharacterClassBuilder()
            .WithPrimaryStat(Stat.Strength)
            .WithSecondaryStat(Stat.Constitution) 
            .WithTertiaryStat(Stat.Dexterity)
            .Build();
            
        var character = new CharacterBuilder()
            .WithClass(characterClass)
            .WithLevel(1)
            .Build();
        
        // Act - Level up from 1 to target level
        _progressionService.LevelUp(character, targetLevel);
        
        // Assert - Verify exact DAoC stat progression
        var strGain = character.GetStatGain(Stat.Strength);
        var conGain = character.GetStatGain(Stat.Constitution);
        var dexGain = character.GetStatGain(Stat.Dexterity);
        
        Assert.Multiple(() =>
        {
            Assert.That(strGain, Is.EqualTo(expectedPrimary), 
                $"Primary stat (STR) should gain {expectedPrimary} points by level {targetLevel}");
            Assert.That(conGain, Is.EqualTo(expectedSecondary), 
                $"Secondary stat (CON) should gain {expectedSecondary} points by level {targetLevel}");
            Assert.That(dexGain, Is.EqualTo(expectedTertiary), 
                $"Tertiary stat (DEX) should gain {expectedTertiary} points by level {targetLevel}");
        });
    }
}
```

### Item System Testing
```csharp
[TestFixture]
public class ItemBonusCapTests
{
    /// <summary>
    /// Validates DAoC item bonus caps by character level.
    /// 
    /// DAoC Rules:
    /// - Level 1-14: 0 bonus cap
    /// - Level 15-19: 5 bonus cap
    /// - Level 20-24: 10 bonus cap
    /// - Level 25-29: 15 bonus cap
    /// - Level 30-34: 20 bonus cap
    /// - Level 35-39: 25 bonus cap
    /// - Level 40-44: 30 bonus cap
    /// - Level 45+: 35 bonus cap
    /// 
    /// Reference: Core_Systems_Game_Rules.md - Item and Equipment System - Bonus System
    /// </summary>
    [TestCase(10, 0, TestName = "Level 10: No bonus cap")]
    [TestCase(15, 5, TestName = "Level 15: 5 point bonus cap")]
    [TestCase(20, 10, TestName = "Level 20: 10 point bonus cap")]
    [TestCase(45, 35, TestName = "Level 45: 35 point bonus cap")]
    [TestCase(50, 35, TestName = "Level 50: Still 35 point bonus cap")]
    public void GetBonusCapForLevel_ShouldReturnCorrectCap_ForDAoCLevelRanges(int level, int expectedCap)
    {
        // Act
        var actualCap = _bonusCalculator.GetBonusCapForLevel(level);
        
        // Assert
        Assert.That(actualCap, Is.EqualTo(expectedCap), 
            $"Level {level} must have {expectedCap} bonus cap according to DAoC rules");
    }
    
    /// <summary>
    /// Validates that item bonuses are properly capped when exceeding level limits.
    /// 
    /// DAoC Rule: Item bonuses cannot exceed the character's level-based cap
    /// Test Scenario: Level 20 character with 15 STR bonus should be capped at 10
    /// </summary>
    [Test]
    public void CalculateEffectiveBonuses_ShouldCapAtLevelLimit_WhenItemBonusExceedsLevelCap()
    {
        // Arrange
        var character = new CharacterBuilder().WithLevel(20).Build(); // 10 point cap
        var items = new[]
        {
            new ItemBuilder().WithBonus(Property.Strength, 15).Build() // Exceeds cap
        };
        
        // Act
        var effectiveBonuses = _bonusCalculator.CalculateEffectiveBonuses(character, items);
        
        // Assert
        Assert.That(effectiveBonuses[Property.Strength], Is.EqualTo(10), 
            "STR bonus of 15 should be capped at 10 for level 20 character");
    }
}
```

---

## 3. Test Documentation Requirements

### Mandatory Test Documentation Elements
```csharp
/// <summary>
/// Brief description of what game rule or functionality is being tested
/// 
/// DAoC Rule: [Specific rule from game documentation]
/// Reference: [Link to Core_Systems_Game_Rules.md section]
/// Test Scenario: [Specific setup being tested]
/// Expected Result: [Exact expected outcome with calculations if applicable]
/// </summary>
[Test]
public void TestMethodName_ShouldExpectedBehavior_WhenSpecificCondition()
{
    // Implementation
}
```

### Documentation Categories Required

#### Combat Tests
```csharp
/// <summary>
/// Validates parry chance reduction for multiple attackers.
/// 
/// DAoC Rule: Defense chances are divided by (number of attackers / 2)
/// Reference: Core_Systems_Game_Rules.md - Combat System - Defense Mechanics - Parry
/// Test Scenario: Defender with 30% base parry vs 2 attackers
/// Expected Result: 30% / (2/2) = 30% / 1 = 30% (no reduction for 2 attackers)
/// vs 4 attackers: 30% / (4/2) = 30% / 2 = 15%
/// </summary>
```

#### Character Progression Tests  
```csharp
/// <summary>
/// Validates experience table matches official DAoC progression.
/// 
/// DAoC Rule: Experience requirements per level from official tables
/// Reference: Core_Systems_Game_Rules.md - Character Progression - Experience System
/// Test Data: Level 10 = 51,200 XP, Level 20 = 1,638,400 XP, Level 50 = 1,073,741,824 XP
/// Expected Result: Exact match to official DAoC experience table values
/// </summary>
```

#### Item System Tests
```csharp
/// <summary>
/// Validates weapon damage calculation with quality modifier.
/// 
/// DAoC Rule: Effective DPS = Base DPS * (Quality / 100)
/// Reference: Core_Systems_Game_Rules.md - Item and Equipment System - Item Properties
/// Test Scenario: 165 DPS weapon at 94% quality
/// Expected Result: 165 * 0.94 = 155.1 effective DPS
/// </summary>
```

---

## 4. Test Structure & Organization

### Test Class Organization
```csharp
// ✅ GOOD: Organized by system and functionality
[TestFixture]
public class CombatDamageCalculationTests
{
    #region Setup
    private IDamageCalculator _damageCalculator;
    private Mock<IWeapon> _mockWeapon;
    
    [SetUp]
    public void Setup()
    {
        _damageCalculator = new DamageCalculator();
        _mockWeapon = new Mock<IWeapon>();
    }
    #endregion
    
    #region Base Damage Tests
    [Test]
    public void CalculateBaseDamage_ShouldUseCorrectFormula_WhenStandardWeapon() { }
    
    [Test] 
    public void CalculateBaseDamage_ShouldApplySlowWeaponBonus_WhenSpeedAbove20() { }
    #endregion
    
    #region Weapon Skill Modifier Tests
    [Test]
    public void CalculateWeaponSkillModifier_ShouldIncludeInherentSkill_Always() { }
    #endregion
    
    #region Integration Tests
    [Test]
    public void CalculateFinalDamage_ShouldCombineAllModifiers_WhenFullCombatScenario() { }
    #endregion
}

// ❌ BAD: Mixed concerns and poor organization
[TestFixture]
public class GameTests // Too broad
{
    [Test]
    public void TestDamage() { } // Vague
    
    [Test] 
    public void TestInventory() { } // Different system - should be separate class
    
    [Test]
    public void TestGuild() { } // Another different system
}
```

### Test Categories and Attributes
```csharp
// ✅ GOOD: Proper categorization for different test types
[TestFixture]
[Category("Combat")]
[Category("Core")]
public class CombatSystemTests
{
    [Test]
    [Category("Performance")]
    [Category("Critical")]
    public void ProcessAttack_ShouldComplete_WithinOneMillisecond()
    {
        // Performance-critical test with timing validation
    }
    
    [Test]
    [Category("GameRules")]
    [Category("Combat")]
    public void ProcessAttack_ShouldFollowDAoCAttackOrder_Always()
    {
        // Game rule validation test
    }
    
    [Test]
    [Category("Integration")]
    public void CombatWithMultipleSystems_ShouldMaintainConsistency_WhenCrossSystemInteraction()
    {
        // Integration test across multiple systems
    }
}
```

---

## 5. Mock Usage Guidelines

### When to Mock vs Use Real Objects
```csharp
// ✅ GOOD: Mock external dependencies, use real value objects
[Test]
public void ProcessAttack_ShouldCalculateCorrectDamage_WhenValidAttack()
{
    // Mock external dependencies
    var mockAttacker = new Mock<IAttacker>();
    var mockDefender = new Mock<IDefender>();
    var mockDamageCalculator = new Mock<IDamageCalculator>();
    
    // Use real value objects
    var attackData = new AttackData
    {
        AttackType = AttackType.Melee,
        DamageType = DamageType.Slash
    };
    
    var attackContext = new AttackContext
    {
        AttackerCount = 1,
        IsPvP = false
    };
    
    // Test implementation
}

// ❌ BAD: Mocking value objects or over-mocking
[Test]
public void BadTest()
{
    var mockAttackData = new Mock<AttackData>(); // Value object - use real instance
    var mockAttackContext = new Mock<AttackContext>(); // Value object - use real instance
    var mockString = new Mock<string>(); // Primitive - never mock
}
```

### Mock Verification for Behavior Testing
```csharp
// ✅ GOOD: Verify behavior and interaction patterns
[Test]
public void ProcessAttack_ShouldCheckDefensesInCorrectOrder_WhenProcessingAttack()
{
    // Test validates: DAoC Rule - Attack resolution order
    
    // Arrange
    var mockDefender = new Mock<IDefender>();
    var sequence = new MockSequence();
    
    // Setup ordered expectations matching DAoC attack resolution
    mockDefender.InSequence(sequence).Setup(d => d.TryEvade(It.IsAny<AttackData>())).Returns(false);
    mockDefender.InSequence(sequence).Setup(d => d.TryParry(It.IsAny<AttackData>())).Returns(false);
    mockDefender.InSequence(sequence).Setup(d => d.TryBlock(It.IsAny<AttackData>())).Returns(false);
    
    // Act
    _combatSystem.ProcessAttack(CreateAttacker(), mockDefender.Object, new AttackContext());
    
    // Assert - Verify correct order was followed
    mockDefender.Verify(d => d.TryEvade(It.IsAny<AttackData>()), Times.Once);
    mockDefender.Verify(d => d.TryParry(It.IsAny<AttackData>()), Times.Once);
    mockDefender.Verify(d => d.TryBlock(It.IsAny<AttackData>()), Times.Once);
}
```

---

## 6. Performance Testing Standards

### Critical Path Performance Tests
```csharp
[TestFixture]
[Category("Performance")]
public class CombatPerformanceTests
{
    /// <summary>
    /// Validates combat calculation performance meets server requirements.
    /// 
    /// Performance Requirement: Combat calculations must complete within 1ms
    /// Rationale: Server must handle hundreds of concurrent combats
    /// Test Method: 1000 iterations to ensure consistent performance
    /// </summary>
    [Test]
    public void ProcessAttack_ShouldComplete_WithinOneMillisecondTarget()
    {
        // Arrange
        var attacker = CreateOptimalAttacker();
        var defender = CreateOptimalDefender();
        var context = new AttackContext();
        
        // Warm up JIT
        for (int i = 0; i < 100; i++)
        {
            _combatSystem.ProcessAttack(attacker, defender, context);
        }
        
        // Act & Assert - Test performance over many iterations
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            _combatSystem.ProcessAttack(attacker, defender, context);
        }
        
        stopwatch.Stop();
        
        var averageTime = stopwatch.ElapsedMilliseconds / 1000.0;
        Assert.That(averageTime, Is.LessThan(1.0), 
            $"Average combat calculation time ({averageTime:F3}ms) exceeds 1ms target");
    }
}
```

### Memory Allocation Testing
```csharp
/// <summary>
/// Validates combat system doesn't create excessive allocations.
/// 
/// Performance Requirement: Minimize allocations in hot paths
/// Test Method: Monitor GC pressure during combat operations
/// </summary>
[Test]
public void ProcessAttack_ShouldMinimizeAllocations_WhenProcessingManyAttacks()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(true);
    var attacker = CreateAttacker();
    var defender = CreateDefender();
    
    // Act - Process many attacks
    for (int i = 0; i < 10000; i++)
    {
        _combatSystem.ProcessAttack(attacker, defender, new AttackContext());
    }
    
    // Assert - Check memory usage didn't grow excessively
    var finalMemory = GC.GetTotalMemory(false);
    var memoryGrowth = finalMemory - initialMemory;
    
    Assert.That(memoryGrowth, Is.LessThan(1024 * 1024), // Less than 1MB growth
        $"Memory growth ({memoryGrowth} bytes) indicates excessive allocations");
}
```

---

## 7. Test Data Management

### Fluent Test Builders
```csharp
// ✅ GOOD: Realistic test data using fluent builders
public class CharacterBuilder
{
    private readonly MockCharacter _character = new MockCharacter();
    
    public CharacterBuilder WithLevel(int level)
    {
        _character.Level = level;
        return this;
    }
    
    public CharacterBuilder WithClass(CharacterClass characterClass)
    {
        _character.Class = characterClass;
        return this;
    }
    
    public CharacterBuilder AsWarrior()
    {
        return WithClass(new CharacterClassBuilder()
            .WithName("Warrior")
            .WithType(ClassType.Tank)
            .WithSpecializationMultiplier(20)
            .WithPrimaryStat(Stat.Strength)
            .Build());
    }
    
    public CharacterBuilder WithRealisticCombatStats()
    {
        return this
            .WithStats(strength: 100, constitution: 85, dexterity: 60)
            .WithWeaponSkill(WeaponType.Sword, 350)
            .WithSpecialization("Slash", 50);
    }
    
    public MockCharacter Build() => _character;
}

// Usage in tests
var warrior = new CharacterBuilder()
    .WithLevel(50)
    .AsWarrior()
    .WithRealisticCombatStats()
    .Build();
```

### Scenario-Based Test Data
```csharp
// ✅ GOOD: Pre-defined scenarios for common test cases
public static class CombatScenarios
{
    /// <summary>
    /// Classic DAoC scenario: Level 50 Warrior vs Level 50 Warrior
    /// Represents typical end-game PvP encounter
    /// </summary>
    public static (IAttacker attacker, IDefender defender) Level50WarriorVsWarrior()
    {
        var attackerWeapon = new WeaponBuilder()
            .WithDPS(165)
            .WithSpeed(37)
            .WithType(WeaponType.Sword)
            .Build();
            
        var attacker = new CharacterBuilder()
            .WithLevel(50)
            .AsWarrior()
            .WithWeapon(attackerWeapon)
            .WithSpecialization("Slash", 50)
            .Build();
            
        var defender = new CharacterBuilder()
            .WithLevel(50)
            .AsWarrior() 
            .WithSpecialization("Parry", 35)
            .WithSpecialization("Shield", 40)
            .Build();
            
        return (attacker, defender);
    }
    
    /// <summary>
    /// Low level PvE scenario: Level 10 character vs Level 8 mob
    /// Tests level difference calculations in PvE context
    /// </summary>
    public static (IAttacker attacker, IDefender defender) Level10VsLevel8Mob()
    {
        // Implementation with appropriate level difference
    }
}
```

---

## 8. Integration Testing Guidelines

### Cross-System Integration Tests
```csharp
[TestFixture]
[Category("Integration")]
public class CombatPropertyIntegrationTests
{
    /// <summary>
    /// Validates combat system integrates correctly with property calculator system.
    /// 
    /// Integration Test: Combat damage should reflect character's modified stats
    /// including item bonuses, buffs, and debuffs through property system
    /// 
    /// DAoC Rule: All stat modifiers affect combat calculations
    /// </summary>
    [Test]
    public void CombatDamage_ShouldReflectPropertyCalculations_WhenCharacterHasStatModifiers()
    {
        // Arrange - Character with multiple stat sources
        var character = new CharacterBuilder()
            .WithLevel(50)
            .WithBaseStrength(100)
            .Build();
            
        // Add item bonus
        var strengthRing = new ItemBuilder()
            .WithBonus(Property.Strength, 15)
            .Build();
        character.Inventory.Equip(strengthRing, EquipmentSlot.Ring1);
        
        // Add buff effect
        var strengthBuff = new EffectBuilder()
            .WithProperty(Property.Strength)
            .WithValue(25)
            .WithType(EffectType.Buff)
            .Build();
        _effectService.AddEffect(character, strengthBuff);
        
        // Ensure property system calculates total strength correctly
        var totalStrength = _propertyService.GetModifiedValue(character, Property.Strength);
        Assert.That(totalStrength, Is.EqualTo(140), // 100 + 15 + 25
            "Property system should calculate total strength including all modifiers");
        
        // Act - Combat damage should use modified strength
        var baseDamage = CalculateBaseDamageWithoutModifiers();
        var actualDamage = _combatSystem.CalculateDamage(character, CreateStandardDefender());
        
        // Assert - Verify damage reflects strength bonus
        var expectedIncrease = CalculateStrengthDamageBonus(40); // 40 bonus strength
        Assert.That(actualDamage, Is.EqualTo(baseDamage + expectedIncrease).Within(1),
            "Combat damage must reflect property-calculated strength bonuses");
    }
}
```

### Database Integration Tests
```csharp
[TestFixture]
[Category("Integration")]
[Category("Database")]
public class CharacterPersistenceIntegrationTests
{
    /// <summary>
    /// Validates character progression data persists correctly across save/load cycles.
    /// 
    /// Integration Test: Character progression service with database repository
    /// DAoC Requirement: All character progress must be accurately preserved
    /// </summary>
    [Test]
    public void CharacterProgression_ShouldPersistCorrectly_WhenSavedAndLoaded()
    {
        // Arrange - Character with progression
        var originalCharacter = new CharacterBuilder()
            .WithLevel(25)
            .WithExperience(512000)
            .WithSpecialization("Slash", 35)
            .WithSpecialization("Parry", 20)
            .Build();
            
        // Act - Save and reload character
        _characterRepository.Save(originalCharacter);
        var loadedCharacter = _characterRepository.GetById(originalCharacter.ID);
        
        // Assert - All progression data preserved
        Assert.Multiple(() =>
        {
            Assert.That(loadedCharacter.Level, Is.EqualTo(25), "Level must be preserved");
            Assert.That(loadedCharacter.Experience, Is.EqualTo(512000), "Experience must be preserved");
            Assert.That(loadedCharacter.GetSpecializationLevel("Slash"), Is.EqualTo(35), 
                "Specialization levels must be preserved");
            Assert.That(loadedCharacter.GetSpecializationLevel("Parry"), Is.EqualTo(20), 
                "All specializations must be preserved");
        });
    }
}
```

---

## 9. Test Coverage Requirements

### Coverage Targets by System Priority

#### Critical Systems (95% Coverage Required)
- **Combat System**: All attack resolution, damage calculation, defense mechanics
- **Character Progression**: Leveling, experience, specialization points  
- **Property Calculations**: All stat modifiers, bonuses, caps

#### Core Systems (85% Coverage Required)  
- **Item System**: Equipment bonuses, quality effects, durability
- **Guild System**: Permissions, ranks, member management
- **Spell System**: Casting, damage, effects

#### Supporting Systems (70% Coverage Required)
- **Housing System**: Permissions, decorations, rent
- **Crafting System**: Recipes, quality, success rates
- **Quest System**: Progress tracking, rewards

### Coverage Verification
```csharp
// ✅ GOOD: Verify coverage of critical game rules
[TestFixture]
public class CombatCoverageValidationTests
{
    /// <summary>
    /// Ensures all DAoC combat formulas have corresponding test coverage.
    /// 
    /// This test validates we have specific tests for each documented formula
    /// in Core_Systems_Game_Rules.md Combat System section.
    /// </summary>
    [Test]
    public void CombatTests_ShouldCoverAllDAoCFormulas_FromGameRulesDocumentation()
    {
        var requiredCombatTests = new[]
        {
            "Hit/Miss calculation with 18% base miss",
            "Level difference modifier ±1.33% per level",
            "Multi-attacker penalty -0.5% per additional",
            "Weapon skill calculation with inherent bonus",
            "Damage formula with slow weapon modifier",
            "Evade chance with multiple attackers",
            "Parry chance with two-handed weapon penalty",
            "Block chance with shield size limits",
            "Attack resolution order validation"
        };
        
        foreach (var requiredTest in requiredCombatTests)
        {
            var testExists = DoesTestExistForScenario(requiredTest);
            Assert.That(testExists, Is.True, 
                $"Missing test coverage for critical DAoC rule: {requiredTest}");
        }
    }
}
```

---

## 10. Common Test Smells & Anti-Patterns

### Test Smells to Avoid

#### Vague or Unclear Tests
```csharp
// ❌ BAD: Unclear what's being tested
[Test]
public void TestCombat()
{
    var result = _combat.DoSomething();
    Assert.IsTrue(result.Success);
}

// ✅ GOOD: Clear, specific test with documented rule
[Test]
public void CalculateEvadeChance_ShouldReturn25Percent_WhenDexQuiIs150AndNoAbilityLevel()
{
    // Test validates: DAoC Rule - Base Evade = ((Dex + Qui) / 2 - 50) * 0.05
    // Scenario: (150 / 2 - 50) * 0.05 = (75 - 50) * 0.05 = 25 * 0.05 = 1.25%
}
```

#### Magic Numbers Without Context
```csharp
// ❌ BAD: Magic numbers with no explanation
[Test]
public void TestDamage()
{
    var result = _calculator.Calculate(165, 37);
    Assert.That(result, Is.EqualTo(798)); // What do these numbers mean?
}

// ✅ GOOD: Documented values with DAoC context
[Test]
public void CalculateBaseDamage_ShouldReturn798_WhenMyrmidonSlashSpecSwordUsed()
{
    // Test validates: DAoC Rule - BaseDamage = DPS * Speed * 0.1 * SlowWeaponModifier
    // Scenario: Mymidon Slash Spec Sword (DPS: 165, Speed: 45)
    // Calculation: 165 * 45 * 0.1 * 1.075 = 798.1875 ≈ 798
    
    var myrmidonSword = new WeaponBuilder()
        .WithDPS(165)      // Standard Mymidon DPS
        .WithSpeed(45)     // Slow weapon - triggers bonus
        .Build();
}
```

#### Over-Mocking or Under-Mocking
```csharp
// ❌ BAD: Over-mocking value objects
[Test] 
public void BadTest()
{
    var mockLevel = new Mock<int>(); // Never mock primitives
    var mockAttackData = new Mock<AttackData>(); // Value object - use real instance
}

// ❌ BAD: Under-mocking dependencies
[Test]
public void BadTest()
{
    var realDatabase = new SqlDatabase(); // External dependency - should be mocked
    var realNetworkClient = new GameClient(); // External system - should be mocked
}

// ✅ GOOD: Appropriate mocking strategy
[Test]
public void GoodTest()
{
    // Mock external dependencies
    var mockRepository = new Mock<ICharacterRepository>();
    var mockLogger = new Mock<ILogger>();
    
    // Use real value objects
    var attackData = new AttackData { /* real data */ };
    var character = new CharacterBuilder().Build(); // Builder creates real mock object
}
```

#### Testing Implementation Instead of Behavior
```csharp
// ❌ BAD: Testing internal implementation details
[Test]
public void BadTest()
{
    _combatSystem.ProcessAttack(attacker, defender, context);
    
    // Testing internal method calls instead of behavior
    Mock.Get(_combatSystem).Verify(x => x.InternalCalculateMethod(), Times.Once);
}

// ✅ GOOD: Testing observable behavior and DAoC rules
[Test]
public void ProcessAttack_ShouldReturnEvadeResult_WhenDefenderSuccessfullyEvades()
{
    // Arrange - Setup for guaranteed evade
    var defender = new DefenderBuilder()
        .WithEvadeChance(1.0) // 100% evade chance
        .Build();
    
    // Act
    var result = _combatSystem.ProcessAttack(attacker, defender, context);
    
    // Assert - Test observable outcome
    Assert.That(result.Type, Is.EqualTo(AttackResult.Evaded),
        "Attack should result in evade when defender has 100% evade chance");
}
```

---

## Test Review Checklist Template

### Test Quality ✅
- [ ] Test name clearly describes what is being tested
- [ ] Test follows AAA (Arrange-Act-Assert) pattern
- [ ] Test has proper XML documentation explaining the DAoC rule
- [ ] Test uses appropriate categories and attributes
- [ ] Assertions are specific and meaningful

### Game Rule Validation ✅
- [ ] Test validates a specific DAoC game mechanic or formula
- [ ] Expected results match documented DAoC behavior exactly
- [ ] Test references Core_Systems_Game_Rules.md documentation
- [ ] Test scenarios are realistic and representative
- [ ] Edge cases and boundary conditions are tested

### Test Documentation ✅
- [ ] Clear documentation of what DAoC rule is being tested
- [ ] Reference to specific section in game rules documentation
- [ ] Test scenario clearly explained with expected calculations
- [ ] Any special setup or conditions documented
- [ ] Performance expectations documented for critical tests

### Mock Usage ✅
- [ ] Mocks used only for external dependencies
- [ ] Value objects use real instances, not mocks
- [ ] Mock setup is clear and focused
- [ ] Behavior verification is meaningful
- [ ] No over-mocking or under-mocking

### Performance & Integration ✅
- [ ] Performance tests validate critical timing requirements
- [ ] Integration tests cover cross-system interactions
- [ ] Database integration tests use appropriate isolation
- [ ] Memory allocation tests monitor resource usage
- [ ] Performance targets clearly documented

### Coverage & Organization ✅
- [ ] Test contributes to appropriate coverage targets
- [ ] Test is properly categorized by system and priority
- [ ] Test class organization follows project standards
- [ ] Related tests are grouped logically
- [ ] Test data builders used appropriately

---

## Conclusion

This test review guide ensures OpenDAoC tests validate both functionality and authentic DAoC game mechanics. Every test must clearly document what specific rule or behavior it validates, use realistic scenarios, and contribute to maintaining the authentic DAoC experience.

**Remember: Tests are documentation of how DAoC should work. Make them clear, accurate, and comprehensive.**

For detailed game rules and testing infrastructure, refer to:
- `Core_Systems_Game_Rules.md` for DAoC mechanics to test
- `Core_Systems_Testing_Framework.md` for testing infrastructure
- `Core_Systems_Interface_Design.md` for mock implementations 