# Aggro/Hate System

## Document Status
- Status: Comprehensive
- Implementation: Complete

## Overview

**Game Rule Summary**: Every monster keeps a "hate list" of everyone who has attacked or threatened it, with the person who has done the most damage (adjusted for distance) becoming the primary target. You can use taunts to force monsters to attack you, or use abilities like Protect to reduce how much other players appear to threaten the monster.

The aggro/hate system determines which targets NPCs will attack. Aggro is tracked per-target and modified by distance, with the highest effective aggro determining the current attack target.

## Core Mechanics

### Aggro List Management

#### Adding to Aggro List
```csharp
public virtual void AddToAggroList(GameLiving living, long aggroAmount)
{
    if (Body.IsConfused || !Body.IsAlive || living == null)
        return;

    ForceAddToAggroList(living, aggroAmount);
}
```

#### Protect Ability Mitigation
- **Protect I**: Prevents 10% of aggro amount
- **Protect II**: Prevents 20% of aggro amount  
- **Protect III**: Prevents 30% of aggro amount
- Range: `PROTECT_DISTANCE` units
- Protector must not be incapacitated or sitting

### Effective Aggro Calculation

#### Distance-Based Modification
```csharp
// Effective aggro reduces exponentially beyond threshold
const double EFFECTIVE_AGGRO_DISTANCE_THRESHOLD = 800;
const double EFFECTIVE_AGGRO_EXPONENT = -0.002;

double distanceOverThreshold = distance - EFFECTIVE_AGGRO_DISTANCE_THRESHOLD;

if (distanceOverThreshold <= 0)
    aggroAmount.Effective = aggroAmount.Base;
else
    aggroAmount.Effective = (long) Math.Ceiling(aggroAmount.Base * 
                           Math.Exp(EFFECTIVE_AGGRO_EXPONENT * distanceOverThreshold));
```

### Attack Target Selection

#### Priority System
1. Highest effective aggro within attack range
2. If tied, current target maintains priority
3. Shades are tracked but ignored until pets die
4. Maximum tracking distance: `MAX_AGGRO_LIST_DISTANCE`

#### Target Filtering
Removed from aggro list if:
- Not alive
- Not active object state
- Different region
- Beyond max aggro distance
- Not valid attack target (except shades)

### Damage-Based Aggro

#### Base Conversion
```csharp
protected void ConvertAttackToAggroAmount(AttackData ad)
{
    if (!ad.GeneratesAggro || !Body.IsAlive)
        return;

    int damage = Math.Max(1, ad.Damage + ad.CriticalDamage);
```

#### Pet/Owner Split
When attacked by controlled pet:
- **Pet**: 85% of damage as aggro
- **Owner**: 15% of damage as aggro

### Taunt Mechanics

#### Spell Taunt
```csharp
// Taunt spell aggro calculation
aggroAmount = Math.Max(1, (int)(Spell.Value * Caster.Level * 0.1));
```

#### Style Taunt/Detaunt
```csharp
// Style taunt values (default: 17 for taunt, -19 for detaunt)
// Scales with damage dealt
AttackData attackData = Caster.attackComponent.attackAction.LastAttackData;
brain.AddToAggroList(Caster, 
    (long) Math.Floor((attackData.Damage + attackData.CriticalDamage) * 
                      Spell.Value * 0.1));
```

### Special Brain Types

#### Turret Brains
- Fire-and-Forget (FnF) turrets randomly select from valid targets
- Regular turrets use standard highest-threat targeting
- Check LOS before adding to aggro list (configurable)

#### Friend Brain (Spell Summons)
- Attacks all valid enemy targets in range
- Excludes summoner from aggro
- 1 base aggro for players
- `Level << 1` aggro for NPCs

#### Commander Pet Brain
- Controls minion aggression states
- Can toggle taunting behavior
- Synchronizes aggression across controlled minions

### Aggro Range Mechanics

#### Base Aggro Range
- Configured per NPC in database
- Modified by brain type (turrets use spell range)
- BAF range typically matches aggro range

#### BAF (Bring A Friend) System
```csharp
public virtual bool CanAggroTarget(GameLiving target)
{
    // Check server rules
    if (!GameServer.ServerRules.IsAllowedToAttack(Body, target, true))
        return false;

    // Grey con check
    if (realTarget.IsObjectGreyCon(Body))
        return false;

    // Faction check
    if (Body.Faction != null)
    {
        if (realTarget is GamePlayer)
            return Body.Faction.GetStandingToFaction(realTarget) 
                   is Faction.Standing.AGGRESIVE;
    }

    return AggroLevel > 0;
}
```

### Temporary Aggro Lists

#### Swapping Mechanic
```csharp
public bool SetTemporaryAggroList()
{
    if (_tempAggroList != null)
        return false;

    _tempAggroList = AggroList;
    AggroList = new();
    return true;
}
```

Used for special mechanics like fear effects that require temporary target changes.

## System Interactions

### Group Mechanics
- Aggro propagates to group members via protect ability
- Group leader's threat can influence initial targeting
- BAF pulls add entire groups to combat

### Pet Systems
- Pets split aggro with owners (85/15 split)
- Pet death transfers remaining aggro to owner
- Shades kept in list for later targeting

### Spell Interactions
- Direct damage: 1:1 damage to aggro ratio
- DoTs: Generate aggro per tick
- Heals: Can generate aggro on all enemies attacking healed target
- Taunts: Bypass damage requirements

### Stealth Detection
- Stealthed players excluded from aggro checks
- Vanish/stealth drops aggro if successful
- Detection breaks stealth and allows aggro

## Implementation Notes

### Thread Safety
- Concurrent dictionary for aggro list
- Ordered list uses locks for thread safety
- Atomic operations for aggro updates

### Performance
- Lazy calculation of effective aggro
- Ordered list only built when needed
- Distance calculations cached per think cycle

### Memory Management
- Automatic cleanup of dead/distant targets
- Maximum list size enforced
- Temporary lists properly swapped back

## Test Scenarios

### Basic Aggro Tests
- Single target damage aggro
- Multiple attacker priority
- Distance-based reduction
- Maximum range cutoff

### Special Mechanics Tests
- Protect ability reduction
- Pet/owner split ratios
- Taunt effectiveness
- Style taunt scaling

### Edge Cases
- Shade targeting after pet death
- Grey con filtering
- Faction-based aggro
- Temporary list swapping

## Change Log
- Initial documentation created
- Added distance formulas
- Documented special brain types
- Added BAF mechanics

## References
- GameServer/ai/brain/StandardMob/StandardMobBrain.cs
- GameServer/spells/TauntSpellHandler.cs
- GameServer/spells/StyleTaunt.cs
- GameServer/ai/brain/ControlledMob/ 