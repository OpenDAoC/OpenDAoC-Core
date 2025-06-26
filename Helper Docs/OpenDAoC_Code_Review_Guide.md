# OpenDAoC Code Review Guide

This guide provides comprehensive criteria for conducting code reviews on the OpenDAoC (Dark Age of Camelot) server emulator project. Each section outlines specific aspects to examine, common issues to identify, and project-specific requirements to enforce.

## Quick Navigation

1. **Architecture & Design Patterns** - ECS, interfaces, service layer
2. **SOLID Principles & Clean Code** - Engineering best practices
3. **Game Rule Compliance** - DAoC mechanics accuracy
4. **Performance & Optimization** - Critical performance targets
5. **Interface-First Design** - Contract-driven development
6. **Testing & Quality** - TDD and coverage standards
7. **Code Organization** - Structure and maintainability
8. **Documentation** - Game rules and API documentation
9. **Legacy Code Migration** - Modernization patterns
10. **Security & Stability** - Server reliability

## Project Context

**Game System**: Dark Age of Camelot Server Emulator
**Framework**: .NET 9.0 with modern C# features (Lock, etc.)
**Architecture**: Interface-first, ECS (Entity Component System), Service Layer
**Core Principles**: SOLID, DRY, performance-critical gaming server
**Critical Requirements**: Sub-millisecond combat calculations, authentic DAoC mechanics
**Reference Documentation**: SRD (System Reference Document) for all game mechanics

---

## 1. Architecture & Design Patterns

### Core Architecture Requirements

#### Entity Component System (ECS)
```csharp
// ✅ GOOD: Proper ECS separation
public class AttackComponent : IComponent
{
    public int WeaponSkill { get; set; }
    public IWeapon ActiveWeapon { get; set; }
    // Data only - no behavior
}

public class CombatService : IGameService
{
    public void ProcessCombatRound() 
    {
        // Logic operates on components
    }
}

// ❌ BAD: Mixing data and behavior
public class Character
{
    public int WeaponSkill { get; set; }
    public void ProcessAttack() { } // Behavior mixed with data
}
```

#### Interface-First Design
```csharp
// ✅ GOOD: Interface defines contract first
public interface ICombatSystem
{
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context);
    DamageResult CalculateDamage(AttackData attackData);
}

// ❌ BAD: Direct implementation without interface
public class CombatCalculator
{
    // No interface - violates interface-first principle
}
```

#### Service Layer Pattern
```csharp
// ✅ GOOD: Business logic in services
public class CharacterProgressionService : ICharacterProgressionService
{
    private readonly IExperienceCalculator _experienceCalculator;
    private readonly ISpecializationService _specializationService;
    
    public void LevelUp(ICharacter character) 
    {
        // Stateless service with injected dependencies
    }
}

// ❌ BAD: Business logic in entities
public class Character
{
    public void LevelUp() 
    {
        // Business logic in entity - violates service layer pattern
    }
}
```

---

## 2. SOLID Principles & Clean Code

### Single Responsibility Principle (SRP) - CRITICAL ENFORCEMENT

#### Class Size Limits (STRICTLY ENFORCED)
```csharp
// ❌ CRITICAL VIOLATION: Class > 500 lines
public class GamePlayer : GameLiving  // 7,000+ lines - IMMEDIATE REFACTORING REQUIRED
{
    // Multiple responsibilities violating SRP
}

// ✅ GOOD: Decomposed responsibilities
public class GamePlayer : ICharacter  // ~200 lines
{
    private readonly IPlayerInventory _inventory;
    private readonly IPlayerStats _stats;
    private readonly IPlayerCombat _combat;
    // Single responsibility: coordinate player state
}
```

**Review Actions**:
- **300+ lines**: Requires architectural review
- **500+ lines**: CANNOT be approved - must refactor
- **1000+ lines**: CRITICAL - halt development until fixed

### Open/Closed Principle (OCP)
```csharp
// ✅ GOOD: Extensible via interface
public interface IPropertyCalculator
{
    Property TargetProperty { get; }
    int Calculate(IPropertySource source);
}

public class PropertyService
{
    public void RegisterCalculator(Property property, IPropertyCalculator calculator)
    {
        _calculators[property] = calculator; // Extends without modification
    }
}

// ❌ BAD: Requires modification for extension
public class PropertyCalculator
{
    public int Calculate(Property property, IPropertySource source)
    {
        switch (property) // Must modify for new properties
        {
            case Property.Strength: return CalculateStrength(source);
            // Must add cases here for new properties
        }
    }
}
```

---

## 3. Game Rule Compliance (CRITICAL - Must Match SRD)

### SRD Compliance Requirements
All game mechanics MUST match the specifications in the OpenDAoC System Reference Document (SRD).

```csharp
// ✅ GOOD: Implementation matches SRD specification
/// <summary>
/// Implements attack resolution according to SRD specification.
/// Reference: SRD/01_Combat_Systems/Attack_Resolution.md
/// </summary>
public AttackResult ProcessAttack(AttackData attackData)
{
    // Implementation follows exact SRD attack resolution order
    if (CheckIntercept(attackData)) return AttackResult.Intercepted;
    if (CheckEvade(attackData)) return AttackResult.Evaded;
    if (CheckParry(attackData)) return AttackResult.Parried;
    if (CheckBlock(attackData)) return AttackResult.Blocked;
    if (CheckGuard(attackData)) return AttackResult.Guarded;
    if (CheckMiss(attackData)) return AttackResult.Missed;
    if (CheckBladeturn(attackData)) return AttackResult.Bladeturned;
    
    return ProcessHit(attackData);
}

// ❌ BAD: Implementation differs from SRD
public AttackResult ProcessAttack(AttackData attackData)
{
    if (CheckMiss(attackData)) return AttackResult.Missed; // Wrong order per SRD
    // Missing required checks from SRD specification
}
```

### SRD Update Requirements
When reviewing code that implements game mechanics:
1. **Verify against SRD**: Check that implementation matches SRD specifications
2. **Update SRD if needed**: If new mechanics are discovered, update the SRD
3. **Document SRD references**: Code should reference relevant SRD sections
4. **Flag discrepancies**: Any deviation from SRD must be justified or corrected

### Combat System Rules (Per SRD)

#### Attack Resolution Order
```csharp
// ✅ GOOD: Correct DAoC attack resolution sequence per SRD
// Reference: SRD/01_Combat_Systems/Attack_Resolution.md
public AttackResult ProcessAttack(AttackData attackData)
{
    // Must follow exact order specified in SRD
}
```

#### Hit/Miss Calculation (Exact Formula Required)
```csharp
// ✅ GOOD: Formula matches SRD specification
// Reference: SRD/01_Combat_Systems/Attack_Resolution.md - Base Miss Chance Calculation
public double CalculateHitChance(AttackData attackData)
{
    double baseMissChance = 0.15; // 15% base miss per SRD
    double levelMod = CalculateLevelDifference(attackData); // ±0.33% per level per SRD
    double multiAttackerPenalty = CalculateMultiAttackerPenalty(attackData);
    double toHitBonus = CalculateToHitBonus(attackData);
    
    return 1.0 - Math.Max(0, baseMissChance + levelMod + multiAttackerPenalty - toHitBonus);
}

// ❌ BAD: Incorrect formula not matching SRD
public double CalculateHitChance(AttackData attackData)
{
    return 0.85; // Hardcoded value - violates SRD mechanics
}
```

---

## 4. Performance & Optimization (CRITICAL)

### Performance Targets (STRICTLY ENFORCED)

#### Combat System Performance
```csharp
// ✅ REQUIRED: Combat calculations must be < 1ms
[Benchmark]
public AttackResult ProcessAttack_Performance_Test()
{
    var stopwatch = Stopwatch.StartNew();
    var result = _combatSystem.ProcessAttack(attacker, defender, context);
    stopwatch.Stop();
    
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1), 
        "Combat calculation exceeded 1ms performance target");
    return result;
}

// ❌ CRITICAL: Expensive operations in hot paths
public AttackResult ProcessAttack(AttackData data)
{
    // Database query in combat calculation - PERFORMANCE VIOLATION
    var weaponData = Database.GetWeaponData(data.Weapon.ID);
    
    // String operations in hot path - AVOID
    var logMessage = $"Processing attack from {data.Attacker.Name}";
    
    // Complex LINQ in performance-critical code - OPTIMIZE
    var modifiers = data.Modifiers.Where(m => m.Active).Select(m => m.Value).ToList();
}
```

---

## 5. Review Checklist Template

### Architecture & Design ✅
- [ ] Follows interface-first design pattern
- [ ] Implements ECS architecture (data in components, logic in services)
- [ ] Uses dependency injection properly
- [ ] Follows established design patterns (Repository, Factory, Observer)
- [ ] No circular dependencies

### SOLID Principles ✅
- [ ] Single Responsibility: Classes < 500 lines, methods < 50 lines
- [ ] Open/Closed: Extensions via interfaces, not modifications
- [ ] Liskov Substitution: Derived classes properly substitutable
- [ ] Interface Segregation: Focused, role-based interfaces
- [ ] Dependency Inversion: Depends on abstractions, not concretions

### Game Rule Compliance ✅
- [ ] **SRD Compliance**: Implementation matches SRD specifications exactly
- [ ] **SRD References**: Code references relevant SRD sections
- [ ] **SRD Updates**: Any new mechanics discovered are added to SRD
- [ ] **Formula Accuracy**: All calculations match SRD formulas
- [ ] **Edge Cases**: Implementation handles all SRD-documented edge cases

### Performance ✅
- [ ] Combat calculations < 1ms execution time
- [ ] Property calculations < 0.5ms execution time
- [ ] No blocking database calls in hot paths
- [ ] Appropriate caching strategies implemented
- [ ] Memory allocations minimized in performance-critical code

### Code Quality ✅
- [ ] Test coverage meets requirements (80%+ business logic)
- [ ] Proper error handling with specific exceptions
- [ ] Consistent naming conventions
- [ ] XML documentation for public APIs
- [ ] No code duplication (DRY principle)

### Security & Stability ✅
- [ ] Input validation on all public methods
- [ ] Proper exception handling and logging
- [ ] No security vulnerabilities (SQL injection, etc.)
- [ ] Graceful degradation on errors

### SRD Integration ✅
- [ ] **New Features**: Documented in SRD before implementation
- [ ] **Bug Fixes**: Verified against SRD expected behavior
- [ ] **Discoveries**: New mechanics added to SRD with source documentation
- [ ] **Test Alignment**: Tests reference SRD specifications

---

## Conclusion

This code review guide ensures OpenDAoC maintains high code quality while preserving authentic DAoC gameplay mechanics as documented in the SRD. Focus on interface-first design, performance targets, and accurate game rule implementation per SRD specifications.

**Remember: The goal is not to change the game, but to improve the code that runs it.**

For detailed requirements, refer to:
- `SRD/` for authoritative game mechanics specifications
- `Core_Systems_Interface_Design.md` for architecture patterns  
- `Development_Standards_Rule.md` for coding standards 