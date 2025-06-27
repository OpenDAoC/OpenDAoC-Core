# Administrative Tools System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: Administrative tools provide Game Masters and admins with powerful commands to help players, manage the game world, and create content. GMs can assist with stuck characters, restore lost items, investigate cheating, run events, and maintain fair gameplay through comprehensive management and support systems.

The Administrative Tools System provides comprehensive server management capabilities for GameMasters (GMs) and Administrators. It includes extensive command frameworks, player management tools, object manipulation systems, scripting utilities, and server monitoring capabilities that enable efficient server administration and player support.

## Core Architecture

### Command Privilege System

```csharp
public enum ePrivLevel
{
    Player = 1,     // Basic player commands only
    GM = 2,         // GameMaster commands (most admin functions)
    Admin = 3       // Administrator commands (full access)
}

// Command attribute system
[CmdAttribute("&commandname", ePrivLevel.GM, "Command description")]
public class CommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Command implementation
    }
}
```

### Command Framework Architecture

```csharp
public static class ScriptMgr
{
    private static readonly Dictionary<string, GameCommand> m_gameCommands = new();
    
    public static GameCommand GuessCommand(string commandName)
    {
        // Exact match first
        if (m_gameCommands.TryGetValue(commandName, out GameCommand cmd))
            return cmd;
        
        // Fuzzy matching for partial commands
        var commands = m_gameCommands.Where(kv => 
            kv.Value != null && 
            kv.Key.StartsWith(commandName, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value);
        
        return commands.Count() == 1 ? commands.First() : null;
    }
    
    // Command execution with logging
    private static void ExecuteCommand(GameClient client, GameCommand myCommand, string[] pars)
    {
        // Security check
        if (client.Account.PrivLevel < myCommand.m_lvl)
        {
            client.Out.SendMessage("You don't have permission to use this command!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        // Command logging for GM/Admin commands
        if (client.Account.PrivLevel > 1 || myCommand.m_lvl > 1)
        {
            string commandText = String.Join(" ", pars);
            string targetName = client.Player?.TargetObject?.Name ?? "(no target)";
            string playerName = client.Player?.Name ?? "(player is null)";
            string accountName = client.Account?.Name ?? "account is null";
            
            AuditMgr.LogGMCommand(commandText, playerName, targetName, accountName);
        }
        
        // Execute command
        myCommand.m_cmdHandler.OnCommand(client, pars);
    }
}
```

## Player Management Commands

### Character Modification System

```csharp
[Cmd("&player", ePrivLevel.GM, "Various Admin/GM commands to edit characters.")]
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
                
            case "levelup":
                LevelUpPlayer(client);
                break;
                
            case "reset":
                ResetAndReLevelPlayer(client);
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
                RespecPlayer(client, args[2], args.Length > 3 ? int.Parse(args[3]) : 1);
                break;
                
            case "model":
                ChangePlayerModel(client, args);
                break;
        }
    }
    
    private void ChangePlayerName(GameClient client, string newName)
    {
        var target = client.Player.TargetObject as GamePlayer;
        if (target == null)
        {
            client.Out.SendMessage("You must target a player.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        string oldName = target.Name;
        target.Name = newName;
        target.SaveIntoDatabase();
        
        // Broadcast name change
        target.Out.SendMessage($"Your name has been changed from {oldName} to {newName}", 
                              eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        
        // Log the action
        GameServer.Instance.LogGMAction($"Character rename: {oldName} -> {newName} by {client.Player.Name}");
    }
}
```

### Player State Management

```csharp
// Player resurrection system
case "rez":
    if (args.Length > 2)
    {
        var realm = ParseRealm(args[2]);
        ResurrectRealm(realm);
    }
    else
    {
        ResurrectTarget(client);
    }
    break;

// Player killing system
case "kill":
    if (args.Length > 2)
    {
        var realm = ParseRealm(args[2]);
        KillRealm(realm);
    }
    else
    {
        KillTarget(client);
    }
    break;

// Group operations
case "jump":
    if (args[2] == "group" || args[2] == "guild" || args[2] == "cg" || args[2] == "bg")
    {
        JumpGroupToLocation(client, args[2], args[3]);
    }
    break;
```

## Object Manipulation System

### Item Creation and Modification

```csharp
[Cmd("&item", ePrivLevel.GM, "Various Item commands!")]
public class ItemCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateItemFromTemplate(client, args[2], args.Length > 3 ? int.Parse(args[3]) : 1);
                break;
                
            case "blank":
                CreateBlankItem(client);
                break;
                
            case "model":
                ChangeItemModel(client, ushort.Parse(args[2]), GetSlot(args));
                break;
                
            case "name":
                ChangeItemName(client, args[2], GetSlot(args));
                break;
                
            case "bonuslevel":
                SetItemBonusLevel(client, int.Parse(args[2]), GetSlot(args));
                break;
                
            case "save":
                SaveItemTemplate(client, args[2], GetSlot(args));
                break;
                
            case "load":
                LoadItemFromDatabase(client, args[2]);
                break;
        }
    }
    
    private void CreateItemFromTemplate(GameClient client, string templateId, int count)
    {
        var template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateId);
        if (template == null)
        {
            client.Out.SendMessage($"Item template '{templateId}' not found!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        var item = GameInventoryItem.Create(template);
        item.Count = count;
        
        if (!client.Player.Inventory.AddItem(eInventorySlot.FirstBackpack, item))
        {
            client.Out.SendMessage("Your inventory is full!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        client.Out.SendMessage($"Created {count} x {template.Name}", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

### NPC and Object Creation

```csharp
[Cmd("&create", ePrivLevel.GM, "Create objects")]
public class CreateCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "npc":
                CreateNPC(client, args[2]);
                break;
                
            case "object":
                CreateStaticObject(client, args[2]);
                break;
                
            case "item":
                CreateItem(client, args[2]);
                break;
        }
    }
    
    private void CreateNPC(GameClient client, string npcClassType)
    {
        try
        {
            var npcType = ScriptMgr.GetType(npcClassType);
            if (npcType == null)
            {
                client.Out.SendMessage($"NPC class '{npcClassType}' not found!", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            var npc = Activator.CreateInstance(npcType) as GameNPC;
            if (npc == null)
            {
                client.Out.SendMessage("Failed to create NPC instance!", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            // Position NPC near GM
            npc.X = client.Player.X;
            npc.Y = client.Player.Y;
            npc.Z = client.Player.Z;
            npc.Heading = client.Player.Heading;
            npc.CurrentRegionID = client.Player.CurrentRegionID;
            npc.Realm = client.Player.Realm;
            
            npc.AddToWorld();
            npc.SaveIntoDatabase();
            
            client.Out.SendMessage($"Created NPC: {npc.Name} (OID: {npc.ObjectID})", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        catch (Exception ex)
        {
            client.Out.SendMessage($"Error creating NPC: {ex.Message}", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
```

## Keep and Territory Management

### Keep Component System

```csharp
[Cmd("&keepcomponent", ePrivLevel.GM, "Various keep component creation commands!")]
public class KeepComponentCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateKeepComponent(client, args[2], args.Length > 3 ? int.Parse(args[3]) : 0);
                break;
                
            case "skin":
                ChangeKeepComponentSkin(client, int.Parse(args[2]));
                break;
                
            case "delete":
                DeleteKeepComponent(client);
                break;
        }
    }
    
    private void CreateKeepComponent(GameClient client, string componentType, int keepId)
    {
        var component = new GameKeepComponent();
        component.X = client.Player.X;
        component.Y = client.Player.Y;
        component.Z = client.Player.Z;
        component.Heading = client.Player.Heading;
        component.CurrentRegionID = client.Player.CurrentRegionID;
        
        // Find nearest keep if keepId not specified
        if (keepId == 0)
        {
            var nearestKeep = KeepMgr.GetClosestKeep(client.Player);
            if (nearestKeep != null)
                keepId = nearestKeep.KeepID;
        }
        
        component.Keep = KeepMgr.GetKeepByID(keepId);
        component.ComponentType = Enum.Parse<eKeepComponentType>(componentType);
        
        component.AddToWorld();
        component.SaveIntoDatabase();
        
        client.Out.SendMessage("Keep component created successfully!", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

### Keep Guard Management

```csharp
[Cmd("&keepguard", ePrivLevel.GM, "Various keep guard commands!")]
public class KeepGuardCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateKeepGuard(client, args[2], args.Length > 3 && args[3] == "static");
                break;
                
            case "position":
                ManageGuardPosition(client, args[2], args.Length > 3 ? int.Parse(args[3]) : 0);
                break;
                
            case "path":
                ManageGuardPath(client, args[2]);
                break;
        }
    }
    
    private void CreateKeepGuard(GameClient client, string guardType, bool isStatic)
    {
        var component = client.Player.TargetObject as GameKeepComponent;
        if (component == null)
        {
            client.Out.SendMessage("You must target a keep component!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        GameKeepGuard guard;
        
        switch (guardType.ToLower())
        {
            case "lord":
                guard = new GuardLord();
                break;
            case "fighter":
                guard = new GuardFighter();
                break;
            case "archer":
                guard = isStatic ? new GuardStaticArcher() : new GuardArcher();
                break;
            case "caster":
                guard = isStatic ? new GuardStaticCaster() : new GuardCaster();
                break;
            default:
                client.Out.SendMessage($"Unknown guard type: {guardType}", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
        }
        
        guard.Component = component;
        guard.X = client.Player.X;
        guard.Y = client.Player.Y;
        guard.Z = client.Player.Z;
        guard.Heading = client.Player.Heading;
        guard.CurrentRegionID = client.Player.CurrentRegionID;
        guard.Realm = component.Keep.Realm;
        
        guard.AddToWorld();
        guard.SaveIntoDatabase();
        
        component.Keep.Guards.Add(guard.ObjectID.ToString(), guard);
        
        client.Out.SendMessage("Guard created successfully!", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

## Housing Administration

### House Management System

```csharp
[Cmd("&house", ePrivLevel.Player, "Show various housing information")]
public class HouseCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // Admin-only commands
        if (client.Account.PrivLevel > (int)ePrivLevel.Player)
        {
            if (args.Length > 1)
            {
                HouseAdmin(client.Player, args);
                return;
            }
        }
        
        // Regular house info
        House house = HouseMgr.GetHouseByPlayer(client.Player);
        if (house != null)
            house.SendHouseInfo(client.Player);
        else
            DisplayMessage(client, "You do not own a house.");
    }
    
    public void HouseAdmin(GamePlayer player, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "info":
                ShowHouseInfo(player);
                break;
                
            case "model":
                if (player.Client.Account.PrivLevel == (int)ePrivLevel.Admin)
                    ChangeHouseModel(player, int.Parse(args[2]));
                break;
                
            case "remove":
                if (player.Client.Account.PrivLevel == (int)ePrivLevel.Admin)
                    RemoveHouse(player, args.Length > 2 ? args[2] : "");
                break;
                
            case "restart":
                if (player.Client.Account.PrivLevel == (int)ePrivLevel.Admin)
                    RestartHousingManager(player);
                break;
                
            case "addhookpoints":
                if (player.Client.Account.PrivLevel == (int)ePrivLevel.Admin)
                    ToggleHookpointAdding(player);
                break;
        }
    }
    
    private void ChangeHouseModel(GamePlayer player, int newModel)
    {
        var houses = HouseMgr.GetHousesCloseToSpot(player.CurrentRegionID, player.X, player.Y, 700);
        if (houses.Count != 1)
        {
            DisplayMessage(player.Client, "You need to stand closer to a house!");
            return;
        }
        
        if (newModel < 1 || newModel > 12)
        {
            DisplayMessage(player.Client, "Valid house models are 1 - 12!");
            return;
        }
        
        var house = houses[0] as House;
        if (house.Model != newModel)
        {
            HouseMgr.RemoveHouseItems(house);
            house.Model = newModel;
            house.SaveIntoDatabase();
            house.SendUpdate();
            
            DisplayMessage(player.Client, $"House model changed to {newModel}!");
            GameServer.Instance.LogGMAction($"{player.Name} changed house #{house.HouseNumber} model to {newModel}");
        }
    }
}
```

### Hookpoint Management

```csharp
// Housing hookpoint debugging and management
private void ToggleHookpointAdding(GamePlayer player)
{
    bool currentState = player.TempProperties.GetProperty<bool>(HousingConstants.AllowAddHouseHookpoint);
    player.TempProperties.SetProperty(HousingConstants.AllowAddHouseHookpoint, !currentState);
    
    DisplayMessage(player.Client, $"Add hookpoints turned {(!currentState ? "on" : "off")}!");
}

// Hookpoint offset logging system
private void LogHookpointLocation(GamePlayer player, uint hookpointId)
{
    if (player.CurrentHouse == null)
        return;
    
    var hookpointOffset = new DbHouseHookPointOffset
    {
        HouseModel = player.CurrentHouse.Model,
        HookpointID = hookpointId,
        X = player.X - player.CurrentHouse.X,
        Y = player.Y - player.CurrentHouse.Y,
        Z = player.Z - 25000,
        Heading = player.Heading - player.CurrentHouse.Heading
    };
    
    if (GameServer.Database.AddObject(hookpointOffset) && House.AddNewOffset(hookpointOffset))
    {
        string action = $"HOUSING: {player.Name} logged new HouseHookpointOffset for model {hookpointOffset.HouseModel}, position {hookpointOffset.HookpointID}";
        GameServer.Instance.LogGMAction(action);
    }
}
```

## Server Management Tools

### Player Appeal System

```csharp
[Cmd("&gmappeal", new string[] {"&gmhelp"}, ePrivLevel.GM, "Commands for server staff to assist players with their Appeals.")]
public class GMAppealCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
        {
            AppealMgr.MessageToClient(client, "Appeal system is disabled.");
            return;
        }
        
        switch (args[1].ToLower())
        {
            case "view":
                ViewPlayerAppeal(client, args[2]);
                break;
                
            case "list":
                ListAppeals(client, false);
                break;
                
            case "listall":
                ListAppeals(client, true);
                break;
                
            case "assist":
                TakeAppealOwnership(client, args[2]);
                break;
                
            case "jumpto":
                JumpToAssistedPlayer(client);
                break;
                
            case "jumpback":
                JumpBackFromAssist(client);
                break;
                
            case "close":
                CloseAppeal(client, args[2]);
                break;
                
            case "release":
                ReleaseAppeal(client, args[2]);
                break;
                
            case "mute":
                ToggleAppealNotifications(client);
                break;
        }
    }
    
    private void TakeAppealOwnership(GameClient client, string playerName)
    {
        var appeal = AppealMgr.GetAppeal(playerName);
        if (appeal == null)
        {
            client.Out.SendMessage($"No appeal found for player {playerName}", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        if (appeal.AssistingGM != null)
        {
            client.Out.SendMessage("That player is already being helped.", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        appeal.AssistingGM = client.Player;
        AppealMgr.NotifyGMAssistance(appeal);
        
        // Send random greeting to player
        string[] greetings = {
            "Howdy {0}, thanks for waiting. How may I help you?",
            "Hiya {0}, what can I do for you today?",
            "Greetings {0}! I'm here to assist. How can I be of service?",
            "Hi {0}, I understand you need some help, what can I do for you today?"
        };
        
        string greeting = greetings[Util.Random(greetings.Length)];
        appeal.Player.Out.SendMessage(string.Format(greeting, appeal.Player.Name), 
                                     eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
    }
}
```

### Voting System

```csharp
[Cmd("&gmvote", ePrivLevel.GM, "Various voting commands!")]
public class GMVoteCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                VotingMgr.CreateVoting(client.Player);
                break;
                
            case "add":
                string choice = string.Join(" ", args, 2, args.Length - 2);
                VotingMgr.AddChoice(client.Player, choice);
                break;
                
            case "desc":
                string description = string.Join(" ", args, 2, args.Length - 2);
                VotingMgr.AddDescription(client.Player, description);
                break;
                
            case "start":
                if (args.Length > 2)
                    VotingMgr.StartVoting(client.Player, args[2]);
                else
                    VotingMgr.StartVoting(client.Player);
                break;
                
            case "cancel":
                VotingMgr.CancelVoting(client.Player);
                break;
                
            case "last":
                VotingMgr.ShowLastVotingResults(client.Player);
                break;
                
            case "list":
                if (args.Length > 2)
                    VotingMgr.ListVotings(client.Player, args[2]);
                else
                    VotingMgr.ListVotings(client.Player);
                break;
                
            case "info":
                if (args.Length > 2)
                    VotingMgr.ShowVotingInfo(client.Player, args[2]);
                else
                    VotingMgr.ShowVotingInfo(client.Player);
                break;
                
            case "remove":
                VotingMgr.ClearVoting(client.Player);
                break;
        }
    }
}
```

## Faction and AI Management

### Faction System Administration

```csharp
[Cmd("&faction", ePrivLevel.GM, "Create a faction and assign friend and enemy faction")]
public class FactionCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        switch (args[1].ToLower())
        {
            case "create":
                CreateFaction(client, args[2], int.Parse(args[3]));
                break;
                
            case "assign":
                AssignFactionToNPC(client);
                break;
                
            case "addfriend":
                AddFactionFriend(client, int.Parse(args[2]));
                break;
                
            case "addenemy":
                AddFactionEnemy(client, int.Parse(args[2]));
                break;
                
            case "relations":
                ShowFactionRelations(client);
                break;
                
            case "list":
                ListFactions(client);
                break;
                
            case "select":
                SelectFaction(client, int.Parse(args[2]));
                break;
        }
    }
    
    private void CreateFaction(GameClient client, string name, int baseAggro)
    {
        var faction = new DbFaction
        {
            Name = name,
            BaseAggroLevel = baseAggro
        };
        
        GameServer.Database.AddObject(faction);
        FactionMgr.LoadFactions(); // Reload faction cache
        
        client.Out.SendMessage($"Created new faction: {name} with base aggro {baseAggro}", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
    
    private void AssignFactionToNPC(GameClient client)
    {
        var target = client.Player.TargetObject as GameNPC;
        if (target == null)
        {
            client.Out.SendMessage("You must target an NPC!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        var selectedFaction = client.Player.TempProperties.GetProperty<DbFaction>("SelectedFaction");
        if (selectedFaction == null)
        {
            client.Out.SendMessage("You must select a faction first using /faction select <id>", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        target.Faction = selectedFaction;
        target.SaveIntoDatabase();
        
        client.Out.SendMessage($"NPC {target.Name} has joined the faction {selectedFaction.Name}", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

## Security and Monitoring

### Command Logging and Auditing

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
            TimeStamp = DateTime.Now,
            ServerInstance = GameServer.Instance.Configuration.ServerName
        };
        
        GameServer.Database.AddObject(auditEntry);
        
        // Real-time logging
        log.Info($"GM Command: {playerName}({accountName}) -> {targetName}: /{command}");
        
        // Alert on sensitive commands
        if (IsSensitiveCommand(command))
        {
            NotifyAdministrators($"ALERT: Sensitive command executed: {command} by {playerName}");
        }
    }
    
    private static bool IsSensitiveCommand(string command)
    {
        string[] sensitiveCommands = { "ban", "kick", "kill", "delete", "reset", "account" };
        return sensitiveCommands.Any(cmd => command.StartsWith(cmd, StringComparison.OrdinalIgnoreCase));
    }
    
    private static void NotifyAdministrators(string message)
    {
        var admins = ClientService.GetAllClients()
            .Where(c => c.Account.PrivLevel >= (int)ePrivLevel.Admin)
            .ToList();
        
        foreach (var admin in admins)
        {
            admin.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
        }
    }
}
```

### Spam Protection System

```csharp
public static class SpamProtection
{
    public static bool IsSpammingCommand(GamePlayer player, string commandName, int delay = 500)
    {
        // GMs and Admins bypass spam protection
        if ((ePrivLevel)player.Client.Account.PrivLevel > ePrivLevel.Player)
            return false;
        
        string spamKey = commandName + "NOSPAM";
        long lastCommandTime = player.TempProperties.GetProperty<long>(spamKey);
        long currentTime = player.CurrentRegion.Time;
        
        if (lastCommandTime > 0 && currentTime - lastCommandTime < delay)
        {
            player.Out.SendMessage($"You must wait {delay}ms between {commandName} commands!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }
        
        player.TempProperties.SetProperty(spamKey, currentTime);
        return false;
    }
}
```

## Configuration and Setup

### Administrative Settings

```ini
# Administrative system configuration
LOG_ALL_GM_COMMANDS = true           # Log all GM/Admin commands
DISABLED_COMMANDS = "shutdown;ban"   # Semicolon-separated list of disabled commands
COMMAND_SPAM_DELAY = 500            # Default command spam protection (ms)
DISABLE_APPEALSYSTEM = false        # Enable/disable player appeal system
ADMIN_BROADCAST_LEVEL = 2           # Minimum level for admin broadcasts

# GM stealth and invisibility
GM_STEALTH_ENABLED = true           # Allow GM stealth mode
GM_INVULNERABILITY = true           # GMs immune to damage/effects

# Audit and security
AUDIT_SENSITIVE_COMMANDS = true     # Extra logging for sensitive commands
SECURITY_ALERT_THRESHOLD = 3        # Failed command attempts before alert
```

### Command Categories

**Player Commands (ePrivLevel.Player = 1)**:
- Basic communication: `/say`, `/guild`, `/group`
- Character actions: `/sit`, `/stand`, `/quit`
- Information: `/who`, `/time`, `/stats`
- Social features: `/friend`, `/anonymous`, `/afk`

**GM Commands (ePrivLevel.GM = 2)**:
- Player management: `/player`, `/summon`, `/kick`, `/mute`
- Object creation: `/create`, `/mob`, `/item`
- World manipulation: `/jump`, `/gmstealth`
- Diagnostic tools: `/diag`, `/viewreports`

**Admin Commands (ePrivLevel.Admin = 3)**:
- Server control: `/shutdown`, `/restart`
- Account management: `/account`, `/ban`
- Database operations: `/reload`, `/migrate`
- System configuration: `/property`, `/debug`

## Performance Considerations

### Command Processing Optimization

**Command Caching**:
- Commands cached after initial loading for O(1) lookup
- Fuzzy matching disabled for high-frequency commands
- Command handlers instantiated once and reused

**Security Checks**:
- Privilege level validation before execution
- Rate limiting for non-privileged users
- Audit logging only for GM+ level commands

**Memory Management**:
- Temporary properties cleaned up automatically
- Command history limited to prevent memory leaks
- Weak references for event subscriptions

## Integration Points

### Database Integration

```csharp
// Character modifications are immediately persisted
target.SaveIntoDatabase();

// Audit trail for all administrative actions
AuditMgr.LogGMAction(description);

// Configuration changes trigger immediate saves
ServerProperties.Properties.Save();
```

### Event System Integration

```csharp
// Administrative actions trigger events
GameEventMgr.Notify(AdminEvent.PlayerModified, new PlayerModifiedEventArgs(target, modification));

// Security events for monitoring
GameEventMgr.Notify(SecurityEvent.SensitiveCommandExecuted, new CommandEventArgs(command, executor));
```

### Network Integration

```csharp
// Special GM/Admin packet handling
if (client.Account.PrivLevel >= (int)ePrivLevel.GM)
{
    // Enhanced packet processing for administrative tools
    ProcessAdminPacket(client, packet);
}
``` 