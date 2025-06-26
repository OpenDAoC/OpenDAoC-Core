# OpenDAoC Core Systems - Testing Framework Plan

## Testing Strategy Overview

### Goals
- Enable comprehensive unit testing of all core systems
- Support integration testing between systems
- Allow performance benchmarking
- Facilitate behavior-driven development (BDD)
- Ensure regression prevention

### Testing Principles
- Test behavior, not implementation
- Mock external dependencies
- Use real data structures where possible
- Keep tests isolated and repeatable
- Maintain test code quality standards

## Testing Infrastructure

### Test Framework Stack
- **Unit Testing**: NUnit or xUnit
- **Mocking**: Moq or NSubstitute  
- **Assertions**: FluentAssertions
- **BDD**: SpecFlow (optional)
- **Performance**: BenchmarkDotNet
- **Coverage**: dotCover or Coverlet

### Project Structure
```
Tests/
├── UnitTests/
│   ├── Combat/
│   ├── Character/
│   ├── Items/
│   ├── Guilds/
│   ├── Housing/
│   ├── Crafting/
│   └── Quests/
├── IntegrationTests/
│   ├── Systems/
│   ├── Database/
│   └── Network/
├── PerformanceTests/
│   └── Benchmarks/
├── TestUtilities/
│   ├── Builders/
│   ├── Mocks/
│   └── Fixtures/
└── TestData/
    └── SampleData/
```

## Mock Implementations

### Combat System Mocks

```csharp
public class MockCombatSystem : ICombatSystem
{
    private readonly List<AttackRecord> _attackHistory = new();
    
    public AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context)
    {
        var result = new AttackResult
        {
            Hit = attacker.Level >= defender.Level,
            Damage = CalculateMockDamage(attacker, defender)
        };
        
        _attackHistory.Add(new AttackRecord(attacker, defender, result));
        return result;
    }
    
    public IReadOnlyList<AttackRecord> AttackHistory => _attackHistory;
    public void ClearHistory() => _attackHistory.Clear();
}

public class MockAttacker : IAttacker
{
    public int Level { get; set; } = 50;
    public IWeapon ActiveWeapon { get; set; }
    public ICombatStats CombatStats { get; set; }
    public IList<IEffect> ActiveEffects { get; set; } = new List<IEffect>();
    
    public AttackData PrepareAttack(IDefender target, AttackType type)
    {
        return new AttackData
        {
            Attacker = this,
            Target = target,
            Type = type,
            Weapon = ActiveWeapon
        };
    }
}

public class MockDefender : IDefender
{
    public int Level { get; set; } = 50;
    public Dictionary<ArmorSlot, IArmor> Armor { get; set; } = new();
    public IDefenseStats DefenseStats { get; set; }
    
    public IArmor GetArmor(ArmorSlot slot) => Armor.GetValueOrDefault(slot);
    
    public DefenseResult TryDefend(AttackData attack)
    {
        return new DefenseResult
        {
            Success = DefenseStats?.GetEvadeChance(1) > 0.5,
            Type = DefenseType.Evade
        };
    }
}
```

### Character System Mocks

```csharp
public class MockCharacter : ICharacter
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "TestCharacter";
    public ICharacterClass Class { get; set; }
    public IStats BaseStats { get; set; } = new MockStats();
    public IStats ModifiedStats { get; set; } = new MockStats();
    public ISpecializationList Specializations { get; set; } = new MockSpecializationList();
    public IAbilityList Abilities { get; set; } = new MockAbilityList();
    public IInventory Inventory { get; set; } = new MockInventory();
    public IQuestLog QuestLog { get; set; } = new MockQuestLog();
}

public class MockCharacterClass : ICharacterClass
{
    public string ID { get; set; } = "warrior";
    public string Name { get; set; } = "Warrior";
    public ClassType Type { get; set; } = ClassType.Tank;
    public Stat PrimaryStat { get; set; } = Stat.Strength;
    public Stat SecondaryStat { get; set; } = Stat.Constitution;
    public Stat TertiaryStat { get; set; } = Stat.Dexterity;
    public Stat ManaStat { get; set; } = Stat.None;
    public int SpecializationMultiplier { get; set; } = 20;
    public int BaseHP { get; set; } = 880;
    public int WeaponSkillBase { get; set; } = 400;
    
    public IList<string> AllowedRaces { get; set; } = new List<string> { "Human", "Dwarf" };
    
    public IList<IAbility> GetAbilitiesAtLevel(int level)
    {
        return level >= 5 ? new List<IAbility> { new MockAbility("Shield Bash") } : new List<IAbility>();
    }
}
```

### Item System Mocks

```csharp
public class MockInventory : IInventory
{
    private readonly Dictionary<InventorySlot, IItem> _items = new();
    
    public IItem GetItem(InventorySlot slot) => _items.GetValueOrDefault(slot);
    
    public bool AddItem(IItem item, InventorySlot slot)
    {
        if (_items.ContainsKey(slot))
            return false;
            
        _items[slot] = item;
        return true;
    }
    
    public bool RemoveItem(InventorySlot slot) => _items.Remove(slot);
    
    public bool MoveItem(InventorySlot from, InventorySlot to)
    {
        if (!_items.TryGetValue(from, out var item) || _items.ContainsKey(to))
            return false;
            
        _items.Remove(from);
        _items[to] = item;
        return true;
    }
}

public class MockWeapon : IWeapon
{
    public string TemplateID { get; set; } = "test_sword";
    public string Name { get; set; } = "Test Sword";
    public int Level { get; set; } = 50;
    public int Quality { get; set; } = 100;
    public int Condition { get; set; } = 100;
    public int Durability { get; set; } = 100;
    public ItemType Type { get; set; } = ItemType.Weapon;
    public EquipmentSlot Slot { get; set; } = EquipmentSlot.RightHand;
    public int DPS { get; set; } = 165;
    public int Speed { get; set; } = 37;
    public WeaponType WeaponType { get; set; } = WeaponType.Sword;
    public DamageType DamageType { get; set; } = DamageType.Slash;
    public IList<IItemBonus> Bonuses { get; set; } = new List<IItemBonus>();
}
```

### Guild System Mocks

```csharp
public class MockGuild : IGuild
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Test Guild";
    public Realm Realm { get; set; } = Realm.Albion;
    public IGuildRankSystem Ranks { get; set; } = new MockGuildRankSystem();
    public IGuildMemberList Members { get; set; } = new MockGuildMemberList();
    public IGuildBank Bank { get; set; } = new MockGuildBank();
    public IAlliance Alliance { get; set; }
    public IGuildBonuses Bonuses { get; set; } = new MockGuildBonuses();
}

public class MockGuildRankSystem : IGuildRankSystem
{
    private readonly Dictionary<int, IGuildRank> _ranks = new();
    private readonly Dictionary<int, HashSet<GuildPermission>> _permissions = new();
    
    public IGuildRank this[int level] => _ranks.GetValueOrDefault(level);
    
    public void SetPermission(int rankLevel, GuildPermission permission, bool value)
    {
        if (!_permissions.ContainsKey(rankLevel))
            _permissions[rankLevel] = new HashSet<GuildPermission>();
            
        if (value)
            _permissions[rankLevel].Add(permission);
        else
            _permissions[rankLevel].Remove(permission);
    }
    
    public bool HasPermission(IGuildMember member, GuildPermission permission)
    {
        return _permissions.GetValueOrDefault(member.Rank.Level)?.Contains(permission) ?? false;
    }
}
```

## Test Builders

### Fluent Builders for Test Data

```csharp
public class CharacterBuilder
{
    private readonly MockCharacter _character = new();
    
    public CharacterBuilder WithName(string name)
    {
        _character.Name = name;
        return this;
    }
    
    public CharacterBuilder WithClass(ICharacterClass characterClass)
    {
        _character.Class = characterClass;
        return this;
    }
    
    public CharacterBuilder WithLevel(int level)
    {
        _character.Level = level;
        return this;
    }
    
    public CharacterBuilder WithStats(int str, int con, int dex, int qui, int intel)
    {
        _character.BaseStats[Stat.Strength] = str;
        _character.BaseStats[Stat.Constitution] = con;
        _character.BaseStats[Stat.Dexterity] = dex;
        _character.BaseStats[Stat.Quickness] = qui;
        _character.BaseStats[Stat.Intelligence] = intel;
        return this;
    }
    
    public CharacterBuilder WithWeapon(IWeapon weapon)
    {
        _character.Inventory.AddItem(weapon, InventorySlot.RightHand);
        return this;
    }
    
    public MockCharacter Build() => _character;
}

public class CombatScenarioBuilder
{
    private IAttacker _attacker;
    private IDefender _defender;
    private AttackContext _context = new();
    
    public CombatScenarioBuilder WithAttacker(Action<CharacterBuilder> configure)
    {
        var builder = new CharacterBuilder();
        configure(builder);
        _attacker = new MockAttacker { Character = builder.Build() };
        return this;
    }
    
    public CombatScenarioBuilder WithDefender(Action<CharacterBuilder> configure)
    {
        var builder = new CharacterBuilder();
        configure(builder);
        _defender = new MockDefender { Character = builder.Build() };
        return this;
    }
    
    public CombatScenarioBuilder WithMultipleAttackers(int count)
    {
        _context.AttackerCount = count;
        return this;
    }
    
    public (IAttacker attacker, IDefender defender, AttackContext context) Build()
    {
        return (_attacker, _defender, _context);
    }
}
```

## Unit Test Examples

### Combat System Tests

```csharp
[TestFixture]
public class CombatCalculationTests
{
    private ICombatSystem _combatSystem;
    private CombatScenarioBuilder _scenarioBuilder;
    
    [SetUp]
    public void Setup()
    {
        _combatSystem = new CombatSystem();
        _scenarioBuilder = new CombatScenarioBuilder();
    }
    
    [Test]
    public void Attack_ShouldHit_WhenAttackerLevelHigher()
    {
        // Arrange
        var (attacker, defender, context) = _scenarioBuilder
            .WithAttacker(a => a.WithLevel(50).WithWeapon(new MockWeapon()))
            .WithDefender(d => d.WithLevel(45))
            .Build();
            
        // Act
        var result = _combatSystem.ProcessAttack(attacker, defender, context);
        
        // Assert
        result.Hit.Should().BeTrue();
        result.Damage.Should().BeGreaterThan(0);
    }
    
    [TestCase(1, 1.0)]
    [TestCase(2, 0.5)]
    [TestCase(3, 0.33)]
    public void DefenseChances_ShouldDecrease_WithMultipleAttackers(int attackers, double expectedMultiplier)
    {
        // Arrange
        var defender = new MockDefender();
        var baseEvadeChance = 0.3;
        defender.DefenseStats = new MockDefenseStats { BaseEvadeChance = baseEvadeChance };
        
        // Act
        var actualChance = defender.DefenseStats.GetEvadeChance(attackers);
        
        // Assert
        actualChance.Should().BeApproximately(baseEvadeChance * expectedMultiplier, 0.01);
    }
}
```

### Character Progression Tests

```csharp
[TestFixture]
public class CharacterProgressionTests
{
    private ICharacter _character;
    private ICharacterProgressionService _progressionService;
    
    [SetUp]
    public void Setup()
    {
        _character = new CharacterBuilder()
            .WithClass(new MockCharacterClass())
            .WithLevel(1)
            .Build();
            
        _progressionService = new CharacterProgressionService();
    }
    
    [Test]
    public void LevelUp_ShouldIncreaseStats_AccordingToClass()
    {
        // Arrange
        var initialStr = _character.BaseStats[Stat.Strength];
        
        // Act
        _progressionService.LevelUp(_character, 6); // Level 6 is when stats start increasing
        
        // Assert
        _character.BaseStats[Stat.Strength].Should().Be(initialStr + 1);
    }
    
    [Test]
    public void SpecializationPoints_ShouldIncrease_WithLevel()
    {
        // Arrange & Act
        var points = _progressionService.CalculateSpecPoints(_character, 10);
        
        // Assert
        points.Should().Be(10 * _character.Class.SpecializationMultiplier / 10);
    }
}
```

### Item System Tests

```csharp
[TestFixture]
public class InventoryManagementTests
{
    private IInventory _inventory;
    
    [SetUp]
    public void Setup()
    {
        _inventory = new MockInventory();
    }
    
    [Test]
    public void AddItem_ShouldSucceed_WhenSlotEmpty()
    {
        // Arrange
        var item = new MockWeapon();
        var slot = InventorySlot.FirstBackpack;
        
        // Act
        var result = _inventory.AddItem(item, slot);
        
        // Assert
        result.Should().BeTrue();
        _inventory.GetItem(slot).Should().Be(item);
    }
    
    [Test]
    public void MoveItem_ShouldFail_WhenTargetSlotOccupied()
    {
        // Arrange
        var item1 = new MockWeapon { Name = "Item 1" };
        var item2 = new MockWeapon { Name = "Item 2" };
        _inventory.AddItem(item1, InventorySlot.FirstBackpack);
        _inventory.AddItem(item2, InventorySlot.FirstBackpack + 1);
        
        // Act
        var result = _inventory.MoveItem(InventorySlot.FirstBackpack, InventorySlot.FirstBackpack + 1);
        
        // Assert
        result.Should().BeFalse();
        _inventory.GetItem(InventorySlot.FirstBackpack).Should().Be(item1);
        _inventory.GetItem(InventorySlot.FirstBackpack + 1).Should().Be(item2);
    }
}
```

## Integration Test Examples

### Cross-System Integration

```csharp
[TestFixture]
public class CombatIntegrationTests
{
    private ICombatSystem _combatSystem;
    private IPropertyService _propertyService;
    private IEffectService _effectService;
    
    [SetUp]
    public void Setup()
    {
        // Use real implementations with mock data layer
        var mockDataContext = new MockDataContext();
        _propertyService = new PropertyService(new PropertyCalculatorRegistry());
        _effectService = new EffectService();
        _combatSystem = new CombatSystem(_propertyService, _effectService);
    }
    
    [Test]
    public void CombatWithBuffs_ShouldApplyCorrectDamageModifiers()
    {
        // Arrange
        var attacker = new CharacterBuilder()
            .WithLevel(50)
            .WithWeapon(new MockWeapon { DPS = 165 })
            .Build();
            
        var defender = new CharacterBuilder()
            .WithLevel(50)
            .Build();
            
        // Apply strength buff to attacker
        var strBuff = new MockEffect
        {
            Property = Property.Strength,
            Value = 50,
            Type = EffectType.Buff
        };
        _effectService.AddEffect(attacker, strBuff);
        
        // Act
        var unbuffedDamage = CalculateBaseDamage(attacker, defender);
        _propertyService.RecalculateProperties(attacker);
        var buffedDamage = CalculateBaseDamage(attacker, defender);
        
        // Assert
        buffedDamage.Should().BeGreaterThan(unbuffedDamage);
    }
}
```

## Performance Benchmarks

### Combat Performance Tests

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class CombatBenchmarks
{
    private ICombatSystem _combatSystem;
    private IAttacker _attacker;
    private IDefender _defender;
    private AttackContext _context;
    
    [GlobalSetup]
    public void Setup()
    {
        _combatSystem = new CombatSystem();
        _attacker = new MockAttacker();
        _defender = new MockDefender();
        _context = new AttackContext();
    }
    
    [Benchmark]
    public AttackResult ProcessSingleAttack()
    {
        return _combatSystem.ProcessAttack(_attacker, _defender, _context);
    }
    
    [Benchmark]
    public void Process100Attacks()
    {
        for (int i = 0; i < 100; i++)
        {
            _combatSystem.ProcessAttack(_attacker, _defender, _context);
        }
    }
}
```

## Test Data Management

### Sample Data Factory

```csharp
public static class TestDataFactory
{
    public static ICharacter CreateWarrior(int level = 50)
    {
        return new CharacterBuilder()
            .WithName("Test Warrior")
            .WithClass(GetWarriorClass())
            .WithLevel(level)
            .WithStats(100, 85, 60, 50, 15)
            .WithWeapon(CreateSword(level))
            .Build();
    }
    
    public static IWeapon CreateSword(int level)
    {
        return new MockWeapon
        {
            Name = $"Level {level} Sword",
            Level = level,
            DPS = 150 + level,
            Speed = 37,
            WeaponType = WeaponType.Sword,
            Bonuses = new List<IItemBonus>
            {
                new MockItemBonus { Property = Property.Strength, Value = level / 10 }
            }
        };
    }
    
    public static IGuild CreateGuild(string name, Realm realm)
    {
        var guild = new MockGuild
        {
            Name = name,
            Realm = realm
        };
        
        // Initialize with default rank structure
        for (int i = 0; i < 10; i++)
        {
            guild.Ranks[i] = new MockGuildRank { Level = i, Name = $"Rank {i}" };
        }
        
        return guild;
    }
}
```

## Continuous Integration Setup

### Test Execution Pipeline

```yaml
# .github/workflows/tests.yml
name: Test Suite

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Run Unit Tests
      run: dotnet test Tests/UnitTests --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Run Integration Tests
      run: dotnet test Tests/IntegrationTests --no-build --verbosity normal
      
    - name: Generate Coverage Report
      uses: codecov/codecov-action@v2
      with:
        files: ./Tests/UnitTests/TestResults/**/coverage.cobertura.xml
```

## Testing Best Practices

### Test Organization
- One test class per production class
- Group related tests in nested classes
- Use descriptive test names following Given_When_Then pattern
- Keep tests focused on single behavior

### Mock Usage Guidelines
- Mock external dependencies only
- Use real objects for value types and DTOs
- Avoid over-mocking - prefer integration tests for complex scenarios
- Verify mock interactions when testing behavior

### Test Data Management
- Use builders for complex object creation
- Centralize test data in factories
- Avoid magic numbers - use named constants
- Keep test data realistic

### Performance Testing
- Benchmark critical paths regularly
- Set performance budgets
- Monitor memory allocations
- Test with realistic data volumes

### Coverage Goals
- Aim for 80%+ unit test coverage
- Focus on business logic coverage
- Don't test framework code
- Quality over quantity 