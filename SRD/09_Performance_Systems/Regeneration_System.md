# Regeneration System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from regeneration implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Your character automatically recovers health, mana, and endurance over time. Higher Constitution increases health recovery, sitting doubles most regeneration rates, and being in combat slows recovery. Casters regenerate mana faster with higher mental stats and meditation skill.

The Regeneration System manages automatic recovery of Health, Power (mana), and Endurance for all living entities. This system operates on regular tick intervals with various modifiers affecting regeneration rates.

## Core Architecture

### Regeneration Types
```csharp
public enum RegenerationType
{
    Health,
    Power,
    Endurance
}

public interface IRegenerative
{
    int HealthRegenRate { get; }
    int PowerRegenRate { get; }
    int EnduranceRegenRate { get; }
    
    void RegenerateHealth(int amount);
    void RegeneratePower(int amount);
    void RegenerateEndurance(int amount);
}
```

## Health Regeneration

### Player Health Regen
```csharp
public static void ProcessHealthRegen(GamePlayer player)
{
    if (player.Health >= player.MaxHealth)
        return;
        
    int regenAmount = GetHealthRegenAmount(player);
    
    // Apply modifiers
    regenAmount = ApplyHealthRegenModifiers(player, regenAmount);
    
    // Cap at max health
    player.Health = Math.Min(player.MaxHealth, player.Health + regenAmount);
}

private static int GetHealthRegenAmount(GamePlayer player)
{
    // Base: 1 HP per tick + Constitution bonus
    int baseRegen = 1;
    int conBonus = (player.Constitution - 50) / 4; // 1 HP per 4 CON above 50
    
    return Math.Max(1, baseRegen + conBonus);
}

private static int ApplyHealthRegenModifiers(GamePlayer player, int baseRegen)
{
    double modifier = 1.0;
    
    // Toughness Realm Ability
    modifier += player.GetAbilityLevel(Abilities.Toughness) * 0.05;
    
    // Item bonuses
    modifier += player.ItemBonus[eProperty.HealthRegeneration] * 0.01;
    
    // Sitting bonus
    if (player.IsSitting)
        modifier += 1.0; // Double regen when sitting
        
    return (int)(baseRegen * modifier);
}
```

### NPC Health Regen
```csharp
public static void ProcessNPCHealthRegen(GameNPC npc)
{
    if (npc.Health >= npc.MaxHealth)
        return;
        
    // NPCs regenerate much faster than players
    int regenAmount = (int)(npc.MaxHealth * 0.05); // 5% per tick
    
    // Reduce regen if in combat
    if (npc.InCombat)
        regenAmount /= 4; // 25% of normal regen
        
    npc.Health = Math.Min(npc.MaxHealth, npc.Health + regenAmount);
}
```

## Power Regeneration

### Mana Recovery System
```csharp
public static void ProcessPowerRegen(GamePlayer player)
{
    if (player.Mana >= player.MaxMana)
        return;
        
    int regenAmount = GetPowerRegenAmount(player);
    
    // Apply power regen modifiers
    regenAmount = ApplyPowerRegenModifiers(player, regenAmount);
    
    player.Mana = Math.Min(player.MaxMana, player.Mana + regenAmount);
}

private static int GetPowerRegenAmount(GamePlayer player)
{
    // Base: 1 mana per tick + Acuity bonus
    int baseRegen = 1;
    int acuityBonus = (player.Acuity - 50) / 4; // 1 mana per 4 acuity above 50
    
    return Math.Max(1, baseRegen + acuityBonus);
}

private static int ApplyPowerRegenModifiers(GamePlayer player, int baseRegen)
{
    double modifier = 1.0;
    
    // Meditation skill bonus
    modifier += player.GetSkillLevel(Skill.Meditation) * 0.01;
    
    // Item power regen bonuses
    modifier += player.ItemBonus[eProperty.PowerRegeneration] * 0.01;
    
    // Sitting bonus
    if (player.IsSitting)
        modifier += 1.0; // Double regen when sitting
        
    // Exhaustion penalty
    if (player.Endurance < player.MaxEndurance * 0.25)
        modifier *= 0.5; // Halved when exhausted
        
    return (int)(baseRegen * modifier);
}
```

## Endurance Regeneration

### Stamina Recovery
```csharp
public static void ProcessEnduranceRegen(GamePlayer player)
{
    if (player.Endurance >= player.MaxEndurance)
        return;
        
    int regenAmount = GetEnduranceRegenAmount(player);
    
    // Apply endurance regen modifiers
    regenAmount = ApplyEnduranceRegenModifiers(player, regenAmount);
    
    player.Endurance = Math.Min(player.MaxEndurance, player.Endurance + regenAmount);
}

private static int GetEnduranceRegenAmount(GamePlayer player)
{
    // Base: 5% of max endurance per tick
    return (int)(player.MaxEndurance * 0.05);
}

private static int ApplyEnduranceRegenModifiers(GamePlayer player, int baseRegen)
{
    double modifier = 1.0;
    
    // Constitution affects endurance regen
    modifier += (player.Constitution - 50) * 0.002; // 0.2% per CON point
    
    // Sitting bonus
    if (player.IsSitting)
        modifier += 0.5; // 150% when sitting
        
    // Moving penalty
    if (player.IsMoving)
        modifier *= 0.3; // 30% when moving
        
    // Combat penalty
    if (player.InCombat)
        modifier *= 0.1; // 10% in combat
        
    return (int)(baseRegen * modifier);
}
```

## Regeneration Timing

### Tick System Integration
```csharp
public static class RegenerationIntervals
{
    public const int HEALTH_REGEN_INTERVAL = 6000; // 6 seconds
    public const int POWER_REGEN_INTERVAL = 4000;  // 4 seconds  
    public const int ENDURANCE_REGEN_INTERVAL = 2000; // 2 seconds
}

public static void ProcessRegeneration(GameLiving living)
{
    long currentTime = GameLoop.GameLoopTime;
    
    if (currentTime - living.LastHealthRegenTick >= HEALTH_REGEN_INTERVAL)
    {
        ProcessHealthRegeneration(living);
        living.LastHealthRegenTick = currentTime;
    }
    
    if (living is GamePlayer player)
    {
        if (currentTime - player.LastPowerRegenTick >= POWER_REGEN_INTERVAL)
        {
            ProcessPowerRegeneration(player);
            player.LastPowerRegenTick = currentTime;
        }
        
        if (currentTime - player.LastEnduranceRegenTick >= ENDURANCE_REGEN_INTERVAL)
        {
            ProcessEnduranceRegeneration(player);
            player.LastEnduranceRegenTick = currentTime;
        }
    }
}
```

## Special Regeneration Effects

### Realm Abilities
```csharp
// Toughness - Health regen bonus
public static double GetToughnessBonus(GamePlayer player)
{
    return player.GetAbilityLevel(Abilities.Toughness) * 0.05; // 5% per level
}

// Augmented Acuity - Faster power regen
public static double GetAugmentedAcuityBonus(GamePlayer player)
{
    return player.GetAbilityLevel(Abilities.AugmentedAcuity) * 0.03; // 3% per level
}
```

### Spell Effects
```csharp
// Regeneration spells that boost natural regen
public static void ApplyRegenerationSpell(GamePlayer target, Spell spell)
{
    var effect = new RegenerationEffect(spell.Duration)
    {
        HealthRegenBonus = spell.Value,
        PowerRegenBonus = spell.Damage, // Damage value used for power
        Source = spell
    };
    
    target.EffectList.Add(effect);
}
```

## Configuration

```csharp
[ServerProperty("regen", "health_regen_interval", 6000)]
public static int HEALTH_REGEN_INTERVAL;

[ServerProperty("regen", "power_regen_interval", 4000)]
public static int POWER_REGEN_INTERVAL;

[ServerProperty("regen", "endurance_regen_interval", 2000)]
public static int ENDURANCE_REGEN_INTERVAL;

[ServerProperty("regen", "sitting_bonus_multiplier", 2.0)]
public static double SITTING_BONUS_MULTIPLIER;

[ServerProperty("regen", "combat_regen_penalty", 0.1)]
public static double COMBAT_REGEN_PENALTY;
```

## TODO: Missing Documentation

- Advanced regeneration spell interactions
- Environmental regeneration modifiers
- Group-based regeneration bonuses
- Performance optimization for large-scale regeneration

## References

- `GameServer/gameobjects/GameLiving.cs` - Base regeneration
- `GameServer/ECS-Services/EffectListService.cs` - Effect processing
- Various spell handlers for regeneration effects 