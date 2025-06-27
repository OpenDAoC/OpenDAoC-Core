# Ability Handler System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The ability handler system processes every special action you can perform - from combat abilities like Berserk and Sprint to realm abilities and class-specific skills. It manages cooldowns, checks prerequisites, and applies effects, ensuring each ability works correctly and fairly within the game's rules.

The Ability Handler System manages all player abilities, realm abilities, and skill-based actions in OpenDAoC. It provides a unified framework for executing abilities, managing cooldowns, checking prerequisites, and handling ability effects through a sophisticated interface-based architecture.

## Core Architecture

### Interface Hierarchy

```csharp
// Base ability action handler interface
public interface IAbilityActionHandler
{
    void Execute(Ability ability, GamePlayer player);
}

// Spell-casting ability interface
public interface ISpellCastingAbilityHandler
{
    Spell Spell { get; }
    SpellLine SpellLine { get; }
    Ability Ability { get; }
}

// Specialization action handler interface  
public interface ISpecActionHandler
{
    void Execute(Specialization spec, GamePlayer player);
}
```

### Ability Base Classes

```csharp
// Core ability class
public abstract class Ability : NamedSkill
{
    public virtual void Execute(GameLiving living);
    public virtual void Activate(GameLiving living, bool sendUpdates);
    public virtual void Deactivate(GameLiving living, bool sendUpdates);
    public virtual void OnLevelChange(int oldLevel, int newLevel);
    
    protected string m_spec;
    protected int m_speclevel;
    protected GameLiving m_activeLiving;
}

// Property-changing abilities
public class PropertyChangingAbility : Ability
{
    protected eProperty[] m_property;
    public virtual int GetAmountForLevel(int level);
    public int Amount { get; }
    public virtual void SendUpdates(GameLiving target);
}

// Stat-changing abilities
public class StatChangingAbility : PropertyChangingAbility
{
    public override int GetAmountForLevel(int level);
    public override void SendUpdates(GameLiving target);
}

// Level-based stat abilities
public class LevelBasedStatChangingAbility : StatChangingAbility
{
    public override int Level { get; set; } // Uses living's level
    public override string Name { get; set; } // Includes stat amount
}
```

## Handler Registration System

### Handler Discovery

```csharp
// Attribute-based handler registration
[SkillHandlerAttribute(Abilities.Berserk)]
public class BerserkAbilityHandler : IAbilityActionHandler
{
    public void Execute(Ability ab, GamePlayer player) { }
}

// Handler loading process
private static void LoadAbilityHandlers()
{
    // 1. Search GameServer assembly
    IList<KeyValuePair<string, Type>> handlers = 
        ScriptMgr.FindAllAbilityActionHandler(Assembly.GetExecutingAssembly());
    
    // 2. Search script assemblies (override GameServer handlers)
    foreach (Assembly asm in ScriptMgr.Scripts)
    {
        handlers = ScriptMgr.FindAllAbilityActionHandler(asm);
        // Register handlers with compiled constructors for performance
    }
}

// Handler storage
private static readonly Dictionary<string, Func<IAbilityActionHandler>> 
    m_abilityActionHandler = new();
```

### Handler Factory System

```csharp
// Compiled constructor factory for performance
private static Func<IAbilityActionHandler> GetNewAbilityActionHandlerConstructor(Type type)
{
    try
    {
        return CompiledConstructorFactory.CompileConstructor(type, []) 
            as Func<IAbilityActionHandler>;
    }
    catch (Exception e)
    {
        log.Error(e);
        return null;
    }
}

// Handler instantiation
public static IAbilityActionHandler GetAbilityActionHandler(string keyName)
{
    if (m_abilityActionHandler.TryGetValue(keyName, out var constructor))
        return constructor();
    return null;
}
```

## Ability Types

### 1. Active Abilities

```csharp
// Instant activation abilities
[SkillHandlerAttribute(Abilities.Sprint)]
public class SprintAbilityHandler : IAbilityActionHandler
{
    public void Execute(Ability ab, GamePlayer player)
    {
        // Precondition checks
        if (player.IsStunned || player.IsMezzed) return;
        
        // Apply effect
        new SprintECSGameEffect(new ECSGameEffectInitParams(player, 0, 1));
        
        // Set cooldown
        player.DisableSkill(ab, REUSE_TIMER);
    }
}

// Toggled abilities
[SkillHandlerAttribute(Abilities.Engage)]
public class EngageAbilityHandler : IAbilityActionHandler
{
    public void Execute(Ability ab, GamePlayer player)
    {
        // Toggle engage state
        if (player.EngageTarget != null)
            player.Disengage();
        else
            player.Engage(player.TargetObject as GameLiving);
    }
}
```

### 2. Spell-Casting Abilities

```csharp
// Base spell-casting handler
public class SpellCastingAbilityHandler : IAbilityActionHandler, ISpellCastingAbilityHandler
{
    public virtual int SpellID { get; }
    public virtual long Preconditions { get; }
    public virtual Spell Spell { get; }
    public virtual SpellLine SpellLine { get; }
    
    public void Execute(Ability ab, GamePlayer player)
    {
        if (CheckPreconditions(player, Preconditions)) return;
        
        if (SpellLine != null && Spell != null)
            player.CastSpell(this);
    }
}

// Specific spell ability
[SkillHandlerAttribute(Abilities.Fury)]
public class FuryAbilityHandler : SpellCastingAbilityHandler
{
    public override long Preconditions => DEAD | SITTING | MEZZED | STUNNED;
    public override int SpellID => 14374;
}
```

### 3. Spell Line Abilities

```csharp
// Abstract spell line ability
public abstract class SpellLineAbstractAbility : Ability, ISpellCastingAbilityHandler
{
    public Spell Spell => GetSpellForLevel(Level);
    
    public SpellLine SpellLine 
    {
        get
        {
            var line = SkillBase.GetSpellLine(KeyName);
            if (line != null) line.Level = Level;
            return line;
        }
    }
    
    public Spell GetSpellForLevel(int level)
    {
        var line = SpellLine;
        return line?.GetSpellList().FirstOrDefault(spell => spell.Level == level);
    }
}

// Active spell line ability
public class SpellLineActiveAbility : SpellLineAbstractAbility
{
    public override void Execute(GameLiving living)
    {
        base.Execute(living);
        if (Spell != null && SpellLine != null)
            living.CastSpell(this);
    }
}
```

### 4. Realm Abilities

```csharp
// Base realm ability
public abstract class RealmAbility : Ability
{
    public virtual int CostForUpgrade(int level);
    public virtual bool CheckRequirement(GamePlayer player);
    public abstract int MaxLevel { get; }
}

// Timed realm abilities
public class TimedRealmAbility : RealmAbility
{
    public virtual int GetReUseDelay(int level) => 0;
    public virtual void DisableSkill(GameLiving living)
    {
        living.DisableSkill(this, GetReUseDelay(Level) * 1000);
    }
    
    public override int MaxLevel => 
        ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING ? 5 : 3;
}

// RR5 abilities
public abstract class RR5RealmAbility : TimedRealmAbility
{
    public override bool CheckRequirement(GamePlayer player)
    {
        return player.RealmLevel >= 40; // RR5L0
    }
}
```

## Ability Execution Flow

### 1. Ability Activation

```csharp
// Player uses ability (UI click, command, etc.)
public void UseAbility(string abilityKey)
{
    // 1. Get ability from player's ability list
    Ability ability = GetAbility(abilityKey);
    if (ability == null) return;
    
    // 2. Check if ability is disabled (cooldown)
    if (IsSkillDisabled(ability)) return;
    
    // 3. Get registered handler
    IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(abilityKey);
    if (handler == null) return;
    
    // 4. Execute handler
    handler.Execute(ability, this);
}
```

### 2. Precondition Checking

```csharp
// Standard precondition flags
public const long DEAD = 0x01;
public const long SITTING = 0x02;
public const long MEZZED = 0x04;
public const long STUNNED = 0x08;
public const long INCOMBAT = 0x10;
public const long STEALTHED = 0x20;

// Precondition check utility
public static bool CheckPreconditions(GamePlayer player, long preconditions)
{
    if ((preconditions & DEAD) != 0 && !player.IsAlive) return true;
    if ((preconditions & SITTING) != 0 && player.IsSitting) return true;
    if ((preconditions & MEZZED) != 0 && player.IsMezzed) return true;
    if ((preconditions & STUNNED) != 0 && player.IsStunned) return true;
    if ((preconditions & INCOMBAT) != 0 && player.InCombat) return true;
    if ((preconditions & STEALTHED) != 0 && player.IsStealthed) return true;
    return false;
}
```

### 3. Cooldown Management

```csharp
// Disable ability for specific duration
public void DisableSkill(Ability ability, int duration)
{
    // Add to disabled skills with timestamp
    m_disabledSkills[ability] = GameLoop.GameLoopTime + duration;
    
    // Send UI update
    Out.SendUpdateIcons(null, ref m_lastUpdateEffectsCount);
}

// Check if ability is on cooldown
public bool IsSkillDisabled(Ability ability)
{
    if (m_disabledSkills.TryGetValue(ability, out long enableTime))
        return GameLoop.GameLoopTime < enableTime;
    return false;
}
```

## Ability Categories

### Combat Abilities

```csharp
// Berserk ability
[SkillHandlerAttribute(Abilities.Berserk)]
public class BerserkAbilityHandler : IAbilityActionHandler
{
    protected const int REUSE_TIMER = 60000 * 7; // 7 minutes
    public const int DURATION = 20000; // 20 seconds
    
    public void Execute(Ability ab, GamePlayer player)
    {
        // Apply berserk effect
        new BerserkECSGameEffect(new ECSGameEffectInitParams(player, DURATION, 1));
        player.DisableSkill(ab, REUSE_TIMER);
    }
}

// Triple Wield ability
[SkillHandler(Abilities.Triple_Wield)]
public class TripleWieldAbilityHandler : IAbilityActionHandler
{
    protected const int REUSE_TIMER = 7 * 60; // 7 minutes
    public const int DURATION = 30; // 30 seconds
}
```

### Stealth Abilities

```csharp
// Stealth ability
[SkillHandlerAttribute(Abilities.Stealth)]
public class StealthAbilityHandler : IAbilityActionHandler
{
    public void Execute(Ability ab, GamePlayer player)
    {
        if (player.IsStealthed)
            player.Stealth(false); // Unstealth
        else
            player.Stealth(true); // Stealth
    }
}

// Distraction ability
[SkillHandlerAttribute(Abilities.Distraction)]
public class DistractionAbilityHandler : IAbilityActionHandler
{
    protected const int REUSE_TIMER = 10000; // 10 seconds
    public const int DURATION = 4000; // 4 seconds
}
```

### Ranged Combat Abilities

```csharp
// Sure Shot ability
[SkillHandlerAttribute(Abilities.SureShot)]
public class SureShotAbilityHandler : IAbilityActionHandler
{
    public void Execute(Ability ab, GamePlayer player)
    {
        // Toggle sure shot effect
        if (EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) 
            is SureShotECSGameEffect sureShot)
        {
            sureShot.Stop();
            return;
        }
        
        // Cancel conflicting effects
        RapidFireECSGameEffect rapidFire = 
            EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire) 
            as RapidFireECSGameEffect;
        rapidFire?.Stop(false);
        
        // Apply sure shot effect
        new SureShotECSGameEffect(new ECSGameEffectInitParams(player, 0, 1));
    }
}
```

### Class-Specific Abilities

```csharp
// Climbing ability for specific classes
[SkillHandlerAttribute(Abilities.ClimbSpikes)]
public class ClimbingAbilityHandler : SpellCastingAbilityHandler
{
    public override long Preconditions => DEAD | SITTING | MEZZED | STUNNED;
    
    public override int SpellID
    {
        get
        {
            // Dynamic spell lookup by name
            DbSpell climbSpell = DOLDB<DbSpell>.SelectObject(
                DB.Column("Name").IsEqualTo(Abilities.ClimbSpikes));
            return climbSpell?.SpellID ?? 0;
        }
    }
}
```

## Ability Effect Integration

### ECS Effect System

```csharp
// Ability effects use ECS system
public class BerserkECSGameEffect : ECSGameSpellEffect
{
    public BerserkECSGameEffect(ECSGameEffectInitParams initParams) 
        : base(initParams) { }
    
    public override void OnStartEffect()
    {
        // Apply berserk bonuses
        Owner.AbilityBonus[eProperty.MeleeDamage] += 25;
        Owner.AbilityBonus[eProperty.SpellResistChance] += 25;
    }
    
    public override void OnStopEffect()
    {
        // Remove bonuses
        Owner.AbilityBonus[eProperty.MeleeDamage] -= 25;
        Owner.AbilityBonus[eProperty.SpellResistChance] -= 25;
    }
}
```

### Effect Stacking

```csharp
// Effect conflicts and stacking
public enum eEffect
{
    Berserk,
    TripleWield,
    SureShot,
    RapidFire,
    TrueShot,
    Sprint,
    Engage,
    // ... many more
}

// Effect management
public static class EffectListService
{
    public static T GetAbilityEffectOnTarget<T>(GameLiving target, eEffect effectType) 
        where T : ECSGameEffect;
    
    public static void CancelEffect(GameLiving target, eEffect effectType);
    
    public static bool HasEffect(GameLiving target, eEffect effectType);
}
```

## Ability Learning and Progression

### Ability Acquisition

```csharp
// Learn ability at specific level
public bool LearnAbility(string abilityKey, int level, string spec = "")
{
    Ability ability = SkillBase.GetAbility(abilityKey, level);
    if (ability == null) return false;
    
    // Check requirements
    if (!CanLearnAbility(ability)) return false;
    
    // Add to player's abilities
    m_abilities[abilityKey] = ability;
    ability.Activate(this, true);
    
    return true;
}

// Remove ability
public bool RemoveAbility(string abilityKey)
{
    if (m_abilities.TryGetValue(abilityKey, out Ability ability))
    {
        ability.Deactivate(this, true);
        m_abilities.Remove(abilityKey);
        return true;
    }
    return false;
}
```

### Level-Based Abilities

```csharp
// Abilities that scale with character level
public class LevelBasedStatChangingAbility : StatChangingAbility
{
    public override int Level
    {
        get => m_activeLiving?.Level ?? int.MaxValue;
        set => base.Level = m_activeLiving?.Level ?? value;
    }
    
    public override string Name => m_activeLiving != null ? 
        $"{base.Name} +{GetAmountForLevel(Level)}" : base.Name;
}
```

## Configuration and Rules

### Server Properties

```csharp
public static class ServerProperties
{
    // Realm ability scaling
    public static bool USE_NEW_ACTIVES_RAS_SCALING;
    
    // Ability-specific settings
    public static int BERSERK_DAMAGE_BONUS = 25;
    public static int BERSERK_DURATION = 20000;
    public static int SPRINT_SPEED_BONUS = 50;
}
```

### Game Rules Integration

```csharp
// Ability cooldown formulas
public virtual int GetReUseDelay(int level)
{
    // Standard timed RA formula
    if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
    {
        return level switch
        {
            1 => 300, // 5 minutes
            2 => 240, // 4 minutes  
            3 => 180, // 3 minutes
            4 => 120, // 2 minutes
            5 => 60,  // 1 minute
            _ => 300
        };
    }
    else
    {
        return 300 - (level - 1) * 60; // Original scaling
    }
}
```

## Performance Considerations

### Handler Caching

```csharp
// Compiled constructors for performance
private static readonly Dictionary<string, Func<IAbilityActionHandler>> 
    m_abilityActionHandler = new();

// Fast handler lookup
public static IAbilityActionHandler GetAbilityActionHandler(string keyName)
{
    return m_abilityActionHandler.TryGetValue(keyName, out var constructor) 
        ? constructor() : null;
}
```

### Memory Management

```csharp
// Ability instances are reused when possible
private static readonly Dictionary<string, DbAbility> m_abilityIndex = new();

// Lazy loading of ability data
public static Ability GetAbility(string keyname, int level)
{
    if (m_abilityIndex.TryGetValue(keyname, out DbAbility dba))
        return GetNewAbilityInstance(dba, level);
    
    return null;
}
```

## Testing Framework

### Mock Abilities

```csharp
public class MockAbility : Ability
{
    public bool ExecuteCalled { get; private set; }
    public GameLiving LastExecuteTarget { get; private set; }
    
    public override void Execute(GameLiving living)
    {
        ExecuteCalled = true;
        LastExecuteTarget = living;
        base.Execute(living);
    }
}
```

### Handler Testing

```csharp
[Test]
public void BerserkHandler_ShouldApplyEffect_WhenExecuted()
{
    // Arrange
    var player = TestDataFactory.CreateWarrior();
    var ability = SkillBase.GetAbility(Abilities.Berserk);
    var handler = new BerserkAbilityHandler();
    
    // Act
    handler.Execute(ability, player);
    
    // Assert
    var effect = EffectListService.GetAbilityEffectOnTarget(player, eEffect.Berserk);
    effect.Should().NotBeNull();
    player.AbilityBonus[eProperty.MeleeDamage].Should().Be(25);
}
```

## Error Handling

### Handler Exceptions

```csharp
public void Execute(Ability ab, GamePlayer player)
{
    try
    {
        // Validate inputs
        if (ab == null || player == null)
        {
            log.Error("Invalid ability or player in handler execution");
            return;
        }
        
        // Execute ability logic
        ExecuteAbilityLogic(ab, player);
    }
    catch (Exception ex)
    {
        log.Error($"Error executing ability {ab?.Name} for player {player?.Name}: {ex}");
        
        // Send error message to player
        player?.Out.SendMessage("Ability failed to execute.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

### Validation

```csharp
public static bool ValidateAbilityExecution(Ability ability, GamePlayer player)
{
    // Check player state
    if (!player.IsAlive)
    {
        player.Out.SendMessage("You cannot use abilities while dead.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    // Check ability cooldown
    if (player.IsSkillDisabled(ability))
    {
        player.Out.SendMessage("That ability is not ready yet.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    return true;
}
```

## Integration Points

### Combat System

```csharp
// Abilities that affect combat
public class TripleWieldEffect : ECSGameSpellEffect
{
    public override void OnStartEffect()
    {
        // Modify combat mechanics
        Owner.TempProperties.Set("TripleWieldActive", true);
        Owner.AttackWeapon(Owner.ActiveWeapon, null, true); // Extra attack
    }
}
```

### Spell System

```csharp
// Spell-casting abilities integrate with magic system
public void CastSpell(ISpellCastingAbilityHandler handler)
{
    if (handler.Spell != null && handler.SpellLine != null)
    {
        // Use standard spell casting with ability context
        CastSpell(handler.Spell, handler.SpellLine, 
            sourceType: eSpellCastSourceType.Ability);
    }
}
```

### Property System

```csharp
// Abilities that modify properties
public override void Activate(GameLiving living, bool sendUpdates)
{
    if (m_activeLiving == null)
    {
        m_activeLiving = living;
        foreach (eProperty property in m_property)
        {
            living.AbilityBonus[property] += GetAmountForLevel(living.CalculateSkillLevel(this));
        }
        if (sendUpdates) SendUpdates(living);
    }
}
```

## Future Enhancements

### TODO: Missing Documentation
- Realm ability effect scaling formulas
- Cross-realm ability restrictions
- Ability macro system
- Dynamic ability acquisition based on equipment
- Temporary ability grants from items/buffs

## Change Log

- **v1.0** (2025-01-20): Initial comprehensive documentation
  - Complete interface hierarchy
  - All handler types documented
  - Execution flow and lifecycle
  - Integration with other systems
  - Performance considerations
  - Testing framework

## References

- Core_Systems_Game_Rules.md - Game mechanics
- ECS_Performance_System.md - Effect management
- Combat_Magic_Integration.md - Cross-system interactions
- Property_Calculator_System.md - Property modifications 