# Language & Communication System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from communication implementations  
**Implementation Status:** Live

## Overview

The Language & Communication System manages all forms of player interaction including chat channels, language barriers, translation mechanics, and communication restrictions. This system creates immersion and realm identity while enabling necessary communication.

## Core Architecture

### Chat Channel Types
```csharp
public enum eChatType
{
    CT_Say = 0,          // Local area chat
    CT_Tell = 1,         // Private message
    CT_Group = 2,        // Group chat
    CT_Guild = 3,        // Guild chat
    CT_Alliance = 4,     // Alliance chat
    CT_Broadcast = 5,    // Server-wide announcements
    CT_Officer = 6,      // Guild officer chat
    CT_Realm = 7,        // Realm chat (RvR zones)
    CT_Region = 8,       // Regional chat
    CT_Emote = 9,        // Emote actions
    CT_Help = 10,        // Help channel
    CT_LFG = 11,         // Looking for group
    CT_Trade = 12,       // Trade channel
    CT_Advice = 13,      // Advice channel
    CT_System = 14,      // System messages
    CT_Important = 15,   // Important notifications
    CT_Damage = 16,      // Combat messages
    CT_Staff = 17        // Staff/GM chat
}
```

### Language System
```csharp
public enum eLanguage
{
    Common = 0,      // Universal language
    Albion = 1,      // Albion realm language
    Hibernia = 2,    // Hibernia realm language
    Midgard = 3,     // Midgard realm language
    Ancient = 4      // Ancient/magical language
}

public class LanguageSkill
{
    public eLanguage Language { get; set; }
    public int SkillLevel { get; set; }  // 0-1000
    public bool IsNative { get; set; }   // Native speakers always understand
}
```

## Language Mechanics

### Language Understanding
```csharp
public class LanguageProcessor
{
    public static string ProcessMessage(GamePlayer speaker, GamePlayer listener, 
        string message, eLanguage language)
    {
        if (speaker == listener)
            return message; // Always understand yourself
            
        if (language == eLanguage.Common)
            return message; // Everyone understands Common
            
        var comprehension = GetLanguageComprehension(listener, language);
        
        if (comprehension >= 100)
            return message; // Full understanding
            
        if (comprehension <= 0)
            return ScrambleMessage(message, 100); // Complete scrambling
            
        return ScrambleMessage(message, 100 - comprehension);
    }
    
    private static int GetLanguageComprehension(GamePlayer player, eLanguage language)
    {
        // Native realm language
        if (GetNativeLanguage(player.Realm) == language)
            return 100;
            
        // Learned language skill
        var languageSkill = player.GetLanguageSkill(language);
        return languageSkill?.SkillLevel / 10 ?? 0; // Convert 0-1000 to 0-100
    }
}
```

### Message Scrambling
```csharp
public class MessageScrambler
{
    private static readonly string[] _scrambleWords = 
    {
        "gah", "grr", "ugh", "hmm", "err", "bah", "tsk", "meh"
    };
    
    public static string ScrambleMessage(string message, int scramblePercent)
    {
        if (scramblePercent <= 0)
            return message;
            
        var words = message.Split(' ');
        var scrambledWords = new List<string>();
        
        foreach (var word in words)
        {
            if (Util.Chance(scramblePercent))
            {
                scrambledWords.Add(GetScrambledWord(word));
            }
            else
            {
                scrambledWords.Add(word);
            }
        }
        
        return string.Join(" ", scrambledWords);
    }
    
    private static string GetScrambledWord(string originalWord)
    {
        // Preserve word length and some structure
        if (originalWord.Length <= 2)
            return _scrambleWords[Util.Random(_scrambleWords.Length)];
            
        var scrambled = _scrambleWords[Util.Random(_scrambleWords.Length)];
        
        // Add extra characters for longer words
        while (scrambled.Length < originalWord.Length)
        {
            scrambled += _scrambleWords[Util.Random(_scrambleWords.Length)];
        }
        
        return scrambled.Substring(0, originalWord.Length);
    }
}
```

## Chat Channel Management

### Channel Permissions
```csharp
public class ChatChannelPermissions
{
    public static bool CanSpeakInChannel(GamePlayer player, eChatType chatType)
    {
        return chatType switch
        {
            eChatType.CT_Say => true, // Anyone can speak locally
            eChatType.CT_Tell => true, // Anyone can send tells
            eChatType.CT_Group => player.Group != null,
            eChatType.CT_Guild => player.Guild != null,
            eChatType.CT_Alliance => player.Guild?.Alliance != null,
            eChatType.CT_Officer => IsGuildOfficer(player),
            eChatType.CT_Realm => IsInRvRZone(player),
            eChatType.CT_Region => true,
            eChatType.CT_Emote => true,
            eChatType.CT_Help => player.Level >= 5, // Prevent newbie spam
            eChatType.CT_LFG => player.Level >= 10,
            eChatType.CT_Trade => player.Level >= 15,
            eChatType.CT_Advice => player.Level >= 20,
            eChatType.CT_Broadcast => player.Client.Account.PrivLevel >= 200, // GM only
            eChatType.CT_Staff => player.Client.Account.PrivLevel >= 100, // Staff only
            _ => false
        };
    }
}
```

### Channel Range and Distribution
```csharp
public class ChatDistribution
{
    public static List<GamePlayer> GetChannelRecipients(GamePlayer speaker, eChatType chatType)
    {
        return chatType switch
        {
            eChatType.CT_Say => GetPlayersInSayRange(speaker),
            eChatType.CT_Group => speaker.Group?.Members.ToList() ?? new(),
            eChatType.CT_Guild => speaker.Guild?.Members.Select(m => m.Player).ToList() ?? new(),
            eChatType.CT_Alliance => GetAllianceMembers(speaker),
            eChatType.CT_Officer => GetGuildOfficers(speaker),
            eChatType.CT_Realm => GetRealmMembersInZone(speaker),
            eChatType.CT_Region => GetPlayersInRegion(speaker),
            eChatType.CT_Broadcast => GetAllOnlinePlayers(),
            _ => new List<GamePlayer>()
        };
    }
    
    private static List<GamePlayer> GetPlayersInSayRange(GamePlayer speaker)
    {
        return speaker.GetPlayersInRadius(SAY_RANGE)
            .Where(p => p.IsAlive) // Dead players can't hear
            .ToList();
    }
    
    private const int SAY_RANGE = 512; // Units for local chat range
}
```

## Spam Protection

### Anti-Spam Measures
```csharp
public class SpamProtection
{
    private readonly Dictionary<string, SpamTracker> _playerSpamTrackers = new();
    
    public bool CheckForSpam(GamePlayer player, string message, eChatType chatType)
    {
        var tracker = GetOrCreateTracker(player);
        
        // Check message frequency
        if (tracker.IsMessageTooFrequent())
        {
            player.SendMessage("You are speaking too fast!", eChatType.CT_System);
            return true; // Block message
        }
        
        // Check for repeated messages
        if (tracker.IsRepeatingMessages(message))
        {
            player.SendMessage("Please don't repeat the same message!", eChatType.CT_System);
            return true; // Block message
        }
        
        // Check for excessive capitals
        if (IsExcessivelyCapitalized(message))
        {
            player.SendMessage("Please don't use excessive capital letters!", eChatType.CT_System);
            return true; // Block message
        }
        
        tracker.RecordMessage(message);
        return false; // Allow message
    }
    
    private SpamTracker GetOrCreateTracker(GamePlayer player)
    {
        if (!_playerSpamTrackers.ContainsKey(player.Name))
            _playerSpamTrackers[player.Name] = new SpamTracker();
            
        return _playerSpamTrackers[player.Name];
    }
}

public class SpamTracker
{
    private readonly Queue<long> _messageTimes = new();
    private readonly Queue<string> _recentMessages = new();
    private const int MAX_MESSAGES_PER_MINUTE = 20;
    private const int REPEAT_MESSAGE_LIMIT = 3;
    
    public bool IsMessageTooFrequent()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        // Remove old messages (older than 1 minute)
        while (_messageTimes.Count > 0 && 
               currentTime - _messageTimes.Peek() > 60000)
        {
            _messageTimes.Dequeue();
        }
        
        return _messageTimes.Count >= MAX_MESSAGES_PER_MINUTE;
    }
    
    public bool IsRepeatingMessages(string message)
    {
        int repeatCount = _recentMessages.Count(m => m.Equals(message, StringComparison.OrdinalIgnoreCase));
        return repeatCount >= REPEAT_MESSAGE_LIMIT;
    }
    
    public void RecordMessage(string message)
    {
        _messageTimes.Enqueue(GameLoop.GameLoopTime);
        _recentMessages.Enqueue(message);
        
        // Keep only recent messages
        while (_recentMessages.Count > 10)
        {
            _recentMessages.Dequeue();
        }
    }
}
```

## Emote System

### Emote Processing
```csharp
public class EmoteSystem
{
    private static readonly Dictionary<string, EmoteAction> _emotes = new()
    {
        ["bow"] = new EmoteAction("bows gracefully.", "bows gracefully to {target}.", EmoteType.Targeted),
        ["wave"] = new EmoteAction("waves.", "waves at {target}.", EmoteType.Targeted),
        ["laugh"] = new EmoteAction("laughs.", "laughs at {target}.", EmoteType.Targeted),
        ["cry"] = new EmoteAction("cries.", EmoteType.Solo),
        ["dance"] = new EmoteAction("dances.", EmoteType.Solo),
        ["salute"] = new EmoteAction("salutes.", "salutes {target}.", EmoteType.Targeted)
    };
    
    public static bool ProcessEmote(GamePlayer player, string emoteName, GamePlayer target = null)
    {
        if (!_emotes.TryGetValue(emoteName.ToLower(), out var emote))
        {
            player.SendMessage($"Unknown emote: {emoteName}", eChatType.CT_System);
            return false;
        }
        
        string emoteMessage;
        
        if (target != null && emote.Type == EmoteType.Targeted)
        {
            emoteMessage = emote.TargetedMessage.Replace("{target}", target.Name);
        }
        else
        {
            emoteMessage = emote.SoloMessage;
        }
        
        // Broadcast emote to nearby players
        var nearbyPlayers = player.GetPlayersInRadius(SAY_RANGE);
        foreach (var nearbyPlayer in nearbyPlayers)
        {
            nearbyPlayer.SendMessage($"{player.Name} {emoteMessage}", eChatType.CT_Emote);
        }
        
        return true;
    }
}

public record EmoteAction(string SoloMessage, string TargetedMessage = "", EmoteType Type = EmoteType.Solo);

public enum EmoteType
{
    Solo,      // No target required
    Targeted   // Can target another player
}
```

## Cross-Realm Communication

### Realm Communication Restrictions
```csharp
public class RealmCommunicationRules
{
    public static bool CanCommunicateAcrossRealms(GamePlayer sender, GamePlayer recipient, 
        eChatType chatType)
    {
        if (sender.Realm == recipient.Realm)
            return true; // Same realm always allowed
            
        return chatType switch
        {
            eChatType.CT_Tell => false,      // No cross-realm tells
            eChatType.CT_Say => CanHearSay(sender, recipient), // Same location only
            eChatType.CT_Emote => CanSeeEmote(sender, recipient), // Same location only
            eChatType.CT_Help => true,       // Help channel is universal
            eChatType.CT_Advice => true,     // Advice channel is universal
            _ => false                       // All other channels are realm-restricted
        };
    }
    
    private static bool CanHearSay(GamePlayer sender, GamePlayer recipient)
    {
        // Cross-realm say only in neutral zones
        return sender.CurrentRegion.IsNeutralZone && 
               sender.IsWithinRadius(recipient, SAY_RANGE);
    }
}
```

## Chat Filtering

### Profanity Filter
```csharp
public class ProfanityFilter
{
    private static readonly HashSet<string> _bannedWords = LoadBannedWords();
    private static readonly Dictionary<string, string> _replacements = new()
    {
        ["damn"] = "darn",
        ["hell"] = "heck"
        // Additional replacements...
    };
    
    public static string FilterMessage(string message)
    {
        string filteredMessage = message;
        
        foreach (var bannedWord in _bannedWords)
        {
            if (filteredMessage.Contains(bannedWord, StringComparison.OrdinalIgnoreCase))
            {
                var replacement = _replacements.GetValueOrDefault(bannedWord, 
                    new string('*', bannedWord.Length));
                filteredMessage = filteredMessage.Replace(bannedWord, replacement, 
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        
        return filteredMessage;
    }
}
```

## Configuration

```csharp
[ServerProperty("chat", "enable_language_system", true)]
public static bool ENABLE_LANGUAGE_SYSTEM;

[ServerProperty("chat", "say_range", 512)]
public static int SAY_RANGE;

[ServerProperty("chat", "enable_spam_protection", true)]
public static bool ENABLE_SPAM_PROTECTION;

[ServerProperty("chat", "max_messages_per_minute", 20)]
public static int MAX_MESSAGES_PER_MINUTE;

[ServerProperty("chat", "enable_profanity_filter", true)]
public static bool ENABLE_PROFANITY_FILTER;

[ServerProperty("chat", "cross_realm_chat_enabled", false)]
public static bool CROSS_REALM_CHAT_ENABLED;
```

## TODO: Missing Documentation

- Advanced language learning progression systems
- Custom emote creation and scripting
- Voice chat integration and proximity audio
- Chat logging and moderation tools
- Dynamic translation services
- Role-playing chat formatting and styles

## References

- `GameServer/language/LanguageMgr.cs` - Language system core
- `GameServer/packets/Client/ChatRequestHandler.cs` - Chat processing
- `GameServer/gameobjects/GamePlayer.cs` - Chat methods
- Various chat command handlers for channel management 