# OpenDAoC Core Systems - Interface and Data Structure Design

## Design Principles

### Interface-First Design
- Define contracts before implementation
- Clear separation of concerns
- Dependency injection ready
- Testable and mockable interfaces
- Minimize coupling between systems

### Core Architecture Patterns
- Entity Component System (ECS) for game objects
- Service layer for system logic
- Repository pattern for data access
- Observer pattern for events
- Strategy pattern for calculations

## Core Interfaces

### Combat System Interfaces

```csharp
public interface ICombatSystem
{
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context);
    DamageResult CalculateDamage(AttackData attackData);
    void ApplyDamage(ILiving target, DamageResult damage);
}

public interface IAttacker
{
    int Level { get; }
    IWeapon ActiveWeapon { get; }
    ICombatStats CombatStats { get; }
    IList<IEffect> ActiveEffects { get; }
    AttackData PrepareAttack(IDefender target, AttackType type);
}

public interface IDefender
{
    int Level { get; }
    IArmor GetArmor(ArmorSlot slot);
    IDefenseStats DefenseStats { get; }
    DefenseResult TryDefend(AttackData attack);
    void OnAttacked(AttackData attack);
}

public interface ICombatStats
{
    int GetWeaponSkill(IWeapon weapon);
    int GetStyleDamage(IStyle style);
    double GetCriticalChance(AttackType type);
    double GetAttackSpeed(IWeapon weapon);
    IModifier GetDamageModifier(DamageType type);
}

public interface IDefenseStats
{
    double GetEvadeChance(int attackerCount);
    double GetParryChance(int attackerCount);
    double GetBlockChance(int attackerCount);
    int GetArmorFactor(ArmorSlot slot);
    double GetAbsorb(ArmorSlot slot);
    double GetResist(DamageType type);
}
```

### Character System Interfaces

```csharp
public interface ICharacter : ILiving
{
    ICharacterClass Class { get; }
    IStats BaseStats { get; }
    IStats ModifiedStats { get; }
    ISpecializationList Specializations { get; }
    IAbilityList Abilities { get; }
    IInventory Inventory { get; }
    IQuestLog QuestLog { get; }
}

public interface ICharacterClass
{
    string ID { get; }
    string Name { get; }
    ClassType Type { get; }
    Stat PrimaryStat { get; }
    Stat SecondaryStat { get; }
    Stat TertiaryStat { get; }
    Stat ManaStat { get; }
    int SpecializationMultiplier { get; }
    int BaseHP { get; }
    int WeaponSkillBase { get; }
    IList<string> AllowedRaces { get; }
    IList<IAbility> GetAbilitiesAtLevel(int level);
    IList<ISpecialization> GetSpecializations();
}

public interface ISpecialization
{
    string KeyName { get; }
    string DisplayName { get; }
    int Level { get; set; }
    int MaxLevel { get; }
    SkillType Type { get; }
    IList<ISkill> GetSkillsAtLevel(int level);
}

public interface IStats
{
    int this[Stat stat] { get; set; }
    int GetModified(Stat stat);
    void ApplyModifier(IStatModifier modifier);
    void RemoveModifier(IStatModifier modifier);
}
```

### Property Calculator Interfaces

```csharp
public interface IPropertyCalculator
{
    Property TargetProperty { get; }
    int Calculate(IPropertySource source);
}

public interface IPropertySource
{
    int GetBase(Property property);
    int GetItemBonus(Property property);
    int GetBuffBonus(Property property);
    int GetDebuffPenalty(Property property);
    IList<IPropertyModifier> GetModifiers(Property property);
}

public interface IPropertyModifier
{
    Property Target { get; }
    ModifierType Type { get; }
    int Value { get; }
    int Priority { get; }
}

public interface IPropertyCalculatorRegistry
{
    void Register(Property property, IPropertyCalculator calculator);
    IPropertyCalculator Get(Property property);
    int Calculate(IPropertySource source, Property property);
}
```

### Item System Interfaces

```csharp
public interface IItem
{
    string TemplateID { get; }
    string Name { get; }
    int Level { get; }
    int Quality { get; set; }
    int Condition { get; set; }
    int Durability { get; }
    ItemType Type { get; }
    IList<IItemBonus> Bonuses { get; }
}

public interface IEquipment : IItem
{
    EquipmentSlot Slot { get; }
    int ArmorFactor { get; }
    double Absorb { get; }
    DamageType DamageType { get; }
    IEquipmentRequirements Requirements { get; }
}

public interface IWeapon : IEquipment
{
    int DPS { get; }
    int Speed { get; }
    WeaponType WeaponType { get; }
    int Range { get; }
    IList<IWeaponProc> Procs { get; }
}

public interface IInventory
{
    IItem GetItem(InventorySlot slot);
    bool AddItem(IItem item, InventorySlot slot);
    bool RemoveItem(InventorySlot slot);
    bool MoveItem(InventorySlot from, InventorySlot to);
    IList<IItem> GetItemsInSlotRange(InventorySlot start, InventorySlot end);
    int GetFreeSlotCount(InventorySlot start, InventorySlot end);
}
```

### Guild System Interfaces

```csharp
public interface IGuild
{
    string ID { get; }
    string Name { get; }
    Realm Realm { get; }
    IGuildRankSystem Ranks { get; }
    IGuildMemberList Members { get; }
    IGuildBank Bank { get; }
    IAlliance Alliance { get; set; }
    IGuildBonuses Bonuses { get; }
}

public interface IGuildRankSystem
{
    IGuildRank this[int level] { get; }
    void SetPermission(int rankLevel, GuildPermission permission, bool value);
    bool HasPermission(IGuildMember member, GuildPermission permission);
}

public interface IGuildMember
{
    ICharacter Character { get; }
    IGuildRank Rank { get; set; }
    DateTime JoinDate { get; }
    string Note { get; set; }
    long ContributedBountyPoints { get; }
}

public interface IAlliance
{
    string ID { get; }
    string Name { get; }
    IGuild Leader { get; }
    IList<IGuild> Members { get; }
    void AddGuild(IGuild guild);
    void RemoveGuild(IGuild guild);
}
```

### Housing System Interfaces

```csharp
public interface IHouse
{
    int HouseNumber { get; }
    HouseModel Model { get; }
    IHouseOwner Owner { get; }
    IHousePermissions Permissions { get; }
    IHouseVaultSystem Vaults { get; }
    IHouseDecorations Decorations { get; }
    IConsignmentMerchant ConsignmentMerchant { get; }
}

public interface IHousePermissions
{
    bool CanEnter(ICharacter character);
    bool CanUseVault(ICharacter character, int vaultIndex, VaultPermission permission);
    bool CanDecorate(ICharacter character, DecorationType type);
    bool CanUseConsignment(ICharacter character, ConsignmentPermission permission);
    void SetPermissionLevel(ICharacter character, int level);
}

public interface IHouseVaultSystem
{
    int VaultCount { get; }
    IVault GetVault(int index);
    IVault GetAccountVault(string accountID, int index);
}

public interface IConsignmentMerchant : IMerchant
{
    long TotalMoney { get; }
    void SetPrice(IItem item, long price);
    bool BuyItem(ICharacter buyer, IItem item);
    void WithdrawMoney(ICharacter owner, long amount);
}
```

### Crafting System Interfaces

```csharp
public interface ICraftingSystem
{
    ICraftingResult Craft(ICrafter crafter, IRecipe recipe);
    IList<IRecipe> GetRecipesForSkill(CraftingSkill skill, int level);
    bool CanCraft(ICrafter crafter, IRecipe recipe);
}

public interface ICrafter
{
    int GetSkillLevel(CraftingSkill skill);
    bool HasTool(ToolType tool);
    IInventory Inventory { get; }
    ICraftingBonuses Bonuses { get; }
}

public interface IRecipe
{
    string ID { get; }
    IItemTemplate Product { get; }
    CraftingSkill RequiredSkill { get; }
    int RequiredLevel { get; }
    IList<IIngredient> Ingredients { get; }
    ToolType RequiredTool { get; }
    int CraftingTime { get; }
}

public interface IIngredient
{
    IItemTemplate Material { get; }
    int Count { get; }
    int MinQuality { get; }
}

public interface ICraftingResult
{
    bool Success { get; }
    IItem Product { get; }
    int Quality { get; }
    int SkillGain { get; }
    string FailureReason { get; }
}
```

### Quest System Interfaces

```csharp
public interface IQuestSystem
{
    IList<IQuest> GetAvailableQuests(ICharacter character);
    bool CanStartQuest(ICharacter character, IQuest quest);
    void StartQuest(ICharacter character, IQuest quest);
    void AbandonQuest(ICharacter character, IQuest quest);
}

public interface IQuest
{
    string ID { get; }
    string Name { get; }
    QuestType Type { get; }
    int MinLevel { get; }
    int MaxLevel { get; }
    IList<IQuestStep> Steps { get; }
    IQuestRewards Rewards { get; }
}

public interface IQuestStep
{
    string Description { get; }
    StepType Type { get; }
    bool IsComplete(IQuestProgress progress);
    void OnStepComplete(ICharacter character);
}

public interface IQuestProgress
{
    IQuest Quest { get; }
    int CurrentStep { get; }
    Dictionary<string, object> Variables { get; }
    void AdvanceStep();
    void SetVariable(string key, object value);
}
```

## Data Structures

### Combat Data

```csharp
public class AttackData
{
    public IAttacker Attacker { get; set; }
    public IDefender Target { get; set; }
    public AttackType Type { get; set; }
    public IWeapon Weapon { get; set; }
    public IStyle Style { get; set; }
    public DamageType DamageType { get; set; }
    public AttackResult Result { get; set; }
    public ArmorSlot ArmorHitLocation { get; set; }
    public double HitChance { get; set; }
    public double EvadeChance { get; set; }
    public double ParryChance { get; set; }
    public double BlockChance { get; set; }
}

public class DamageResult
{
    public int BaseDamage { get; set; }
    public int ModifiedDamage { get; set; }
    public int CriticalDamage { get; set; }
    public int ResistAmount { get; set; }
    public int AbsorbAmount { get; set; }
    public int TotalDamage => ModifiedDamage + CriticalDamage - ResistAmount - AbsorbAmount;
    public bool WasCritical { get; set; }
    public List<DamageModifier> Modifiers { get; set; }
}

public enum AttackType
{
    Unknown,
    Melee,
    Ranged,
    Spell
}

public enum AttackResult
{
    Any,
    Missed,
    Fumbled,
    HitUnstyled,
    HitStyle,
    Evaded,
    Blocked,
    Parried,
    NoTarget,
    NoValidTarget
}

public enum WeaponType
{
    OneHanded,
    TwoHanded,
    Longbow,
    Crossbow,
    Staff,
    Polearm
}

public enum ShieldSize
{
    Small,
    Medium,
    Large
}

public enum StylePositional
{
    Any,
    Front,
    Side,
    Back
}

public enum StyleOpeningType
{
    Any,
    Parry,
    Block,
    Evade,
    Hit,
    Miss
}

public enum AmmoType
{
    Rough,
    Standard,
    Footed
}

public enum Position
{
    Front,
    Side, 
    Back
}

public enum Abilities
{
    MasteryOfMagic,
    PenetratingArrow
}

public class AttackContext
{
    public int AttackerCount { get; set; }
    public bool IsPvP { get; set; }
}

public interface IAmmo : IItem
{
    AmmoType AmmoType { get; }
}

public interface IShield : IEquipment
{
    ShieldSize Size { get; }
    int GetMaxSimultaneousBlocks();
}

public interface IStyle
{
    string ID { get; }
    string Name { get; }
    StylePositional PositionalRequirement { get; }
    StyleOpeningType OpeningRequirement { get; }
    int GrowthRate { get; }
}

public interface ICaster : ILiving
{
    int GetAbilityLevel(Abilities ability);
}

public interface ISpell
{
    string ID { get; }
    string Name { get; }
    int Damage { get; }
    int Level { get; }
    SpellType Type { get; }
}
```

### Character Data

```csharp
public class CharacterStats : IStats
{
    private readonly Dictionary<Stat, int> _baseStats;
    private readonly Dictionary<Stat, List<IStatModifier>> _modifiers;
    
    public int this[Stat stat]
    {
        get => GetModified(stat);
        set => _baseStats[stat] = value;
    }
    
    public int GetModified(Stat stat)
    {
        int baseValue = _baseStats.GetValueOrDefault(stat, 0);
        var modifiers = _modifiers.GetValueOrDefault(stat, new List<IStatModifier>());
        return CalculateModifiedValue(baseValue, modifiers);
    }
}

public class SpecializationData
{
    public string KeyName { get; set; }
    public int Level { get; set; }
    public int SpentPoints { get; set; }
    public List<ISkill> UnlockedSkills { get; set; }
    public DateTime LastModified { get; set; }
}
```

### Item Data

```csharp
public class ItemBonus : IItemBonus
{
    public BonusType Type { get; set; }
    public int Value { get; set; }
    public Property Property { get; set; }
    public bool IsPercentage { get; set; }
    public int RequiredLevel { get; set; }
}

public class EquipmentSet
{
    public Dictionary<EquipmentSlot, IEquipment> Equipped { get; }
    public Dictionary<Property, int> TotalBonuses { get; }
    
    public void RecalculateBonuses()
    {
        TotalBonuses.Clear();
        foreach (var equipment in Equipped.Values)
        {
            foreach (var bonus in equipment.Bonuses)
            {
                TotalBonuses[bonus.Property] = 
                    TotalBonuses.GetValueOrDefault(bonus.Property, 0) + bonus.Value;
            }
        }
    }
}
```

### Guild Data

```csharp
public class GuildData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public Realm Realm { get; set; }
    public Dictionary<int, GuildRankData> Ranks { get; set; }
    public List<GuildMemberData> Members { get; set; }
    public long BountyPoints { get; set; }
    public long MeritPoints { get; set; }
    public string AllianceID { get; set; }
}

public class GuildPermissionSet
{
    private readonly HashSet<GuildPermission> _permissions;
    
    public bool Has(GuildPermission permission) => _permissions.Contains(permission);
    public void Grant(GuildPermission permission) => _permissions.Add(permission);
    public void Revoke(GuildPermission permission) => _permissions.Remove(permission);
}
```

## Service Layer Design

### Core Services

```csharp
public interface IGameService
{
    void Initialize();
    void Start();
    void Stop();
    void Update(long tick);
}

public interface ICombatService : IGameService
{
    void RegisterCombatant(ILiving combatant);
    void UnregisterCombatant(ILiving combatant);
    void ProcessCombatRound();
}

public interface IPropertyService : IGameService
{
    IPropertyCalculatorRegistry Calculators { get; }
    void RecalculateProperties(IPropertySource source);
    int GetModifiedValue(IPropertySource source, Property property);
}

public interface IEffectService : IGameService
{
    void AddEffect(ILiving target, IEffect effect);
    void RemoveEffect(ILiving target, IEffect effect);
    void ProcessEffects();
}

public interface IWorldService : IGameService
{
    void AddObject(IGameObject obj);
    void RemoveObject(IGameObject obj);
    IList<T> GetObjectsInRange<T>(IPoint3D center, int range) where T : IGameObject;
}
```

### Repository Interfaces

```csharp
public interface IRepository<T> where T : class
{
    T GetById(object id);
    IList<T> GetAll();
    IList<T> Find(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SaveChanges();
}

public interface ICharacterRepository : IRepository<ICharacter>
{
    ICharacter GetByName(string name);
    IList<ICharacter> GetByAccount(string accountId);
    IList<ICharacter> GetByGuild(string guildId);
}

public interface IItemRepository : IRepository<IItem>
{
    IList<IItem> GetByOwner(string ownerId);
    IList<IItem> GetByTemplate(string templateId);
}
```

## Event System Design

```csharp
public interface IEventManager
{
    void Subscribe<T>(IEventHandler<T> handler) where T : IGameEvent;
    void Unsubscribe<T>(IEventHandler<T> handler) where T : IGameEvent;
    void Publish<T>(T gameEvent) where T : IGameEvent;
}

public interface IEventHandler<T> where T : IGameEvent
{
    void Handle(T gameEvent);
}

public interface IGameEvent
{
    DateTime Timestamp { get; }
    IGameObject Source { get; }
}

// Example Events
public class CombatEvent : IGameEvent
{
    public DateTime Timestamp { get; set; }
    public IGameObject Source { get; set; }
    public AttackData AttackData { get; set; }
    public DamageResult DamageResult { get; set; }
}

public class PropertyChangedEvent : IGameEvent
{
    public DateTime Timestamp { get; set; }
    public IGameObject Source { get; set; }
    public Property Property { get; set; }
    public int OldValue { get; set; }
    public int NewValue { get; set; }
}
```

## Configuration System

```csharp
public interface IGameConfiguration
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
    void Load(string path);
    void Save(string path);
}

public interface ICombatConfiguration
{
    double BaseMissChance { get; }
    double MissReductionPerAttacker { get; }
    double PvPDamageModifier { get; }
    double PvEDamageModifier { get; }
    bool UseAdvancedCombatLog { get; }
}

public interface IServerConfiguration
{
    string ServerName { get; }
    int MaxPlayers { get; }
    int TickRate { get; }
    bool EnablePvP { get; }
    IDictionary<string, object> CustomSettings { get; }
}
```

## Factory Interfaces

```csharp
public interface IGameObjectFactory
{
    T Create<T>(string templateId) where T : IGameObject;
    T CreateCustom<T>(Action<T> customizer) where T : IGameObject, new();
}

public interface IItemFactory
{
    IItem CreateFromTemplate(string templateId);
    IItem CreateRandom(int level, ItemType type);
    IItem CreateUnique(string uniqueId);
}

public interface IEffectFactory
{
    IEffect CreateSpellEffect(ISpell spell, int duration);
    IEffect CreateItemEffect(IItemBonus bonus);
    IEffect CreateAbilityEffect(IAbility ability, int level);
}
```

## Migration Strategy

### Phase 1: Interface Definition
1. Define all core interfaces
2. Create adapter classes for existing code
3. Implement interface segregation
4. Add dependency injection setup

### Phase 2: Data Structure Refactoring
1. Create new data models
2. Implement mappers from old to new
3. Parallel run for validation
4. Gradual migration of systems

### Phase 3: Service Implementation
1. Implement service layer
2. Move logic from entities to services
3. Implement proper separation of concerns
4. Add comprehensive logging

### Phase 4: Testing Infrastructure
1. Create mock implementations
2. Unit test coverage for services
3. Integration test suites
4. Performance benchmarks 

## Combat Calculation Services

```csharp
public interface IMissChanceCalculator
{
    double CalculateBaseMissChance(AttackData attackData);
    double CalculateMissChance(AttackData attackData);
}

public interface IDamageCalculator
{
    double CalculateBaseDamage(IWeapon weapon);
    double CalculateWeaponSkill(AttackData attackData);
    double CalculateDamageMod(double weaponSkill, int armorFactor);
    int CalculateCriticalDamage(AttackData attackData);
}

public interface IDefenseCalculator
{
    double CalculateEvadeChance(IDefender defender, int evadeAbilityLevel, int attackerCount);
    double CalculateParryChance(IDefender defender, int parrySpec, int masteryOfParry, int attackerCount, IWeapon attackerWeapon = null);
    double CalculateBlockChance(IDefender defender, IShield shield, int shieldSpec);
}

public interface IStyleValidator
{
    bool CanUseStyle(AttackData attackData);
}

public interface IStyleCalculator
{
    double CalculateEnduranceCost(IStyle style, IWeapon weapon);
    int CalculateStyleDamage(IStyle style, int baseDamage);
}

public interface ISpellDamageCalculator
{
    int CalculateBaseDamage(ICaster caster, ISpell spell);
    double CalculateHitChance(ICaster caster, IDefender target, ISpell spell);
    (double min, double max) CalculateDamageVariance(ICaster caster, int masteryLevel);
}
```

## Character Progression Services

```csharp
public interface ICharacterProgressionService : IGameService
{
    void LevelUp(ICharacter character, int levels = 1);
    void GrantExperience(ICharacter character, long experience);
    void GrantRealmPoints(ICharacter character, long realmPoints);
    void GrantChampionExperience(ICharacter character, int experience);
}

public interface IExperienceCalculator
{
    long GetExperienceForLevel(int level);
    double CalculateGroupBonus(IGroup group);
    double CalculateCampBonus(ICharacter killer, INPC target);
    long CalculateExperienceReward(ICharacter killer, INPC target);
}

public interface ISpecializationService
{
    int GetSpecializationLevel(ICharacter character, string specializationKey);
    bool CanTrainSpecialization(ICharacter character, string specializationKey, int level);
    void TrainSpecialization(ICharacter character, string specializationKey, int level);
    int GetAvailablePoints(ICharacter character);
}

public interface ISpecializationCalculator
{
    double CalculatePointsForLevel(ICharacter character, int level);
    double CalculateTotalPoints(ICharacter character);
    double CalculateTotalPointsWithBonus(ICharacter character);
}

public interface IStatProgressionCalculator
{
    int CalculateStatGain(ICharacter character, Stat stat, int level);
    void ApplyLevelStatGains(ICharacter character, int fromLevel, int toLevel);
}

public interface IChampionLevelService
{
    bool CanGainChampionLevels(ICharacter character);
    int GetTotalPointsForLevel(int championLevel);
    IList<IAbility> GetUnlockedAbilities(ICharacter character);
    void GrantChampionLevel(ICharacter character);
}

public interface IRealmRankCalculator
{
    (int rank, int level) CalculateRealmRank(long realmPoints);
    int GetAbilityPointsForRank(int rank, int level);
    int GetBonusHitPoints(int rank, int level);
    long GetRealmPointsForRank(int rank, int level);
}

public interface IRealmRankService
{
    bool HasRealmRankAbility(ICharacter character);
    IList<IRealmAbility> GetAvailableAbilities(ICharacter character);
    bool PurchaseRealmAbility(ICharacter character, IRealmAbility ability);
}

// Character extensions
public interface ICharacter : ILiving
{
    ICharacterClass Class { get; }
    IStats BaseStats { get; }
    IStats ModifiedStats { get; }
    ISpecializationList Specializations { get; }
    IAbilityList Abilities { get; }
    IInventory Inventory { get; }
    IQuestLog QuestLog { get; }
    
    // Progression properties
    int Level { get; }
    long Experience { get; }
    int RealmRank { get; }
    long RealmPoints { get; }
    int ChampionLevel { get; }
    int ChampionExperience { get; }
    int BonusSpecPoints { get; set; }
}

// Group interface
public interface IGroup
{
    int MemberCount { get; }
    IList<ICharacter> Members { get; }
    double GetGroupBonus();
}

// NPC extensions
public interface INPC : ILiving
{
    bool HasBeenKilledInArea { get; set; }
}

// Stat enum
public enum Stat
{
    Strength,
    Constitution,
    Dexterity,
    Quickness,
    Intelligence,
    Piety,
    Empathy,
    Charisma,
    None
}

// Helper classes for test implementation
public class ExperienceCalculator : IExperienceCalculator
{
    // Simplified implementation for testing
    private readonly Dictionary<int, long> _experienceTable = new Dictionary<int, long>
    {
        {1, 0},
        {10, 51200},
        {20, 1638400},
        {50, 1073741824}
    };
    
    public long GetExperienceForLevel(int level)
    {
        return _experienceTable.GetValueOrDefault(level, 0);
    }
    
    public double CalculateGroupBonus(IGroup group)
    {
        return 1.0 + (group.MemberCount - 2) * 0.125;
    }
    
    public double CalculateCampBonus(ICharacter killer, INPC target)
    {
        return target.HasBeenKilledInArea ? 1.2 : 1.0;
    }
    
    public long CalculateExperienceReward(ICharacter killer, INPC target)
    {
        // Simplified calculation
        return 1000 * target.Level;
    }
}

public class SpecializationCalculator : ISpecializationCalculator
{
    public double CalculatePointsForLevel(ICharacter character, int level)
    {
        return level * character.Class.SpecializationMultiplier / 10.0;
    }
    
    public double CalculateTotalPoints(ICharacter character)
    {
        double total = 0;
        for (int i = 1; i <= character.Level; i++)
        {
            total += CalculatePointsForLevel(character, i);
        }
        return total;
    }
    
    public double CalculateTotalPointsWithBonus(ICharacter character)
    {
        return CalculateTotalPoints(character) + character.BonusSpecPoints;
    }
}

public class StatProgressionCalculator : IStatProgressionCalculator
{
    public int CalculateStatGain(ICharacter character, Stat stat, int level)
    {
        if (level < 6) return 0;
        
        if (character.Class.PrimaryStat == stat)
            return 1;
        else if (character.Class.SecondaryStat == stat)
            return (level - 6) % 2 == 0 ? 1 : 0;
        else if (character.Class.TertiaryStat == stat)
            return (level - 6) % 3 == 0 ? 1 : 0;
        
        return 0;
    }
    
    public void ApplyLevelStatGains(ICharacter character, int fromLevel, int toLevel)
    {
        // Implementation
    }
}

public class ChampionLevelService : IChampionLevelService
{
    public bool CanGainChampionLevels(ICharacter character)
    {
        return character.Level >= 50;
    }
    
    public int GetTotalPointsForLevel(int championLevel)
    {
        return championLevel * (championLevel + 1) / 2;
    }
    
    public IList<IAbility> GetUnlockedAbilities(ICharacter character)
    {
        return new List<IAbility>();
    }
    
    public void GrantChampionLevel(ICharacter character)
    {
        // Implementation
    }
}

public class RealmRankCalculator : IRealmRankCalculator
{
    public (int rank, int level) CalculateRealmRank(long realmPoints)
    {
        // Simplified calculation
        if (realmPoints < 25) return (1, 0);
        if (realmPoints < 125) return (1, 1);
        if (realmPoints < 6325) return (1, 2);
        if (realmPoints < 513325) return (2, 0);
        return (5, 0);
    }
    
    public int GetAbilityPointsForRank(int rank, int level)
    {
        return rank * 5 + level;
    }
    
    public int GetBonusHitPoints(int rank, int level)
    {
        return rank > 1 ? (rank - 1) * 20 : 0;
    }
    
    public long GetRealmPointsForRank(int rank, int level)
    {
        // Simplified
        return (long)Math.Pow(rank, 4) * 1000 + level * 100;
    }
}

public class RealmRankService : IRealmRankService
{
    public bool HasRealmRankAbility(ICharacter character)
    {
        return character.RealmRank >= 5;
    }
    
    public IList<IRealmAbility> GetAvailableAbilities(ICharacter character)
    {
        return new List<IRealmAbility>();
    }
    
    public bool PurchaseRealmAbility(ICharacter character, IRealmAbility ability)
    {
        return true;
    }
}
```

## Item System Services

```csharp
public interface IItemService : IGameService
{
    IItem GetItem(string itemId);
    void UpdateItem(IItem item);
    void RepairItem(IItem item, int repairAmount);
    void DegradeItem(IItem item, int degradeAmount);
}

public interface IItemFactory
{
    IItem CreateFromTemplate(string templateId);
    IItem CreateRandom(int level, ItemType type);
    IItem CreateUnique(string uniqueId);
}

public interface IItemBonusCalculator
{
    Dictionary<Property, int> CalculateTotalBonuses(IEnumerable<IItem> items);
    Dictionary<Property, int> CalculateEffectiveBonuses(ICharacter character, IEnumerable<IItem> items);
}

public interface IItemEffectivenessCalculator
{
    int CalculateEffectiveness(IItem item);
    double CalculateConditionModifier(IItem item);
}

public interface IWeaponDamageCalculator
{
    int CalculateBaseDamage(IWeapon weapon);
    double CalculateAttackSpeed(IWeapon weapon);
}

public interface IBonusCapCalculator
{
    int GetBonusCapForLevel(int level);
    int GetPropertyCap(Property property, int level);
}

// Extended Item interfaces
public interface IItem
{
    string TemplateID { get; }
    string Name { get; }
    int Level { get; }
    int BonusLevel { get; }
    int Quality { get; set; }
    int Condition { get; set; }
    int Durability { get; }
    int MaxCondition { get; }
    ItemType Type { get; }
    EquipmentSlot Slot { get; }
    Dictionary<Property, int> Bonuses { get; }
    bool IsUnique { get; }
    Dictionary<string, object> SpecialProperties { get; }
    int GetMaxBonus(Property property);
}

public interface IArtifact : IItem
{
    int ArtifactLevel { get; set; }
    int ArtifactExperience { get; set; }
    List<IItemBonus> UnlockedBonuses { get; }
}

// Item generation interfaces
public interface IRandomItemGenerator
{
    IItem GenerateRandomItem(int level, ItemType type);
    IItem ApplyRandomModifiers(IItem baseItem);
}

public interface IUniqueItemFactory
{
    IItem CreateUnique(string uniqueId);
    bool IsUniqueAvailable(string uniqueId);
}

public interface IArtifactService
{
    void GrantExperience(IArtifact artifact, int experience);
    void LevelUp(IArtifact artifact);
    List<IItemBonus> GetAvailableBonuses(IArtifact artifact);
}

public interface ICraftingService
{
    IItem CraftItem(ICrafter crafter, IRecipe recipe);
    int CalculateQuality(ICrafter crafter, IRecipe recipe);
}

// Inventory management
public interface ICharacterInventory : IInventory
{
    bool CanEquip(IItem item, EquipmentSlot slot);
    bool Equip(IItem item, EquipmentSlot slot);
    IItem GetEquippedItem(EquipmentSlot slot);
    Dictionary<EquipmentSlot, IItem> GetAllEquippedItems();
}

// Helper implementations for tests
public class ItemEffectivenessCalculator : IItemEffectivenessCalculator
{
    public int CalculateEffectiveness(IItem item)
    {
        return item.Quality;
    }
    
    public double CalculateConditionModifier(IItem item)
    {
        return (double)item.Condition / item.Durability;
    }
}

public class WeaponDamageCalculator : IWeaponDamageCalculator
{
    public int CalculateBaseDamage(IWeapon weapon)
    {
        return weapon.DPS * weapon.Speed / 10;
    }
    
    public double CalculateAttackSpeed(IWeapon weapon)
    {
        return weapon.Speed / 10.0;
    }
}

public class BonusCapCalculator : IBonusCapCalculator
{
    public int GetBonusCapForLevel(int level)
    {
        if (level < 15) return 0;
        if (level < 20) return 5;
        if (level < 25) return 10;
        if (level < 30) return 15;
        if (level < 35) return 20;
        if (level < 40) return 25;
        if (level < 45) return 30;
        return 35;
    }
    
    public int GetPropertyCap(Property property, int level)
    {
        int baseCap = GetBonusCapForLevel(level);
        // Special cases for different properties
        if (property == Property.HitPoints) return baseCap * 6;
        if (property >= Property.Resist_Body && property <= Property.Resist_Spirit) 
            return (int)(baseCap * 0.74);
        return baseCap;
    }
}

public class ItemBonusCalculator : IItemBonusCalculator
{
    private readonly IBonusCapCalculator _capCalculator = new BonusCapCalculator();
    
    public Dictionary<Property, int> CalculateTotalBonuses(IEnumerable<IItem> items)
    {
        var totals = new Dictionary<Property, int>();
        foreach (var item in items)
        {
            foreach (var bonus in item.Bonuses)
            {
                if (!totals.ContainsKey(bonus.Key))
                    totals[bonus.Key] = 0;
                totals[bonus.Key] += bonus.Value;
            }
        }
        return totals;
    }
    
    public Dictionary<Property, int> CalculateEffectiveBonuses(ICharacter character, IEnumerable<IItem> items)
    {
        var totals = CalculateTotalBonuses(items);
        var effective = new Dictionary<Property, int>();
        
        foreach (var kvp in totals)
        {
            int cap = _capCalculator.GetPropertyCap(kvp.Key, character.Level);
            effective[kvp.Key] = Math.Min(kvp.Value, cap);
        }
        
        return effective;
    }
}

public class CharacterInventory : ICharacterInventory
{
    private readonly Dictionary<EquipmentSlot, IItem> _equipped = new Dictionary<EquipmentSlot, IItem>();
    private readonly Dictionary<InventorySlot, IItem> _inventory = new Dictionary<InventorySlot, IItem>();
    
    public bool CanEquip(IItem item, EquipmentSlot slot)
    {
        // Check if item can go in slot
        if (item.Slot != slot && item.Slot != EquipmentSlot.Ring && item.Slot != EquipmentSlot.Bracer)
            return false;
            
        // Check for two-handed conflicts
        if (slot == EquipmentSlot.LeftHand && _equipped.ContainsKey(EquipmentSlot.TwoHand))
            return false;
        if (slot == EquipmentSlot.TwoHand && (_equipped.ContainsKey(EquipmentSlot.LeftHand) || 
                                               _equipped.ContainsKey(EquipmentSlot.RightHand)))
            return false;
            
        return true;
    }
    
    public bool Equip(IItem item, EquipmentSlot slot)
    {
        if (!CanEquip(item, slot))
            return false;
            
        _equipped[slot] = item;
        return true;
    }
    
    public IItem GetEquippedItem(EquipmentSlot slot)
    {
        return _equipped.GetValueOrDefault(slot);
    }
    
    public Dictionary<EquipmentSlot, IItem> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentSlot, IItem>(_equipped);
    }
    
    // IInventory implementation
    public IItem GetItem(InventorySlot slot)
    {
        return _inventory.GetValueOrDefault(slot);
    }
    
    public bool AddItem(IItem item, InventorySlot slot)
    {
        if (_inventory.ContainsKey(slot))
            return false;
        _inventory[slot] = item;
        return true;
    }
    
    public bool RemoveItem(InventorySlot slot)
    {
        return _inventory.Remove(slot);
    }
    
    public bool MoveItem(InventorySlot from, InventorySlot to)
    {
        if (!_inventory.TryGetValue(from, out var item) || _inventory.ContainsKey(to))
            return false;
        _inventory.Remove(from);
        _inventory[to] = item;
        return true;
    }
    
    public IList<IItem> GetItemsInSlotRange(InventorySlot start, InventorySlot end)
    {
        var items = new List<IItem>();
        for (var slot = start; slot <= end; slot++)
        {
            if (_inventory.TryGetValue(slot, out var item))
                items.Add(item);
        }
        return items;
    }
    
    public int GetFreeSlotCount(InventorySlot start, InventorySlot end)
    {
        int count = 0;
        for (var slot = start; slot <= end; slot++)
        {
            if (!_inventory.ContainsKey(slot))
                count++;
        }
        return count;
    }
}

public class RandomItemGenerator : IRandomItemGenerator
{
    private readonly Random _random = new Random();
    
    public IItem GenerateRandomItem(int level, ItemType type)
    {
        // Simplified implementation
        var item = new Mock<IItem>();
        item.Setup(x => x.Level).Returns(level);
        item.Setup(x => x.Type).Returns(type);
        item.Setup(x => x.Quality).Returns(_random.Next(85, 101));
        item.Setup(x => x.Bonuses).Returns(new Dictionary<Property, int>());
        return item.Object;
    }
    
    public IItem ApplyRandomModifiers(IItem baseItem)
    {
        var modCount = _random.Next(1, 6);
        var bonuses = new Dictionary<Property, int>(baseItem.Bonuses);
        
        var properties = Enum.GetValues<Property>();
        for (int i = 0; i < modCount; i++)
        {
            var prop = properties[_random.Next(properties.Length)];
            var value = _random.Next(1, 11);
            bonuses[prop] = value;
        }
        
        // Return modified copy
        var modifiedItem = new Mock<IItem>();
        modifiedItem.Setup(x => x.Bonuses).Returns(bonuses);
        // Copy other properties...
        return modifiedItem.Object;
    }
}

public class UniqueItemFactory : IUniqueItemFactory
{
    private readonly Dictionary<string, Func<IItem>> _uniqueTemplates = new Dictionary<string, Func<IItem>>();
    
    public IItem CreateUnique(string uniqueId)
    {
        var item = new Mock<IItem>();
        item.Setup(x => x.IsUnique).Returns(true);
        item.Setup(x => x.Name).Returns(uniqueId);
        item.Setup(x => x.SpecialProperties).Returns(new Dictionary<string, object> { { "Special", true } });
        return item.Object;
    }
    
    public bool IsUniqueAvailable(string uniqueId)
    {
        return _uniqueTemplates.ContainsKey(uniqueId);
    }
}

public class ArtifactService : IArtifactService
{
    public void GrantExperience(IArtifact artifact, int experience)
    {
        artifact.ArtifactExperience += experience;
        if (artifact.ArtifactExperience >= GetRequiredExperience(artifact.ArtifactLevel + 1))
        {
            LevelUp(artifact);
        }
    }
    
    public void LevelUp(IArtifact artifact)
    {
        artifact.ArtifactLevel++;
        // Unlock new bonuses
        artifact.UnlockedBonuses.Add(new Mock<IItemBonus>().Object);
    }
    
    public List<IItemBonus> GetAvailableBonuses(IArtifact artifact)
    {
        return artifact.UnlockedBonuses;
    }
    
    private int GetRequiredExperience(int level)
    {
        return level * 1000;
    }
}

public class CraftingService : ICraftingService
{
    private readonly Random _random = new Random();
    
    public IItem CraftItem(ICrafter crafter, IRecipe recipe)
    {
        var quality = CalculateQuality(crafter, recipe);
        var item = new Mock<IItem>();
        item.Setup(x => x.Quality).Returns(quality);
        item.Setup(x => x.CrafterName).Returns(crafter.Name);
        item.Setup(x => x.HasCraftingBonus).Returns(quality >= 94);
        return item.Object;
    }
    
    public int CalculateQuality(ICrafter crafter, IRecipe recipe)
    {
        var skill = crafter.GetSkillLevel(CraftingSkill.Weaponcrafting);
        var diff = skill - recipe.RequiredLevel;
        if (diff >= 50) return 94 + _random.Next(7); // 94-100
        if (diff >= 0) return 85 + _random.Next(16); // 85-100
        return 85; // Minimum
    }
}

// Extended item interfaces for crafting
public interface IItem
{
    string CrafterName { get; }
    bool HasCraftingBonus { get; }
}

public interface ICrafter
{
    string Name { get; }
    int GetSkillLevel(CraftingSkill skill);
    bool HasTool(ToolType tool);
    IInventory Inventory { get; }
    ICraftingBonuses Bonuses { get; }
}
``` 