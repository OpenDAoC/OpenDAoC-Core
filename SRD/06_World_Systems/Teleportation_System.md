# Teleportation System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from teleportation implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Teleportation is the magical art of traveling instantly across great distances. You can teleport using spells, magical items, NPC teleporters, and portal networks scattered throughout the world. Each method has different restrictions - some don't work in combat, others require you to be in the same group as your target, and many are blocked in enemy territory. Teleportation is essential for getting around DAoC's vast world quickly, but the restrictions prevent it from being abused in combat situations.

The Teleportation System provides various methods of magical transportation including spells, items, NPCs, and portal networks. This system enables rapid movement across the game world while maintaining balance and restrictions.

## Core Architecture

### Teleportation Types

**Game Rule Summary**: There are many different ways to teleport, each with their own rules and limitations. Spells are the most common but don't work in combat. Items can be used more freely but have cooldowns. NPCs provide fixed routes between important locations. Portals offer instant travel but only work when your realm controls the connected keeps. Understanding which type to use in different situations is key to mastering movement in DAoC.
```csharp
public enum TeleportationType
{
    Spell,          // Magic spells (Recall, Gate, Summon)
    Item,           // Magical items (rings, potions)
    NPC,            // NPC teleporters
    Portal,         // Static portals
    BindStone,      // Bind/recall mechanics
    Guild,          // Guild recall/summon
    GroupSummon,    // Group member summoning
    KeepTeleport    // Keep portal networks
}

public interface ITeleportation
{
    bool CanTeleport(GamePlayer player, Point3D destination);
    bool ExecuteTeleport(GamePlayer player, Point3D destination);
    TeleportationType Type { get; }
    int Range { get; }
    bool RequiresLineOfSight { get; }
    bool RestrictedInCombat { get; }
}
```

## Spell-Based Teleportation

### Recall Spells

**Game Rule Summary**: Recall spells teleport you back to your bind point - the location where you last bound your soul at a bind stone. You can't recall while in combat or from certain restricted areas like dungeons or enemy keeps. This spell is your emergency escape route and primary way to return home quickly when adventuring far from civilization.
```csharp
public class RecallSpell : Spell
{
    public override bool StartSpell(GameLiving target)
    {
        if (!(target is GamePlayer player))
            return false;
            
        if (!CanRecall(player))
            return false;
            
        var bindLocation = GetBindLocation(player);
        return TeleportPlayer(player, bindLocation);
    }
    
    private bool CanRecall(GamePlayer player)
    {
        // Cannot recall in combat
        if (player.InCombat)
        {
            MessageToCaster("You cannot recall while in combat!", eChatType.CT_SpellResisted);
            return false;
        }
        
        // Cannot recall in certain zones
        if (player.CurrentRegion.IsDisableRecall)
        {
            MessageToCaster("You cannot recall from this location!", eChatType.CT_SpellResisted);
            return false;
        }
        
        return true;
    }
}
```

### Gate Travel

**Game Rule Summary**: Gate travel teleports you to another group member's location, letting you quickly reunite with scattered allies. Both you and your target must be in the same group, within range of each other, and not in combat. This spell is perfect for rescuing lost group members or gathering everyone at a rally point before a dangerous encounter.
```csharp
public class GateSpell : Spell
{
    public override bool StartSpell(GameLiving target)
    {
        if (!(Caster is GamePlayer caster))
            return false;
            
        if (!(target is GamePlayer targetPlayer))
            return false;
            
        // Must be in same group
        if (!caster.Group?.IsInTheGroup(targetPlayer) == true)
        {
            MessageToCaster("Target must be in your group!", eChatType.CT_SpellResisted);
            return false;
        }
        
        // Range check
        if (!caster.IsWithinRadius(targetPlayer, Range))
        {
            MessageToCaster("Target is too far away!", eChatType.CT_SpellResisted);
            return false;
        }
        
        return TeleportPlayer(caster, targetPlayer.Position);
    }
}
```

### Summon Spells

**Game Rule Summary**: Summon spells bring a group member to your location, which is the opposite of gate travel. The target can't be in combat and must be in a zone that allows summoning. This is ideal when one person has found something interesting and wants to bring the whole group quickly, or when rescuing someone who is stuck or lost.
```csharp
public class SummonSpell : Spell
{
    public override bool StartSpell(GameLiving target)
    {
        if (!(Caster is GamePlayer caster))
            return false;
            
        if (!(target is GamePlayer targetPlayer))
            return false;
            
        // Group member check
        if (!AreInSameGroup(caster, targetPlayer))
            return false;
            
        // Target restrictions
        if (!CanBeSummoned(targetPlayer))
            return false;
            
        return TeleportPlayer(targetPlayer, GetSummonLocation(caster));
    }
    
    private bool CanBeSummoned(GamePlayer player)
    {
        // Cannot summon from combat
        if (player.InCombat)
            return false;
            
        // Cannot summon from certain zones
        if (player.CurrentRegion.IsDisableSummon)
            return false;
            
        return true;
    }
}
```

## Portal Networks

### Keep Portal System

**Game Rule Summary**: Portal networks connect your realm's keeps and major cities, providing rapid transportation across the frontiers. You can only use portals between areas your realm controls, and the connections follow logical geographical paths. This system is crucial for RvR warfare, letting you quickly respond to attacks on distant keeps or redeploy forces where needed.
```csharp
public class KeepPortalNetwork
{
    private static readonly Dictionary<string, List<string>> _portalConnections = new()
    {
        ["Camelot Hills"] = new[] { "Salisbury Plains", "Black Mountains" }.ToList(),
        ["West Sauvage"] = new[] { "Camelot Hills", "Forest Sauvage" }.ToList(),
        ["Uppland"] = new[] { "Mularn", "Yggdra Forest" }.ToList(),
        ["Connacht"] = new[] { "Lough Derg", "Silvermine Mountains" }.ToList()
    };
    
    public static bool CanUsePortal(GamePlayer player, string fromZone, string toZone)
    {
        // Must control origin keep
        if (!IsKeepControlled(fromZone, player.Realm))
            return false;
            
        // Must have valid connection
        if (!_portalConnections.ContainsKey(fromZone))
            return false;
            
        return _portalConnections[fromZone].Contains(toZone);
    }
}
```

### Border Keep Teleporters

**Game Rule Summary**: Border keep teleporters are NPCs that provide fixed travel routes between major strategic locations. You can only use teleporters controlled by your realm, and they typically connect frontier zones to safer homeland areas. These are reliable travel options that don't require spells or items, but they only work when your realm holds the territory.
```csharp
public class BorderKeepTeleporter : GameNPC
{
    public override bool Interact(GamePlayer player)
    {
        if (!CanUseTeleporter(player))
        {
            SayTo(player, "You cannot use this teleporter!");
            return false;
        }
        
        ShowTeleportDestinations(player);
        return true;
    }
    
    private bool CanUseTeleporter(GamePlayer player)
    {
        // Must be correct realm
        if (player.Realm != this.Realm)
            return false;
            
        // Keep must be owned by player's realm
        var keep = GetNearestKeep();
        return keep?.Realm == player.Realm;
    }
}
```

## Bind Stone System

### Binding Mechanics

**Game Rule Summary**: Bind stones let you set your recall destination by binding your soul to that location. You can only bind at stones controlled by your realm and not while in combat. Choose your bind location carefully - it's where you'll appear when you die or use recall spells. Most players bind in their realm's main city, but strategic binding near adventure areas can save lots of travel time.
```csharp
public class BindStoneSystem
{
    public static bool BindPlayer(GamePlayer player, GameBindStone bindstone)
    {
        if (!CanBind(player, bindstone))
            return false;
            
        // Update bind location
        player.BindRegion = bindstone.CurrentRegion;
        player.BindXpos = bindstone.X;
        player.BindYpos = bindstone.Y;  
        player.BindZpos = bindstone.Z;
        player.BindHeading = bindstone.Heading;
        
        player.SendMessage("You are now bound to this location!", eChatType.CT_System);
        player.SaveIntoDatabase();
        
        return true;
    }
    
    private static bool CanBind(GamePlayer player, GameBindStone bindstone)
    {
        // Must be correct realm
        if (bindstone.Realm != player.Realm)
            return false;
            
        // Cannot bind in combat
        if (player.InCombat)
            return false;
            
        return true;
    }
}
```

## Anti-Exploit Systems

### Teleportation Security

**Game Rule Summary**: The teleportation system has many built-in protections to prevent cheating and exploitation. You can't teleport into solid walls, underwater, or into enemy territory where you don't belong. All teleportation is logged for security purposes. These restrictions ensure teleportation enhances gameplay without breaking immersion or providing unfair advantages.
```csharp
public class TeleportationSecurity
{
    public static bool ValidateDestination(GamePlayer player, Point3D destination)
    {
        // Cannot teleport into solid objects
        if (IsLocationBlocked(destination))
            return false;
            
        // Cannot teleport into enemy keeps
        if (IsEnemyTerritory(destination, player.Realm))
            return false;
            
        // Cannot teleport underwater
        if (IsUnderwater(destination))
            return false;
            
        return true;
    }
    
    public static void LogTeleportation(GamePlayer player, Point3D from, Point3D to, 
        TeleportationType type)
    {
        GameServer.Database.AddObject(new DbTeleportLog
        {
            PlayerName = player.Name,
            FromX = from.X,
            FromY = from.Y,
            ToX = to.X,
            ToY = to.Y,
            TeleportType = type.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Combat Restrictions

**Game Rule Summary**: Most forms of teleportation are blocked while you're in combat to prevent cheap escapes from fights. You can't use recall spells, teleport items, or NPC teleporters when enemies are attacking you. However, some portals may still work during combat since they represent permanent magical gateways. Plan your retreats carefully - once a fight starts, your teleportation options become very limited.
```csharp
public static bool CanTeleportInCombat(GamePlayer player, TeleportationType type)
{
    if (!player.InCombat)
        return true;
        
    return type switch
    {
        TeleportationType.Spell => false,      // No spell teleports in combat
        TeleportationType.Item => false,       // No item teleports in combat
        TeleportationType.NPC => false,        // No NPC teleports in combat
        TeleportationType.Portal => true,      // Portals may allow combat teleport
        TeleportationType.BindStone => false,  // Cannot bind in combat
        TeleportationType.Guild => false,      // No guild recalls in combat
        _ => false
    };
}
```

## Configuration

```csharp
[ServerProperty("teleport", "enable_teleportation", true)]
public static bool ENABLE_TELEPORTATION;

[ServerProperty("teleport", "max_teleport_range", 10000)]
public static int MAX_TELEPORT_RANGE;

[ServerProperty("teleport", "combat_teleport_restriction", true)]
public static bool COMBAT_TELEPORT_RESTRICTION;

[ServerProperty("teleport", "teleport_item_cooldown", 30000)]
public static int TELEPORT_ITEM_COOLDOWN;
```

## TODO: Missing Documentation

- Advanced portal network pathfinding algorithms
- Dynamic teleportation destination calculation
- Multi-player group teleportation coordination
- Cross-server teleportation mechanics

## References

- `GameServer/spells/Teleportation/` - Teleportation spell implementations
- `GameServer/gameobjects/GamePortal.cs` - Portal mechanics
- `GameServer/gameobjects/GameBindStone.cs` - Bind stone system 