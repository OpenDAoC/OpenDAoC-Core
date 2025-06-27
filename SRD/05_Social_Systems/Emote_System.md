# Emote System

**Document Status:** Initial Documentation
**Completeness:** 75%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Emotes let you express emotions and actions through animated gestures that other players can see. You can wave, bow, dance, cheer, and perform many other animations to communicate without words. Some emotes can be targeted at specific players, while others just show your character performing the action. When you're mounted on a horse, you get access to special horse emotes like making your mount rear or graze. Emotes are a fun way to roleplay and interact socially with other players.

The emote system allows players to express emotions and actions through animated gestures. Emotes include both simple animations and horse-specific emotes for mounted players.

## Core Mechanics

### Basic Emote System

**Game Rule Summary**: Emotes work by typing slash commands like `/wave` or `/bow`, and your character will perform the animation. If you target another player first, some emotes will be directed specifically at them, like bowing to someone or pointing at them. Other players near you will see the animation and might get a text message describing what you're doing.

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

**Game Rule Summary**: There are dozens of different emotes covering all kinds of expressions and actions. You can show emotions (happy, sad, angry), perform actions (dance, salute, flex), or interact socially (hug, wave, bow). If you're riding a horse, you get access to special mounted emotes that show you and your horse performing actions together.

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

**Game Rule Summary**: When you're riding a horse, you get access to special emotes that show both you and your mount performing actions. You can make your horse rear up dramatically, graze peacefully, or perform tricks. These emotes only work when you're actually mounted and add a lot of character to mounted roleplay.

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

**Game Rule Summary**: When you use emotes, other players see both the animation and text messages describing what you're doing. If you target someone specifically, they'll get a personal message that you're emoting at them. Players from other realms might not understand your custom emote text, but they'll still see you "making strange motions."

#### Message Types
1. **No Target Messages**:
   - To Source: Direct message
   - To Others: "{0} does emote"

2. **Targeted Messages**:
   - To Source: "You emote at {0}"
   - To Target: "{0} emotes at you"
   - To Others: "{0} emotes at {1}"

#### Custom Emote System

**Game Rule Summary**: Besides the preset emotes, you can create custom emotes by typing `/emote <your action>`, `/em <your action>`, or `/e <your action>`. This lets you describe any action you want, like "checks his sword for nicks" or "looks around nervously." Players from your own realm will see exactly what you type, but players from enemy realms will just see that you're "making strange motions."

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

**Game Rule Summary**: You can't emote when you're dead, and most emotes are blocked while you're actively fighting. If you're stunned, mesmerized, or otherwise incapacitated, you also can't emote. There's also anti-spam protection to prevent people from flooding the area with rapid-fire emotes. If someone has you on their ignore list, they won't see your emotes.

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

**Game Rule Summary**: Emotes have limited range - you need to be reasonably close to other players for them to see your emotes. If you're targeting someone specifically with an emote, they can be a bit further away than the general viewing distance. Players who have you on their ignore list won't see your emotes at all.

#### Range Checks
- **Target Range**: 2048 units (EMOTE_RANGE_TO_TARGET)
- **Area Broadcast**: 512 units (EMOTE_RANGE_TO_OTHERS)
- **Animation Visibility**: WorldMgr.VISIBILITY_DISTANCE

#### Ignore System
- Ignored players don't see emotes
- Applies to both animations and messages

## System Interactions

### Cross-Realm Communication

**Game Rule Summary**: The language barrier affects emotes just like speech. Players from your own realm can see exactly what your custom emotes say, but players from enemy realms only see that you're "making strange motions." The animations still work for everyone regardless of realm, but the text is filtered by the language system.

- Same realm: Full emote text visible
- Different realm: Generic "makes strange motions"
- Follows IsAllowedToUnderstand rules

### Combat Integration

**Game Rule Summary**: Most emotes are disabled while you're in active combat to prevent them from interfering with fighting. However, some emotes can still be used while mounted, and the special horse emotes require you to actually be riding a horse to work.

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

**Game Rule Summary**: Horse emotes are special - they only work when you're actually mounted on a horse. If you try to use them while on foot, they simply won't work. Different types of mounts might have different emote sets available.

- Only work when mounted
- Different message sets for horse emotes
- Some require specific mount types

### Cross-Region Emotes
- Not transmitted across region boundaries
- Lost if player zones during animation

### Realm Restrictions

**Game Rule Summary**: While the animations for preset emotes work across all realms, the text messages for custom emotes are filtered by the language barrier system. This means enemy realm players can see you waving or bowing, but can't understand what you're saying when you use custom emotes.

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