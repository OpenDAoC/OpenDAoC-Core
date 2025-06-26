# Bounty Points System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs, AbstractServerRules.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Bounty Points (BP) are a secondary RvR currency earned alongside Realm Points. They are used to purchase items, supplies, and special equipment from Bounty Point merchants in frontier zones.

## Core Mechanics

### BP Acquisition

#### Player Kill Value
```csharp
// Player BP value formula
public override int BountyPointsValue
{
    get { return (int)(1 + Level * 0.6); }
}
```
- **Base Formula**: 1 + (Level * 0.6)
- **Level 50**: 1 + (50 * 0.6) = 31 BP value
- **Level 1**: 1 + (1 * 0.6) = 1.6 = 1 BP value (rounded)

**Source**: `GamePlayer.cs:BountyPointsValue`

#### BP Rewards from Kills
```csharp
// BP reward calculation
void RewardBountyPoints()
{
    int npcBpValue = killedNpc.BountyPointsValue;
    int bountyPoints;
    
    // Keeps and tower captures reward full BP
    if (killedNpc is GuardLord)
        bountyPoints = npcBpValue;
    else
    {
        int bpCap = playerToAward.BountyPointsValue * 2;
        bountyPoints = Math.Min(bpCap, (int)(npcBpValue * damagePercent));
    }
    
    if (bountyPoints > 0)
        playerToAward.GainBountyPoints(bountyPoints);
}
```

### BP Modifiers

#### Server Rate Modifier
```csharp
// BP rate modifier applied
if (modify)
{
    double modifier = ServerProperties.Properties.BP_RATE;
    if (modifier != -1)
        amount = (long)(amount * modifier);
}
```

#### Guild Bonuses
```csharp
// Guild BP bonus
if (player.Guild.BonusType == Guild.eBonusType.BountyPoints)
{
    long bonusBountyPoints = (long)Math.Ceiling(
        (double)bpsArgs.BountyPoints * 
        ServerProperties.Properties.GUILD_BUFF_BP / 100);
    
    player.GainBountyPoints(bonusBountyPoints, false, false, false);
    player.Guild.BountyPoints += bonusBountyPoints;
}
```

#### Outpost Bonuses
```csharp
// Outpost BP bonuses
if (KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_5, realm))
    bonus = bountyPoints * 5 / 100;  // +5%
else if (KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_3, realm))
    bonus = bountyPoints * 3 / 100;  // +3%
```

### BP Caps and Restrictions

#### Damage-Based Capping
```csharp
// Player vs Player kills
int bpCap = playerToAward.BountyPointsValue * 2;
bountyPoints = Math.Min(bpCap, (int)(baseBpReward * damagePercent));
```
- **Cap**: 2x player's own BP value
- **Damage Percent**: Proportional to damage dealt

#### Battleground Restrictions
```csharp
// BG BP restrictions
baseBpReward = (!Properties.ALLOW_BPS_IN_BGS && killedPlayer.CurrentZone.IsBG ? 
                0 : killedPlayer.BountyPointsValue) / entityCount;
```
- **Server Setting**: `ALLOW_BPS_IN_BGS` controls BG BP
- **Default**: Often disabled in battlegrounds

### NPC BP Values

#### Guard Lords (Keeps/Towers)
```csharp
public override int BountyPointsValue
{
    get
    {
        // PvE Lords drop dreaded seals instead
        if (Realm == eRealm.None && ServerType == EGameServerType.GST_PvE)
            return 0;
            
        // Check worth timer
        long duration = (GameLoopTime - m_lastSpawnTime) / 1000L;
        if (duration < Properties.LORD_BP_WORTH_SECONDS)
            return 0;
            
        return Component?.Keep?.BountyPointsValue() ?? 5000;
    }
}
```

#### Keep Calculation
```csharp
public virtual int GetBountyPointsForKeep(AbstractGameKeep keep)
{
    // Base implementation returns 0
    // Subclass implementations vary by server rules
    return 0;
}
```

### Player Actions

#### Spending BP
```csharp
public bool RemoveBountyPoints(long amount, string message)
{
    if (BountyPoints < amount)
    {
        Out.SendMessage("You don't have enough bounty points!", 
                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    BountyPoints -= amount;
    
    if (message != null)
        Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        
    Out.SendUpdatePoints();
    return true;
}
```

#### BP Gain Messages
```csharp
if (sendMessage && amount > 0)
    Out.SendMessage("You gain " + amount + " bounty points!", 
                   eChatType.CT_Important, eChatLoc.CL_SystemWindow);
```

## System Interactions

### With Guild System
- **Guild Accumulation**: Guilds track total BP earned
- **Guild Bonuses**: BP bonus type increases BP gain
- **Guild Buffs**: Percentage-based bonus system

### With RvR System
- **Parallel Currency**: Earned alongside Realm Points
- **Same Sources**: Player kills, keep captures
- **Same Restrictions**: Damage caps, worth timers

### With Keep System
- **Keep Bonuses**: Outpost ownership provides BP bonuses
- **Lord Kills**: Major BP rewards for keep captures
- **Worth Timers**: Lords must be alive minimum time

### With Merchant System
- **BP Merchants**: Special vendors accepting only BP
- **Equipment**: RvR-focused gear and supplies
- **Consumables**: Siege equipment, potions

## Database Storage

### Character Storage
```sql
DOLCharacters table:
- BountyPoints: Current BP balance (long)
```

### Guild Storage
```sql
Guild table:
- BountyPoints: Guild total BP accumulated
```

## Configuration

### Server Properties
```csharp
[ServerProperty("rates", "bp_rate", 
    "The Bounty Points Rate Modifier", 1.0)]
public static double BP_RATE;

[ServerProperty("guild", "guild_buff_bp", 
    "Guild BP buff percentage", 10)]
public static int GUILD_BUFF_BP;

[ServerProperty("rvr", "allow_bps_in_bgs", 
    "Allow bounty points in battlegrounds", false)]
public static bool ALLOW_BPS_IN_BGS;

[ServerProperty("rvr", "lord_bp_worth_seconds", 
    "Seconds lord must be alive to be worth BP", 300)]
public static int LORD_BP_WORTH_SECONDS;
```

## BP Economy

### Typical Sources
1. **Player Kills**: 1-31 BP (based on target level)
2. **Keep Lords**: 1000-5000+ BP (server dependent)
3. **Tower Lords**: 100-1000 BP (server dependent)
4. **Guild Bonuses**: +10% standard guild bonus
5. **Outpost Bonuses**: +3% or +5% with keep control

### Typical Sinks
1. **Equipment**: Weapons, armor, jewelry
2. **Siege Equipment**: Rams, catapults
3. **Consumables**: Potions, tools
4. **Mounts**: Special RvR mounts
5. **Supplies**: Arrows, repair materials

## Calculation Examples

### Level 50 Player Kill
```
Base BP Value: 1 + (50 * 0.6) = 31 BP
Group Split (4 players): 31 / 4 = 7.75 = 7 BP each
With Guild Bonus (10%): 7 + 0.7 = 7.7 = 7 BP
With Outpost Bonus (5%): 7 + 0.35 = 7.35 = 7 BP
Total per player: 7 BP
```

### Keep Lord Kill
```
Base Lord Value: 5000 BP
No Group Split: Full value to all participants
With Guild Bonus: 5000 + 500 = 5500 BP
With Outpost Bonus: 5500 + 275 = 5775 BP
Total per participant: 5775 BP
```

### Damage-Based Award
```
Player deals 40% damage to kill:
Base Award: 31 BP
Damage Factor: 31 * 0.4 = 12.4 = 12 BP
Cap Check: Min(12, playerBPValue * 2)
If player is level 50: Min(12, 31 * 2) = Min(12, 62) = 12 BP
```

## Edge Cases and Special Rules

### PvE Server Differences
- **Guard Lords**: Drop Dreaded Seals instead of BP
- **Alternative Rewards**: Item-based progression
- **RvR Objectives**: May have different BP values

### Worth Timers
- **Lord Spawns**: Must be alive minimum time to be worth BP
- **Default Timer**: 300 seconds (5 minutes)
- **Prevents Exploitation**: Stops rapid respawn farming

### Group vs Solo
- **Group Kills**: BP split among contributing members
- **Solo Kills**: Full BP value to killer
- **Damage Contribution**: Proportional awards

## Testing Scenarios

### Basic BP Award Test
```
Given: Level 30 target (BP value = 19)
When: Killed by level 50 player
Then: Killer receives 19 BP
Message: "You gain 19 bounty points!"
```

### Guild Bonus Test
```
Given: Guild has BP bonus active
When: Member gains 100 BP
Then: Member gains additional 10 BP from guild
Guild total increases by 110 BP
```

### Battleground Test
```
Given: ALLOW_BPS_IN_BGS = false
When: Player killed in battleground
Then: No BP awarded
Only RP awarded
```

### Cap Test
```
Given: Level 20 player (BP value = 13)
When: Deals 20% damage to level 50 kill
Expected: 31 * 0.2 = 6.2 = 6 BP
Cap: Min(6, 13 * 2) = Min(6, 26) = 6 BP
Result: 6 BP awarded
```

## References
- **Core Logic**: `GameServer/gameobjects/GamePlayer.cs`
- **Server Rules**: `GameServer/serverrules/AbstractServerRules.cs`
- **Guild Events**: `GameServer/gameutils/GuildEvents.cs`
- **Properties**: `GameServer/serverproperty/ServerProperties.cs`
- **Keep Lords**: `GameServer/keeps/Gameobjects/Guards/Lord.cs` 