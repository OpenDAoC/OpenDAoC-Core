# Death & Resurrection System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The death and resurrection system handles player death, respawn mechanics, penalties, and revival options. Death penalties vary based on the cause of death (PvE, PvP, RvR) and player level.

## Core Mechanics

### Death Types
```csharp
public enum eDeathType
{
    None,
    PvE,    // Death from NPCs
    PvP,    // Death from players (same realm)
    RvR     // Death from enemy realm players
}
```

### Death Process

#### Release Timer
- **Standard Release**: 10 minutes (`RELEASE_TIME`)
- **Minimum Wait**: 10 seconds (`RELEASE_MINIMUM_WAIT`)
- **Duel Death**: Automatic release after minimum wait
- Timer displays in real-time window

#### Death Location
- Corpse remains at death location
- Gravestone placed if experience lost (PvE only)
- Death message broadcast to area

### Experience Penalties

#### PvE Death (Level 6+)
```csharp
// Base XP loss percentage calculation
int xpLossPercent;
if (Level < 40)
    xpLossPercent = MaxLevel - Level;
else
    xpLossPercent = MaxLevel - 40;

// Death count modifiers
switch (DeathCount)
{
    case 0:  // First death this level
        xpLossPercent /= 3;
        break;
    case 1:  // Second death this level
        xpLossPercent = xpLossPercent * 2 / 3;
        break;
    default: // Third+ death
        // Full penalty
        break;
}

// Calculate actual XP loss
long xpLoss = (ExperienceForNextLevel - ExperienceForCurrentLevel) 
              * xpLossPercent / 1000;
```

#### Level Requirements
- **No XP Loss**: Below level `PVE_EXP_LOSS_LEVEL` (default: 6)
- **No Con Loss**: Below level `PVE_CON_LOSS_LEVEL`

#### PvP/RvR Death
- **Normal Server**: No experience loss
- **PvP Server**: No XP loss, 3 constitution loss if enabled

### Constitution Penalties

#### PvE Constitution Loss
```csharp
int conLoss = DeathCount;
if (conLoss > 3)
    conLoss = 3;
else if (conLoss < 1)
    conLoss = 1;
```

#### PvP Constitution Loss (PvP servers only)
- Fixed 3 point loss if `PVP_DEATH_CON_LOSS` enabled
- No scaling based on death count

### Gravestone System

#### Creation Conditions
- Only created if experience was lost
- Placed at death location
- Removes any existing gravestone

#### Recovery Mechanics
- Players can `/pray` at gravestone
- Returns all lost experience
- Gravestone deleted after prayer
- Must be within interaction distance

### Resurrection Options

#### Player Resurrection
- Requires resurrection spell
- Can be cast before release
- Resurrection sickness applied

#### Resurrection Sickness
```csharp
// PvE/PvP Sickness (Curable)
SpellID: 2435
- Stat debuff
- Can be cured by healer NPCs

// RvR Sickness (Not curable)
SpellID: 8181
- Stat debuff
- Cannot be cured by NPCs
- Must wait for duration
```

#### Sickness Immunity
- No sickness below level `RESS_SICKNESS_LEVEL`
- Perfect Recovery ability prevents sickness
- 5 second damage immunity after resurrection

#### Resurrection Rewards
```csharp
// Resurrector gains realm points
long rezRps = resurrectedPlayer.LastDeathRealmPoints 
              * (ResurrectHealth + 50) / 1000;
```

### Release Locations

#### Standard Release Points
1. **Bind Point**: Default release location
2. **Portal Keep**: Nearest border keep (RvR zones)
3. **House**: Player's house if owned
4. **City**: Capital city in some cases

#### Release Type Priority
```csharp
switch (m_releaseType)
{
    case eReleaseType.Duel:
        // Release at current location
        break;
    case eReleaseType.House:
        // Release to house
        break;
    default:
        // Release to bind point
        break;
}
```

### Special Death Mechanics

#### Duel Deaths
- Automatic release after minimum wait
- No penalties applied
- Winner announcement to area
- Released at death location

#### Underwater Death
- Drowning damage: 5% max health per tick
- Death updates water breath state
- Normal penalties apply

#### Epic Encounter Deaths
- May have special resurrection mechanics
- Custom release points possible
- Boss-specific death handling

## System Interactions

### Group System
- Death removes from active group combat
- Resurrection by group members preferred
- Experience loss shared considerations

### PvP/RvR System
- Realm points awarded to killer
- Death spam protection (30 second window)
- Bounty point interactions

### Spell System
- Resurrection spells have different strengths
- Higher level spells = more health restored
- Some abilities prevent sickness

### Pet System
- Pets released on owner death
- Controlled NPCs return to spawn
- Siege weapons released

## Implementation Notes

### Property Storage
```csharp
// Temporary properties during death
DEATH_EXP_LOSS_PROPERTY        // XP lost this death
DEATH_CONSTITUTION_LOSS_PROPERTY // Con lost this death
RESURRECT_REZ_SICK_EFFECTIVENESS // Sickness prevention
```

### Database Persistence
- `ConLostAtDeath`: Total constitution lost
- `DeathCount`: Deaths at current level
- `GravestoneRegion`: Active gravestone location

### Timer Management
- Release timer uses real-time
- Automatic cleanup on disconnect
- Resurrection timer for accept window

## Test Scenarios

### Basic Death Tests
- PvE death with XP loss
- First/second/third death modifiers
- Constitution loss scaling
- Gravestone creation

### Resurrection Tests
- Player resurrection spell
- Resurrection sickness application
- Perfect Recovery immunity
- Realm point rewards

### Edge Cases
- Death while releasing
- Multiple gravestones
- Invalid bind points
- Underwater resurrection

### Release Tests
- Timer countdown
- Forced release
- Duel auto-release
- House release option

## Change Log
- Initial documentation created
- Added death penalty formulas
- Documented resurrection mechanics
- Added special death types

## References
- GameServer/gameobjects/GamePlayer.cs (Die, Release, OnRevive)
- GameServer/spells/ResurrectSpellHandler.cs
- GameServer/realmabilities/handlers/PerfectRecoveryAbility.cs
- Properties.PVE_EXP_LOSS_LEVEL, Properties.RESS_SICKNESS_LEVEL 