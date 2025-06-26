# Area of Effect & Targeting System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The area of effect (AoE) and targeting system manages how spells select and affect multiple targets. It includes ground-targeted area effects, point-blank area effects, cone effects, and single-target selection with complex rules for target validation and distance falloff.

## Core Targeting Types

### Target Type Enumeration
```csharp
public enum eSpellTarget
{
    NONE,           // No target
    SELF,           // Caster only
    ENEMY,          // Single enemy
    CORPSE,         // Dead target
    PET,            // Controlled pet
    GROUP,          // Group members
    REALM,          // Same realm players
    AREA,           // Ground-targeted AoE
    CONE            // Cone from caster
}
```

### Single Target Selection

#### Enemy Targeting
```csharp
case eSpellTarget.ENEMY:
{
    if (target == null)
        break;
    
    if (!IsAllowedTarget(target))
        break;
    
    if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
        break;
        
    list.Add(target);
    break;
}
```

#### Target Validation Rules
- **IsAllowedToAttack**: Server rules for PvP/PvE
- **Range Check**: Target within spell range
- **Line of Sight**: Some spells require LoS
- **Realm Rules**: Cross-realm targeting restrictions
- **Special Immunities**: DamageImmunity ability

### Group and Realm Targeting

#### Group Target Selection
```csharp
case eSpellTarget.GROUP:
{
    Group group = Caster.Group;
    if (group == null)
    {
        list.Add(Caster); // Solo caster
        break;
    }
    
    foreach (GamePlayer member in group.GetMembersInTheGroup())
    {
        if (member != null && member.IsWithinRadius(Caster, SPELL_RANGE_FOR_GROUPSPELLS))
            list.Add(member);
    }
}
```

#### Realm Target Special Cases
- **Group and Pets**: `GetGroupAndPets()` for REALM target buffs
- **Range Limits**: 2000 units for group spells
- **Concentration Buffs**: Special handling for buffs

## Area of Effect Mechanics

### Ground-Targeted Area (AREA)

#### Target Selection Process
```csharp
case eSpellTarget.AREA:
{
    if (modifiedRadius > 0)
    {
        foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(
            Caster.CurrentRegionID, 
            Caster.GroundTarget, 
            modifiedRadius))
        {
            if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                list.Add(player);
        }
        
        foreach (GameNPC npc in WorldMgr.GetNPCsCloseToSpot(
            Caster.CurrentRegionID, 
            Caster.GroundTarget, 
            modifiedRadius))
        {
            if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
            {
                if (!npc.HasAbility("DamageImmunity"))
                    list.Add(npc);
            }
        }
    }
}
```

#### Ground Target Requirements
- **Ground Target Set**: `Caster.GroundTarget` must be valid
- **Range Limit**: Ground target within spell range
- **Radius**: `Spell.Radius` determines AoE size

### Point-Blank Area Effect (PBAoE)

#### PBAoE Detection
```csharp
public bool IsPBAoE
{
    get { return (Range == 0 && IsAoE); }
}
```

#### PBAoE Target Selection
```csharp
// Centered on caster for PBAoE
if (Spell.IsPBAoE)
    Target = Caster;

foreach (GameLiving target in Caster.GetLivingsInRadius(Spell.Radius))
{
    if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
        list.Add(target);
}
```

#### Special PBAoE Rules
- **Damage Immunity**: NPCs with ability excluded
- **Range**: Always 0 for PBAoE spells
- **Center Point**: Always the caster's position

### Cone Effects

#### Cone Target Selection
```csharp
case eSpellTarget.CONE:
{
    // Implementation determines targets in cone from caster
    // Based on angle and range calculations
    foreach (GameLiving living in GetTargetsInCone())
    {
        if (IsValidConeTarget(living))
            list.Add(living);
    }
}
```

#### Cone Mechanics
- **Range**: Uses `Spell.Range` for maximum distance
- **Angle**: Typically 90-120 degrees
- **Origin**: Always from caster position
- **Direction**: Based on caster facing or target direction

## Distance Falloff System

### Falloff Calculation
```csharp
protected virtual double CalculateDistanceFallOff(int distance, int radius)
{
    return distance / (double)radius;
}
```

### Falloff Application
```csharp
if (Spell.Target == eSpellTarget.AREA)
    DistanceFallOff = CalculateDistanceFallOff(
        targetInList.GetDistanceTo(Caster.GroundTarget), 
        Spell.Radius);
else if (Spell.Target == eSpellTarget.CONE)
    DistanceFallOff = CalculateDistanceFallOff(
        targetInList.GetDistanceTo(Caster), 
        Spell.Range);
else
    DistanceFallOff = CalculateDistanceFallOff(
        targetInList.GetDistanceTo(Target), 
        Spell.Radius);
```

### Falloff Effects

#### Damage Reduction
```csharp
if (DistanceFallOff > 0)
    spellDamage *= 1 - DistanceFallOff;
```

#### Duration Reduction (RvR Only)
```csharp
// Duration reduced for AoE spells in RvR, but only if no damage component
if (DistanceFallOff > 0 && Spell.Damage == 0 && 
    (target is GamePlayer || (target is GameNPC npcTarget && npcTarget.Brain is IControlledBrain)))
{
    effectiveness *= 1 - DistanceFallOff / 2;
}
```

#### Special Cases
- **Positive Effects**: No falloff for beneficial spells
- **Volley Damage**: 18.5% reduction for archery area attacks
- **Center Point**: 100% effectiveness at exact center

## Pet Targeting System

### Pet Target Selection
```csharp
case eSpellTarget.PET:
{
    // PBAE spells on pets
    if (modifiedRadius > 0 && Spell.Range == 0)
    {
        foreach (GameNPC npcInRadius in Caster.GetNPCsInRadius(modifiedRadius))
        {
            if (Caster.IsControlledNPC(npcInRadius))
                list.Add(npcInRadius);
        }
        return list;
    }
    
    // Single pet targeting
    if (target != null && Caster.IsWithinRadius(target, Spell.Range))
    {
        if (Caster.IsControlledNPC(target))
            list.Add(target);
    }
}
```

### Pet Special Rules
- **Controlled Check**: `Caster.IsControlledNPC()`
- **Bonedancer Special**: Commander and subpet handling
- **Range Limits**: Same as normal spells
- **AoE Pet Buffs**: Affect all pets around target pet

### Pet AoE Mechanics
```csharp
// Buffs affect every pet around the targeted pet (same owner)
if (pet != null)
{
    foreach (GameNPC npcInRadius in pet.GetNPCsInRadius(modifiedRadius))
    {
        if (npcInRadius == pet || !Caster.IsControlledNPC(npcInRadius))
            continue;
            
        list.Add(npcInRadius);
    }
}
```

## Corpse Targeting

### Corpse Target Validation
```csharp
case eSpellTarget.CORPSE:
{
    if (target == null || target.IsAlive)
        break;
        
    if (!IsAllowedTarget(target))
        break;
        
    list.Add(target);
    break;
}
```

### Corpse Mechanics
- **Dead Only**: Target must not be alive
- **Range Check**: Standard range applies
- **Allowed Target**: Server rules validation
- **Resurrection Spells**: Primary use case

## Special Targeting Rules

### Selective Blindness
```csharp
SelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<SelectiveBlindnessEffect>();
if (SelectiveBlindness != null)
{
    GameLiving EffectOwner = SelectiveBlindness.EffectSource;
    if (EffectOwner == player)
    {
        // Target invisible to caster
        (Caster as GamePlayer)?.Out.SendMessage($"{player.GetName(0, true)} is invisible to you!", 
            eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
    }
    else
        list.Add(player);
}
```

### Line of Sight Checks
```csharp
// Cone spells require LoS check
if (Spell.Target == eSpellTarget.CONE)
{
    player.Out.SendCheckLos(Caster, target, new CheckLosResponse(DealDamageCheckLos));
}
```

### Storm Targeting
```csharp
// GameStorm NPCs can be targeted by AoE
if (npc is GameStorm)
    list.Add(npc);
```

## Target Validation System

### IsAllowedTarget Method
```csharp
protected virtual bool IsAllowedTarget(GameLiving target)
{
    if (target == null || !target.IsAlive)
        return false;
        
    if (!Caster.IsWithinRadius(target, CalculateSpellRange()))
        return false;
        
    if (HasPositiveEffect && !GameServer.ServerRules.IsSameRealm(Caster, target, true))
        return false;
        
    if (!HasPositiveEffect && !GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
        return false;
        
    return true;
}
```

### Server Rules Integration
- **Attack Rules**: `IsAllowedToAttack()` for hostile spells
- **Realm Rules**: `IsSameRealm()` for beneficial spells
- **PvP Rules**: Special handling in RvR zones
- **Duel Rules**: Modified targeting during duels

## Performance Optimizations

### Radius Queries
- **WorldMgr.GetPlayersCloseToSpot()**: Optimized player queries
- **WorldMgr.GetNPCsCloseToSpot()**: Optimized NPC queries
- **GetLivingsInRadius()**: Combined living queries

### Target Caching
- Target lists built once per spell cast
- Distance calculations cached
- Line of sight checks deferred when possible

### Region Optimization
- Only search current region
- Use spatial indexing for large areas
- Limit search depth for performance

## Test Scenarios

### Single Target
1. Valid enemy target selection
2. Range validation
3. LoS checking
4. Realm rule enforcement

### Area Effects
1. Ground target AoE selection
2. PBAoE around caster
3. Distance falloff calculation
4. Multiple target validation

### Special Cases
1. Pet targeting mechanics
2. Corpse resurrection
3. Group buff application
4. Cone effect targeting

### Edge Cases
1. Moving targets during cast
2. Targets going out of range
3. Target death during casting
4. Invalid ground targets

## Cross-System Interactions

### With Combat System
- Attack validation rules
- LoS requirement integration
- Damage application to multiple targets

### With Effect System
- Multiple effect application
- Concentration effect management
- Falloff effect calculation

### With Movement System
- Range checking during movement
- Ground target validation
- Cone direction calculation

### With Property System
- Range modification bonuses
- Targeting enhancement effects
- Special targeting abilities 