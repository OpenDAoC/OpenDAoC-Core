# Line of Sight (LoS) System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from CheckLosResponseHandler.cs, various spell/combat implementations
- **Implementation**: Stable

## Overview

**Game Rule Summary**: The Line of Sight (LoS) system determines whether you can "see" your target for spells, attacks, and abilities by checking if there are walls, trees, or other obstacles blocking your view. When you try to cast a spell or attack someone, your client checks if you have a clear line of sight to your target - if there's a wall in the way, the action will fail with a "target is not in view" message. This prevents you from shooting arrows or casting spells through solid walls, making positioning and terrain important tactical elements. The system uses your client's 3D collision data to calculate these checks quickly, and objects that are very close to you (within about 32 units) are always considered in view regardless of obstacles.

The Line of Sight system determines whether one game object can "see" another by checking for obstructions like walls, terrain, and other obstacles. LoS checks are performed client-side using the game's collision data, making them asynchronous operations.

## Core Mechanics

### LoS Check Architecture

#### Client-Based System
```csharp
// LoS checks are performed by the client using local collision data
player.Out.SendCheckLos(source, target, new CheckLosResponse(callback));

// Response enumeration
public enum eLosCheckResponse : byte
{
    FALSE = 0,  // No line of sight
    TRUE = 1    // Clear line of sight
}
```

#### Callback Pattern
```csharp
// Callback delegate
public delegate void CheckLosResponse(
    GamePlayer player, 
    eLosCheckResponse response, 
    ushort sourceOID, 
    ushort targetOID
);

// Example callback implementation
private void OnLosCheckComplete(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
{
    GameObject target = CurrentRegion.GetObject(targetOID);
    
    if (response == eLosCheckResponse.TRUE)
    {
        // LoS confirmed - proceed with action
        ExecuteAction(target);
    }
    else
    {
        // No LoS - handle failure
        MessageToCaster("Target is not in view!", eChatType.CT_System);
    }
}
```

### Timeout System

#### Timeout Configuration
```csharp
// Server property for LoS check timeout
[ServerProperty("world", "los_check_timeout", "LoS check timeout in milliseconds", 2000)]
public static int LOS_CHECK_TIMEOUT;

// Timeout timer management
public class TimeoutTimer : ECSGameTimerWrapperBase
{
    private eLosCheckResponse _response;
    private CheckLosResponse _firstCallback;
    public List<CheckLosResponse> Callbacks { get; private set; }
    
    public void SetResponse(eLosCheckResponse response)
    {
        _response = response;
        // Timer will invoke callbacks when it ticks
    }
}
```

#### Multiple Callbacks
- Multiple LoS checks to same target reuse existing request
- Additional callbacks queued until response received
- All callbacks invoked when response arrives or timeout occurs

## Usage Contexts

### Combat LoS Checks

#### Melee Combat
```csharp
// Basic front check for melee
if (!owner.IsObjectInFront(ad.Target, 120) && owner.TargetInView)
{
    ad.AttackResult = eAttackResult.TargetNotVisible;
    return ad;
}
```

#### Ranged Combat
```csharp
// Archer/siege weapon LoS
public override void Fire()
{
    if (TargetObject == null)
        return;
        
    Owner?.Out.SendCheckLos(Owner, TargetObject, new CheckLosResponse(FireCheckLos));
}

private void FireCheckLos(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
{
    if (response == eLosCheckResponse.TRUE)
        base.Fire();
    else
        Owner?.Out.SendMessage("Target is not in view!", eChatType.CT_Say);
}
```

### Spell LoS Checks

#### Direct Damage Spells
```csharp
public override void OnDirectEffect(GameLiving target)
{
    GamePlayer checkPlayer = GetLosChecker(target);
    
    if (checkPlayer != null)
        checkPlayer.Out.SendCheckLos(Caster, target, new CheckLosResponse(DealDamageCheckLos));
    else
        DealDamage(target); // No player available, skip LoS
}
```

#### During Cast Checks
```csharp
// Configuration for during-cast LoS checks
[ServerProperty("world", "check_los_during_cast", "Check LoS during spell casts", true)]
public static bool CHECK_LOS_DURING_CAST;

// Minimum interval between checks
[ServerProperty("world", "check_los_during_cast_minimum_interval", "Min interval between checks", 200)]
public static int CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL;
```

### NPC LoS Checks

#### Aggro LoS
```csharp
// Check LoS before allowing aggro
[ServerProperty("world", "check_los_before_aggro", "LoS check before NPC aggro", true)]
public static bool CHECK_LOS_BEFORE_AGGRO;

protected virtual void CheckPlayerAggro()
{
    foreach (GamePlayer player in Body.GetPlayersInRadius(AggroRange))
    {
        if (!CanAggroTarget(player))
            continue;
            
        if (Properties.CHECK_LOS_BEFORE_AGGRO)
            SendLosCheckForAggro(player, player);
        else
            AddToAggroList(player, 1);
    }
}
```

#### NPC Spellcasting
```csharp
// NPCs request LoS checks through nearby players
public override bool CastSpell(Spell spell, SpellLine line)
{
    GamePlayer losChecker = GetNearbyPlayer();
    
    if (losChecker == null)
        return base.CastSpell(spell, line); // No LoS check
        
    _spellsWaitingForLosCheck.AddOrUpdate(TargetObject, ...);
    losChecker.Out.SendCheckLos(this, TargetObject, new CheckLosResponse(CastSpellLosCheckReply));
    return false; // Wait for LoS response
}
```

## Special Cases

### Always In View Range
```csharp
// Objects within 32 units are always considered in view
bool IsObjectInFront(GameObject target, double heading, int alwaysTrueRange = 32)
{
    // Check angle first
    float angle = GetAngle(target);
    
    if (angle >= 360 - heading/2 || angle < heading/2)
        return true;
        
    // Very close targets always in view
    return IsWithinRadius(target, alwaysTrueRange);
}
```

### Stealth Detection
```csharp
// LoS check when uncovering stealthed players
public void UncoverLosHandler(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
{
    if (response == eLosCheckResponse.TRUE)
    {
        player.Out.SendMessage(target.GetName(0, true) + " uncovers you!", eChatType.CT_System);
        player.Stealth(false);
    }
}
```

### Keep/Siege LoS
```csharp
// Ballista uses owner for LoS source (workaround)
Owner?.Out.SendCheckLos(Owner, TargetObject, new CheckLosResponse(FireCheckLos));

// Keep door teleportation
if (GameServer.KeepManager.IsEnemy(this, player))
{
    // Enemy - no teleport, check LoS for attack
    return false;
}
```

## Configuration

### Server Properties
```ini
# LoS System Configuration
LOS_CHECK_TIMEOUT = 2000  # Milliseconds before timeout

# Combat LoS
CHECK_LOS_BEFORE_AGGRO = true
CHECK_LOS_BEFORE_AGGRO_FNF = true  # Frontier turrets
CHECK_LOS_BEFORE_NPC_RANGED_ATTACK = true

# Spell LoS
CHECK_LOS_DURING_CAST = true
CHECK_LOS_DURING_CAST_INTERRUPT = false
CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL = 200
CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL = 200
```

### Target In View Flag
```csharp
// Client sends view status with target selection
public class PlayerTargetHandler
{
    // Flags from client:
    // 0x4000 = LOS1 bit
    // 0x2000 = LOS2 bit
    
    bool targetInView = (flags & (0x4000 | 0x2000)) != 0;
    player.TargetInView = targetInView;
}
```

## Performance Considerations

### Caching Strategy
```csharp
// NPCs cache pending spell casts during LoS checks
private ConcurrentDictionary<GameObject, List<SpellWaitingForLosCheck>> _spellsWaitingForLosCheck;

// Clean up old entries
foreach (var pair in _spellsWaitingForLosCheck)
{
    for (int i = list.Count - 1; i >= 0; i--)
    {
        if (ServiceUtils.ShouldTick(list[i].RequestTime + 2000))
            list.SwapRemoveAt(i);
    }
}
```

### Request Batching
- Multiple requests to same source/target pair share single check
- Reduces network traffic and client load
- All callbacks invoked together

### NPC Optimizations
- NPCs use nearby players for LoS checks
- Prefer target player, then owner, then random nearby
- Skip LoS if no players available

## Test Scenarios

### Basic LoS Tests
```csharp
// Given: Clear path between caster and target
// When: Request LoS check
// Then: Callback receives TRUE response

// Given: Wall between caster and target
// When: Request LoS check
// Then: Callback receives FALSE response

// Given: Target within 32 units
// When: Check IsObjectInFront
// Then: Always returns true (skip LoS)
```

### Timeout Tests
```csharp
// Given: LoS check requested
// When: No response within timeout (2s)
// Then: Timeout timer invokes callbacks with FALSE

// Given: Multiple callbacks queued
// When: Response received
// Then: All callbacks invoked with same response
```

### Combat Integration
```csharp
// Given: Archer targeting through wall
// When: Fire command issued
// Then: "Target is not in view!" message

// Given: Spell cast with LoS target
// When: Target moves behind wall during cast
// Then: Spell fails on next LoS check
```

## Known Issues
- Ballista LoS uses owner position instead of weapon
- Some indoor areas have incorrect collision data
- Very thin walls may not block LoS properly
- Height differences can cause false positives

## Future Enhancements
- TODO: Server-side LoS validation option
- TODO: Raycast visualization for debugging
- TODO: LoS caching for static objects
- TODO: Improved height-based LoS

## Change Log
- 2025-01-20: Initial documentation created

## References
- `GameServer/packets/Client/168/CheckLosResponseHandler.cs`
- `GameServer/packets/Server/PacketLib1XX.cs` (SendCheckLos)
- `GameServer/spells/SpellHandler.cs` (During cast checks)
- `GameServer/ai/brain/StandardMobBrain.cs` (Aggro LoS)
- `GameServer/gameobjects/GameObject.cs` (IsObjectInFront) 