# Movement and Speed Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from MaxSpeedCalculator.cs, MovementComponent.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Movement in DAoC involves complex speed calculations based on character state, buffs, encumbrance, and environmental factors. The system balances realistic movement with gameplay requirements.

## Core Mechanics

### Base Movement Speeds

#### Speed Constants
```csharp
SPEED1 = 1.44  // 144% of base
SPEED2 = 1.59  // 159% of base
SPEED3 = 1.74  // 174% of base
SPEED4 = 1.89  // 189% of base
SPEED5 = 2.04  // 204% of base
```

#### Base Speed Values
- **Player Base**: 191 units/second
- **NPC Base**: Variable by template
- **Mount Base**: Variable by mount type

### Speed Calculation Formula

#### Basic Formula
```csharp
FinalSpeed = MaxSpeedBase * SpeedMultiplier + 0.5
```
- **0.5**: Rounding correction for int conversion

#### Speed Multiplier Components
1. **Base Multiplier**: 1.0
2. **Buff Multipliers**: Multiplicative
3. **State Modifiers**: Multiplicative
4. **Debuff Modifiers**: Multiplicative

### Movement States

#### Normal Movement
- **Forward**: 100% speed
- **Backward**: 60% speed (configurable)
- **Strafing**: 85% speed (configurable)

#### Sprint
- **Multiplier**: 1.3x (130%)
- **Duration**: Limited by endurance
- **Restrictions**: Cannot attack while sprinting
- **Endurance Cost**: Continuous drain

#### Stealth Movement
```csharp
StealthSpeed = 0.3 + (StealthSpec + 10) * 0.3 / (Level + 10)
```
- **Base**: 30% speed
- **Max**: ~60% at max spec
- **Modifiers**:
  - Mastery of Stealth RA
  - Shadow Run (2x multiplier)
  - Vanish effect

#### Swimming
- **Surface**: Normal speed
- **Underwater**: Modified by water movement
- **Diving**: Special zones only
- **Drowning**: Damage when breath expires

#### Mounted Movement
```csharp
if (speed > horseSpeed)
    horseSpeed = 1.0;  // Buff doesn't stack
FinalSpeed *= horseSpeed;
```
- **Speed**: Mount-specific (0.01 per speed point)
- **Restrictions**: No indoor areas
- **Combat**: Auto-dismount

### Speed Modifiers

#### Encumbrance System
```csharp
if (IsEncumbered && Properties.ENABLE_ENCUMBERANCE_SPEED_LOSS)
    speed *= MaxSpeedModifierFromEncumbrance;
```
- **Light**: No penalty
- **Medium**: Speed reduction
- **Heavy**: Significant reduction
- **Overloaded**: Cannot move

#### Combat State
- **In Combat**: Normal calculation
- **Out of Combat**: +25% speed (PvE zones)
```csharp
if (!InCombat && !IsStealthed && !CurrentRegion.IsRvR)
    speed *= 1.25;
```

#### Crowd Control
- **Mezzed**: 0 speed
- **Stunned**: 0 speed
- **Rooted**: 0 speed
- **Snared**: Variable reduction

### Special Movement Abilities

#### Speed of Sound (RA)
- **Effect**: Breaks root/snare
- **Duration**: 15-30 seconds
- **Speed**: Normal while rooted
- **Restrictions**: Cannot reapply

#### Charge
- **Speed**: Very high burst
- **Duration**: 10 seconds
- **Target**: Must select enemy
- **Restrictions**: Breaks on root

#### Bard/Minstrel/Skald Speed
- **Speed 1-5**: Progressive bonuses
- **Pulse**: Continuous while playing
- **Range**: Group/radius based
- **Stacking**: Doesn't stack with mounts

### Relic Carrying
```csharp
if (GameRelic.IsPlayerCarryingRelic(player))
{
    if (speed > 1.0)
        speed = 1.0;  // Cap at 100%
    horseSpeed = 1.0;
}
```

### NPC Movement

#### Pet Following
```csharp
if (distance > 20)
    speed *= 1.25;  // Catch-up speed
    
if (ownerIsSprinting)
    speed *= 1.3;   // Match owner sprint
```

#### Health-Based Speed
```csharp
if (healthPercent < 0.33)
    speed *= 0.2 + healthPercent * (0.8 / 0.33);
```
- **33%+ HP**: Full speed
- **0% HP**: 20% speed
- **Linear scale between**

### Movement Components

#### Player Movement
- **Current Speed**: Active movement rate
- **Max Speed**: Calculated maximum
- **Heading**: Direction facing
- **Position Updates**: Client-driven

#### NPC Movement
- **Pathfinding**: A* when available
- **Direct Movement**: Straight line
- **Following**: Stay near target
- **Roaming**: Random within range
- **Returning**: Back to spawn

### Turning and Rotation

#### Turning Disabled
```csharp
bool IsTurningDisabled => TurningDisabledCount > 0
```
- **Casting**: Cannot turn
- **Stunned**: Cannot turn
- **Some Abilities**: Lock facing

#### Turn Speed
- **Instant**: For players
- **Variable**: For NPCs
- **Smooth**: Client interpolation

## Movement Restrictions

### Zone-Based
- **No Mount Zones**: Indoor areas
- **Water Only**: Aquatic creatures
- **Flying**: Special NPCs only
- **Climbing**: Specific surfaces

### State-Based
- **Sitting**: Cannot move
- **Casting**: Reduced/no movement
- **Channeling**: No movement
- **Dead**: Ghost movement only

## Special Cases

### Siege Weapon Operation
```csharp
if (IsSitting)
    return; // Cannot operate while sitting
```

### Teleportation
- **Instant**: Position update
- **Loading**: Region transition
- **Validation**: Anti-cheat checks

### Forced Movement
- **Knockback**: Push effects
- **Pull**: Draw-in effects
- **Teleport**: Instant relocation
- **Fear**: Random movement

## Performance Considerations

### Update Frequency
- **Players**: Every tick while moving
- **NPCs**: Based on visibility
- **Optimization**: Reduce far updates

### Position Validation
- **Server Authority**: Verify client
- **Rubber-banding**: Correction
- **Anti-Speed Hack**: Detection

## Test Scenarios

### Sprint Test
```
Given: Player with full endurance
Action: Activate sprint
Expected:
- Speed = Base * 1.3
- Endurance drains
- Cannot attack
```

### Encumbrance Test
```
Given: Player carrying heavy load
Expected:
- Speed reduced proportionally
- Sprint less effective
- Mount speed unaffected
```

### Stealth Movement
```
Given: Assassin with 50 stealth
Calculation: 0.3 + (50 + 10) * 0.3 / (50 + 10) = 0.6
Expected: 60% of normal speed
```

### Pet Follow
```
Given: Pet 100 units behind sprinting owner
Expected:
- Pet speed = Base * 1.3 * 1.25
- Catches up to owner
- Matches owner speed when close
```

## Configuration

### Server Properties
```csharp
ENABLE_PVE_SPEED           // Out-of-combat speed bonus
ENABLE_ENCUMBERANCE_SPEED_LOSS // Weight penalties
Properties.BACKWARD_SPEED   // Backward movement rate
Properties.STRAFE_SPEED     // Strafing movement rate
```

### Speed Caps
- **Maximum Buff**: No hard cap
- **With Relic**: 100% max
- **While Rooted**: 0% (except SoS)
- **Minimum**: 0%

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added movement state calculations
- Documented special abilities
- Added NPC movement mechanics

## References
- `GameServer/propertycalc/MaxSpeedCalculator.cs`
- `GameServer/ECS-Components/MovementComponent.cs`
- `GameServer/ECS-Components/NpcMovementComponent.cs`
- `GameServer/gameobjects/GamePlayer.cs` 