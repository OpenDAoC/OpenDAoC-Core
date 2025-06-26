# Server Administration System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Server Administration System provides comprehensive management tools for GameMasters and Administrators to maintain server operations, manage players, create content, and monitor server health. It includes command frameworks, player management, object manipulation, and audit logging capabilities.

## Core Architecture

### Command Privilege System

```csharp
public enum ePrivLevel
{
    Player = 1,     // Basic player commands
    GM = 2,         // GameMaster commands  
    Admin = 3       // Full administrator access
}

[CmdAttribute("&commandname", ePrivLevel.GM, "Description")]
public class CommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Command implementation
    }
}
```

### Command Execution Framework

```csharp
private static void ExecuteCommand(GameClient client, GameCommand myCommand, string[] pars)
{
    // Security validation
    if (client.Account.PrivLevel < myCommand.m_lvl)
    {
        client.Out.SendMessage("Insufficient privileges!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return;
    }
    
    // Command logging for GM/Admin actions
    if (client.Account.PrivLevel > 1 || myCommand.m_lvl > 1)
    {
        string commandText = String.Join(" ", pars);
        string targetName = client.Player?.TargetObject?.Name ?? "(no target)";
        string playerName = client.Player?.Name ?? "(unknown)";
        string accountName = client.Account?.Name ?? "(unknown)";
        
        AuditMgr.LogGMCommand(commandText, playerName, targetName, accountName);
    }
    
    // Execute command
    myCommand.m_cmdHandler.OnCommand(client, pars);
}
```

## Player Management Commands

### Character Modification System

```csharp
[Cmd("&player", ePrivLevel.GM, "Edit character properties")]
public class PlayerCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "name":
                ChangePlayerName(client, args[2]);
                break;
            case "level":
                SetPlayerLevel(client, int.Parse(args[2]));
                break;
            case "realm":
                ChangePlayerRealm(client, (eRealm)int.Parse(args[2]));
                break;
            case "stat":
                ModifyPlayerStat(client, args[2], int.Parse(args[3]));
                break;
            case "money":
                ModifyPlayerMoney(client, args[2], long.Parse(args[3]));
                break;
            case "respec":
                RespecPlayer(client, args[2]);
                break;
        }
    }
}
```

### Player State Management

```csharp
// Player resurrection
case "rez":
    if (args.Length > 2)
        ResurrectRealm(ParseRealm(args[2]));
    else
        ResurrectTarget(client);
    break;

// Player termination  
case "kill":
    if (args.Length > 2)
        KillRealm(ParseRealm(args[2]));
    else
        KillTarget(client);
    break;

// Group operations
case "jump":
    if (args[2] == "group")
        JumpGroupToLocation(client, args[3]);
    break;
```

## Object Creation and Management

### Item Administration

```csharp
[Cmd("&item", ePrivLevel.GM, "Item management commands")]
public class ItemCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateItemFromTemplate(client, args[2], GetCount(args));
                break;
            case "model":
                ChangeItemModel(client, ushort.Parse(args[2]), GetSlot(args));
                break;
            case "save":
                SaveItemTemplate(client, args[2]);
                break;
            case "load":
                LoadItemFromDatabase(client, args[2]);
                break;
        }
    }
}
```

### NPC Creation System

```csharp
[Cmd("&create", ePrivLevel.GM, "Create game objects")]
public class CreateCommandHandler : AbstractCommandHandler, ICommandHandler
{
    private void CreateNPC(GameClient client, string npcClassType)
    {
        var npcType = ScriptMgr.GetType(npcClassType);
        var npc = Activator.CreateInstance(npcType) as GameNPC;
        
        // Position near GM
        npc.X = client.Player.X;
        npc.Y = client.Player.Y;
        npc.Z = client.Player.Z;
        npc.Heading = client.Player.Heading;
        npc.CurrentRegionID = client.Player.CurrentRegionID;
        
        npc.AddToWorld();
        npc.SaveIntoDatabase();
        
        client.Out.SendMessage($"Created NPC: {npc.Name} (OID: {npc.ObjectID})", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

## Keep and Territory Administration

### Keep Component Management

```csharp
[Cmd("&keepcomponent", ePrivLevel.GM, "Manage keep components")]
public class KeepComponentCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateKeepComponent(client, args[2]);
                break;
            case "skin":
                ChangeComponentSkin(client, int.Parse(args[2]));
                break;
            case "delete":
                DeleteComponent(client);
                break;
        }
    }
}
```

### Keep Guard System

```csharp
[Cmd("&keepguard", ePrivLevel.GM, "Manage keep guards")]
public class KeepGuardCommandHandler : AbstractCommandHandler, ICommandHandler
{
    private void CreateKeepGuard(GameClient client, string guardType, bool isStatic)
    {
        var component = client.Player.TargetObject as GameKeepComponent;
        if (component == null) return;
        
        GameKeepGuard guard = guardType.ToLower() switch
        {
            "lord" => new GuardLord(),
            "fighter" => new GuardFighter(),
            "archer" => isStatic ? new GuardStaticArcher() : new GuardArcher(),
            "caster" => isStatic ? new GuardStaticCaster() : new GuardCaster(),
            _ => null
        };
        
        if (guard != null)
        {
            guard.Component = component;
            guard.AddToWorld();
            guard.SaveIntoDatabase();
            component.Keep.Guards.Add(guard.ObjectID.ToString(), guard);
        }
    }
}
```

## Housing Administration

### House Management

```csharp
[Cmd("&house", ePrivLevel.Player, "Housing management")]
public class HouseCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void HouseAdmin(GamePlayer player, string[] args)
    {
        if (player.Client.Account.PrivLevel != (int)ePrivLevel.Admin)
            return;
            
        switch (args[1].ToLower())
        {
            case "model":
                ChangeHouseModel(player, int.Parse(args[2]));
                break;
            case "remove":
                RemoveHouse(player);
                break;
            case "addhookpoints":
                ToggleHookpointAdding(player);
                break;
        }
    }
}
```

### Hookpoint Management

```csharp
private void LogHookpointLocation(GamePlayer player, uint hookpointId)
{
    var offset = new DbHouseHookPointOffset
    {
        HouseModel = player.CurrentHouse.Model,
        HookpointID = hookpointId,
        X = player.X - player.CurrentHouse.X,
        Y = player.Y - player.CurrentHouse.Y,
        Z = player.Z - 25000,
        Heading = player.Heading - player.CurrentHouse.Heading
    };
    
    GameServer.Database.AddObject(offset);
    House.AddNewOffset(offset);
}
```

## Player Support Tools

### Appeal System

```csharp
[Cmd("&gmappeal", ePrivLevel.GM, "Player support system")]
public class GMAppealCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "assist":
                TakeAppealOwnership(client, args[2]);
                break;
            case "close":
                CloseAppeal(client, args[2]);
                break;
            case "jumpto":
                JumpToAssistedPlayer(client);
                break;
            case "list":
                ListActiveAppeals(client);
                break;
        }
    }
}
```

### Voting System

```csharp
[Cmd("&gmvote", ePrivLevel.GM, "Server voting system")]
public class GMVoteCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                VotingMgr.CreateVoting(client.Player);
                break;
            case "start":
                VotingMgr.StartVoting(client.Player, args.Length > 2 ? args[2] : null);
                break;
            case "cancel":
                VotingMgr.CancelVoting(client.Player);
                break;
        }
    }
}
```

## Security and Audit System

### Command Logging

```csharp
public class AuditMgr
{
    public static void LogGMCommand(string command, string playerName, string targetName, string accountName)
    {
        var auditEntry = new DbAuditEntry
        {
            AuditType = "GMCommand",
            PlayerName = playerName,
            AccountName = accountName,
            TargetName = targetName,
            Command = command,
            TimeStamp = DateTime.Now
        };
        
        GameServer.Database.AddObject(auditEntry);
        
        // Alert on sensitive commands
        if (IsSensitiveCommand(command))
        {
            NotifyAdministrators($"ALERT: {command} executed by {playerName}");
        }
    }
}
```

### Spam Protection

```csharp
public static bool IsSpammingCommand(GamePlayer player, string commandName, int delay = 500)
{
    // GMs bypass spam protection
    if ((ePrivLevel)player.Client.Account.PrivLevel > ePrivLevel.Player)
        return false;
    
    string spamKey = commandName + "NOSPAM";
    long lastTime = player.TempProperties.GetProperty<long>(spamKey);
    long currentTime = player.CurrentRegion.Time;
    
    if (lastTime > 0 && currentTime - lastTime < delay)
        return true;
    
    player.TempProperties.SetProperty(spamKey, currentTime);
    return false;
}
```

## System Integration

### Database Integration

```csharp
// All modifications are persisted
target.SaveIntoDatabase();

// Audit trail maintained
AuditMgr.LogGMAction(description);

// Configuration changes saved
ServerProperties.Properties.Save();
```

### Event System Integration

```csharp
// Administrative events
GameEventMgr.Notify(AdminEvent.PlayerModified, args);
GameEventMgr.Notify(SecurityEvent.SensitiveCommand, args);
```

## Configuration

### Administrative Settings

```ini
# Command system
LOG_ALL_GM_COMMANDS = true
DISABLED_COMMANDS = "shutdown;restart"
COMMAND_SPAM_DELAY = 500

# Security
DISABLE_APPEALSYSTEM = false
AUDIT_SENSITIVE_COMMANDS = true
ADMIN_BROADCAST_LEVEL = 2
```

## Performance Considerations

**Command Processing**:
- Commands cached for O(1) lookup
- Handlers instantiated once and reused
- Security checks optimized for frequent execution

**Memory Management**:
- Temporary properties auto-cleaned
- Command history size-limited
- Weak references for event handlers

**Database Impact**:
- Audit logging batched for performance
- Object saves use prepared statements
- Cache warming for frequently accessed objects 