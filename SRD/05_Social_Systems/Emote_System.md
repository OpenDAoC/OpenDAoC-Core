# Emote System

**Document Status:** Initial Documentation
**Completeness:** 75%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The emote system allows players to express emotions and actions through animated gestures. Emotes include both simple animations and horse-specific emotes for mounted players.

## Core Mechanics

### Basic Emote System

#### Command Structure
- **Format**: `/emote_name` or through emote menu
- **Targeting**: Can be directed at specific targets
- **Range**: 
  - To Target: 2048 units
  - To Others: 512 units

#### Animation Broadcast
```csharp
foreach (GamePlayer player in sourcePlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
{
    if (!player.IsIgnoring(sourcePlayer))
        player.Out.SendEmoteAnimation(sourcePlayer, emoteID);
}
```

### Emote Types

#### Standard Player Emotes
| Command | Emote ID | Description |
|---------|----------|-------------|
| /angry | eEmote.Angry | Shows anger |
| /bang | eEmote.BangOnShield | Bangs on shield |
| /beckon | eEmote.Beckon | Beckoning motion |
| /beg | eEmote.Beg | Begging gesture |
| /blush | eEmote.Blush | Blushing |
| /bow | eEmote.Bow | Formal bow |
| /charge | eEmote.LetsGo | Rally cry |
| /cheer | eEmote.Cheer | Cheering |
| /clap | eEmote.Clap | Clapping |
| /cry | eEmote.Cry | Crying |
| /curtsey | eEmote.Curtsey | Curtsey |
| /dance | eEmote.Dance | Dancing |
| /dismiss | eEmote.Dismiss | Dismissive gesture |
| /flex | eEmote.Flex | Muscle flex |
| /hug | eEmote.Hug | Hugging motion |
| /kiss | eEmote.BlowKiss | Blow kiss |
| /laugh | eEmote.Laugh | Laughing |
| /military | eEmote.Military | Military salute |
| /no | eEmote.No | Head shake |
| /point | eEmote.Point | Pointing |
| /ponder | eEmote.Ponder | Thinking pose |
| /present | eEmote.Present | Presentation |
| /raise | eEmote.Raise | Raise hand |
| /rude | eEmote.Rude | Rude gesture |
| /salute | eEmote.Salute | Salute |
| /shrug | eEmote.Shrug | Shrugging |
| /slap | eEmote.Slap | Slapping motion |
| /slit | eEmote.Slit | Throat slit |
| /smile | eEmote.Smile | Smiling |
| /surrender | eEmote.Surrender | Surrender |
| /taunt | eEmote.Taunt | Taunting |
| /victory | eEmote.Victory | Victory howl |
| /wave | eEmote.Wave | Waving |
| /yes | eEmote.Yes | Nodding |

#### Horse/Mount Emotes
| Command | Emote ID | Description |
|---------|----------|-------------|
| /lookfar | eEmote.Rider_LookFar | Look into distance |
| /stench | eEmote.Rider_Stench | React to smell |
| /halt | eEmote.Rider_Halt | Stop motion |
| /pet | eEmote.Rider_pet | Pet horse |
| /courbette | eEmote.Horse_Courbette | Horse rear |
| /startle | eEmote.Horse_Startle | Horse startled |
| /nod | eEmote.Horse_Nod | Horse nods |
| /graze | eEmote.Horse_Graze | Horse grazes |
| /rear | eEmote.Horse_rear | Horse rears |
| /trick | eEmote.Rider_Trick | Perform trick |
| /whistle | eEmote.Horse_whistle | Whistle for horse |

#### Special Emotes
| Emote ID | Description | Usage |
|----------|-------------|-------|
| LvlUp | Level up animation | Automatic on level |
| Bind | Binding animation | At bind points |
| SpellGoBoom | Spell failure | Spell interrupts |
| PlayerPrepare | Combat preparation | Pre-combat |

### Message System

#### Message Types
1. **No Target Messages**:
   - To Source: Direct message
   - To Others: "{0} does emote"

2. **Targeted Messages**:
   - To Source: "You emote at {0}"
   - To Target: "{0} emotes at you"
   - To Others: "{0} emotes at {1}"

#### Custom Emote System
```csharp
// /emote <text> or /em <text> or /e <text>
if (GameServer.ServerRules.IsAllowedToUnderstand(source, target))
{
    // Send actual emote text
    player.Out.SendMessage(ownRealm, eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
}
else
{
    // Send generic message for different realms
    player.Out.SendMessage("<" + source.Name + " makes strange motions.>", 
        eChatType.CT_Emote, eChatLoc.CL_ChatWindow);
}
```

### Restrictions

#### State Restrictions
- **Dead**: Cannot emote while dead
- **Combat**: Cannot use most emotes in combat
- **Mezzed/Stunned**: Cannot emote while incapacitated
- **Muted**: Cannot use custom emotes when muted

#### Spam Prevention
```csharp
const string EMOTE_TICK = "Emote_Tick";
if (changeTime < ServerProperties.Properties.EMOTE_DELAY && Tick > 0)
{
    // Anti-spam message
    return;
}
```

### Range and Visibility

#### Range Checks
- **Target Range**: 2048 units (EMOTE_RANGE_TO_TARGET)
- **Area Broadcast**: 512 units (EMOTE_RANGE_TO_OTHERS)
- **Animation Visibility**: WorldMgr.VISIBILITY_DISTANCE

#### Ignore System
- Ignored players don't see emotes
- Applies to both animations and messages

## System Interactions

### Cross-Realm Communication
- Same realm: Full emote text visible
- Different realm: Generic "makes strange motions"
- Follows IsAllowedToUnderstand rules

### Combat Integration
- Attack state blocks most emotes
- Some emotes usable while mounted
- Horse emotes require being on horse

### NPC Usage
```csharp
public virtual void Emote(eEmote emote)
{
    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.Out.SendEmoteAnimation(this, emote);
    }
}
```

## Implementation Notes

### Packet Structure
- Uses SendEmoteAnimation packet
- Includes source object ID and emote ID
- Client handles animation display

### Performance Considerations
- Visibility checks for all nearby players
- Ignore list checks per player
- Spam throttling per source

## Edge Cases

### Mount-Specific Emotes
- Only work when mounted
- Different message sets for horse emotes
- Some require specific mount types

### Cross-Region Emotes
- Not transmitted across region boundaries
- Lost if player zones during animation

### Realm Restrictions
- Language barriers affect custom emotes
- Animation still plays regardless of realm

## Test Scenarios

1. **Basic Functionality**
   - All emotes animate correctly
   - Messages display properly
   - Target detection works

2. **Range Testing**
   - Target out of range detection
   - Area broadcast radius
   - Visibility distance limits

3. **State Restrictions**
   - Dead player cannot emote
   - Combat blocks appropriate emotes
   - Mute affects custom emotes only

4. **Cross-Realm**
   - Different realm sees generic message
   - Same realm sees full text
   - Animations work regardless

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 