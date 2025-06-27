# Achievement System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

**Game Rule Summary**: The Achievement System tracks and rewards your accomplishments across all aspects of DAoC, creating permanent records of your progress that span your entire account. Every time you kill a dragon, defeat other players in RvR, capture a keep, or master a craft, the system records these achievements and may unlock special titles, merchant access, or other rewards. Unlike character progression that's tied to individual characters, achievements are account-wide, so all your characters benefit from your accomplishments. The system recognizes both PvE achievements (like slaying epic monsters) and PvP achievements (like becoming a "Master Soldier" with 2,000+ player kills), providing long-term goals and recognition that extend far beyond reaching level 50. This creates a sense of permanent progression and accomplishment that persists across characters and play sessions.

The Achievement System tracks player accomplishments across PvE, RvR, and social activities. It provides permanent account-level progress tracking with various categories including combat achievements, time-based milestones, and special accomplishments that can unlock titles, rewards, and recognition.

## Core Architecture

### Achievement Categories

#### Combat Achievements
```csharp
public static class AchievementNames
{
    // PvE Achievements
    public const string Dragon_Kills = "Dragon_Kills";
    public const string Epic_Boss_Kills = "Epic_Boss_Kills"; 
    public const string Legion_Kills = "Legion_Kills";
    public const string Demon_Kills = "Demon_Kills";
    
    // RvR Achievements
    public const string Players_Killed = "Players_Killed";
    public const string Alb_Players_Killed = "Alb_Players_Killed";
    public const string Mid_Players_Killed = "Mid_Players_Killed";
    public const string Hib_Players_Killed = "Hib_Players_Killed";
    
    // Deathblow Achievements
    public const string Alb_Deathblows = "Alb_Deathblows";
    public const string Mid_Deathblows = "Mid_Deathblows";
    public const string Hib_Deathblows = "Hib_Deathblows";
    
    // Solo Kill Achievements
    public const string Solo_Kills = "Solo_Kills";
    public const string Alb_Solo_Kills = "Alb_Solo_Kills";
    public const string Mid_Solo_Kills = "Mid_Solo_Kills";
    public const string Hib_Solo_Kills = "Hib_Solo_Kills";
    
    // Realm Warfare
    public const string Realm_Rank = "Realm_Rank";
    public const string Relic_Captures = "Relic_Captures";
    public const string Keeps_Taken = "Keeps_Taken";
}
```

#### Economic Achievements
```csharp
// Crafting and economy
public const string Mastered_Crafts = "Mastered_Crafts";
public const string Orbs_Earned = "Orbs_Earned";
```

### Achievement Database Structure

#### Achievement Storage
```csharp
[DataTable(TableName = "Achievement")]
public class DbAchievement : DataObject
{
    [DataElement(AllowDbNull = false)]
    public string AccountId { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public string AchievementName { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public int Realm { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public int Count { get; set; }
}
```

#### Achievement Tracking
```csharp
public void Achieve(string achievementName, int count = 1)
{
    DbAchievement achievement = DOLDB<DbAchievement>.SelectObject(
        DB.Column("AccountID").IsEqualTo(Client.Account.ObjectId)
        .And(DB.Column("Realm").IsEqualTo((int)Realm))
        .And(DB.Column("AchievementName").IsEqualTo(achievementName)));
    
    if (achievement == null)
    {
        achievement = new DbAchievement();
        achievement.AccountId = Client.Account.ObjectId;
        achievement.AchievementName = achievementName;
        achievement.Realm = (int)Realm;
        achievement.Count = count;
        GameServer.Database.AddObject(achievement);
        return;
    }
    
    achievement.Count += count;
    GameServer.Database.SaveObject(achievement);
}
```

## Achievement Categories

### Player vs Player (RvR) Achievements

#### Kill Tracking
- **Total Player Kills**: Cross-realm kill count
- **Realm-Specific Kills**: Individual realm kill counts
- **Deathblow Tracking**: Final killing blow achievements
- **Solo Kill Recognition**: 1v1 combat achievements

#### Realm Warfare
- **Relic Captures**: Successful relic takings
- **Keep Captures**: Fortress conquests
- **Realm Rank Milestones**: RR advancement achievements

### Player vs Environment (PvE) Achievements

#### Epic Encounters
```csharp
// Dragon encounter achievements
if (killer.KillsDragon >= 10)
    killer.Achieve(AchievementNames.Dragon_Kills, 1);

// Epic boss achievements
if (target.Name.Contains("Epic"))
    killer.Achieve(AchievementNames.Epic_Boss_Kills, 1);
```

#### Specialized Kills
- **Dragon Slayer**: Dragon encounter kills
- **Demon Bane**: Demonic creature kills
- **Legion Vanquisher**: Legion encounter kills
- **Epic Victor**: Epic boss encounter kills

### Social and Progression Achievements

#### Time-Based Milestones
- **Veteran Status**: Character age milestones
- **Elder Recognition**: Account age achievements
- **Loyalty Rewards**: Long-term play recognition

#### Economic Achievements
- **Master Craftsman**: Crafting skill mastery
- **Orb Collector**: Special currency accumulation

## Achievement Verification System

### Kill Credit Validation
```csharp
public static bool CheckPlayerCredit(string mobName, GamePlayer player, int realm)
{
    // Validate kill requirements
    var killRequirement = KillCreditUtils.GetRequiredKillMob(mobName);
    
    if (killRequirement != null)
    {
        return AchievementUtils.CheckPlayerCredit(killRequirement, player, realm);
    }
    
    return false;
}
```

### Credit Requirements
```csharp
// Example kill requirements for special items
public static string GetRequiredKillMob(string itemId)
{
    var killRequirements = new Dictionary<string, string>
    {
        {"dragonscale_shield", "Cuuldurach"},
        {"shadow_armor", "Legion"},
        {"epic_weapon", "Epic_Boss_Name"}
    };
    
    return killRequirements.TryGetValue(itemId, out string mob) ? mob : null;
}
```

## Achievement-Linked Rewards

### Title Unlocks
```csharp
// Achievement-based title eligibility
public class DragonSlayerTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    protected override Func<GamePlayer, bool> SuitableMethod 
    { 
        get { return player => player.KillsDragon >= 50; }
    }
}
```

### Item Access
```csharp
// Achievement-gated merchant access
public override void OnPlayerBuy(GamePlayer player, int itemSlot, int number)
{
    var mobRequirement = KillCreditUtils.GetRequiredKillMob(template.Id_nb);
    
    if (mobRequirement != null && player.Client.Account.PrivLevel == 1)
    {
        var hasCredit = AchievementUtils.CheckPlayerCredit(mobRequirement, player, (int)player.Realm);
        
        if (!hasCredit)
        {
            player.Out.SendMessage("You have not earned the right to purchase this item!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
    }
    
    // Process purchase
}
```

### Special Recognition
- **Public Announcements**: Server-wide recognition
- **Unique Titles**: Exclusive achievement titles
- **Cosmetic Rewards**: Special appearances/effects
- **Economic Benefits**: Special merchant access

## Achievement Display System

### Achievement Lookup
```csharp
public static IList<string> GetAchievementInfoForPlayer(GamePlayer player)
{
    List<string> achievements = new List<string>();
    
    var playerAchievements = DOLDB<DbAchievement>.SelectObjects(
        DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId));
    
    if (playerAchievements == null) return achievements;
    
    foreach (var achievement in playerAchievements)
    {
        achievements.Add($"{achievement.AchievementName}: {achievement.Count}");
    }
    
    return achievements;
}
```

### Progress Tracking
- **Real-time Updates**: Immediate achievement progress
- **Historical Records**: Permanent achievement history
- **Cross-Character**: Account-level achievement sharing
- **Realm-Specific**: Separate achievement tracking per realm

## Integration with Other Systems

### Title System Integration
```csharp
// Automatic title granting based on achievements
private void CheckAchievementTitles(GamePlayer player)
{
    // Check various achievement thresholds
    if (player.KillsAlbionPlayers + player.KillsMidgardPlayers + player.KillsHiberniaPlayers >= 2000)
        player.AddTitle(new MasterSoldierTitle());
        
    if (player.KillsDragon >= 100)
        player.AddTitle(new DragonSlayerTitle());
}
```

### Combat System Integration
```csharp
// Achievement tracking on combat events
protected override void OnLivingDied(GameLiving living, GameObject killer)
{
    if (killer is GamePlayer player && living is GameNPC npc)
    {
        // Track NPC-specific achievements
        if (npc.Name.Contains("Dragon"))
            player.Achieve(AchievementNames.Dragon_Kills, 1);
            
        if (npc.Name.Contains("Legion"))
            player.Achieve(AchievementNames.Legion_Kills, 1);
    }
    
    base.OnLivingDied(living, killer);
}
```

### Merchant System Integration
- **Achievement-Gated Items**: Special purchases requiring achievements
- **Alternative Currencies**: Achievement-based currency systems
- **Exclusive Access**: Special merchant interactions

## Special Event Achievements

### Seasonal Events
```csharp
// Special event achievement tracking
public static void TrackEventAchievement(GamePlayer player, string eventName)
{
    string achievementName = $"Event_{eventName}_{DateTime.Now.Year}";
    player.Achieve(achievementName, 1);
}
```

### Limited-Time Achievements
- **Holiday Events**: Seasonal achievement opportunities
- **Server Events**: Special server-wide achievements
- **Community Goals**: Collaborative achievement targets

## Performance Considerations

### Database Optimization
```csharp
// Batch achievement updates for performance
private static void BatchUpdateAchievements(List<DbAchievement> achievements)
{
    foreach (var achievement in achievements)
    {
        GameServer.Database.SaveObject(achievement);
    }
}
```

### Caching Strategy
- **Player Session**: Cache achievements during gameplay
- **Periodic Sync**: Regular database synchronization
- **Lazy Loading**: Load achievements on demand

## Configuration Options

### Server Properties
```ini
# Achievement system configuration
ENABLE_ACHIEVEMENTS = true
ACHIEVEMENT_ANNOUNCE_THRESHOLD = 1000    # Announce major achievements
ACHIEVEMENT_DB_SYNC_INTERVAL = 300       # 5 minute sync interval
ACHIEVEMENT_CROSS_REALM = false          # Separate realm achievements
```

### Achievement Thresholds
```csharp
// Configurable achievement milestones
public static class AchievementThresholds
{
    public const int DRAGON_SLAYER = 50;
    public const int DRAGON_BANE = 100;
    public const int MASTER_SOLDIER = 2000;
    public const int MASTER_ENFORCER = 25000;
    public const int MASTER_ASSASSIN = 100000;
}
```

## Test Scenarios

### Achievement Tracking
```csharp
// Test achievement increment
[Test]
public void TestAchievementIncrement()
{
    // Given: Player with no dragon kills
    var player = CreateTestPlayer();
    
    // When: Player kills dragon
    player.Achieve(AchievementNames.Dragon_Kills, 1);
    
    // Then: Achievement count should be 1
    var achievement = GetPlayerAchievement(player, AchievementNames.Dragon_Kills);
    Assert.AreEqual(1, achievement.Count);
}
```

### Title Integration
```csharp
// Test achievement-based title granting
[Test]
public void TestAchievementTitle()
{
    // Given: Player with achievement threshold
    var player = CreateTestPlayer();
    player.KillsDragon = 50;
    
    // When: Title check is performed
    CheckAchievementTitles(player);
    
    // Then: Dragon Slayer title should be granted
    Assert.IsTrue(player.HasTitle(typeof(DragonSlayerTitle)));
}
```

### Merchant Integration
```csharp
// Test achievement-gated purchases
[Test]
public void TestAchievementGatedPurchase()
{
    // Given: Player without required achievement
    var player = CreateTestPlayer();
    
    // When: Player attempts to buy achievement-gated item
    var canBuy = ValidateAchievementRequirement(player, "dragon_artifact");
    
    // Then: Purchase should be denied
    Assert.IsFalse(canBuy);
}
```

## Migration and Compatibility

### Achievement Migration
```csharp
// Migrate old kill tracking to achievement system
public static void MigratePlayerKills(GamePlayer player)
{
    // Migrate existing kill counts
    if (player.KillsAlbionPlayers > 0)
        player.Achieve(AchievementNames.Alb_Players_Killed, player.KillsAlbionPlayers);
        
    if (player.KillsMidgardPlayers > 0)
        player.Achieve(AchievementNames.Mid_Players_Killed, player.KillsMidgardPlayers);
        
    if (player.KillsHiberniaPlayers > 0)
        player.Achieve(AchievementNames.Hib_Players_Killed, player.KillsHiberniaPlayers);
}
```

### Data Integrity
- **Duplicate Prevention**: Unique constraints on account/realm/achievement
- **Data Validation**: Achievement count validation
- **Audit Trail**: Achievement modification logging

## Future Expansions

### Potential Achievement Categories
- **Exploration**: Zone discovery achievements
- **Social**: Group participation achievements
- **Seasonal**: Holiday-specific achievements
- **Competitive**: Ranking-based achievements

### Advanced Features
- **Achievement Chains**: Sequential achievement requirements
- **Hidden Achievements**: Secret achievement discovery
- **Achievement Points**: Weighted achievement scoring
- **Leaderboards**: Achievement-based rankings

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | Title system integration |
| Historical | Game | Merchant integration |
| Original | Game | Basic player tracking | 