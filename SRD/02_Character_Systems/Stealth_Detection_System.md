# Stealth & Detection System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: The Stealth & Detection System governs how stealth classes like Infiltrators, Shadowblades, and Nightshades can become invisible and how other players can detect them. To stealth successfully, you need to be out of combat and away from enemies - the higher your stealth skill, the closer you can be to enemies before they prevent you from stealthing. Once stealthed, detection depends on the viewer's level versus your stealth skill: higher-level enemies can see you from farther away, while lower-level enemies might not detect you even when close. Special abilities dramatically affect detection - "Detect Hidden" roughly doubles detection range, while "See Hidden" allows very long-range detection against non-assassin classes. Understanding stealth and detection mechanics is crucial for playing stealth classes effectively and for protecting yourself against stealthy enemies in RvR combat.

The stealth system allows certain classes to become invisible to enemies. Detection mechanics determine when stealthed players can be seen, involving complex calculations based on level, stealth skill, abilities, and distance.

## Core Mechanics

### Stealth Requirements

#### Activation Restrictions
Cannot stealth if:
- Dead
- In combat or casting
- Mezzed or stunned
- Have active pulsing effects
- Carrying a relic
- Enemy player within activation range
- Enemy NPC within activation range

#### Enemy Detection Range for Activation
```csharp
private bool IsObjectTooClose(GameObject obj, GamePlayer player)
{
    float enemyLevel = Math.Max(1f, obj.Level);
    float stealthLevel = player.GetModifiedSpecLevel(Specs.Stealth);
    if(stealthLevel > 50)
        stealthLevel = 50;
    
    float radius;
    if(obj is GamePlayer && ((GamePlayer)obj).HasAbility(Abilities.DetectHidden))
    {
        // Detect Hidden doubles the range
        radius = 2048f - (1792f * stealthLevel / enemyLevel);
    }
    else
    {
        // Normal range
        radius = 1024f - (896f * stealthLevel / enemyLevel);
    }
    
    return obj.IsWithinRadius(player, (int)radius);
}
```

### Detection Mechanics

#### Player Detection Range
```csharp
// Basic detection formula
int enemyStealthLevel = Math.Min(50, enemyPlayer.GetModifiedSpecLevel(Specs.Stealth));
int levelDiff = Math.Max(0, Level - enemyStealthLevel);
int range = 0;

// Standard detection
range = levelDiff * 20 + 125;

// Detect Hidden ability (if not countered)
if (HasAbility(Abilities.DetectHidden) &&
    !enemyPlayer.HasAbility(Abilities.DetectHidden) &&
    !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage) &&
    !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Vanish))
{
    range = levelDiff * 50 + 250;
}

// See Hidden (Ranger ability vs non-assassins)
if (HasAbilityType(typeof(AtlasOF_SeeHidden)) &&
    !enemyPlayer.CharacterClass.IsAssassin &&
    !enemyPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage))
{
    range = Math.Max(range, 2700 - 36 * enemyStealthLevel);
}

// Buff bonuses
range += BaseBuffBonusCategory[eProperty.Skill_Stealth];

// Hard cap
if (range > 1900)
    range = 1900;
```

#### Detection Range Formulas
- **Normal**: `(detector_level - stealth_spec) * 20 + 125`
- **Detect Hidden**: `(detector_level - stealth_spec) * 50 + 250`
- **See Hidden**: `2700 - (36 * stealth_spec)`

### NPC Stealth Detection

#### Detection Parameters
```csharp
double npcLevel = Math.Max(npc.Level, 1.0);
double stealthLevel = player.GetModifiedSpecLevel(Specs.Stealth);
double detectRadius = 125.0 + ((npcLevel - stealthLevel) * 20.0);

// Detect Hidden NPCs get bonus range
if (npc.HasAbility(Abilities.DetectHidden) &&
    !player.HasAbility(Abilities.DetectHidden) &&
    !player.effectListComponent.ContainsEffectForEffectType(eEffect.Camouflage) &&
    !player.effectListComponent.ContainsEffectForEffectType(eEffect.Vanish))
{
    detectRadius += 125;
}

// Minimum detection radius
if (detectRadius < 126) 
    detectRadius = 126;
```

#### Field of View
```csharp
double fieldOfView = 90.0;     // 90 degrees standard FOV
double fieldOfListen = 120.0;  // 120 degrees listening range

// High level NPCs get increased listening
if (npc.Level > 50)
    fieldOfListen += (npc.Level - player.Level) * 3;

// Check if in visual cone (front 90°)
bool canSeePlayer = (angle >= 360 - fieldOfView/2 || angle < fieldOfView/2);

// Check if in listening cones (rear sides)
bool canHearPlayer = // Complex angle calculation for rear detection
```

#### Detection Chance
```csharp
double chanceMod = 1.0;

// Chance decreases after 125 units
if (distanceToPlayer > 125)
    chanceMod = 1f - (distanceToPlayer - 125.0) / (detectRadius - 125.0);

double chanceToUncover = 0.1 + (npc.Level - stealthLevel) * 0.01 * chanceMod;
```

### Stealth Breaking

#### Actions that Break Stealth
- Any attack (except DoT ticks with Vanish)
- Taking damage
- Casting spells
- Using items
- Entering combat

#### Special Cases
- Vanish prevents DoT damage from breaking stealth
- True Sight ignores hard cap on detection range
- GM stealth (`GMStealthed`) bypasses all detection

### Movement Speed

#### Stealth Movement Modifiers
- Base stealth speed varies by class and stealth spec
- Sprint disabled while stealthed
- Speed buffs disabled during stealth
- Vanish can provide speed bonus

### Special Abilities

#### Camouflage (Realm Ability)
- Counters Detect Hidden
- Counters See Hidden
- Does not improve basic detection range

#### Vanish (Realm Ability)
- Allows stealth in combat
- Prevents attacks for 30 seconds
- Countdown timer displayed
- Immunity to DoT breaking stealth

#### Detect Hidden (Ability)
- Increases detection range formula
- Countered by enemy Detect Hidden
- Countered by Camouflage or Vanish

#### See Hidden (Ranger Ability)
- Works only vs non-assassin classes
- Very long range detection
- Countered by Camouflage

## System Interactions

### Combat System
- Attacks automatically break stealth
- Cannot attack for 30s after Vanish
- Aggro cleared when stealthing

### Group System
- Group members always visible to each other
- Detection calculations independent per viewer

### Buff System
- Stealth detection buffs add to range
- Movement speed buffs disabled while stealthed
- Pulse effects prevent stealth activation

### Pet System
- Pets don't uncover stealthed players
- NPCs with player owners excluded from detection

## Implementation Notes

### Network Updates
- Model type 3 sent when stealthed
- Model type 2 sent when unstealthed
- Object deletion sent to players who can't detect
- Object creation sent when becoming visible

### Performance
- Detection checks run every second for NPCs
- Player detection instant on movement update
- Maximum detection range capped at 1900 units

### Properties
```csharp
STEALTH_CHANGE_TICK       // Timestamp of last stealth change
UNCOVER_STEALTH_ACTION_PROP // Timer for NPC detection
VANISH_BLOCK_ATTACK_TIME_KEY // Vanish attack prevention
```

## Test Scenarios

### Activation Tests
- Enemy proximity prevention
- Combat state prevention
- Pulse effect prevention
- Relic carrying prevention

### Detection Tests
- Level vs stealth skill scaling
- Detect Hidden range doubling
- See Hidden vs non-assassins
- Camouflage countering abilities

### NPC Detection Tests
- Field of view angles
- Listening range calculations
- Detection chance over distance
- High level NPC bonuses

### Special Cases
- Vanish in combat
- Group member visibility
- True Sight bypass
- GM stealth immunity

## Change Log
- Initial documentation created
- Added detection formulas
- Documented special abilities
- Added NPC detection mechanics

## References
- GameServer/gameobjects/GamePlayer.cs (CanDetect, UncoverStealthAction)
- GameServer/skillhandler/StealthSpecHandler.cs
- GameServer/ECS-Effects/StealthECSEffect.cs
- GameServer/realmabilities/effects/VanishEffect.cs 