# OpenDAoC Development Standards and Architecture Rule

## Core Architecture Principles

### 1. Interface-First Design
- **Always define interfaces before implementation**
- Create clear contracts that specify behavior, not implementation
- Enable dependency injection and testability
- Support multiple implementations and easy mocking
- **ALWAYS** check the OpenDAoC interfaces for an existing implementation to use. Make a new one only if no other option exists.

```csharp
// ✅ Good - Interface defines contract
public interface ICombatSystem
{
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context);
    DamageResult CalculateDamage(AttackData attackData);
}

// ❌ Bad - Direct implementation dependency
public class Character
{
    private CombatSystem _combat = new CombatSystem(); // Tightly coupled
}
```

### 2. Entity Component System (ECS) Architecture
- **Separate data from behavior**
- Entities hold components (data containers)
- Services process components (system logic)
- Enable efficient, parallel processing

```csharp
// ✅ Good - Data in components, logic in services
public class AttackComponent : IComponent
{
    public int WeaponSkill { get; set; }
    public IWeapon ActiveWeapon { get; set; }
}

public class CombatService : IGameService
{
    public void ProcessCombatRound() { /* Logic here */ }
}
```

### 3. Service Layer Pattern
- **All business logic belongs in services, not entities**
- Services are stateless and testable
- Use dependency injection for service dependencies
- Enable cross-cutting concerns (logging, caching, etc.)

## SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- **Each class should have only one reason to change**
- Separate concerns into focused classes
- Avoid god objects that do everything

```csharp
// ✅ Good - Single responsibility
public class DamageCalculator
{
    public int CalculateBaseDamage(IWeapon weapon) { }
}

public class HitChanceCalculator  
{
    public double CalculateHitChance(AttackData attack) { }
}

// ❌ Bad - Multiple responsibilities
public class CombatManager
{
    public int CalculateDamage() { }
    public double CalculateHitChance() { }
    public void SaveCombatLog() { }
    public void SendPackets() { }
}
```

### Open/Closed Principle (OCP)
- **Open for extension, closed for modification**
- Use interfaces and abstract classes for extensibility
- Implement strategy pattern for variable behaviors

```csharp
// ✅ Good - Extensible via interface
public interface IPropertyCalculator
{
    int Calculate(IPropertySource source);
}

public class PropertyService
{
    private readonly Dictionary<Property, IPropertyCalculator> _calculators;
    
    public void RegisterCalculator(Property property, IPropertyCalculator calculator)
    {
        _calculators[property] = calculator; // Extends behavior
    }
}
```

### Liskov Substitution Principle (LSP)
- **Derived classes must be substitutable for base classes**
- Maintain behavioral contracts in inheritance
- Avoid breaking preconditions or postconditions

### Interface Segregation Principle (ISP)
- **Clients shouldn't depend on interfaces they don't use**
- Create focused, role-based interfaces
- Avoid fat interfaces with many methods

```csharp
// ✅ Good - Focused interfaces
public interface IAttacker
{
    AttackData PrepareAttack(IDefender target, AttackType type);
}

public interface IDefender
{
    DefenseResult TryDefend(AttackData attack);
}

// ❌ Bad - Fat interface
public interface ICombatant
{
    AttackData PrepareAttack();
    DefenseResult TryDefend();
    void UseAbility();
    void CastSpell();
    void Trade();
    void SendMessage();
}
```

### Dependency Inversion Principle (DIP)
- **Depend on abstractions, not concretions**
- Use dependency injection
- Invert control flow through interfaces

```csharp
// ✅ Good - Depends on abstraction
public class CombatService
{
    private readonly IDamageCalculator _damageCalculator;
    private readonly IHitChanceCalculator _hitCalculator;
    
    public CombatService(IDamageCalculator damageCalc, IHitChanceCalculator hitCalc)
    {
        _damageCalculator = damageCalc;
        _hitCalculator = hitCalc;
    }
}
```

## Code Quality Standards

### DRY (Don't Repeat Yourself)
- **Extract common logic into reusable components**
- Use inheritance and composition appropriately
- Create utility classes for shared functionality

```csharp
// ✅ Good - Shared calculation logic
public static class LevelDifferenceCalculator
{
    public static double GetLevelModifier(int attackerLevel, int defenderLevel)
    {
        return (attackerLevel - defenderLevel) * 0.0133;
    }
}

// Usage in multiple places
var hitModifier = LevelDifferenceCalculator.GetLevelModifier(attacker.Level, defender.Level);
var damageModifier = LevelDifferenceCalculator.GetLevelModifier(caster.Level, target.Level);
```

### Quality Metrics
- **Cyclomatic complexity < 10** per method
- **Code duplication < 5%** across codebase  
- **Test coverage > 80%** for business logic
- **Clear separation of concerns** between layers

### Naming Conventions
- Use descriptive, intention-revealing names
- Avoid abbreviations and Hungarian notation
- Use consistent terminology across the codebase

```csharp
// ✅ Good - Clear, descriptive names
public interface ICharacterProgressionService
{
    void GrantExperience(ICharacter character, long experiencePoints);
    bool CanLevelUp(ICharacter character);
}

// ❌ Bad - Unclear, abbreviated names
public interface ICharProgSvc
{
    void GrantExp(IChar chr, long exp);
    bool CanLvlUp(IChar chr);
}
```

## Design Patterns to Use

### 1. Repository Pattern
- **Abstract data access behind interfaces**
- Enable different storage backends
- Simplify testing with mock repositories

```csharp
public interface ICharacterRepository : IRepository<ICharacter>
{
    ICharacter GetByName(string name);
    IList<ICharacter> GetByGuild(string guildId);
}
```

### 2. Factory Pattern
- **Centralize object creation logic**
- Handle complex initialization
- Support different creation strategies

```csharp
public interface IItemFactory
{
    IItem CreateFromTemplate(string templateId);
    IItem CreateRandom(int level, ItemType type);
    IItem CreateUnique(string uniqueId);
}
```

### 3. Observer Pattern
- **Decouple event producers from consumers**
- Enable event-driven architecture
- Support multiple listeners per event

```csharp
public interface IEventManager
{
    void Subscribe<T>(IEventHandler<T> handler) where T : IGameEvent;
    void Publish<T>(T gameEvent) where T : IGameEvent;
}
```

### 4. Strategy Pattern
- **Encapsulate variable algorithms**
- Enable runtime behavior switching
- Support different calculation methods

```csharp
public interface IPropertyCalculator
{
    Property TargetProperty { get; }
    int Calculate(IPropertySource source);
}
```

## Development Workflow

### For New Features
1. **Check SRD first** - Verify if mechanics are already documented
2. **Update SRD if needed** - Document any missing mechanics before coding
3. **Start with interface definition** - Design the contract first
4. **Write tests first** (TDD) - Define expected behavior per SRD
5. **Use established patterns** - Follow existing architecture
6. **Document thoroughly** - Reference SRD in code comments
7. **Review carefully** - Ensure SRD compliance

### For Refactoring Existing Code
1. **Document current behavior** - Capture existing logic
2. **Verify against SRD** - Ensure current behavior matches specifications
3. **Update SRD if needed** - Document any discovered mechanics
4. **Write tests** - Cover edge cases per SRD specifications
5. **Create interfaces** - Define new contracts
6. **Build adapters** - Bridge old and new code
7. **Migrate gradually** - One component at a time
8. **Validate thoroughly** - Run all tests against SRD specs

### Code Review Checklist
- [ ] Follows SOLID principles
- [ ] Has appropriate test coverage
- [ ] Uses dependency injection
- [ ] Implements defined interfaces
- [ ] Maintains separation of concerns
- [ ] Follows naming conventions
- [ ] Includes proper documentation
- [ ] Handles errors appropriately

## Testing Standards

### Test-Driven Development (TDD)
- **Write tests before implementation**
- Use Red-Green-Refactor cycle
- Focus on behavior, not implementation

### Test Structure
- **Arrange-Act-Assert** pattern
- **One assertion per test** (when possible)
- **Descriptive test names** using Given_When_Then format

```csharp
[Test]
public void ProcessAttack_ShouldHit_WhenAttackerLevelHigher()
{
    // Arrange
    var attacker = CreateMockAttacker(level: 50);
    var defender = CreateMockDefender(level: 45);
    var context = new AttackContext();
    
    // Act
    var result = _combatSystem.ProcessAttack(attacker, defender, context);
    
    // Assert
    result.Hit.Should().BeTrue();
}
```

### Mock Usage Guidelines
- **Mock external dependencies only**
- Use real objects for value types and DTOs
- Verify behavior when testing interactions
- Keep mocks simple and focused

### Test Data Management
- **Use builder pattern** for complex object creation
- **Centralize test data** in factories
- **Keep test data realistic** but focused
- **Avoid magic numbers** - use named constants

```csharp
public class CharacterBuilder
{
    public CharacterBuilder WithLevel(int level) { /* */ }
    public CharacterBuilder WithClass(ICharacterClass characterClass) { /* */ }
    public MockCharacter Build() { /* */ }
}
```

## Performance Considerations

### Optimization Guidelines
- **Measure before optimizing** - Use profiling tools
- **Focus on hot paths** - Combat, movement, property calculations
- **Use appropriate data structures** - Consider access patterns
- **Implement caching strategically** - For expensive calculations

### Performance Targets
- **Combat calculations**: < 1ms per attack
- **Property updates**: < 0.5ms per calculation
- **Database queries**: < 10ms average
- **Memory usage**: Stable over time

## Error Handling

### Exception Strategy
- **Use specific exception types** for different error categories
- **Handle exceptions at appropriate level** - Don't swallow errors
- **Log errors with context** - Include relevant state information
- **Fail fast** - Detect problems early

```csharp
public class CharacterService
{
    public void LevelUp(ICharacter character)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));
            
        if (character.Level >= 50)
            throw new InvalidOperationException("Character is already at max level");
            
        try
        {
            // Level up logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to level up character {CharacterName}", character.Name);
            throw;
        }
    }
}
```

## Documentation Requirements

### Code Documentation
- **XML documentation** for public APIs
- **Inline comments** for complex business logic
- **Architecture decision records** (ADRs) for major decisions
- **Keep documentation up to date** with code changes

### API Documentation
- **Document all interfaces** with purpose and usage
- **Include examples** for complex operations
- **Specify preconditions and postconditions**
- **Document threading considerations**

## Common Anti-Patterns to Avoid

### ❌ God Objects
- Classes that do too many things
- Violate Single Responsibility Principle

### ❌ Anemic Domain Models
- Classes with only properties, no behavior
- All logic in service classes

### ❌ Tight Coupling
- Direct dependencies on concrete classes
- Hard to test and maintain

### ❌ Magic Numbers/Strings
- Hardcoded values without explanation
- Should be constants or configuration

### ❌ Deep Inheritance Hierarchies
- More than 3-4 levels deep
- Prefer composition over inheritance

### ❌ Primitive Obsession
- Using primitives instead of value objects
- Missing domain concepts

```csharp
// ❌ Bad - Primitive obsession
public void GrantExperience(string characterId, long experience);

// ✅ Good - Proper domain objects
public void GrantExperience(CharacterId characterId, ExperiencePoints experience);
```

## Migration Strategy

### Phase 1: Foundation
- Set up testing infrastructure
- Define core interfaces
- Build mock implementations

### Phase 2: Core Systems
- Refactor combat system
- Implement property calculators
- Modernize character system

### Phase 3: Integration
- Complete service layer
- Optimize performance
- Full integration testing

Remember: **The goal is not to change the game, but to improve the code that runs it.** 