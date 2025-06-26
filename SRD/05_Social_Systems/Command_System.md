# Command System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from ScriptMgr.cs, AbstractCommandHandler.cs, CmdAttribute.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
The Command System provides a flexible framework for handling slash commands with privilege level enforcement, spam protection, and automatic command discovery. It supports player commands, GM commands, and admin commands with comprehensive help and syntax validation.

## Core Architecture

### Command Attributes
Commands are registered using attributes that define their properties:

```csharp
[CmdAttribute(
    "&player",           // Command trigger
    ePrivLevel.GM,       // Minimum privilege level
    "Various Admin/GM commands to edit characters.",  // Description
    "/player name <newName>",     // Usage examples
    "/player level <newLevel>",
    "/player realm <newRealm>"
)]
public class PlayerCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Command implementation
    }
}
```

### Command Attribute Variants
```csharp
// Basic command
[CmdAttribute("&sit", ePrivLevel.Player, "Sit", "/sit")]

// Command with aliases
[CmdAttribute("&guild", new string[] {"&gu"}, ePrivLevel.Player, "Guild Chat", "/gu <text>")]

// Command with header (for complex command groups)
[CmdAttribute("&plvl", "AdminCommands.Header.Syntax.Plvl", ePrivLevel.Admin, 
    "AdminCommands.Plvl.Description", 
    "AdminCommands.Plvl.Syntax.Comm",
    "AdminCommands.Plvl.Usage.Comm")]
```

### Privilege Levels
```csharp
public enum ePrivLevel : uint
{
    Player = 1,  // Normal players
    GM = 2,      // Game Masters
    Admin = 3    // Administrators
}
```

### Command Handler Interface
```csharp
public interface ICommandHandler
{
    void OnCommand(GameClient client, string[] args);
}

public abstract class AbstractCommandHandler
{
    // Spam protection
    public static bool IsSpammingCommand(GamePlayer player, string commandName);
    public static bool IsSpammingCommand(GamePlayer player, string commandName, int delay);
    
    // Message display helpers
    public virtual void DisplayMessage(GameClient client, string message);
    public virtual void DisplaySyntax(GameClient client);
}
```

## Command Discovery and Registration

### Automatic Registration
Commands are automatically discovered and registered during server startup:

```csharp
public static bool LoadCommands()
{
    foreach (Assembly assembly in ScriptMgr.Scripts)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract)
                continue;
                
            foreach (CmdAttribute attrib in type.GetCustomAttributes<CmdAttribute>(false))
            {
                var cmd = new GameCommand();
                cmd.Usage = attrib.Usage;
                cmd.m_cmd = attrib.Cmd;
                cmd.m_lvl = attrib.Level;
                cmd.m_desc = attrib.Description;
                cmd.m_cmdHandler = (ICommandHandler)Activator.CreateInstance(type);
                m_gameCommands.Add(attrib.Cmd, cmd);
                
                // Register aliases
                if (attrib.Aliases != null)
                {
                    foreach (string alias in attrib.Aliases)
                    {
                        m_gameCommands.Add(alias, cmd);
                    }
                }
            }
        }
    }
}
```

### Command Guessing
Supports partial command matching:

```csharp
public static GameCommand GuessCommand(string cmd)
{
    // Exact match first
    if (m_gameCommands.TryGetValue(cmd, out GameCommand command))
        return command;
        
    // Partial match
    string cmdToLower = cmd.ToLower();
    foreach (var kvp in m_gameCommands)
    {
        if (kvp.Key.ToLower().StartsWith(cmdToLower))
            return kvp.Value;
    }
    
    return null;
}
```

## Command Processing

### Command Parsing
```csharp
public static string[] ParseCmdLine(string cmdLine)
{
    var args = new ArrayList();
    bool inQuotes = false;
    var arg = new StringBuilder();
    
    for (int i = 0; i < cmdLine.Length; i++)
    {
        char c = cmdLine[i];
        
        if (c == '"')
        {
            inQuotes = !inQuotes;
        }
        else if (c == ' ' && !inQuotes)
        {
            if (arg.Length > 0)
            {
                args.Add(arg.ToString());
                arg.Clear();
            }
        }
        else
        {
            arg.Append(c);
        }
    }
    
    if (arg.Length > 0)
        args.Add(arg.ToString());
        
    return (string[])args.ToArray(typeof(string));
}
```

### Permission Checking
```csharp
public static bool HandleCommand(GameClient client, string cmdLine)
{
    string[] pars = ParseCmdLine(cmdLine);
    GameCommand myCommand = GuessCommand(pars[0]);
    
    if (myCommand == null) 
        return false;
        
    // Check privilege level
    if (client.Account.PrivLevel < myCommand.m_lvl)
    {
        // Check single permission system
        if (!SinglePermission.HasPermission(client.Player, pars[0].Substring(1)))
        {
            client.Out.SendMessage("No such command (" + pars[0] + ")", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }
    }
    
    ExecuteCommand(client, myCommand, pars);
    return true;
}
```

### Command Execution
```csharp
private static void ExecuteCommand(GameClient client, GameCommand myCommand, string[] pars)
{
    // Log command usage
    if (client.Account == null || 
        ((ServerProperties.Properties.LOG_ALL_GM_COMMANDS && client.Account.PrivLevel > 1) || 
         myCommand.m_lvl > 1))
    {
        string commandText = String.Join(" ", pars);
        string targetName = client.Player?.TargetObject?.Name ?? "(no target)";
        string playerName = client.Player?.Name ?? "(player is null)";
        string accountName = client.Account?.Name ?? "account is null";
        
        AuditMgr.LogGMCommand(commandText, playerName, targetName, accountName);
    }
    
    // Execute the command
    myCommand.m_cmdHandler.OnCommand(client, pars);
}
```

## Spam Protection

### Command Spam Detection
```csharp
public static bool IsSpammingCommand(GamePlayer player, string commandName, int delay = 500)
{
    // GMs and Admins bypass spam protection
    if ((ePrivLevel)player.Client.Account.PrivLevel > ePrivLevel.Player)
        return false;
        
    string spamKey = commandName + "NOSPAM";
    long tick = player.TempProperties.GetProperty<long>(spamKey);
    
    if (tick > 0 && player.CurrentRegion.Time - tick <= 0)
    {
        player.TempProperties.RemoveProperty(spamKey);
    }
    
    long changeTime = player.CurrentRegion.Time - tick;
    
    if (tick > 0 && changeTime < delay)
    {
        return true; // Still in spam delay
    }
    
    player.TempProperties.SetProperty(spamKey, player.CurrentRegion.Time);
    return false;
}
```

### Default Spam Delays
```ini
# Server Properties
COMMAND_SPAM_DELAY = 500  # Default 500ms between commands
```

## Command Categories

### Player Commands
Available to all players (ePrivLevel.Player):

```csharp
// Basic actions
[CmdAttribute("&sit", ePrivLevel.Player, "Sit", "/sit")]
[CmdAttribute("&stand", ePrivLevel.Player, "Stand up", "/stand")]
[CmdAttribute("&quit", ePrivLevel.Player, "Quit the game", "/quit")]

// Communication
[CmdAttribute("&say", ePrivLevel.Player, "Say something", "/say <message>")]
[CmdAttribute("&guild", new string[] {"&gu"}, ePrivLevel.Player, "Guild chat", "/gu <text>")]
[CmdAttribute("&group", new string[] {"&g"}, ePrivLevel.Player, "Group chat", "/g <text>")]

// Information
[CmdAttribute("&who", ePrivLevel.Player, "List online players", "/who [filter]")]
[CmdAttribute("&time", ePrivLevel.Player, "Show game time", "/time")]
[CmdAttribute("&stats", ePrivLevel.Player, "Show statistics", "/stats")]

// Social
[CmdAttribute("&anonymous", new string[] {"&anon"}, ePrivLevel.Player, "Toggle anonymous mode", "/anon")]
[CmdAttribute("&afk", ePrivLevel.Player, "Set AFK message", "/afk [message]")]
[CmdAttribute("&friend", ePrivLevel.Player, "Manage friends", "/friend <add|remove|list> [name]")]

// Housing
[CmdAttribute("&house", ePrivLevel.Player, "House commands", "/house")]
```

### GM Commands
Available to Game Masters (ePrivLevel.GM):

```csharp
// Player management
[CmdAttribute("&player", ePrivLevel.GM, "Edit players", "/player <property> <value>")]
[CmdAttribute("&mute", ePrivLevel.GM, "Mute players", "/mute <player> [duration]")]
[CmdAttribute("&kick", ePrivLevel.GM, "Kick players", "/kick <player>")]

// Object creation
[CmdAttribute("&create", ePrivLevel.GM, "Create objects", "/create <type> [params]")]
[CmdAttribute("&mob", ePrivLevel.GM, "Create NPCs", "/mob create <mobID>")]
[CmdAttribute("&item", ePrivLevel.GM, "Create items", "/item create <itemID>")]

// World manipulation
[CmdAttribute("&gmstealth", ePrivLevel.GM, "GM stealth", "/gmstealth <on|off>")]
[CmdAttribute("&jump", ePrivLevel.GM, "Teleport", "/jump <player|location>")]
[CmdAttribute("&summon", ePrivLevel.GM, "Summon player", "/summon <player>")]

// Information
[CmdAttribute("&gmappeal", new string[] {"&gmhelp"}, ePrivLevel.GM, "Manage appeals", "/gmappeal <command>")]
[CmdAttribute("&weather", ePrivLevel.GM, "Control weather", "/weather <start|stop|info>")]
```

### Admin Commands
Available to Administrators (ePrivLevel.Admin):

```csharp
// Server management
[CmdAttribute("&shutdown", ePrivLevel.Admin, "Shutdown server", "/shutdown [time] [message]")]
[CmdAttribute("&broadcast", ePrivLevel.Admin, "Server broadcast", "/broadcast <message>")]
[CmdAttribute("&serverproperties", ePrivLevel.Admin, "Manage properties", "/serverproperties <command>")]

// Account management
[CmdAttribute("&account", ePrivLevel.Admin, "Manage accounts", "/account <command> [params]")]
[CmdAttribute("&plvl", ePrivLevel.Admin, "Set privilege levels", "/plvl <level> <player>")]

// Database
[CmdAttribute("&save", ePrivLevel.Admin, "Save database", "/save")]
[CmdAttribute("&refresh", ePrivLevel.Admin, "Refresh cache", "/refresh <module>")]

// Development
[CmdAttribute("&code", ePrivLevel.Admin, "Execute code", "/code <expression>")]
[CmdAttribute("&benchmark", ePrivLevel.Admin, "Run benchmarks", "/benchmark <test>")]
```

## Single Permission System

### Individual Command Access
Allows granting specific commands to players regardless of privilege level:

```csharp
public class SinglePermission
{
    // Check if player has specific command permission
    public static bool HasPermission(GamePlayer player, string command)
    {
        var permission = DOLDB<DbSinglePermission>.SelectObject(
            DB.Column("PlayerID").IsEqualTo(player.InternalID)
              .And(DB.Column("Command").IsEqualTo(command))
        );
        return permission != null;
    }
    
    // Grant command permission to player
    public static void addPermission(GamePlayer player, string command)
    {
        var permission = new DbSinglePermission();
        permission.PlayerID = player.InternalID;
        permission.Command = command;
        GameServer.Database.AddObject(permission);
    }
    
    // Remove command permission
    public static void removePermission(GamePlayer player, string command)
    {
        var permission = DOLDB<DbSinglePermission>.SelectObject(
            DB.Column("PlayerID").IsEqualTo(player.InternalID)
              .And(DB.Column("Command").IsEqualTo(command))
        );
        if (permission != null)
            GameServer.Database.DeleteObject(permission);
    }
}
```

### Account-Wide Permissions
Permissions can be granted to entire accounts:

```csharp
public static void addPermissionAccount(GamePlayer player, string command)
{
    var permission = new DbSinglePermission();
    permission.PlayerID = player.Client.Account.ObjectId;
    permission.Command = command;
    permission.Type = 1; // Account-wide flag
    GameServer.Database.AddObject(permission);
}
```

## Help System

### Command Help
```csharp
[CmdAttribute("&cmdhelp", ePrivLevel.Player, "Show command help", "/cmdhelp [command]")]
public class CmdHelpCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        ePrivLevel privilegeLevel = (ePrivLevel)client.Account.PrivLevel;
        
        if (args.Length == 1)
        {
            ShowUseableCommands(client);
            return;
        }
        
        string commandArg = args[1];
        if (commandArg[0] != '/')
            commandArg = $"/{commandArg}";
            
        GameCommand gameCommand = ScriptMgr.GetCommand($"&{commandArg[1..]}");
        
        if (gameCommand == null || (ePrivLevel)gameCommand.m_lvl > privilegeLevel)
        {
            DisplayMessage(client, $"No such command ({commandArg})");
            return;
        }
        
        DisplayMessage(client, $"Usage for {commandArg}:");
        foreach (string usage in gameCommand.Usage)
            DisplayMessage(client, usage);
    }
}
```

### Syntax Display
```csharp
public virtual void DisplaySyntax(GameClient client)
{
    if (client == null || !client.IsPlaying)
        return;
        
    // Get command info for this handler
    var cmdAttrib = GetType().GetCustomAttribute<CmdAttribute>();
    if (cmdAttrib != null)
    {
        DisplayMessage(client, $"Syntax for {cmdAttrib.Cmd}:");
        foreach (string usage in cmdAttrib.Usage)
            DisplayMessage(client, usage);
    }
}
```

## Localization Support

### Translated Commands
Commands support localized descriptions and usage text:

```csharp
[CmdAttribute("&advice", new[] {"&adv"}, ePrivLevel.Player,
    "PLCommands.Advice.Description",     // Translation key
    "PLCommands.Advice.Syntax.AdvChannel",
    "PLCommands.Advice.Syntax.Advice")]
public class AdviceCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Use LanguageMgr for localized messages
        string message = LanguageMgr.GetTranslation(client.Account.Language, 
            "PLCommands.Advice.Msg.Welcome");
        DisplayMessage(client, message);
    }
}
```

### Language Files
Command text stored in language files:

```ini
# EN/Commands/PlayerCommands.txt
PLCommands.Advice.Description = Lists all flagged Advisors, sends questions, and messages advice channel.
PLCommands.Advice.Syntax.AdvChannel = /adv <message> - Sends message to advice channel
PLCommands.Advice.Msg.Welcome = Welcome to the advice system!
```

## Error Handling

### Command Not Found
```csharp
if (!ScriptMgr.HandleCommand(client, cmdLine))
{
    if (cmdLine[0] == '&')
        cmdLine = "/" + cmdLine.Remove(0, 1);
    client.Out.SendMessage($"No such command ({cmdLine})", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
}
```

### Permission Denied
```csharp
if (client.Account.PrivLevel < myCommand.m_lvl)
{
    if (!SinglePermission.HasPermission(client.Player, commandName))
    {
        client.Out.SendMessage("No such command (" + pars[0] + ")", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return true;
    }
}
```

### Exception Handling
```csharp
try
{
    ExecuteCommand(client, myCommand, pars);
}
catch (Exception e)
{
    if (log.IsErrorEnabled)
        log.Error("HandleCommand", e);
        
    DisplayMessage(client, "An error occurred while executing the command.");
}
```

## Command Logging

### Audit Trail
All GM and Admin commands are logged:

```csharp
if (client.Account == null || 
    ((ServerProperties.Properties.LOG_ALL_GM_COMMANDS && client.Account.PrivLevel > 1) || 
     myCommand.m_lvl > 1))
{
    string commandText = String.Join(" ", pars);
    string targetName = client.Player?.TargetObject?.Name ?? "(no target)";
    string playerName = client.Player?.Name ?? "(player is null)";
    string accountName = client.Account?.Name ?? "account is null";
    
    log.Info($"GM/Admin Command: {commandText} by {playerName}({accountName}) target: {targetName}");
    AuditMgr.LogGMCommand(commandText, playerName, targetName, accountName);
}
```

### Database Logging
```csharp
public class AuditMgr
{
    public static void LogGMCommand(string command, string playerName, string targetName, string accountName)
    {
        var auditEntry = new DbAuditEntry();
        auditEntry.AuditType = "GMCommand";
        auditEntry.PlayerName = playerName;
        auditEntry.AccountName = accountName;
        auditEntry.TargetName = targetName;
        auditEntry.Command = command;
        auditEntry.TimeStamp = DateTime.Now;
        
        GameServer.Database.AddObject(auditEntry);
    }
}
```

## Configuration

### Server Properties
```ini
# Command system settings
COMMAND_SPAM_DELAY = 500              # Default command spam delay (ms)
LOG_ALL_GM_COMMANDS = true            # Log all GM/Admin commands
DISABLE_APPEALSYSTEM = false          # Enable/disable appeal system

# Chat settings
ANON_MODIFIER = 1                     # Anonymous mode: -1=disabled, 0=default off, 1=default on
ADVICE_SLOWMODE_LENGTH = 30           # Advice channel slowmode (seconds)
```

## Performance Considerations

### Command Caching
Commands are cached after initial loading for fast lookup:

```csharp
private static readonly Dictionary<string, GameCommand> m_gameCommands = new Dictionary<string, GameCommand>();
```

### Memory Management
- Command handlers are instantiated once and reused
- Temporary properties are used for spam tracking
- Weak references prevent memory leaks

### Threading
- Commands execute on the game thread
- Thread-safe collections used for command storage
- Lock-free operations where possible

## Test Scenarios

### Basic Command Execution
```csharp
// Given: Player with appropriate privilege level
// When: Valid command is entered
// Then: Command executes successfully
```

### Permission Enforcement
```csharp
// Given: Player without sufficient privilege level
// When: Restricted command is entered
// Then: "No such command" message is displayed
```

### Spam Protection
```csharp
// Given: Player enters command rapidly
// When: Commands are sent faster than spam delay
// Then: Subsequent commands are ignored
```

### Single Permission System
```csharp
// Given: Player granted specific command permission
// When: Command is entered despite insufficient privilege level
// Then: Command executes successfully
```

## Integration Points

### Chat System
Commands are processed through the chat packet handler

### Permission System
Integrates with account privilege levels and single permissions

### Audit System
All privileged commands are logged for security

### Event System
Commands can trigger events for cross-system integration

### Localization System
Command text and messages support multiple languages

## Future Enhancements
- TODO: Command aliases in database for custom shortcuts
- TODO: Command macros for complex operation sequences
- TODO: Command scheduling for delayed execution
- TODO: Context-sensitive help based on current location/state
- TODO: Command auto-completion suggestions

## Change Log
- 2024-01-20: Initial documentation created

## References
- `GameServer/gameutils/ScriptMgr.cs`
- `GameServer/commands/AbstractCommandHandler.cs`
- `GameServer/commands/CmdAttribute.cs`
- `GameServer/packets/Client/168/PlayerCommandHandler.cs`
- `GameServer/commands/playercommands/` - Player command implementations
- `GameServer/commands/gmcommands/` - GM command implementations
- `GameServer/commands/admincommands/` - Admin command implementations
- `GameServer/language/EN/Commands/` - Command localization files 