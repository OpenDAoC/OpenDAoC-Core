# Battleground & Frontier System

**Document Status:** Initial Documentation  
**Completeness:** 80%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

Battlegrounds (BGs) are level-restricted RvR zones where players fight for realm supremacy. Frontiers are open RvR zones without level restrictions. Both provide structured PvP combat with objectives and rewards.

## Core Mechanics

### Battleground Types

#### Level-Based Battlegrounds
| Name | Level Range | Zone ID | Keep Count | Description |
|------|-------------|---------|------------|-------------|
| Thidranki | 20-24 | 238 | 1 per realm | First BG experience |
| Abermenai | 25-29 | 239 | 1 per realm | Transitional combat |
| Caledonia | 30-34 | 240 | 2 per realm | Keep warfare |
| Murdaigean | 35-39 | 241 | 2 per realm | Advanced tactics |
| Cathal Valley | 45-49 | 242 | 3 per realm | Endgame preparation |

### Entry Requirements

#### Level Validation
```csharp
public bool IsAllowedInBattleground(GamePlayer player, int bgZone)
{
    BattlegroundInfo bg = GetBattlegroundInfo(bgZone);
    
    if (player.Level < bg.MinLevel || player.Level > bg.MaxLevel)
    {
        player.Out.SendMessage($"You must be level {bg.MinLevel}-{bg.MaxLevel} to enter this battleground!", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    return true;
}
```

#### Access Restrictions
- **Level Enforcement**: Strict min/max
- **Realm Separation**: Three distinct areas
- **No Buffbots**: External buffs removed
- **Item Scaling**: Stats adjusted to BG level

### Zone Configuration

#### Battleground Properties
```csharp
public class BattlegroundInfo
{
    public int ZoneID { get; set; }
    public string Name { get; set; }
    public byte MinLevel { get; set; }
    public byte MaxLevel { get; set; }
    public bool IsInstance { get; set; }
    public int MaxPlayers { get; set; }
    public GameKeep[] Keeps { get; set; }
}
```

#### Zone Rules
- **RvR Enabled**: Full PvP combat
- **No Safe Zones**: Except portal keeps
- **Death Penalties**: Reduced in BGs
- **Bind Stones**: At portal keeps only

### Teleportation System

#### Entry Portals
```csharp
// Battleground teleporter NPC
public void TeleportToBattleground(GamePlayer player, int bgZone)
{
    if (!IsAllowedInBattleground(player, bgZone))
        return;
        
    // Remove external buffs
    RemoveExternalBuffs(player);
    
    // Scale equipment
    ScaleEquipmentToLevel(player, bg.MaxLevel);
    
    // Teleport to safe area
    player.MoveTo(bgZone, bg.SafeX, bg.SafeY, bg.SafeZ, bg.SafeHeading);
}
```

#### Exit Methods
- **/release**: Return to home realm
- **Portal NPCs**: At portal keeps
- **Level Out**: Auto-removed at max+1
- **Timer Expiry**: Some BGs have time limits

### Combat Mechanics

#### RvR Rules Active
```csharp
public override bool IsRvR
{
    get { return true; }
}

public override bool AllowPvP(GamePlayer attacker, GamePlayer defender)
{
    // Different realms can always fight
    if (attacker.Realm != defender.Realm)
        return true;
        
    return false;
}
```

#### Combat Modifications
- **CC Duration**: Standard RvR timers
- **Damage Scaling**: Level-adjusted
- **Speed Classes**: Normalized to level
- **Resurrection**: RvR rules apply

### Keep Warfare

#### Keep System in BGs
```csharp
public class BattlegroundKeep : GameKeep
{
    public override void StartCombat()
    {
        // Broadcast to battleground
        foreach (GamePlayer player in GetPlayersInRadius(VISIBILITY_DISTANCE))
        {
            player.Out.SendMessage($"{Name} is under attack!", 
                eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
        }
    }
}
```

#### Keep Features
- **Claimable**: By guilds
- **Upgradeable**: Limited levels
- **Siege Weapons**: Available
- **Guards**: Scale to BG level

### Frontier Zones

#### Open RvR Areas
| Zone | Description | Keeps | Objectives |
|------|-------------|-------|------------|
| Hadrian's Wall | Albion frontier | 7 | Darkness Falls |
| Emain Macha | Hibernia frontier | 7 | Passage of Conflict |
| Odin's Gate | Midgard frontier | 7 | Relic temples |

#### Frontier Features
- **No Level Restriction**: All welcome
- **Relic Warfare**: Realm bonuses
- **Keep Battles**: Full siege
- **Darkness Falls**: Conditional access

### Reward System

#### Realm Points (RPs)
```csharp
public override int CalculateRP(GamePlayer killer, GamePlayer killed)
{
    int baseRP = base.CalculateRP(killer, killed);
    
    // Battleground bonus
    if (IsInBattleground)
        baseRP = (int)(baseRP * 1.5); // 50% bonus
        
    return baseRP;
}
```

#### Bounty Points
- **Player Kills**: Based on RR
- **Keep Captures**: Fixed amounts
- **Objective Completion**: Varies
- **Daily Quests**: Bonus BP

### Population Balance

#### Realm Bonuses
```csharp
public float GetUnderdogBonus(eRealm realm)
{
    int realmPop = GetRealmPopulation(realm);
    int totalPop = GetTotalPopulation();
    
    float realmPercent = realmPop / (float)totalPop;
    
    if (realmPercent < 0.25) // Less than 25%
        return 2.0f; // 100% bonus
    else if (realmPercent < 0.33) // Less than 33%
        return 1.5f; // 50% bonus
        
    return 1.0f; // No bonus
}
```

#### Balance Mechanics
- **RP Bonuses**: For underpopulated realms
- **BP Bonuses**: Scaled by population
- **Keep Guards**: Stronger for underdogs
- **Respawn Timers**: Shorter for underdogs

## Special Features

### Darkness Falls Access
```csharp
public bool CanAccessDarknessFalls(eRealm realm)
{
    int keepCount = 0;
    
    foreach (GameKeep keep in KeepManager.Keeps)
    {
        if (keep.Realm == realm && keep.IsInDarknessFallsArea)
            keepCount++;
    }
    
    return keepCount >= REQUIRED_KEEPS_FOR_DF;
}
```

### Supplies System
- **Merchants**: Limited supplies
- **Siege Equipment**: Purchasable
- **Ammunition**: For siege
- **Repair Materials**: Keep maintenance

### Communication
```csharp
// Battleground-wide messages
/bg - Battleground chat
/b - Broadcast channel

// Realm-specific
/as - Alliance say (realm-wide in BG)
```

## System Integration

### Character Progression
- **XP Disabled**: In some BGs
- **RP/BP Enabled**: Always active
- **RR Progression**: Normal rates
- **ML/CL**: Not applicable in BGs

### Guild System
- **Keep Claiming**: Available
- **Guild Bonuses**: Apply in BGs
- **Merit Points**: From BG activities
- **Emblems**: Visible on keeps

### Quest System
- **Daily RvR**: Kill X enemies
- **Weekly Objectives**: Capture keeps
- **Realm Tasks**: Coordinated efforts
- **Personal Goals**: Achievements

## Implementation Notes

### Zone Transition
```csharp
public override void OnPlayerEnter(GamePlayer player)
{
    base.OnPlayerEnter(player);
    
    // Check level requirement
    if (!MeetsLevelRequirement(player))
    {
        player.MoveToBind();
        return;
    }
    
    // Apply battleground rules
    ApplyBattlegroundRules(player);
}
```

### Performance Optimization
- **Player Caps**: Per battleground
- **Object Limits**: Reduced in BGs
- **Update Rates**: Optimized for combat
- **Instance Management**: Load balancing

## Configuration

### Server Properties
```csharp
// Battleground settings
BG_LEVEL_ENFORCEMENT = true
BG_EXTERNAL_BUFF_REMOVAL = true
BG_ITEM_SCALING = true
BG_POPULATION_CAP = 200

// Frontier settings
FRONTIER_KEEP_UPGRADE = true
FRONTIER_RELIC_BONUS = true
FRONTIER_DF_ACCESS = true
```

## Edge Cases

### Level Boundaries
- **Leveling in BG**: Immediate removal
- **Delevel Attempts**: Not allowed
- **Apprentice System**: Disabled
- **Temporary Levels**: Not counted

### Cross-Realm Issues
- **No Communication**: Language barrier
- **No Trading**: Prevented
- **No Grouping**: System blocked
- **No Assistance**: Combat only

### Exploit Prevention
- **Safe Spot Detection**: Auto-move
- **Wall Climbing**: Teleport down
- **Speed Hacking**: Server validation
- **Radar Usage**: Obfuscated positions

## Test Scenarios

1. **Entry Testing**
   - Level requirements enforced
   - Buff removal working
   - Item scaling correct
   - Teleport locations safe

2. **Combat Testing**
   - RvR rules active
   - Damage scaling proper
   - CC durations correct
   - Death penalties applied

3. **Keep Testing**
   - Claiming functional
   - Guards appropriate level
   - Siege weapons working
   - Broadcasts functioning

4. **Balance Testing**
   - Population bonuses
   - Underdog mechanics
   - Queue systems
   - Performance limits

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 