# Siege Warfare System

## Document Status
- Status: Comprehensive
- Implementation: Complete

## Overview

**Game Rule Summary**: The siege warfare system provides the massive weapons and tactics needed to assault and defend keeps in Realm vs Realm warfare. To break into an enemy keep, you'll need rams to smash down the heavy doors - the more players helping push the ram, the faster it moves and hits harder. For long-range destruction, siege weapons like catapults and trebuchets can devastate enemy fortifications and siege equipment from a safe distance, while ballistas provide precise targeting. Defenders can use the keep's built-in siege weapons to rain destruction on attackers. Siege weapons require coordination, positioning, and protection since they're vulnerable to enemy attacks. Mastering siege warfare is essential for successful keep captures and creating the epic large-scale battles that define DAoC's RvR experience.

The siege warfare system allows players to attack and defend keeps using specialized siege weapons. These include rams for doors, and ranged siege weapons like catapults, trebuchets, and ballistas for destroying fortifications and enemy siege equipment.

## Core Mechanics

### Siege Weapon Types

#### Melee Siege - Rams
```
Mini Ram:     Level 0, 2 passengers,  100 damage base
Light Ram:    Level 1, 6 passengers,  125 damage base  
Medium Ram:   Level 2, 8 passengers,  150 damage base
Heavy Ram:    Level 3, 12 passengers, 200 damage base
```

#### Ranged Siege
- **Catapult**: 1000-3000 range, 150 radius, 100 base damage
- **Trebuchet**: 2000-5000 range, 150 radius, 100 base damage (3x vs doors)
- **Ballista**: Lower damage but higher accuracy
- **Cauldron**: Defensive area denial weapon

### Control Mechanics

#### Control Distance
- Maximum control distance: 256 units (`SIEGE_WEAPON_CONTROLE_DISTANCE`)
- Must remain within range to maintain control
- Control released if out of range

#### Action Delays
```csharp
ActionDelay = new int[]
{
    0,      // none
    5000,   // aiming (5 seconds)
    10000,  // arming (10 seconds)  
    0,      // loading
    1100    // firing (1.1 seconds base)
};
```

### Ram Mechanics

#### Target Restrictions
- Can only attack GameKeepDoor or GameRelicDoor
- Cannot attack walls or other structures
- Must be within MeleeAttackRange (300-500 based on level)

#### Ram Limit
```csharp
private const int MAX_RAMS_ATTACKING_TARGET = 2;
```
Only 2 rams can attack the same door simultaneously

#### Damage Calculation
```csharp
public override int CalcDamageToTarget(GameLiving target)
{
    // Base damage + 50% bonus damage per rider
    return BaseDamage + (BaseDamage/2 * CurrentRiders.Length);
}
```

#### Reload Speed
```csharp
private int GetReloadDelay
{
    get
    {
        // 10-14 seconds base, reduced by number of riders
        return 10000 + ((Level + 1) * 2000) - 
               (int)(10000 * ((double)CurrentRiders.Length / 
                              (double)MAX_PASSENGERS));
    }
}
```

#### Movement Speed
```csharp
double speed = 10.0 + 5.0 * Level + 50.0 * CurrentRiders.Length / MAX_PASSENGERS;
```

### Ranged Siege Mechanics

#### Ammunition System
- Different ammo types for each weapon
- Must load appropriate ammunition
- Ammo stored in siege weapon inventory

#### Damage vs Target Types
```csharp
// Trebuchet special damage
public override int CalcDamageToTarget(GameLiving target)
{
    if(target is GameKeepDoor || target is GameRelicDoor)
        return BaseDamage * 3;  // 300 damage to doors
    else
        return BaseDamage;      // 100 damage to others
}
```

#### Area Effect
- AttackRadius: 150 units for catapults/trebuchets
- Damages all targets in radius
- Can hit multiple enemies

### Hookpoint System

#### Hookpoint Types
```
ID < 0x20:  Red (Guards)
ID > 0x20:  Blue (Siege)
ID > 0x40:  Green/Yellow (Specialized)
0x41:       Ballista
0x61:       Trebuchet  
0x81:       Cauldron
```

#### Hookpoint Siege Weapons
- Automatically despawn after 30 minutes
- 5 minute respawn timer after destruction
- Cannot be moved once placed

### Player Siege Equipment

#### Summoning Restrictions
- Cannot use in dungeons
- Cannot use in non-OF zones
- Cannot use in portal keeps
- 500 unit minimum distance between trebuchets/catapults

#### Decay System
- Field siege weapons decay after 3 minutes without control
- Decay period: 240 seconds
- Weapons can be repaired before decay

### Damage Modifiers

#### Against Players
```csharp
// 50% damage reduction for tank classes
if (id == (int)eCharacterClass.Armsman || 
    id == (int)eCharacterClass.Warrior || 
    id == (int)eCharacterClass.Hero)
    ad.Damage /= 2;

// Ram protection (50-80% based on ram level)
if (player.IsRiding && player.Steed is GameSiegeRam)
{
    ad.Damage = (int)(ad.Damage * (1.0 - (50.0 + 
                      player.Steed.Level * 10.0) / 100.0));
}
```

### Special Abilities

#### Lifter (Realm Ability)
- Increases ram movement speed
- Stacks with rider bonuses

#### Siege Bolt (Realm Ability)
- Direct damage: 25,000 base
- Range: 1875 units
- 5 minute reuse timer
- Can one-shot gates and siege equipment

## System Interactions

### Keep System
- Siege weapons required to capture keeps
- Doors must be destroyed to access lord
- Hookpoints provide defensive positions

### Crafting System
- Siege weapons craftable via SiegeCrafting
- Different recipes for each type
- Requires appropriate materials

### Group/Realm System
- Siege weapons inherit owner's realm
- Group members can ride rams together
- Realm points awarded for siege kills

### Stealth System
- Cannot control siege weapon while stealthed
- Siege attacks break stealth

## Implementation Notes

### Network Updates
- Siege weapon interface packet (0xED)
- Special movement sync for siege weapons
- Status updates sent to controller

### Database
- DbKeepHookPoint: Hookpoint definitions
- DbKeepHookPointItem: Placed siege weapons
- Siege weapon items use ItemTemplate

### Performance
- Siege weapons use SiegeTimer for actions
- Efficient area damage calculations
- Limited concurrent rams per target

## Test Scenarios

### Ram Tests
- Maximum 2 rams per door enforcement
- Damage scaling with riders
- Speed scaling with riders
- Reload time reduction

### Ranged Tests
- Range verification (min/max)
- Area damage radius
- Trebuchet triple damage to doors
- Ammunition requirements

### Control Tests
- Distance-based control loss
- Stealth prevention
- Sitting prevention
- Death releases control

### Summoning Tests
- Zone restrictions
- Distance between siege weapons
- Portal keep prevention
- Dungeon prevention

## Change Log
- Initial documentation created
- Added damage formulas
- Documented hookpoint system
- Added special abilities

## References
- GameServer/gameobjects/GameSiegeWeapon.cs
- GameServer/gameobjects/SiegeWeapon/
- GameServer/keeps/HookPointInventory.cs
- GameServer/scripts/siege/ 