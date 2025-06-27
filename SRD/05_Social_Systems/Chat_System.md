# Chat System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The chat system gives you many different ways to communicate with other players depending on the situation. Local chat lets you talk to nearby players, while private messages reach specific people anywhere in your realm. Guild and group channels keep your team coordinated, and public channels like advice let you ask questions to everyone. Different channels have different ranges and restrictions, so you can choose the right way to communicate for each situation.

The chat system provides multiple communication channels for players to interact. Channels range from local proximity chat to realm-wide broadcasts, with various permission and range restrictions.

## Core Mechanics

### Chat Types

**Game Rule Summary**: The game has many different chat channels, each designed for specific purposes. System messages give you game information, local say chat reaches nearby players, private messages go to specific people, and group/guild channels keep your teams organized. Each channel has different rules about who can hear you and how far your message travels.

#### Basic Chat Types
```csharp
public enum eChatType : byte
{
    CT_System = 0x00,         // System messages
    CT_Say = 0x01,           // Local area chat
    CT_Send = 0x02,          // Private messages
    CT_Group = 0x03,         // Group chat
    CT_Guild = 0x04,         // Guild chat
    CT_Broadcast = 0x05,     // Zone-wide broadcast
    CT_Emote = 0x06,         // Emote actions
    CT_Help = 0x07,          // Help channel
    CT_Chat = 0x08,          // Chat groups
    CT_Advise = 0x09,        // Advice channel
    CT_Officer = 0x0a,       // Guild officer chat
    CT_Alliance = 0x0b,      // Alliance chat
    CT_BattleGroup = 0x0c,   // Battlegroup chat
    CT_BattleGroupLeader = 0x0d, // BG leader chat
    CT_Staff = 0xf,          // Staff/GM chat
}
```

#### Chat Locations
```csharp
public enum eChatLoc : byte
{
    CL_ChatWindow = 0x0,     // Main chat window
    CL_PopupWindow = 0x1,    // Popup dialog
    CL_SystemWindow = 0x2    // System/combat window
}
```

### Channel Descriptions

**Game Rule Summary**: Each chat channel works differently depending on who you want to reach. Say chat only reaches nearby players, yell goes a bit further, private messages reach anyone in your realm, and group/guild channels reach your team members anywhere. Public channels like broadcast and advice let you talk to many people but may have restrictions to prevent spam.

#### Say (Local Chat)
- **Range**: 512 units
- **Cross-Realm**: Scrambled text
- **Command**: Default (no prefix)
- **Format**: `{player} says, "{message}"`

#### Yell
- **Range**: 1024 units (2x say)
- **Cross-Realm**: Scrambled text
- **Command**: `/yell` or `/y`
- **Format**: `{player} yells, "{message}"`

#### Private Messages (Send)
- **Range**: Unlimited (same realm)
- **Cross-Realm**: Blocked
- **Command**: `/send <player> <message>`
- **Format**: `{player} sends, "{message}"`

#### Group Chat
- **Range**: Unlimited
- **Requirements**: In group
- **Command**: `/g <message>`
- **Format**: `[Group] {player}: "{message}"`

#### Guild Chat
- **Range**: Unlimited
- **Requirements**: Guild membership, GcSpeak permission
- **Command**: `/gu <message>`
- **Format**: `[Guild] {player}: "{message}"`

#### Officer Chat
- **Range**: Unlimited
- **Requirements**: OcSpeak permission
- **Command**: `/o <message>`
- **Format**: `[Officers] {player}: "{message}"`

#### Alliance Chat
- **Range**: Unlimited
- **Requirements**: Alliance membership, AcSpeak permission
- **Command**: `/as <message>`
- **Format**: `[Alliance] {player}: "{message}"`

#### Broadcast
- **Range**: Current zone
- **Requirements**: None (may have level restrictions)
- **Command**: `/broadcast <message>` or `/br`
- **Format**: `[Broadcast] {player}: "{message}"`

#### Advice
- **Range**: Realm-wide
- **Requirements**: None (slowmode may apply)
- **Command**: `/advice <message>` or `/adv`
- **Format**: `[ADVICE {realm}] {player}: {message}`

#### Battlegroup
- **Range**: Unlimited (battlegroup members)
- **Requirements**: BG membership
- **Command**: `/bc <message>`
- **Format**: `[BG] {player}: "{message}"`

### Permission System

**Game Rule Summary**: Guild and alliance chat channels use permission systems to control who can speak and listen. Your guild rank determines whether you can speak in guild chat, hear officer discussions, or participate in alliance channels. This prevents new or untrusted members from disrupting important communications.

#### Guild Permissions
```csharp
public enum Guild.eRank
{
    GcHear,   // Hear guild chat
    GcSpeak,  // Speak in guild chat
    OcHear,   // Hear officer chat
    OcSpeak,  // Speak in officer chat
    AcHear,   // Hear alliance chat
    AcSpeak,  // Speak in alliance chat
}
```

#### Permission Checks
```csharp
if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.GcSpeak))
{
    DisplayMessage(client, "You don't have permission to speak on the guild channel.");
    return;
}
```

### Language System Integration

**Game Rule Summary**: Players from different realms speak different languages, so when enemies try to talk to you in local chat, their words appear as gibberish. This maintains the faction warfare atmosphere while preventing cross-realm coordination. However, some NPCs like merchants can be understood by everyone regardless of realm.

#### Cross-Realm Scrambling
```csharp
// Say/Yell between different realms
if (!GameServer.ServerRules.IsSameRealm(speaker, listener, true))
{
    // Message appears as gibberish
    message = ScrambleMessage(message);
}
```

#### NPC Exception
- Merchants understood by all
- Quest NPCs use player language
- Guards speak realm language

### Spam Protection

**Game Rule Summary**: The game has built-in protection against chat spam to keep channels usable. If you send messages too quickly, you'll be temporarily blocked with a "slow down" message. Some channels like advice have longer cooldowns to prevent flooding. These restrictions help maintain a civil chat environment for everyone.

#### Command Throttling
```csharp
if (IsSpammingCommand(client.Player, "broadcast", 500))
{
    DisplayMessage(client, "Slow down! Think before you say each word!");
    return;
}
```

#### Advice Slowmode
```csharp
int slowModeLength = Properties.ADVICE_SLOWMODE_LENGTH * 1000;
if (GameLoop.GameLoopTime - lastAdviceTick < slowModeLength)
{
    // Advice message blocked
    return;
}
```

### Mute System

**Game Rule Summary**: Players who violate chat rules can be muted by staff, preventing them from speaking in public channels. Muted players can still receive messages and may be allowed to use private group/guild channels depending on the punishment. This system helps maintain appropriate behavior in public chat spaces.

#### Mute Effects
- Cannot speak in public channels
- Private messages blocked
- Guild/Group chat may be allowed
- System messages still received

#### Mute Check
```csharp
if (client.Player.IsMuted)
{
    client.Player.Out.SendMessage("You have been muted and are not allowed to speak in this channel.", 
        eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return;
}
```

### Message Formatting

#### Standard Format
```csharp
// Guild chat example
string message = $"[Guild] {client.Player.Name}: \"{text}\"";
client.Player.Guild.SendMessageToGuildMembers(message, eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
```

#### Special Formatting
```csharp
// Advice channel with realm tag
string message = $"[ADVICE {GetRealmString(player.Realm)}] {player.Name}: {text}";

// Officer chat prefix
string message = $"[Officers] {player.Name}: \"{text}\"";
```

### Chat Groups

**Game Rule Summary**: Chat groups are custom channels that players can create for specific purposes like trade, roleplay, or coordinating activities. You can make them public for anyone to join or private with password protection. Chat groups let you organize conversations with specific groups of people beyond the standard guild and alliance channels.

#### Chat Group Features
- Player-created channels
- Password protection optional
- Public/Private modes
- Moderator controls

#### Chat Group Commands
| Command | Description |
|---------|-------------|
| `/cg invite <player>` | Invite to chat group |
| `/cg join` | Join chat group |
| `/cg leave` | Leave chat group |
| `/cg listen` | Toggle listen mode |
| `/cg password <pass>` | Set password |
| `/cg public` | Toggle public mode |

### NPC Communication

#### NPC Say Types
```csharp
switch (type)
{
    case "b": // Broadcast without prefix
        foreach (GamePlayer player in GetPlayersInRadius(25000))
            player.Out.SendMessage(text, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
        break;
        
    case "y": // Yell (increased range)
        Yell(text);
        break;
        
    case "s": // System message in area
        Message.MessageToArea(Brain.Body, text, eChatType.CT_System, eChatLoc.CL_SystemWindow, 512, null);
        break;
        
    case "c": // Say without prefix
        Message.MessageToArea(Brain.Body, text, eChatType.CT_Say, eChatLoc.CL_ChatWindow, 512, null);
        break;
        
    case "p": // Popup to interacting player
        ((GamePlayer)living).Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        break;
        
    default: // Normal say with prefix
        Say(text);
        break;
}
```

## Implementation Details

### Message Routing

#### Area Messages
```csharp
public static void MessageToArea(GameObject source, string message, eChatType type, eChatLoc loc, ushort range, GamePlayer exclude)
{
    foreach (GamePlayer player in source.GetPlayersInRadius(range))
    {
        if (player != exclude)
            player.Out.SendMessage(message, type, loc);
    }
}
```

#### Guild Distribution
```csharp
public void SendMessageToGuildMembers(string message, eChatType type, eChatLoc loc)
{
    foreach (GamePlayer player in GetListOfOnlineMembers())
    {
        player.Out.SendMessage(message, type, loc);
    }
}
```

### Ignore List Integration

#### Message Filtering
```csharp
// Alliance chat with ignore check
foreach (GamePlayer ply in gui.GetListOfOnlineMembers())
{
    if (!gui.HasRank(ply, Guild.eRank.AcHear) || ply.IsIgnoring(client.Player))
        continue;
        
    ply.Out.SendMessage(message, eChatType.CT_Alliance, eChatLoc.CL_ChatWindow);
}
```

### Discord Integration

#### External Logging
```csharp
if (Properties.DISCORD_ACTIVE)
    WebhookMessage.LogChatMessage(client.Player, eChatType.CT_Advise, msg);
```

## System Interactions

### Realm Restrictions
- Cross-realm /send blocked
- Public channels realm-separated
- Say/Yell scrambled cross-realm
- GM override available

### Anonymous Mode
- Hidden from /who lists
- Cannot receive /send
- Guild/Group chat unaffected
- Broadcast still visible

### Zone Boundaries
- Say/Yell limited to current area
- Broadcast zone-wide only
- Region channels separate
- Instance isolation

## Edge Cases

### Message Length
- Maximum ~500 characters
- Truncation at limit
- No multi-part messages
- Special character handling

### Rapid Messages
- Spam throttling active
- Per-command cooldowns
- Account-based tracking
- Progressive penalties

### Character Names
- Case-insensitive for /send
- Partial matching supported
- Special character handling
- Cross-realm validation

## Test Scenarios

1. **Basic Communication**
   - Say in various ranges
   - Private message sending
   - Group chat functionality
   - Guild chat permissions

2. **Channel Permissions**
   - Guild rank restrictions
   - Officer chat access
   - Alliance permissions
   - Mute enforcement

3. **Cross-Realm**
   - Language scrambling
   - Send blocking
   - NPC exceptions
   - GM overrides

4. **Spam Protection**
   - Rapid message blocking
   - Advice slowmode
   - Command cooldowns
   - Mute application

## GM Features

### Staff Channel
```csharp
// GM-only communication
player.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
```

### Override Capabilities
- Bypass mute restrictions
- Cross-realm communication
- See anonymous players
- No spam limitations

### Monitoring Tools
- Log all channels
- Real-time filtering
- Player chat history
- Pattern detection

## Performance Considerations

### Message Distribution
- Efficient radius checks
- Cached player lists
- Minimal string operations
- Batched sends

### Scalability
- Channel isolation
- Regional processing
- Async message handling
- Load distribution

## TODO
- Add custom chat channels
- Implement chat filtering options
- Add translation support
- Create chat history API
- Implement voice chat markers 