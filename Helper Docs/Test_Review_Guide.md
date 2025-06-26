# OpenDAoC Test Review Guide

This guide provides comprehensive criteria for reviewing tests in the OpenDAoC (Dark Age of Camelot) server emulator project. Every test must validate both functionality and authentic DAoC game mechanics as specified in the SRD, with clear documentation of what specific rule or behavior is being tested.

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

**Primary Goal**: Validate authentic DAoC gameplay mechanics per SRD specifications
**Framework**: NUnit with FluentAssertions and Moq
**Architecture**: Interface-driven with comprehensive mocking
**Critical Requirement**: Every test must validate specific SRD-documented mechanics
**Performance Targets**: Combat tests < 1ms, Property tests < 0.5ms
**Reference Documentation**: SRD (System Reference Document) for all game mechanics

---

## 1. Test Quality Standards

### Test Naming Convention (STRICTLY ENFORCED)
```csharp
// ✅ GOOD: Descriptive test names following pattern
[TestFixture]
public class CombatHitChanceCalculatorTests
{
    [Test]
    public void CalculateHitChance_ShouldReturn85Percent_WhenBaseAttackWith15PercentBaseMiss()
    {
        // Test validates: SRD/01_Combat_Systems/Attack_Resolution.md - Base Miss Chance
    }
    
    [Test]
    public void CalculateHitChance_ShouldReduceByPoint33Percent_WhenAttackerLevel1Higher()
    {
        // Test validates: SRD/01_Combat_Systems/Attack_Resolution.md - Level Difference
    }
    
    [Test]
    public void CalculateEvadeChance_ShouldHalveChance_WhenTwoAttackers()
    {
        // Test validates: SRD/01_Combat_Systems/Defense_Mechanics.md - Multi-Attacker Rules
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
/// Validates damage calculation formula from SRD for slow weapons.
/// 
/// SRD Reference: SRD/01_Combat_Systems/Damage_Calculation.md - Slow Weapon Bonus
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
    
    // Assert - Verify exact SRD formula
    Assert.That(damage, Is.EqualTo(798), 
        "Slow weapon bonus calculation must match SRD formula exactly");
}
```

### Test Structure Requirements (AAA Pattern)
```csharp
// ✅ GOOD: Clear AAA structure with SRD validation
[Test]
public void CalculateSpecializationPoints_ShouldReturn20Points_WhenLevel10WarriorWithMultiplier20()
{
    // Test validates: SRD/02_Character_Systems/Specialization_Points.md
    
    // Arrange
    var warrior = new CharacterBuilder()
        .WithLevel(10)
        .WithClass(new CharacterClassBuilder()
            .WithSpecializationMultiplier(20) // Warrior spec multiplier per SRD
            .Build())
        .Build();
    
    // Act
    var specPoints = _specCalculator.CalculateSpecPoints(warrior, 10);
    
    // Assert
    Assert.That(specPoints, Is.EqualTo(20), 
        "Warrior at level 10 should have exactly 20 spec points per SRD formula");
}

// ❌ BAD: Unclear structure, missing SRD validation
[Test]
public void TestSpecPoints()
{
    var character = CreateCharacter(); // What kind? What level?
    var result = Calculator.Calculate(character); // What should it return?
    Assert.IsTrue(result > 0); // Weak assertion - doesn't validate SRD rule
}
```

---

## 2. Game Rule Validation Testing

### SRD Compliance Requirements
All tests MUST validate mechanics as specified in the OpenDAoC System Reference Document (SRD).

### Combat Mechanics Testing
```csharp
[TestFixture]
public class CombatAttackResolutionTests
{
    /// <summary>
    /// Validates correct attack resolution order per SRD.
    /// 
    /// SRD Reference: SRD/01_Combat_Systems/Attack_Resolution.md - Attack Resolution Order
    /// Expected Order: Intercept → Evade → Parry → Block → Guard → Miss → Bladeturn
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
            "Evade check must occur before parry check per SRD resolution order");
        
        // Verify parry was never checked due to evade success
        Mock.Get(defender).Verify(d => d.TryParry(It.IsAny<AttackData>()), Times.Never,
            "Parry should not be checked when evade succeeds per SRD");
    }
}
```

### Character Progression Testing
```csharp
[TestFixture]
public class CharacterStatProgressionTests
{
    /// <summary>
    /// Validates stat progression rules from SRD.
    /// 
    /// SRD Reference: SRD/02_Character_Systems/Stat_Systems.md - Stat Progression
    /// Primary stat: +1 per level starting at level 6
    /// Secondary stat: +1 every 2 levels starting at level 6  
    /// Tertiary stat: +1 every 3 levels starting at level 6
    /// </summary>
    [TestCase(1, 0, 0, 0, TestName = "Level 1-5: No stat gains per SRD")]
    [TestCase(6, 1, 1, 1, TestName = "Level 6: All stats gain 1 point per SRD")]
    [TestCase(7, 2, 1, 1, TestName = "Level 7: Only primary stat gains per SRD")]
    [TestCase(8, 3, 2, 1, TestName = "Level 8: Primary and secondary gain per SRD")]
    [TestCase(9, 4, 2, 2, TestName = "Level 9: Primary and tertiary gain per SRD")]
    [TestCase(10, 5, 3, 2, TestName = "Level 10: Primary and secondary gain per SRD")]
    public void CalculateStatGains_ShouldFollowSRDProgression_WhenLevelingFromLevel1(
        int targetLevel, int expectedPrimary, int expectedSecondary, int expectedTertiary)
    {
        // Test implementation following SRD specifications
    }
}
```

### Item System Testing
```csharp
[TestFixture]
public class ItemBonusCapTests
{
    /// <summary>
    /// Validates item bonus caps from SRD.
    /// 
    /// SRD Reference: SRD/04_Item_Systems/Bonus_Systems.md - Level-Based Caps
    /// </summary>
    [TestCase(10, 0, TestName = "Level 10: No bonus cap per SRD")]
    [TestCase(15, 5, TestName = "Level 15: 5 point bonus cap per SRD")]
    [TestCase(20, 10, TestName = "Level 20: 10 point bonus cap per SRD")]
    [TestCase(45, 35, TestName = "Level 45: 35 point bonus cap per SRD")]
    [TestCase(50, 35, TestName = "Level 50: Still 35 point bonus cap per SRD")]
    public void GetBonusCapForLevel_ShouldReturnCorrectCap_ForSRDLevelRanges(int level, int expectedCap)
    {
        // Act
        var actualCap = _bonusCalculator.GetBonusCapForLevel(level);
        
        // Assert
        Assert.That(actualCap, Is.EqualTo(expectedCap), 
            $"Level {level} must have {expectedCap} bonus cap according to SRD");
    }
}
```

---

## 3. Test Documentation Requirements

### Mandatory Test Documentation Elements
```csharp
/// <summary>
/// Brief description of what SRD rule or functionality is being tested
/// 
/// SRD Reference: [Path to specific SRD document and section]
/// Test Scenario: [Specific setup being tested]
/// Expected Result: [Exact expected outcome per SRD with calculations if applicable]
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
/// SRD Reference: SRD/01_Combat_Systems/Defense_Mechanics.md - Parry Multi-Attacker Rules
/// Test Scenario: Defender with 30% base parry vs 2 attackers
/// Expected Result: 30% / (2/2) = 30% / 1 = 30% (no reduction for 2 attackers)
/// vs 4 attackers: 30% / (4/2) = 30% / 2 = 15%
/// </summary>
```

#### Character Progression Tests  
```csharp
/// <summary>
/// Validates experience table from SRD.
/// 
/// SRD Reference: SRD/02_Character_Systems/Character_Progression.md - Experience Table
/// Test Data: Level 10 = 51,200 XP, Level 20 = 1,638,400 XP, Level 50 = 1,073,741,824 XP
/// Expected Result: Exact match to SRD experience table values
/// </summary>
```

#### Item System Tests
```csharp
/// <summary>
/// Validates weapon damage calculation with quality modifier.
/// 
/// SRD Reference: SRD/04_Item_Systems/Quality_Effects.md - Quality Damage Modifier
/// Test Scenario: 165 DPS weapon at 94% quality
/// Expected Result: 165 * 0.94 = 155.1 effective DPS per SRD formula
/// </summary>
```

---

## 4. Performance Testing Standards

### Critical Path Performance Tests
```csharp
[TestFixture]
[Category("Performance")]
public class CombatPerformanceTests
{
    /// <summary>
    /// Validates combat calculation performance meets SRD requirements.
     
     
    /// SRD Reference: SRD/09_Performance_Systems/Server_Mechanics.md - Combat Performance
    /// Requirement: Combat calculations must complete within 1ms
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
            $"Average combat calculation time ({averageTime:F3}ms) exceeds SRD 1ms target");
    }
}
```

---

## 5. Test Review Checklist Template

### Test Quality ✅
- [ ] Test name clearly describes what is being tested
- [ ] Test follows AAA (Arrange-Act-Assert) pattern
- [ ] Test has proper XML documentation with SRD reference
- [ ] Test uses appropriate categories and attributes
- [ ] Assertions are specific and meaningful

### Game Rule Validation ✅
- [ ] **SRD Reference**: Test references specific SRD document and section
- [ ] **SRD Accuracy**: Expected results match SRD specifications exactly
- [ ] **Formula Validation**: Calculations follow SRD formulas precisely
- [ ] **Edge Cases**: Tests cover SRD-documented edge cases
- [ ] **Scenario Coverage**: Realistic scenarios based on SRD mechanics

### Test Documentation ✅
- [ ] Clear documentation of what SRD rule is being tested
- [ ] Reference to specific SRD document path and section
- [ ] Test scenario clearly explained with SRD context
- [ ] Expected calculations match SRD formulas
- [ ] Performance expectations documented per SRD requirements

### Mock Usage ✅
- [ ] Mocks used only for external dependencies
- [ ] Value objects use real instances, not mocks
- [ ] Mock setup is clear and focused
- [ ] Behavior verification is meaningful
- [ ] No over-mocking or under-mocking

### Performance & Integration ✅
- [ ] Performance tests validate SRD timing requirements
- [ ] Integration tests cover SRD cross-system interactions
- [ ] Database integration tests use appropriate isolation
- [ ] Memory allocation tests monitor resource usage
- [ ] Performance targets clearly documented from SRD

### Coverage & Organization ✅
- [ ] Test contributes to appropriate coverage targets
- [ ] Test is properly categorized by system and priority
- [ ] Test class organization follows project standards
- [ ] Related tests are grouped logically
- [ ] Test data builders used appropriately

### SRD Integration ✅
- [ ] **New Tests**: Reference relevant SRD specifications
- [ ] **Test Updates**: Verified against latest SRD version
- [ ] **Discoveries**: New mechanics fed back to SRD updates
- [ ] **Test Alignment**: Test scenarios match SRD examples

---

## Conclusion

This test review guide ensures OpenDAoC tests validate both functionality and authentic DAoC game mechanics as specified in the SRD. Every test must clearly reference and validate specific SRD rules, use realistic scenarios, and contribute to maintaining the authentic DAoC experience.

**Remember: Tests are validation of SRD specifications. Make them clear, accurate, and comprehensive.**

For detailed game rules and testing infrastructure, refer to:
- `SRD/` for authoritative DAoC mechanics specifications
- `Core_Systems_Testing_Framework.md` for testing infrastructure
- `Core_Systems_Interface_Design.md` for mock implementations 