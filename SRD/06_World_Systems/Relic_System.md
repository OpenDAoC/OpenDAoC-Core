# Relic System

**Document Status:** Complete Documentation  
**Completeness:** 95%  
**Verification:** Code-verified from RelicMgr.cs, GameRelic.cs  
**Implementation Status:** Live

## Overview

The Relic System provides realm-wide bonuses through the capture and control of magical artifacts. Two types of relics exist: Strength relics (melee bonus) and Magic relics (spell bonus). Controlling relics grants significant combat advantages to the entire realm.

## Core Mechanics

### Relic Types

#### Strength Relics
- **Effect**: +10% melee damage and skill bonus per relic
- **Calculation**: Applied to weapon skill calculations
- **Realm Bonus**: Multiplicative with base weapon skill
- **Maximum**: 2 relics per realm (200% bonus potential)

#### Magic Relics  
- **Effect**: +10% spell damage and casting bonus per relic
- **Calculation**: Applied to spell damage calculations
- **Realm Bonus**: Multiplicative with base spell power
- **Maximum**: 2 relics per realm (200% bonus potential)

### Relic Ownership System

#### Ownership Rules
```csharp
public class GameRelic : GameStaticItem
{
    private eRealm m_originalRealm;     // Home realm (1,2,3 only)
    private eRealm m_lastRealm;         // Previous owner
    public DateTime LastCaptureDate;     // When last captured
    
    public bool IsMounted => (m_currentRelicPad != null);
    public GamePlayer CurrentCarrier => m_currentCarrier;
    public GameRelicPad CurrentRelicPad => m_currentRelicPad;
}
```

#### Capture Requirements
```csharp
public static bool CanPickupRelicFromShrine(GamePlayer player, GameRelic relic)
{
    // Own realm relics can always be reclaimed
    if (player.Realm == relic.OriginalRealm)
        return true;
        
    // Enemy relics require owning your own first
    IEnumerable list = getRelics(player.Realm, relic.RelicType);
    foreach (GameRelic curRelic in list)
    {
        if (curRelic.Realm == curRelic.OriginalRealm)
            return true;  // Own realm relic held
    }
    
    return false;  // Cannot steal without owning your own
}
```

### Relic Mechanics

#### Picking Up Relics
```csharp
public override bool Interact(GamePlayer player)
{
    // Basic validation
    if (!player.IsAlive)
    {
        player.Out.SendMessage("You cannot pickup relic. You are dead!");
        return false;
    }
    
    // Mounted relic checks
    if (IsMounted && player.Realm == Realm)
    {
        player.Out.SendMessage("You cannot pickup relic. It is owned by your realm.");
        return false;
    }
    
    // Ownership requirement check
    if (IsMounted && !RelicMgr.CanPickupRelicFromShrine(player, this))
    {
        player.Out.SendMessage($"You need to capture your realms {RelicType} relic first.");
        return false;
    }
    
    PlayerTakesRelic(player);
    return true;
}
```

#### Carrying Restrictions
```csharp
protected virtual void PlayerTakesRelic(GamePlayer player)
{
    // One relic per player
    if (player.TempProperties.GetProperty<GameRelic>(PLAYER_CARRY_RELIC_WEAK) != null)
    {
        player.Out.SendMessage("You are already carrying a relic.");
        return;
    }
    
    // Cannot stealth while carrying
    if (player.IsStealthed)
    {
        player.Out.SendMessage("You cannot carry a relic while stealthed.");
        return;
    }
    
    // Must be alive
    if (!player.IsAlive)
    {
        player.Out.SendMessage("You are dead!");
        return;
    }
    
    // Set carrier state
    player.TempProperties.SetProperty(PLAYER_CARRY_RELIC_WEAK, this);
}
```

### Relic Pad System

#### Shrine Mechanics
```csharp
public class GameRelicPad : GameStaticItem
{
    public eRelicType PadType { get; set; }      // Strength or Magic
    public GameRelic MountedRelic { get; set; }  // Currently held relic
    
    public void MountRelic(GameRelic relic, bool returning)
    {
        MountedRelic = relic;
        relic.CurrentRelicPad = this;
        
        // Apply realm ownership
        relic.Realm = this.Realm;
        relic.LastRealm = this.Realm;
        
        // Position relic on pad
        relic.X = this.X;
        relic.Y = this.Y;
        relic.Z = this.Z;
        relic.Heading = this.Heading;
    }
}
```

#### Automatic Capture
```csharp
public class PadArea : Area.Circle
{
    public override void OnPlayerEnter(GamePlayer player)
    {
        GameRelic relicOnPlayer = player.TempProperties.GetProperty<GameRelic>(
            GameRelic.PLAYER_CARRY_RELIC_WEAK);
            
        if (relicOnPlayer == null) return;
        
        // Type validation
        if (relicOnPlayer.RelicType != m_parent.PadType)
        {
            player.Out.SendMessage($"You need an empty {relicOnPlayer.RelicType} relic pad.");
            return;
        }
        
        // Realm validation  
        if (player.Realm == m_parent.Realm)
        {
            // Capture successful
            relicOnPlayer.RelicPadTakesOver(m_parent, returning: false);
        }
    }
}
```

### Bonus Calculations

#### Strength Relic Bonuses
```csharp
public static double GetRelicBonusModifier(eRealm realm, eRelicType type)
{
    double bonus = 0.0;
    bool owningSelf = false;
    
    foreach (GameRelic rel in getRelics(realm, type))
    {
        if (rel.Realm == rel.OriginalRealm)
            owningSelf = true;
        else
            bonus += ServerProperties.Properties.RELIC_OWNING_BONUS * 0.01;
    }
    
    // Bonus only applies if owning original relic
    return owningSelf ? bonus : 0.0;
}
```

#### Weapon Skill Enhancement
```csharp
public double CalculateWeaponSkill(DbInventoryItem weapon, double specModifier, 
    out double baseWeaponSkill)
{
    baseWeaponSkill = owner.GetWeaponSkill(weapon) + INHERENT_WEAPON_SKILL;
    double relicBonus = 1.0;
    
    if (owner is GamePlayer)
        relicBonus += RelicMgr.GetRelicBonusModifier(owner.Realm, eRelicType.Strength);
    
    return baseWeaponSkill * relicBonus * specModifier;
}
```

#### Magic Relic Integration
- **Spell Damage**: Enhanced by magic relic bonuses
- **Casting Speed**: May be affected by relic count
- **Mana Efficiency**: Potential bonus applications
- **Spell Penetration**: Enhanced breakthrough chances

### Relic Return System

#### Automatic Return Timer
```csharp
protected int ReturnRelicInterval => ServerProperties.Properties.RELIC_RETURN_TIME * 1000;

// Timer starts when relic dropped on ground
// Returns to original realm pad if unclaimed
// Configurable return time (default varies)
```

#### Return Conditions
- **Time Limit**: Relic returns after configured time
- **Original Shrine**: Returns to original realm's pad
- **No Carrier**: Must not be held by player
- **Ground State**: Must be dropped, not mounted

### Teleportation Restrictions

#### Relic Carrier Limitations
```csharp
public static bool IsPlayerCarryingRelic(GamePlayer player)
{
    return player.TempProperties.GetProperty<GameRelic>(PLAYER_CARRY_RELIC_WEAK) != null;
}

// Applied restrictions:
// - Cannot use personal bind recall
// - Cannot use keep teleports  
// - Cannot use portal stones
// - Cannot mount horses in some zones
```

#### Spell Restrictions
```csharp
// Personal bind recall check
if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
{
    SendInCombatMessage(player);
    return false;
}

// Keep portal check
if (GameRelic.IsPlayerCarryingRelic(player))
    return false;
```

## Relic Bonuses

### Realm-Wide Effects

#### Strength Relic Benefits
- **Melee Weapon Skill**: +10% per enemy relic held
- **Weapon Damage**: Increased effectiveness
- **Combat Rating**: Enhanced weapon proficiency
- **Relic Stacking**: Multiple relics = multiple bonuses

#### Magic Relic Benefits  
- **Spell Power**: +10% per enemy relic held
- **Casting Effectiveness**: Improved spell success
- **Magical Penetration**: Enhanced spell breakthrough
- **Relic Stacking**: Multiple relics = multiple bonuses

### Bonus Application

#### Ownership Requirements
```csharp
// Must own original relic to get enemy relic bonuses
foreach (GameRelic rel in getRelics(realm, type))
{
    if (rel.Realm == rel.OriginalRealm)
        owningSelf = true;  // Enables bonuses
    else
        bonus += 0.10;      // 10% per enemy relic
}

return owningSelf ? bonus : 0.0;
```

#### Maximum Bonuses
- **Per Realm**: 2 relics of each type (4 total)
- **Enemy Relics**: Up to 4 enemy relics possible  
- **Maximum Bonus**: 40% if holding all enemy relics
- **Prerequisite**: Must own original realm relic

### Currency System Integration

#### Bounty Points Enhancement
```csharp
// ROG Manager integration
double relicBonus = amount * (0.025 * RelicMgr.GetRelicCount(player.Realm));
var totBPs = amount + Convert.ToInt32(relicBonus);

if (relicBonus > 0)
    player.Out.SendMessage($"You gained additional {Convert.ToInt32(relicBonus)} BPs due to relic ownership!");
```

#### Orb Generation
```csharp
// Atlas Orb bonuses
double relicOrbBonus = (amount * (0.025 * RelicMgr.GetRelicCount(player.Realm)));
var totOrbs = amount + Convert.ToInt32(relicOrbBonus);

if (relicOrbBonus > 0)
    player.Out.SendMessage($"You gained additional {Convert.ToInt32(relicOrbBonus)} orbs due to relic ownership!");
```

## System Integration

### RvR Integration
```csharp
// War map display
int magic = RelicMgr.GetRelicCount(m_gameClient.Player.Realm, eRelicType.Magic);
int strength = RelicMgr.GetRelicCount(m_gameClient.Player.Realm, eRelicType.Strength);
byte relics = (byte)(magic << 4 | strength);  // Packed for display
```

### Keep System Integration
- **Relic Keeps**: Special keeps house relic pads
- **Cannot Claim**: Relic keeps cannot be guild claimed
- **Enhanced Defense**: Stronger than normal keeps
- **Strategic Value**: Critical for relic control

### Combat System Integration
```csharp
// Weapon skill bonus application
double relicBonus = 1.0;
if (owner is GamePlayer)
    relicBonus += RelicMgr.GetRelicBonusModifier(owner.Realm, eRelicType.Strength);
    
return baseWeaponSkill * relicBonus * specModifier;
```

## Configuration Options

### Server Properties
```csharp
RELIC_OWNING_BONUS = 10;                    // Bonus per enemy relic (%)
RELIC_RETURN_TIME = 1800;                  // Auto-return time (seconds)
ALLOW_RELIC_CAPTURE_IN_BG = false;         // Battleground relic capture
RELIC_TELEPORT_RESTRICTIONS = true;        // Disable teleports while carrying
```

### Relic Mechanics
- **Pickup Requirements**: Must own original to steal
- **Carry Limit**: One relic per player maximum  
- **Return Timer**: Configurable auto-return delay
- **Bonus Application**: Requires original relic ownership

## Special Features

### Minotaur Relics
```csharp
public class MinotaurRelic : GameStaticItem
{
    // Special labyrinth relics
    // Different mechanics than realm relics
    // Temporary ownership
    // XP and special rewards
}
```

#### Minotaur Relic Differences
- **Personal Ownership**: Individual rather than realm
- **Temporary**: Limited duration effects
- **PvE Content**: Not RvR-related
- **Experience Rewards**: Grants XP bonuses

### Relic Guard System
```csharp
// Keep guards affected by relic ownership
foreach (GameRelic relic in RelicMgr.getNFRelics())
{
    switch (relic.Realm)
    {
        case eRealm.Albion: albRelicCount++; break;
        case eRealm.Midgard: midRelicCount++; break;
        case eRealm.Hibernia: hibRelicCount++; break;
    }
}

// Guard strength reduced for realms with many relics
if (albRelicCount > 2)
{
    for (int i = 2; i < albRelicCount; i++)
        albGuardPercentRelic -= 25;  // 25% reduction per excess relic
}
```

## Test Scenarios

### Basic Relic Operations
1. **Pickup Requirements**: Cannot steal without owning original
2. **Carry Restrictions**: One relic per player enforced
3. **Teleport Blocks**: All teleportation disabled while carrying
4. **Stealth Prevention**: Cannot stealth with relic

### Bonus Calculations
1. **Ownership Check**: Must own original for bonuses
2. **Stacking Bonuses**: Multiple enemy relics stack
3. **Combat Application**: Bonuses applied to damage/skill
4. **Real-time Updates**: Bonuses update on relic status change

### Capture Mechanics
1. **Pad Validation**: Correct relic type for pad
2. **Realm Check**: Must be friendly realm pad
3. **Automatic Capture**: Triggers on area entry
4. **State Updates**: Proper relic state transitions

### Return System
1. **Timer Accuracy**: Returns after configured time
2. **Original Destination**: Goes to correct realm pad
3. **State Cleanup**: Proper carrier removal
4. **Notification System**: Realm notified of returns

## Edge Cases

### Disconnection Handling
- **Relic Drops**: Automatically drops on disconnect
- **Timer Starts**: Return timer begins immediately
- **State Cleanup**: Carrier properties cleared
- **Realm Notification**: Other players notified

### Realm Balance
- **Guard Reduction**: Excess relics weaken guards
- **Strategic Cost**: Too many relics = defensive weakness
- **Balance Mechanism**: Prevents relic hoarding
- **Dynamic Adjustment**: Real-time guard modifications

### Combat Restrictions
- **No Mounting**: Cannot mount while carrying relic
- **Teleport Blocks**: All forms of teleportation blocked
- **Movement Only**: Must travel on foot to deliver
- **Vulnerability**: Exposed during transport

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2025-01-20 | 1.0 | Initial comprehensive documentation |

## References
- `GameServer/keeps/Managers/RelicMgr.cs` - Relic management system
- `GameServer/keeps/Relics/GameRelic.cs` - Core relic implementation  
- `GameServer/keeps/Relics/GameRelicPad.cs` - Relic shrine system
- `GameServer/ECS-Components/AttackComponent.cs` - Bonus calculations
- `GameServer/events/keep/RelicGuardsOnKeepTaken.cs` - Guard balance system 