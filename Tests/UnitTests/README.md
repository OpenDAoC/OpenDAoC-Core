# OpenDAoC Core Systems - Unit Testing Documentation

## Overview

This directory contains comprehensive unit tests for the OpenDAoC core systems. The tests are designed to verify that all game mechanics work according to the documented game rules while following interface-first design principles.

## Test Structure

### Directory Organization
```
UnitTests/
├── Combat/
│   └── CombatSystemTests.cs
├── Character/
│   └── CharacterProgressionTests.cs
├── Items/
│   └── ItemSystemTests.cs
├── Guilds/
│   └── GuildSystemTests.cs (TODO)
├── Housing/
│   └── HousingSystemTests.cs (TODO)
├── Crafting/
│   └── CraftingSystemTests.cs (TODO)
├── Quests/
│   └── QuestSystemTests.cs (TODO)
└── PropertyCalculators/
    └── PropertyCalculatorTests.cs (TODO)
```

## Testing Approach

### 1. Interface-First Design
All tests are written against interfaces, not concrete implementations. This allows:
- Easy mocking of dependencies
- Testing behavior without implementation details
- Flexible refactoring without breaking tests

### 2. Game Rules Verification
Each test verifies specific game mechanics as documented in the game rules:
- Attack formulas and resolution order
- Character progression mechanics
- Item bonuses and restrictions
- All other core systems

### 3. Test Patterns

#### Parameterized Tests
Used extensively for testing variations of the same mechanic:
```csharp
[TestCase(50, 49, 0.1867)] // +1 level = +1.33% hit chance
[TestCase(50, 51, 0.1733)] // -1 level = -1.33% hit chance
public void HitMissCalculation_LevelDifference_ShouldAffectMissChance_PvEOnly(
    int attackerLevel, int defenderLevel, double expectedMissChance) { }
```

#### Mock Creation Helpers
Each test class includes helper methods for creating mocks:
```csharp
private Mock<IAttacker> CreateMockAttacker(int level = 50) { }
private Mock<IWeapon> CreateMockWeapon(int dps = 165, int speed = 37) { }
```

#### Clear Test Names
Tests follow the pattern: `SystemUnderTest_Scenario_ExpectedBehavior`
```csharp
public void DamageCalculation_BaseDamage_ShouldFollowFormula() { }
public void ItemQuality_ShouldAffectEffectiveness() { }
```

## Key Test Coverage Areas

### Combat System
- **Attack Resolution**: Correct order of defense checks
- **Hit/Miss**: Base 18% miss chance with modifiers
- **Damage Calculation**: Weapon skill, armor factor, critical hits
- **Defense Mechanics**: Evade, parry, block calculations
- **Style Combat**: Positional and opening requirements
- **Spell Damage**: Stat modifiers and variance

### Character Progression
- **Experience**: Level requirements, group bonuses
- **Stats**: Primary/secondary/tertiary stat gains
- **Specializations**: Point calculations and caps
- **Champion Levels**: Post-50 progression
- **Realm Ranks**: Points to rank conversion

### Item System
- **Properties**: Quality, condition, durability effects
- **Bonuses**: Level-based caps and stacking
- **Equipment**: Slot restrictions and conflicts
- **Generation**: Random items, uniques, artifacts
- **Crafting**: Quality calculations

## Running Tests

### Basic Test Execution
```bash
dotnet test Tests/UnitTests --filter "FullyQualifiedName~DOL.Tests.Unit"
```

### Run Specific Test Categories
```bash
# Combat tests only
dotnet test --filter "FullyQualifiedName~DOL.Tests.Unit.Combat"

# Character progression tests
dotnet test --filter "FullyQualifiedName~DOL.Tests.Unit.Character"

# Item system tests
dotnet test --filter "FullyQualifiedName~DOL.Tests.Unit.Items"
```

### With Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Test Dependencies

### NuGet Packages Required
- NUnit (3.13+)
- FluentAssertions (6.0+)
- Moq (4.18+)
- Coverlet (for code coverage)

### Project References
- Tests project references the interface definitions
- No direct references to implementation projects

## Writing New Tests

### 1. Create Test Class
```csharp
[TestFixture]
public class NewSystemTests
{
    private INewSystem _system;
    private Mock<IDependency> _dependencyMock;
    
    [SetUp]
    public void Setup()
    {
        _dependencyMock = new Mock<IDependency>();
        _system = new NewSystem(_dependencyMock.Object);
    }
}
```

### 2. Follow Testing Principles
- Test one behavior per test method
- Use descriptive test names
- Arrange-Act-Assert pattern
- Mock external dependencies
- Use real data structures when possible

### 3. Verify Game Rules
- Reference the game rules documentation
- Include formula comments in tests
- Test edge cases and boundaries
- Verify all documented mechanics

## Integration with CI/CD

Tests should be run automatically on:
- Every pull request
- Before merging to main
- Nightly builds
- Release candidates

### Quality Gates
- All tests must pass
- Code coverage >80% for new code
- No performance regressions
- Memory leak detection

## Future Enhancements

### Planned Test Coverage
- [ ] Guild system mechanics
- [ ] Housing permissions and features
- [ ] Keep/siege warfare
- [ ] Crafting recipes and success rates
- [ ] Quest system and rewards
- [ ] Property calculator integration
- [ ] Realm abilities and restrictions
- [ ] ECS component interactions

### Testing Tools
- [ ] Performance benchmarks with BenchmarkDotNet
- [ ] Integration test framework
- [ ] Test data generators
- [ ] Automated regression detection

## Contributing

When adding new tests:
1. Follow existing patterns and conventions
2. Document any new test helpers
3. Update this README if adding new test categories
4. Ensure tests are deterministic and isolated
5. Include appropriate test data
6. Reference game rules documentation

## Resources

- [Game Rules Documentation](../../Helper%20Docs/Core_Systems_Game_Rules.md)
- [Interface Design](../../Helper%20Docs/Core_Systems_Interface_Design.md)
- [Testing Framework Plan](../../Helper%20Docs/Core_Systems_Testing_Framework.md) 