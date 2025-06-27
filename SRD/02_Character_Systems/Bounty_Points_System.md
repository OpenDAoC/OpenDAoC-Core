# Bounty Points System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs, AbstractServerRules.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Bounty Points are a special currency earned alongside Realm Points from RvR combat, used to buy equipment and supplies from frontier merchants. You earn BP from killing enemy players and capturing keeps, with the amount based on your target's level and your contribution to the fight. Unlike Realm Points which determine your rank, Bounty Points are spent like money to purchase RvR gear and siege equipment.

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