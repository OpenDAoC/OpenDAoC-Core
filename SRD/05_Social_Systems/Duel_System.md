# Duel System

**Document Status:** Initial Documentation  
**Completeness:** 85%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The duel system allows players to engage in consensual one-on-one combat without death penalties. Duels provide a safe way to test skills and settle disputes within the same realm.

## Core Mechanics

### Duel Initiation

#### Challenge Command
```csharp
// Command: /duel <player>
public void DuelChallenge(GamePlayer target)
{
    // Range check
    if (!IsWithinRadius(target, WorldMgr.VISIBILITY_DISTANCE))
    {
        Out.SendMessage("You must be closer to challenge someone to a duel!", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return;
    }
    
    // Same realm check
    if (Realm != target.Realm)
    {
        Out.SendMessage("You can't duel players from other realms!", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return;
    }
}
```

#### Challenge Requirements
- **Range**: Within visibility distance
- **Realm**: Same realm only
- **State**: Both players alive and not in combat
- **Zone**: Allowed in current zone
- **Level**: No level restrictions (configurable)

### Duel States

#### Player Duel Status
```csharp
public enum eDuelStatus
{
    NoDuel = 0,
    Challenged = 1,
    WaitingForAccept = 2,
    DuelStarting = 3,
    InDuel = 4,
    DuelEnding = 5
}
```

#### State Transitions
1. **No Duel** → Challenge sent → **Challenged**
2. **Challenged** → Accept → **Duel Starting**
3. **Duel Starting** → Countdown → **In Duel**
4. **In Duel** → Victory/Yield → **Duel Ending**
5. **Duel Ending** → Reset → **No Duel**

### Duel Rules

#### Combat Restrictions
```csharp
public bool CanAttack(GameLiving target)
{
    if (DuelTarget == target && InDuel)
        return true;
        
    if (target.DuelTarget == this && target.InDuel)
        return true;
        
    return false; // Cannot attack others during duel
}
```

#### Allowed Actions
- **Combat**: All abilities allowed
- **Movement**: Free movement
- **Items**: Potions and items usable
- **Pets**: Pets can participate
- **Buffs**: Pre-existing buffs remain

#### Disallowed Actions
- **Stealth**: Cannot re-stealth during duel
- **Groups**: Cannot invite/join groups
- **Trading**: Cannot trade during duel
- **Zoning**: Ends the duel
- **External Help**: No interference

### Duel Countdown

#### Start Sequence
```csharp
private void StartDuelCountdown()
{
    // 10 second countdown
    for (int i = 10; i > 0; i--)
    {
        BroadcastMessage($"Duel begins in {i}...");
        Thread.Sleep(1000);
    }
    
    BroadcastMessage("FIGHT!");
    DuelStatus = eDuelStatus.InDuel;
}
```

#### Positioning
- Players separated to starting positions
- Face each other automatically
- Cannot move during countdown
- Buffs/preparations allowed

### Victory Conditions

#### Duel Ends When
1. **Health Threshold**: Player reaches 10% health
2. **Yield Command**: Player types /yield
3. **Zone Change**: Either player zones
4. **Disconnect**: Either player logs out
5. **Death**: Accidental death (bug/exploit)
6. **Time Limit**: Optional server setting

#### Victory Determination
```csharp
public void CheckDuelVictory()
{
    if (DuelTarget.HealthPercent <= 10)
    {
        EndDuel(this, DuelTarget); // This player wins
    }
    else if (HealthPercent <= 10)
    {
        EndDuel(DuelTarget, this); // Target wins
    }
}
```

### Duel Completion

#### End of Duel Process
```csharp
private void EndDuel(GamePlayer winner, GamePlayer loser)
{
    // Restore health
    loser.Health = loser.MaxHealth;
    winner.Health = winner.MaxHealth;
    
    // Clear targets
    winner.TargetObject = null;
    loser.TargetObject = null;
    
    // Announcement
    foreach (GamePlayer player in winner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.Out.SendMessage($"{winner.Name} has defeated {loser.Name} in a duel!", 
            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }
    
    // Reset status
    winner.DuelTarget = null;
    loser.DuelTarget = null;
}
```

#### Post-Duel Effects
- **No Death Penalty**: No experience loss
- **No Loot**: No item drops
- **Full Restoration**: Health/Mana restored
- **No Resurrection**: Not needed
- **Honor System**: Victory logged

## System Interactions

### PvP System Integration
```csharp
// Duel bypasses normal PvP restrictions
if (attacker.DuelTarget == defender && attacker.InDuel)
{
    return true; // Allow attack regardless of PvP rules
}
```

### Zone Restrictions
```csharp
// Check if zone allows duels
if (CurrentZone.IsSafeZone)
{
    Out.SendMessage("Duels are not allowed in safe zones!", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return;
}
```

### Group System
- Cannot duel group members
- Duel prevents group invites
- Existing group unaffected

### Guild System
- Can duel guildmates
- No guild penalties
- Honor among allies

## Commands

### Duel Commands
| Command | Description |
|---------|-------------|
| `/duel <player>` | Challenge player to duel |
| `/duel accept` | Accept duel challenge |
| `/duel decline` | Decline duel challenge |
| `/yield` | Surrender current duel |
| `/duel cancel` | Cancel sent challenge |

### Status Commands
| Command | Description |
|---------|-------------|
| `/duel status` | Show current duel status |
| `/duel stats` | Show duel statistics |

## Configuration

### Server Properties
```csharp
// Allow duels server-wide
ALLOW_DUELS = true

// Minimum level for duels
DUEL_MIN_LEVEL = 5

// Maximum duel duration (minutes)
DUEL_TIME_LIMIT = 10

// Health threshold for victory (percent)
DUEL_END_HEALTH_PERCENT = 10

// Allow duels in RvR zones
ALLOW_DUELS_IN_RVR = false
```

## Messages

### Challenge Messages
```csharp
// To challenger
"You challenge {0} to a duel!"

// To target
"{0} has challenged you to a duel! Type /duel accept to accept."

// To area
"{0} has challenged {1} to a duel!"
```

### Duel Messages
```csharp
// Countdown
"Duel begins in {0} seconds..."

// Start
"FIGHT!"

// Victory
"{0} has defeated {1} in a duel!"

// Yield
"{0} yields to {1}!"
```

## Anti-Exploit Measures

### Interference Prevention
- Non-participants cannot affect duel
- AoE spells ignore duel participants
- Pets bound to duel rules

### Abuse Prevention
```csharp
// Cooldown between duels
if (LastDuelTime + DUEL_COOLDOWN > GameLoop.GameLoopTime)
{
    Out.SendMessage("You must wait before dueling again!", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return;
}
```

## Edge Cases

### Simultaneous Challenges
- Can only have one active challenge
- New challenges cancel old ones
- Clear messaging to all parties

### Zone Boundaries
- Crossing zone line ends duel
- Warning when near boundaries
- No exploiting zone mechanics

### Crowd Control
- CC abilities work normally
- Duration limits prevent griefing
- Yield always available option

### Pet Classes
- Pets follow duel rules
- Cannot attack non-participants
- Despawn doesn't end duel

## Test Scenarios

1. **Basic Duel Flow**
   - Challenge and accept
   - Fight to 10% health
   - Verify restoration
   - Check messaging

2. **Interruption Tests**
   - Zone during duel
   - Disconnect handling
   - Third party interference
   - Yield command

3. **Edge Cases**
   - Multiple challenges
   - Pet participation
   - Boundary testing
   - CC duration limits

4. **Anti-Exploit**
   - No death penalties
   - No loot generation
   - Cooldown enforcement
   - Zone restriction respect

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 