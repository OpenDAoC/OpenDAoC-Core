# Server Rules and Configuration System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from ServerProperties.cs, PvEServerRules.cs, PvPServerRules.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Server rules and configuration determine the core gameplay experience - from experience rates and PvP rules to death penalties and cross-realm interaction. Administrators can customize these settings to create different server types like pure PvE, hardcore PvP, or casual-friendly servers that match their community's preferences.

The Server Rules and Configuration System provides flexible server behavior control through property-based configuration and rule-based server types. It enables administrators to customize gameplay mechanics, damage rates, progression speed, and server-specific features.

## Core Architecture

### Server Types

#### EGameServerType Enumeration
```csharp
public enum EGameServerType
{
    GST_Normal = 0,    // Standard RvR server
    GST_PvP = 1,       // Player vs Player focused
    GST_PvE = 2,       // Player vs Environment focused
    GST_Roleplay = 3,  // Roleplay server
    GST_Casual = 4,    // Casual gameplay
    GST_Test = 5       // Test server
}
```

#### Server Rules Architecture
```csharp
[ServerRules(EGameServerType.GST_PvE)]
public class PvEServerRules : AbstractServerRules
{
    public override string RulesDescription() => "standard PvE server rules";
    
    // Override specific behavior for PvE servers
    public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet);
    public override byte GetColorHandling(GameClient client);
    public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet);
}
```

### ServerProperties System

#### Property Definition Structure
```csharp
[ServerProperty("category", "property_name", "Description", defaultValue)]
public static TYPE PROPERTY_NAME;
```

#### Property Categories
1. **server**: Core server functionality
2. **rates**: Experience, damage, and progression rates
3. **pvp**: Player vs Player mechanics
4. **pve**: Player vs Environment mechanics
5. **guild**: Guild system configuration
6. **housing**: Housing system settings
7. **npc**: NPC behavior and AI
8. **atlas**: Custom server features

## PvE Server Rules

### Combat Restrictions
```csharp
public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
{
    // Players cannot attack other players
    if (attacker.Realm != eRealm.None && defender.Realm != eRealm.None)
    {
        // Exception: Duel partners can attack each other
        if (attacker is GamePlayer && ((GamePlayer)attacker).IsDuelPartner(defender))
            return true;
            
        if (!quiet) 
            MessageToLiving(attacker, "You can not attack other players on this server!");
        return false;
    }
    
    // Same realm attacks only allowed if attacker is confused
    if (attacker.Realm == defender.Realm)
    {
        if (attacker is GameNPC && (attacker as GameNPC).IsConfused)
            return true;
        return false;
    }
    
    return base.IsAllowedToAttack(attacker, defender, quiet);
}
```

### Social System Rules
```csharp
// PvE servers allow cross-realm interaction
public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet) => true;
public override bool IsAllowedToJoinGuild(GamePlayer source, Guild guild) => true;
public override bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet) => true;
public override bool IsAllowedToUnderstand(GameLiving source, GamePlayer target) => true;
public override bool IsAllowedCharsInAllRealms(GameClient client) => true;
```

### Color Handling
```csharp
// PvE color scheme: All players friendly, realm 0 NPCs enemy
public override byte GetColorHandling(GameClient client) => 3;
```

**Color Handling Values**:
- **0**: Standard - other realm PC red, own realm NPC green
- **1**: PvP - all PC red, NPC by level color
- **2**: Realm-based - same realm friendly, other realm enemy
- **3**: PvE - all PC friendly, realm 0 NPC enemy
- **4**: All NPC enemy, all players friendly

## PvP Server Rules

### Death Mechanics
```csharp
protected const string KILLED_BY_PLAYER_PROP = "PvP killed by player";

public override void OnLivingKilled(GameLiving killedLiving, GameObject killer)
{
    if (killedLiving is GamePlayer killedPlayer && killer is GamePlayer)
    {
        // Set immunity timer for PvP death
        killedPlayer.TempProperties.SetProperty(KILLED_BY_PLAYER_PROP, true);
        
        // Constitution loss on PvP death (configurable)
        if (Properties.PVP_DEATH_CON_LOSS)
        {
            int conPenalty = 3;
            killedPlayer.TempProperties.SetProperty(DEATH_CONSTITUTION_LOSS_PROPERTY, conPenalty);
        }
    }
}
```

### Keep Control System
```csharp
public override void ResetKeep(GuardLord lord, GameObject killer)
{
    eRealm realm = eRealm.None;
    
    // Keep changes to group leader's realm
    if (killer is GamePlayer player)
    {
        Group group = player.Group;
        realm = group?.Leader.Realm ?? player.Realm;
    }
    else if (killer is GameNPC npc && npc.Brain is IControlledBrain controlled)
    {
        GamePlayer owner = controlled.GetPlayerOwner();
        Group group = owner?.Group;
        realm = group?.Leader.Realm ?? killer.Realm;
    }
    
    lord.Component.Keep.Reset(realm);
}
```

### Safety System
```csharp
protected int m_safetyLevel = 10;  // Safety flag ineffective above this level

public override void ImmunityExpiredCallback(GamePlayer player)
{
    if (player.Level < m_safetyLevel && player.SafetyFlag)
        player.Out.SendMessage("Your temporary invulnerability timer has expired, but your /safety flag is still on.");
    else
        player.Out.SendMessage("Your temporary invulnerability timer has expired.");
}
```

## Configuration Categories

### Rate Modifiers
```csharp
[ServerProperty("rates", "xp_rate", "Experience Points Rate Modifier", 1.0)]
public static double XP_RATE;

[ServerProperty("rates", "rp_rate", "Realm Points Rate Modifier", 1.0)]
public static double RP_RATE;

[ServerProperty("rates", "bp_rate", "Bounty Points Rate Modifier", 1.0)]
public static double BP_RATE;

[ServerProperty("rates", "cl_xp_rate", "Champion Level XP Rate Modifier", 1.0)]
public static double CL_XP_RATE;

[ServerProperty("rates", "rvr_zones_xp_rate", "RvR zones XP Rate Modifier", 1.0)]
public static double RvR_XP_RATE;
```

### Damage Modifiers
```csharp
[ServerProperty("rates", "pve_melee_damage", "PvE Melee Damage Modifier", 1.0)]
public static double PVE_MELEE_DAMAGE;

[ServerProperty("rates", "pve_spell_damage", "PvE Spell Damage Modifier", 1.0)]
public static double PVE_SPELL_DAMAGE;

[ServerProperty("rates", "pvp_melee_damage", "PvP Melee Damage Modifier", 1.0)]
public static double PVP_MELEE_DAMAGE;

[ServerProperty("rates", "pvp_spell_damage", "PvP Spell Damage Modifier", 1.0)]
public static double PVP_SPELL_DAMAGE;
```

### Defense Caps
```csharp
[ServerProperty("rates", "block_cap", "Block Rate Cap Modifier", 1.00)]
public static double BLOCK_CAP;

[ServerProperty("rates", "evade_cap", "Evade Rate Cap Modifier", 0.50)]
public static double EVADE_CAP;

[ServerProperty("rates", "parry_cap", "Parry Rate Cap Modifier", 0.50)]
public static double PARRY_CAP;
```

### PvP Immunity Timers
```csharp
[ServerProperty("pvp", "Timer_Killed_By_Mob", "Immunity timer when killed by mob", 30)]
public static int TIMER_KILLED_BY_MOB;

[ServerProperty("pvp", "Timer_Killed_By_Player", "Immunity timer when killed by player", 120)]
public static int TIMER_KILLED_BY_PLAYER;

[ServerProperty("pvp", "Timer_Region_Changed", "Immunity timer when changing regions", 10)]
public static int TIMER_REGION_CHANGED;

[ServerProperty("pvp", "Timer_Game_Entered", "Immunity timer when entering game", 10)]
public static int TIMER_GAME_ENTERED;

[ServerProperty("pvp", "Timer_PvP_Teleport", "Immunity timer when teleporting in region", 30)]
public static int TIMER_PVP_TELEPORT;
```

### Experience and Progression
```csharp
[ServerProperty("rates", "XP_Cap_Percent", "Maximum XP percent of level", 125)]
public static int XP_CAP_PERCENT;

[ServerProperty("rates", "XP_PVP_Cap_Percent", "Maximum PvP XP percent of level", 125)]
public static int XP_PVP_CAP_PERCENT;

[ServerProperty("pve", "pve_exp_loss_level", "Level to start losing XP when killed", 6)]
public static byte PVE_EXP_LOSS_LEVEL;

[ServerProperty("pve", "pve_con_loss_level", "Level to start losing CON when killed", 6)]
public static byte PVE_CON_LOSS_LEVEL;
```

### Server Features
```csharp
[ServerProperty("server", "enable_pve_speed", "Enable 25% speed boost outside combat/RvR", true)]
public static bool ENABLE_PVE_SPEED;

[ServerProperty("server", "enable_encumberance_speed_loss", "Enable encumbrance speed loss", true)]
public static bool ENABLE_ENCUMBERANCE_SPEED_LOSS;

[ServerProperty("server", "free_respec", "Always allow respecs", false)]
public static bool FREE_RESPEC;

[ServerProperty("server", "autoselect_caster", "Beneficial spells target caster if no valid target", false)]
public static bool AUTOSELECT_CASTER;
```

### PvE Mob Configuration
```csharp
[ServerProperty("atlas", "pve_mob_damage_f1", "PvE mob damage factor 1", 3.2)]
public static double PVE_MOB_DAMAGE_F1;

[ServerProperty("atlas", "pve_mob_damage_f2", "PvE mob damage factor 2", 150.0)]
public static double PVE_MOB_DAMAGE_F2;
```

### Battleground Settings
```csharp
[ServerProperty("pvp", "allow_bps_in_bgs", "Allow bounty points in battlegrounds", false)]
public static bool ALLOW_BPS_IN_BGS;

[ServerProperty("pvp", "bg_zones_open", "Can players teleport to battlegrounds", true)]
public static bool BG_ZONES_OPENED;

[ServerProperty("pvp", "bg_zones_closed_message", "Message when BG zones closed", "The battlegrounds are not open on this server.")]
public static string BG_ZONES_CLOSED_MESSAGE;
```

### Guild System
```csharp
[ServerProperty("guild", "guild_buff_xp", "Guild PvE XP buff percentage", 5)]
public static ushort GUILD_BUFF_XP;

[ServerProperty("guild", "guild_buff_rp", "Guild RP buff percentage", 2)]
public static ushort GUILD_BUFF_RP;

[ServerProperty("guild", "guild_buff_bp", "Guild BP buff percentage", 0)]
public static ushort GUILD_BUFF_BP;

[ServerProperty("guild", "guild_buff_crafting", "Guild crafting speed buff percentage", 5)]
public static ushort GUILD_BUFF_CRAFTING;

[ServerProperty("guild", "guilds_claim_limit", "Guild claim limit", 1)]
public static int GUILDS_CLAIM_LIMIT;
```

### NPC Behavior
```csharp
[ServerProperty("npc", "allow_roam", "Allow NPCs to roam", true)]
public static bool ALLOW_ROAM;

[ServerProperty("npc", "gamenpc_roam_cooldown_min", "Minimum roam cooldown seconds", 5)]
public static int GAMENPC_ROAM_COOLDOWN_MIN;

[ServerProperty("npc", "gamenpc_roam_cooldown_max", "Maximum roam cooldown seconds", 40)]
public static int GAMENPC_ROAM_COOLDOWN_MAX;

[ServerProperty("npc", "gamenpc_chances_to_style", "NPC style usage chance", 20)]
public static int GAMENPC_CHANCES_TO_STYLE;

[ServerProperty("npc", "gamenpc_chances_to_cast", "NPC spell casting chance", 25)]
public static int GAMENPC_CHANCES_TO_CAST;
```

## Property Loading and Management

### Property Loading Process
1. **Default Values**: Loaded from ServerProperty attributes
2. **Database Override**: Properties table overrides defaults
3. **Runtime Modification**: Properties can be changed via admin commands
4. **Validation**: Type checking and range validation

### Property Storage
```sql
ServerProperty table:
- Property: String key (category.property_name)
- Value: String value (parsed to appropriate type)
- DefaultValue: Original default value
- Description: Human-readable description
```

### Runtime Access
```csharp
// Direct property access
double xpRate = ServerProperties.Properties.XP_RATE;

// Dynamic property access
string value = ServerProperties.Properties.GetProperty("rates.xp_rate");
```

## Admin Commands

### Property Management
```
/serverproperty <property> [value]     # View or set property
/serverproperty list [category]        # List properties
/serverproperty save                   # Save properties to database
/serverproperty reload                 # Reload from database
```

### Rule Testing
```
/serverrules                           # Display current server rules
/colorhandling                         # Test color handling scheme
```

## System Interactions

### With Combat System
- **Damage Modifiers**: PVP/PVE damage multipliers applied
- **Defense Caps**: Block/parry/evade caps enforced
- **Critical Chances**: Configurable critical hit rates

### With Progression System
- **XP Rates**: Configurable experience gain rates
- **RP/BP Rates**: Realm/bounty point gain modifiers
- **Level Caps**: Experience caps by level percentage

### With Death System
- **Immunity Timers**: Prevent immediate re-engagement
- **Constitution Loss**: Configurable death penalties
- **Experience Loss**: Level-based death penalties

### With Social Systems
- **Cross-Realm**: Server type determines interaction rules
- **Guild Benefits**: Configurable guild buff bonuses
- **Trading**: Server type controls trade permissions

## Configuration Examples

### High-Rate Leveling Server
```ini
rates.xp_rate = 5.0
rates.rp_rate = 3.0
rates.bp_rate = 2.0
server.free_respec = true
rates.XP_Cap_Percent = 200
```

### Hardcore PvP Server
```ini
pvp.Timer_Killed_By_Player = 300
pvp.pvp_death_con_loss = true
rates.pvp_melee_damage = 1.5
rates.pvp_spell_damage = 1.5
server.free_respec = false
```

### PvE Cooperative Server
```ini
pve.pve_melee_damage = 0.8
pve.pve_spell_damage = 0.8
guild.guild_buff_xp = 25
server.enable_pve_speed = true
npc.allow_roam = true
```

### Casual Friendly Server
```ini
rates.xp_rate = 2.0
server.free_respec = true
server.enable_pve_speed = true
pve.pve_exp_loss_level = 20
pve.pve_con_loss_level = 20
```

## Performance Considerations

### Property Caching
- **Static Access**: Properties cached as static variables
- **Type Conversion**: Parsed once at startup
- **Runtime Changes**: Immediate effect without restart

### Memory Usage
- **Property Count**: ~500+ configurable properties
- **Storage**: Minimal memory footprint
- **Database**: Efficient storage and retrieval

## Testing Scenarios

### Server Type Tests
1. **PvE Rules**: Verify player vs player attacks blocked
2. **PvP Rules**: Confirm immunity timers work
3. **Color Handling**: Test client display colors
4. **Cross-Realm**: Validate interaction permissions

### Configuration Tests
1. **Rate Modifiers**: XP/RP/BP gains match settings
2. **Damage Modifiers**: PvP/PvE damage scaling
3. **Property Persistence**: Database storage/retrieval
4. **Default Values**: Fallbacks when not configured

### Edge Case Tests
1. **Invalid Values**: Graceful handling of bad data
2. **Type Conversion**: String to numeric conversion
3. **Missing Properties**: Default value usage
4. **Runtime Changes**: Dynamic property updates

## References
- **Core System**: `GameServer/serverproperty/ServerProperties.cs`
- **PvE Rules**: `GameServer/serverrules/PvEServerRules.cs`
- **PvP Rules**: `GameServer/serverrules/PvPServerRules.cs`
- **Abstract Base**: `GameServer/serverrules/AbstractServerRules.cs`
- **Configuration**: `GameServer/config/GameServerConfiguration.cs` 