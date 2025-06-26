# Loot Distribution System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from loot implementations  
**Implementation Status:** Live

## Overview

The Loot Distribution System manages item drops from NPCs, treasure generation, and group loot sharing mechanics. This system determines what items are dropped, their quality, and how they're distributed among players.

## Core Architecture

### Loot Generation Types
```csharp
public enum eLootType
{
    NormalDrop,     // Standard NPC drops
    BonusDrop,      // Additional chance drops
    UniqueDrop,     // Named/special items
    ArtifactDrop,   // Artifact encounters
    TreasureDrop,   // Chest/container loot
    QuestDrop,      // Quest-specific items
    CraftDrop,      // Crafting materials
    CurrencyDrop    // Money, bounty points, etc.
}

public interface ILootGenerator
{
    List<ILootDrop> GenerateLoot(GameNPC killer, GamePlayer mostDamage);
    double GetDropChance(ILootTemplate template, GamePlayer player);
    int DetermineItemQuality(ILootTemplate template, int killerLevel);
}
```

## Drop Chance Calculation

### Base Drop Mechanics
```csharp
public class LootDropCalculator
{
    public static bool ShouldDropItem(ILootTemplate template, GameNPC npc, GamePlayer killer)
    {
        double baseChance = template.DropChance;
        
        // Level difference modifier
        int levelDiff = killer.Level - npc.Level;
        double levelMod = CalculateLevelModifier(levelDiff);
        
        // Luck modifier
        double luckMod = CalculateLuckModifier(killer);
        
        // Group size modifier
        double groupMod = CalculateGroupModifier(killer.Group);
        
        // Server modifier
        double serverMod = Properties.LOOT_DROP_MODIFIER;
        
        double finalChance = baseChance * levelMod * luckMod * groupMod * serverMod;
        
        return Util.ChanceDouble(finalChance);
    }
    
    private static double CalculateLevelModifier(int levelDiff)
    {
        if (levelDiff <= -10) return 0.1;   // Much higher level target
        if (levelDiff <= -5) return 0.5;    // Higher level target
        if (levelDiff <= 0) return 1.0;     // Equal or slightly higher level
        if (levelDiff <= 5) return 0.8;     // Lower level target
        if (levelDiff <= 10) return 0.5;    // Much lower level target
        return 0.1;                         // Very low level target
    }
    
    private static double CalculateLuckModifier(GamePlayer player)
    {
        // Base luck from stats
        double luck = 1.0 + (player.GetModified(eProperty.LuckBonus) * 0.01);
        
        // Realm abilities
        luck += player.GetAbilityLevel(Abilities.Serenity) * 0.02;
        
        return Math.Min(luck, 2.0); // Cap at 200%
    }
}
```

### Quality Determination
```csharp
public class ItemQualityCalculator
{
    public static int DetermineQuality(ILootTemplate template, GamePlayer killer, GameNPC npc)
    {
        int baseQuality = template.Quality;
        
        // Random variance (±5%)
        int variance = Util.Random(-5, 6);
        
        // Level bonus (higher level NPCs drop better quality)
        int levelBonus = Math.Min(10, npc.Level / 5);
        
        // Named/boss bonus
        int namedBonus = npc.IsNamedBoss ? 10 : 0;
        
        // Player luck influence
        int luckBonus = (int)(killer.GetModified(eProperty.LuckBonus) * 0.1);
        
        int finalQuality = baseQuality + variance + levelBonus + namedBonus + luckBonus;
        
        return Math.Max(70, Math.Min(100, finalQuality));
    }
}
```

## Group Loot Distribution

### Loot Distribution Rules
```csharp
public enum eLootRule
{
    FreeForAll,     // Anyone can loot
    RoundRobin,     // Take turns
    MasterLooter,   // Leader distributes
    NeedGreed,      // Need/Greed/Pass system
    GroupLeader     // Leader gets all loot
}

public class GroupLootManager
{
    public void DistributeLoot(GameNPC npc, List<ILootDrop> loot, Group group)
    {
        switch (group.LootRule)
        {
            case eLootRule.FreeForAll:
                DistributeFreeForAll(loot, group);
                break;
                
            case eLootRule.RoundRobin:
                DistributeRoundRobin(loot, group);
                break;
                
            case eLootRule.MasterLooter:
                DistributeToMasterLooter(loot, group);
                break;
                
            case eLootRule.NeedGreed:
                InitiateNeedGreedRoll(loot, group);
                break;
                
            case eLootRule.GroupLeader:
                DistributeToLeader(loot, group);
                break;
        }
    }
    
    private void DistributeRoundRobin(List<ILootDrop> loot, Group group)
    {
        var eligibleMembers = group.Members
            .Where(m => IsEligibleForLoot(m, group))
            .ToList();
            
        if (eligibleMembers.Count == 0)
            return;
            
        foreach (var item in loot)
        {
            var recipient = eligibleMembers[group.LootIndex % eligibleMembers.Count];
            GiveLootToPlayer(recipient, item);
            group.LootIndex++;
        }
    }
}
```

### Need/Greed System
```csharp
public class NeedGreedSystem
{
    private readonly Dictionary<ILootDrop, LootRoll> _activeRolls = new();
    
    public void InitiateLootRoll(ILootDrop loot, Group group)
    {
        var roll = new LootRoll
        {
            Item = loot,
            Group = group,
            RollTimeRemaining = 30000, // 30 seconds
            EligiblePlayers = GetEligiblePlayers(group, loot)
        };
        
        _activeRolls[loot] = roll;
        
        // Notify eligible players
        foreach (var player in roll.EligiblePlayers)
        {
            ShowLootRollWindow(player, loot);
        }
        
        // Start countdown timer
        StartRollTimer(roll);
    }
    
    public void ProcessRoll(GamePlayer player, ILootDrop loot, eLootRollType rollType)
    {
        if (!_activeRolls.TryGetValue(loot, out var roll))
            return;
            
        if (!roll.EligiblePlayers.Contains(player))
            return;
            
        roll.PlayerRolls[player] = new LootRollResult
        {
            RollType = rollType,
            RollValue = rollType == eLootRollType.Pass ? 0 : Util.Random(1, 101)
        };
        
        // Check if all players have rolled
        if (roll.PlayerRolls.Count >= roll.EligiblePlayers.Count)
        {
            ResolveLootRoll(roll);
        }
    }
    
    private void ResolveLootRoll(LootRoll roll)
    {
        var needRolls = roll.PlayerRolls
            .Where(kvp => kvp.Value.RollType == eLootRollType.Need)
            .OrderByDescending(kvp => kvp.Value.RollValue);
            
        var greedRolls = roll.PlayerRolls
            .Where(kvp => kvp.Value.RollType == eLootRollType.Greed)
            .OrderByDescending(kvp => kvp.Value.RollValue);
        
        GamePlayer winner = null;
        
        // Need rolls win over greed rolls
        if (needRolls.Any())
        {
            winner = needRolls.First().Key;
        }
        else if (greedRolls.Any())
        {
            winner = greedRolls.First().Key;
        }
        
        if (winner != null)
        {
            GiveLootToPlayer(winner, roll.Item);
            NotifyGroupOfWinner(roll.Group, winner, roll.Item);
        }
        
        _activeRolls.Remove(roll.Item);
    }
}

public enum eLootRollType
{
    Need,
    Greed,
    Pass
}
```

## Treasure Generation

### Container Loot
```csharp
public class TreasureGenerator
{
    public static List<ILootDrop> GenerateContainerLoot(ITreasureContainer container, GamePlayer opener)
    {
        var loot = new List<ILootDrop>();
        
        // Generate money
        if (container.MoneyChance > 0 && Util.ChanceDouble(container.MoneyChance))
        {
            long money = CalculateTreasureMoney(container, opener);
            loot.Add(new MoneyLoot(money));
        }
        
        // Generate items
        foreach (var template in container.LootTemplates)
        {
            if (ShouldDropItem(template, opener))
            {
                var item = CreateLootItem(template, opener);
                loot.Add(item);
            }
        }
        
        // Special treasure bonuses
        if (container.IsRareChest && Util.Chance(5))
        {
            var rareItem = GenerateRareItem(opener.Level);
            loot.Add(rareItem);
        }
        
        return loot;
    }
    
    private static long CalculateTreasureMoney(ITreasureContainer container, GamePlayer opener)
    {
        long baseMoney = container.BaseMoney;
        
        // Level scaling
        double levelMod = 1.0 + (opener.Level * 0.02);
        
        // Random variance (50% to 150% of base)
        double variance = Util.RandomDouble(0.5, 1.5);
        
        return (long)(baseMoney * levelMod * variance);
    }
}
```

### Special Drop Mechanics
```csharp
public class SpecialDrops
{
    public static void ProcessBossKill(GameNPC boss, GamePlayer killer)
    {
        // Guaranteed artifact credit
        if (boss.IsArtifactBoss)
        {
            GrantArtifactCredit(killer, boss.ArtifactID);
        }
        
        // Epic encounter bonuses
        if (boss.IsEpicBoss)
        {
            var contributors = GetEpicContributors(boss);
            DistributeEpicLoot(boss, contributors);
        }
        
        // First kill bonuses
        if (IsFirstKill(boss, killer))
        {
            GrantFirstKillBonus(killer, boss);
        }
        
        // Realm point bonuses for RvR kills
        if (boss.IsRvRBoss)
        {
            GrantRealmPointBonus(killer, boss);
        }
    }
    
    private static void DistributeEpicLoot(GameNPC boss, List<GamePlayer> contributors)
    {
        // Epic bosses have special loot tables
        var epicLoot = GenerateEpicLoot(boss);
        
        foreach (var item in epicLoot)
        {
            // Use need/greed system for epic loot
            if (contributors.Count > 1)
            {
                var group = GetLargestGroup(contributors);
                if (group != null)
                {
                    InitiateNeedGreedRoll(item, group);
                }
                else
                {
                    // Random distribution if no group
                    var randomWinner = contributors[Util.Random(contributors.Count)];
                    GiveLootToPlayer(randomWinner, item);
                }
            }
            else
            {
                GiveLootToPlayer(contributors[0], item);
            }
        }
    }
}
```

## Anti-Exploitation Measures

### Loot Security
```csharp
public class LootSecurity
{
    public static bool IsEligibleForLoot(GamePlayer player, GameNPC npc)
    {
        // Must have damaged the NPC
        if (!npc.XPGainers.ContainsKey(player))
            return false;
            
        // Must be within level range
        int levelDiff = Math.Abs(player.Level - npc.Level);
        if (levelDiff > MAX_LOOT_LEVEL_DIFF)
            return false;
            
        // Must be within range when NPC dies
        if (!player.IsWithinRadius(npc, LOOT_RANGE))
            return false;
            
        // Must not be exploiting (AFK farming, etc.)
        if (IsPlayerExploiting(player))
            return false;
            
        return true;
    }
    
    private static bool IsPlayerExploiting(GamePlayer player)
    {
        // Check for rapid consecutive kills (farming)
        var recentKills = player.TempProperties.GetProperty<List<long>>("RecentKillTimes");
        if (recentKills?.Count > 10)
        {
            var timespan = recentKills.Last() - recentKills.First();
            if (timespan < 300000) // 5 minutes
            {
                return true; // Too many kills too quickly
            }
        }
        
        // Check for movement (AFK detection)
        var lastMovement = player.TempProperties.GetProperty<long>("LastMovementTime");
        if (GameLoop.GameLoopTime - lastMovement > 600000) // 10 minutes
        {
            return true; // Player appears AFK
        }
        
        return false;
    }
}
```

## Configuration

```csharp
[ServerProperty("loot", "drop_modifier", 1.0)]
public static double LOOT_DROP_MODIFIER;

[ServerProperty("loot", "quality_bonus_enabled", true)]
public static bool QUALITY_BONUS_ENABLED;

[ServerProperty("loot", "max_level_difference", 8)]
public static int MAX_LOOT_LEVEL_DIFF;

[ServerProperty("loot", "loot_range", 1000)]
public static int LOOT_RANGE;

[ServerProperty("loot", "need_greed_timer", 30)]
public static int NEED_GREED_TIMER; // seconds
```

## TODO: Missing Documentation

- Advanced loot scaling algorithms
- Cross-server loot synchronization
- Seasonal and event-specific loot modifications
- Player loot history tracking and analysis
- Advanced anti-farming measures

## References

- `GameServer/gameobjects/GameNPC.cs` - Loot generation triggers
- `GameServer/commands/LootManager.cs` - Loot distribution management
- `GameServer/packets/Server/LootWindowHandler.cs` - Client loot interface
- Various loot template implementations 