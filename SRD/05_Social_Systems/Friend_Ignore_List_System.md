# Friend & Ignore List System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The Friend & Ignore List system allows players to manage social connections and control communication. Friends receive online/offline notifications and can be easily tracked, while ignored players cannot send messages to the ignoring player.

## Core Mechanics

### Friend List System

#### Storage & Management
```csharp
// Friends stored as serialized string array in database
[DataElement(AllowDbNull = true)]
public string SerializedFriendsList { get; set; }

// Cached in concurrent dictionary for performance
private ConcurrentDictionary<GamePlayer, string[]> PlayersFriendsListsCache
private ConcurrentDictionary<GamePlayer, FriendStatus[]> PlayersFriendsStatusCache
```

#### Friend Operations

**Adding Friends**:
```csharp
public bool AddFriendToPlayerList(GamePlayer player, string friend)
{
    // Validation
    friend = friend.Trim();
    if (string.IsNullOrEmpty(friend))
        return false;
        
    // Update cache
    string[] currentFriendsList = PlayersFriendsListsCache[player];
    if (!currentFriendsList.Contains(friend))
    {
        PlayersFriendsListsCache[player] = currentFriendsList.Concat(new[] { friend }).ToArray();
        player.Out.SendAddFriends(new[] { friend });
        player.SerializedFriendsList = this[player];
    }
}
```

**Removing Friends**:
```csharp
public bool RemoveFriendFromPlayerList(GamePlayer player, string friend)
{
    string[] currentFriendsList = PlayersFriendsListsCache[player];
    PlayersFriendsListsCache[player] = currentFriendsList.Except(new[] { friend }, StringComparer.OrdinalIgnoreCase).ToArray();
    player.Out.SendRemoveFriends(new[] { friend });
    player.SerializedFriendsList = this[player];
}
```

#### Friend Status Tracking

**Online Status**:
- Online friends show current location
- Offline friends show last played time
- Anonymous players hidden from lists

**Status Information**:
```csharp
public struct FriendStatus
{
    public string Name;
    public int Level;
    public int ClassID;
    public DateTime LastPlayed;
}
```

#### Friend Notifications

**Login Notifications**:
```csharp
private void NotifyPlayerFriendsEnteringGame(GamePlayer player)
{
    foreach (GamePlayer friend in PlayersFriendsListsCache
        .Where(kv => kv.Value.Contains(player.Name))
        .Select(kv => kv.Key))
    {
        friend.Out.SendAddFriends(new[] { player.Name });
    }
}
```

**Logout Notifications**:
```csharp
private void NotifyPlayerFriendsExitingGame(GamePlayer player)
{
    foreach (GamePlayer friend in PlayersFriendsListsCache
        .Where(kv => kv.Value.Contains(player.Name))
        .Select(kv => kv.Key))
    {
        friend.Out.SendRemoveFriends(new[] { player.Name });
    }
}
```

### Ignore List System

#### Storage & Management
```csharp
// Stored as ArrayList, serialized to string array
public ArrayList IgnoreList
{
    get 
    { 
        if (SerializedIgnoreList.Length > 0)
            return new ArrayList(SerializedIgnoreList);
        return new ArrayList(0);
    }
    set 
    { 
        SerializedIgnoreList = value?.OfType<string>().ToArray() ?? new string[0];
        GameServer.Database.SaveObject(DBCharacter);
    }
}
```

#### Ignore Operations

**Adding to Ignore List**:
```csharp
public void ModifyIgnoreList(string Name, bool remove)
{
    ArrayList currentIgnores = IgnoreList;
    if (!remove && !currentIgnores.Contains(Name))
    {
        currentIgnores.Add(Name);
        IgnoreList = currentIgnores;
    }
}
```

**Removing from Ignore List**:
```csharp
public void ModifyIgnoreList(string Name, bool remove)
{
    ArrayList currentIgnores = IgnoreList;
    if (remove && currentIgnores.Contains(Name))
    {
        currentIgnores.Remove(Name);
        IgnoreList = currentIgnores;
    }
}
```

#### Ignore Effects

**Communication Blocking**:
- Private messages blocked
- Public messages still visible
- Group/Guild messages unaffected

**Ignore Check**:
```csharp
public bool IsIgnoring(GamePlayer player)
{
    return IgnoreList.Contains(player.Name);
}
```

### Commands

#### Friend Commands
| Command | Description |
|---------|-------------|
| `/friend` | Display friends list snapshot |
| `/friend window` | Open social window with friends |
| `/friend <name>` | Add/remove friend |

#### Ignore Commands
| Command | Description |
|---------|-------------|
| `/ignore` | Display ignore list |
| `/ignore <name>` | Add/remove from ignore |

### Social Window Display

#### Friend List Format
```csharp
// Format: "F,{index},{name},{level},{class},\"{location}\""
player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
    index++,
    friend.Name,
    friend.Level,
    friend.CharacterClass.ID,
    friend.CurrentZone?.Description ?? friend.LastPlayed),
    eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
```

#### Window Operations
1. **Clear List**: "TF" message clears display
2. **Add Friends**: Populates with online friends first
3. **Add Offline**: Adds offline friends with last played
4. **Update Status**: Real-time online/offline updates

## Implementation Details

### Cache Management

#### Player Login
```csharp
public async Task AddPlayerFriendsListToCache(GamePlayer player)
{
    string[] friends = player.SerializedFriendsList;
    PlayersFriendsListsCache.TryAdd(player, friends);
    
    // Load offline friend data
    IList<DbCoreCharacter> offlineFriends = await DOLDB<DbCoreCharacter>
        .SelectObjectsAsync(DB.Column("Name").IsIn(friends));
    FriendStatus[] offlineFriendStatus = offlineFriends
        .Select(chr => new FriendStatus(chr.Name, chr.Level, chr.Class, chr.LastPlayed))
        .ToArray();
    PlayersFriendsStatusCache.TryAdd(player, offlineFriendStatus);
}
```

#### Player Logout
```csharp
public void RemovePlayerFriendsListFromCache(GamePlayer player)
{
    PlayersFriendsListsCache.TryRemove(player, out _);
    PlayersFriendsStatusCache.TryRemove(player, out _);
}
```

### Event Handling

#### Registered Events
```csharp
GameEventMgr.AddHandler(GameClientEvent.StateChanged, OnClientStateChanged);
GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerGameEntered);
GameEventMgr.AddHandler(GamePlayerEvent.Quit, OnPlayerQuit);
GameEventMgr.AddHandler(GamePlayerEvent.ChangeAnonymous, OnPlayerChangeAnonymous);
```

### Anonymous Mode Handling

**Friend Visibility**:
- Anonymous players hidden from friend lists
- No online notifications sent
- Still visible in guild/group

**Code Implementation**:
```csharp
var pair = PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key != null && kv.Key.Name == name);
return pair.Key != null && !pair.Key.IsAnonymous;
```

## System Interactions

### Realm Restrictions
- Cannot friend cross-realm players
- Cross-realm names not resolvable
- GM override available

### Guild Integration
- Guild members visible regardless
- Guild chat unaffected by ignore
- Officer chat respects permissions

### Group System
- Group chat overrides ignore
- Group formation not blocked
- Loot distribution unaffected

## Edge Cases

### Name Resolution
- Partial name matching supported
- Case-insensitive operations
- Multiple matches rejected

### Offline Players
- Can add offline players by exact name
- Can remove offline friends
- Status cached until next login

### List Limits
- No hard limit on friend count
- Performance considerations at 100+
- Ignore list unlimited

### Character Deletion
- Deleted characters remain on lists
- No automatic cleanup
- Manual removal required

## Test Scenarios

1. **Basic Operations**
   - Add/remove online friend
   - Add/remove offline friend
   - Add/remove ignored player
   - View lists

2. **Notifications**
   - Friend login notification
   - Friend logout notification
   - Anonymous mode changes
   - Zone change updates

3. **Communication**
   - Message ignored player
   - Receive from ignored
   - Guild/group overrides
   - Public channel visibility

4. **Edge Cases**
   - Partial name matching
   - Cross-realm attempts
   - Duplicate additions
   - Case sensitivity

## GM Features

### Friend Management
```csharp
case "friend":
{
    if (args[2] == "list")
    {
        string[] list = player.SerializedFriendsList;
        client.Out.SendCustomTextWindow(player.Name + "'s Friend List", list);
        return;
    }
    
    // Add/remove friends for other players
    if (player.AddFriend(name))
        player.Out.SendMessage($"{client.Player.Name} has added {name} to your friend list!");
}
```

### Ignore Override
- GMs bypass ignore lists
- Can view player ignore lists
- Can modify ignore lists

## Performance Considerations

### Cache Strategy
- Concurrent dictionaries for thread safety
- Lazy loading of offline data
- Minimal database queries

### Update Optimization
- Batch friend updates
- Throttled notifications
- Efficient list operations

## TODO
- Add friend list size limits
- Implement friend notes/categories
- Add account-wide friends option
- Create API for friend queries
- Add friend activity history 