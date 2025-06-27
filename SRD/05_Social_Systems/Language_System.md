# Language System

**Document Status:** Initial Documentation  
**Completeness:** 80%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Each realm speaks its own language, so when you talk to enemies from other realms, they see scrambled gibberish instead of your actual words. This prevents cross-realm coordination in PvP while maintaining the fantasy of separate cultures. Everyone can understand merchants and system messages, but player chat, emotes, and most communication appears as nonsense to enemies.

The language system manages cross-realm communication barriers, ensuring players from different realms cannot understand each other's text while maintaining game immersion through filtered messages.

## Core Mechanics

### Language Rules

#### Basic Principles
- **Same Realm**: Full understanding
- **Different Realm**: Scrambled text
- **Neutral NPCs**: Understood by all
- **System Messages**: Always readable

#### Communication Channels Affected
```csharp
// Channels subject to language filtering
case eChatType.CT_Say:
case eChatType.CT_Yell:
case eChatType.CT_Emote:
case eChatType.CT_Group:
case eChatType.CT_Guild:
case eChatType.CT_Send:
    // Apply language filter
```

### Language Detection

#### Realm Identification
```csharp
public static bool IsAllowedToUnderstand(GameLiving source, GamePlayer target)
{
    // Same realm check
    if (source.Realm == target.Realm)
        return true;
        
    // Staff override
    if (target.Client.Account.PrivLevel > 1)
        return true;
        
    // Special NPC handling
    if (source is GameNPC && ((GameNPC)source).CanUnderstand)
        return true;
        
    return false;
}
```

#### Special Cases
- **GMs/Staff**: Can understand all languages
- **Merchants**: Universal understanding
- **Quest NPCs**: Context-dependent
- **Guards**: Realm-specific

### Message Scrambling

#### Scramble Algorithm
```csharp
public static string Scramble(string message, GamePlayer receiver)
{
    // Character replacement mapping
    char[] replacements = GetRealmSpecificChars(receiver.Realm);
    
    StringBuilder scrambled = new StringBuilder();
    foreach (char c in message)
    {
        if (char.IsLetter(c))
            scrambled.Append(replacements[GetCharIndex(c)]);
        else
            scrambled.Append(c); // Preserve punctuation
    }
    
    return scrambled.ToString();
}
```

#### Scramble Patterns
| Original | Albion Sees | Midgard Sees | Hibernia Sees |
|----------|-------------|--------------|---------------|
| "Hello" | "Hello" | "Mjrrk" | "Siabh" |
| "Attack!" | "Attack!" | "Gvvgho!" | "Deefnu!" |

### Communication Types

#### Say/Yell
- **Range**: Say = 512, Yell = 1024
- **Cross-Realm**: Scrambled
- **Emotes**: Show as "makes strange motions"

#### Emote System
```csharp
if (!IsAllowedToUnderstand(source, player))
{
    // Generic emote message
    player.Out.SendMessage("<" + source.Name + " makes strange motions.>", 
        eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
}
else
{
    // Actual emote text
    player.Out.SendMessage(emoteText, eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
}
```

### Message Display

#### Formatted Output
```csharp
// Same realm format
"[PlayerName]: Hello everyone!"

// Different realm format
"[PlayerName]: Mjrrk kvkthrmk!"

// System indication
"<PlayerName makes strange motions.>" // For emotes
```

#### Visual Indicators
- **Name Colors**: Realm-based
- **Chat Colors**: Channel-specific
- **Scrambled Text**: Italicized (client-dependent)

## System Interactions

### Chat System Integration
- Pre-processes all realm-affected channels
- Maintains original for logging
- Scrambles per-recipient

### PvP/RvR Context
```csharp
// Battlefield messages
if (IsInRvRZone(player))
{
    // Always scramble enemy communication
    // Even in mixed zones
}
```

### NPC Interactions
- **Merchants**: Always understood
- **Guards**: Realm-language only
- **Trainers**: Realm-specific
- **Quest NPCs**: Varies by design

### Special Zones

#### Neutral Zones
```csharp
// Housing zones
if (player.CurrentZone.IsHousingZone)
{
    // May allow cross-realm understanding
    // Server configuration dependent
}
```

#### Battlegrounds
- Always enforces language barriers
- No exceptions for level-restricted zones
- Maintains competitive integrity

## Implementation Notes

### Performance Optimization
```csharp
// Cache scrambled messages
Dictionary<string, string> scrambleCache = new Dictionary<string, string>();

// Reuse common scrambles
if (scrambleCache.ContainsKey(originalMessage))
    return scrambleCache[originalMessage];
```

### Client Handling
- Server sends appropriate version
- No client-side processing needed
- Prevents client modifications

### Logging Considerations
```csharp
// Log original message
ChatLog.Log(source, target, originalMessage, channelType);

// Send scrambled to client
target.Out.SendMessage(scrambledMessage, channelType, location);
```

## Configuration Options

### Server Properties
```csharp
// Allow cross-realm understanding in specific zones
ALLOW_CROSS_REALM_CHAT = false

// GM always understand all
GM_UNDERSTAND_ALL = true

// Scramble algorithm version
LANGUAGE_SCRAMBLE_VERSION = 2
```

### Custom Implementations
- Alternative scramble algorithms
- Zone-specific rules
- Event-based exceptions

## Edge Cases

### Mixed Groups
- Cannot group cross-realm
- Prevents communication issues
- Maintains faction integrity

### Pets and Summons
```csharp
// Pet messages follow owner's realm
if (source is GamePet)
{
    GamePet pet = (GamePet)source;
    return IsAllowedToUnderstand(pet.Owner, target);
}
```

### Morphed Players
- Realm disguise doesn't affect language
- True realm used for checks
- Prevents espionage exploits

### Special Events
- GM events may override
- Temporary understanding flags
- Server-controlled exceptions

## Test Scenarios

1. **Basic Communication**
   - Same realm chat works normally
   - Different realm properly scrambled
   - Punctuation preserved

2. **Special NPCs**
   - Merchants understood by all
   - Guards realm-specific
   - Quest NPCs contextual

3. **Channel Testing**
   - Say/Yell scrambled
   - Emotes show generic message
   - System messages unaffected

4. **Edge Cases**
   - Pet messages follow owner
   - Morphed players use true realm
   - GM override functions

## Cultural Notes

### Realm Languages (Lore)
- **Albion**: Common tongue (English)
- **Midgard**: Norse-influenced
- **Hibernia**: Celtic-influenced

### Immersion Elements
- Maintains faction separation
- Encourages realm loyalty
- Prevents cross-teaming

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 