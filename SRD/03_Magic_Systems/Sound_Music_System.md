# Sound & Music System

**Document Status:** Initial Documentation  
**Completeness:** 75%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The sound and music system encompasses spell sounds, ambient audio, instrument-based songs (Minstrel/Bard/Skald abilities), and general game audio feedback. Instruments are required equipment for song-based abilities.

## Core Mechanics

### Sound Types

#### Sound Categories
```csharp
public enum eSoundType
{
    Craft = 0x01,
    Ambient = 0x02,
    Spell = 0x03,
    Combat = 0x04,
    UserInterface = 0x05,
    Music = 0x06,
    Divers = 0x07
}
```

#### Sound Triggers
- **Spell Casts**: Effect-specific sounds
- **Combat**: Weapon swings, impacts
- **Ambient**: Zone-based atmosphere
- **Music**: Instrument songs
- **UI**: Interface feedback

### Instrument System

#### Instrument Requirements
```csharp
// Instruments are weapon-type objects
public bool CheckInstrument()
{
    DbInventoryItem instrument = Caster.ActiveWeapon;
    
    // Must be instrument type
    if (instrument == null || instrument.Object_Type != (int)eObjectType.Instrument)
        return false;
        
    return true;
}
```

#### Instrument Types (Historical)
```csharp
// Pre-1.97 patch - Specific instruments for song types
// DPS_AF field indicated instrument class:
// 1 = Drum (Warsongs)
// 2 = Lute (Peace songs)  
// 3 = Flute (Travel songs)
// 4 = Any instrument (post-1.97)
```

#### Current Implementation
- **Universal Instruments**: Any instrument plays any song
- **Quality Matters**: Affects duration/effectiveness
- **Condition**: Degrades with use
- **Level**: Impacts song power

### Song Mechanics

#### Song Properties
```csharp
public class Song : Spell
{
    public override eSkillPage SkillType 
    { 
        get { return eSkillPage.Songs; }
    }
    
    public int InstrumentRequirement { get; set; }
    public bool IsPulsing { get; set; }
    public int PulseFrequency { get; set; }
}
```

#### Song Duration Calculation
```csharp
protected override int CalculateEffectDuration(GameLiving target)
{
    double duration = Spell.Duration;
    duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
    
    if (Spell.InstrumentRequirement != 0)
    {
        DbInventoryItem instrument = Caster.ActiveWeapon;
        if (instrument != null)
        {
            // Up to 200% duration for songs
            duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level);
            
            // Quality and condition affect duration
            duration *= instrument.Condition / (double)instrument.MaxCondition * instrument.Quality / 100;
        }
    }
    
    return (int)duration;
}
```

### Pulsing Songs

#### Pulse Mechanics
```csharp
public virtual void OnSpellPulse(PulsingSpellEffect effect)
{
    // Check if still playing
    if (!CheckInstrument())
    {
        MessageToCaster("You stop playing your song.", eChatType.CT_Spell);
        effect.Cancel(false);
        return;
    }
    
    // Consume power per pulse
    if (Caster.Mana >= Spell.PulsePower)
    {
        Caster.Mana -= Spell.PulsePower;
        SendEffectAnimation(Caster, 0, true, 1);
        StartSpell(Target);
    }
}
```

#### Pulse Types
- **Speed Songs**: Continuous movement buff
- **Healing Songs**: Periodic health restoration
- **Mana Songs**: Power regeneration
- **Damage Songs**: Offensive pulses

### Sound Playback

#### Playing Sounds
```csharp
public void PlaySound(GamePlayer player, ushort soundID, eSoundType type)
{
    player.Out.SendPlaySound(type, soundID);
}

// Area sounds
foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
{
    player.Out.SendPlaySound(eSoundType.Spell, spellSoundID);
}
```

#### Sound Properties
- **ID**: Unique identifier
- **Type**: Category for volume control
- **Range**: Area of effect
- **Priority**: Overlapping sounds

### Music-Specific Features

#### Bard/Minstrel Songs
```csharp
// Movement enhancement
public class SpeedSong : SpellHandler
{
    public override void ApplyEffectOnTarget(GameLiving target)
    {
        // Cannot affect mounted players
        if (target is GamePlayer && ((GamePlayer)target).IsRiding)
            return;
            
        // Apply speed bonus
        target.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, 1.5);
    }
}
```

#### Song Interruption
- **Movement**: Doesn't interrupt songs
- **Combat**: Interrupts non-instant songs
- **Mezz/Stun**: Stops song playing
- **Instrument Change**: Cancels active song

### Instrument Interaction

#### Weapon Slot Usage
```csharp
// Songs require instrument in active weapon slot
if (m_spell.InstrumentRequirement != 0)
{
    if (Caster.ActiveWeapon?.Object_Type != (int)eObjectType.Instrument)
    {
        MessageToCaster("You are not wielding the right type of instrument!", 
            eChatType.CT_SpellResisted);
        return false;
    }
}
```

#### Instrument Switching
- Cannot switch during song
- Two-handed slot only
- No shield while playing
- Quick-swap penalties

### Special Song Effects

#### Mezmerize Songs
```csharp
// Flute mezz can be maintained while moving
if (newSpell.SpellType == eSpellType.Mesmerize && 
    newSpell.InstrumentRequirement != 0)
{
    // Special handling for instrument mezz
    // Can pulse while moving
}
```

#### Charm Songs
- **Duration**: Instrument-enhanced
- **Power Cost**: Per pulse
- **Range**: Line of sight required
- **Breaking**: Standard charm rules

### Ambient Music

#### Zone Music
```csharp
public class ZoneMusic
{
    public ushort DayMusicID { get; set; }
    public ushort NightMusicID { get; set; }
    public ushort CombatMusicID { get; set; }
    
    public void UpdateMusic(GamePlayer player)
    {
        if (player.InCombat)
            player.Out.SendRegionEnterSound(CombatMusicID);
        else if (WorldMgr.IsNight)
            player.Out.SendRegionEnterSound(NightMusicID);
        else
            player.Out.SendRegionEnterSound(DayMusicID);
    }
}
```

#### Music Transitions
- **Combat Start**: Immediate switch
- **Combat End**: Fade to ambient
- **Zone Change**: New zone theme
- **Time Change**: Day/night cycle

## System Integration

### Spell System
```csharp
// Spell sounds tied to effects
public override void SendSpellEffectAnimation()
{
    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.Out.SendSpellEffectAnimation(this, target, spell.ClientEffect, 
            boltDuration, noSound, success);
            
        if (!noSound)
            player.Out.SendPlaySound(eSoundType.Spell, spell.SoundID);
    }
}
```

### Combat System
- Hit sounds based on weapon
- Armor impact sounds
- Critical hit emphasis
- Miss/parry/block audio

### Crafting System
- Tool sounds per trade
- Success/failure audio
- Quality completion chimes
- Material-specific sounds

## Implementation Notes

### Client-Side Handling
- Sound files in MPK archives
- Client manages playback
- Server sends triggers only
- No streaming required

### Performance Considerations
```csharp
// Limit simultaneous sounds
const int MAX_CONCURRENT_SOUNDS = 32;

// Priority system
if (activeSounds.Count >= MAX_CONCURRENT_SOUNDS)
{
    RemoveLowestPriority();
}
```

### Network Optimization
- Sound IDs only (2 bytes)
- Batched area updates
- Client-side caching
- Minimal bandwidth usage

## Configuration

### Server Properties
```csharp
// Sound system settings
ENABLE_SOUND_SYSTEM = true
MAX_SOUND_DISTANCE = 5000
AMBIENT_MUSIC_ENABLED = true
COMBAT_MUSIC_ENABLED = true

// Instrument settings
INSTRUMENT_DEGRADATION_RATE = 0.1
UNIVERSAL_INSTRUMENTS = true // Post-1.97
```

### Volume Controls
- Master volume (client)
- Category volumes (client)
- Distance attenuation
- Directional audio (3D)

## Edge Cases

### Overlapping Songs
- Same type doesn't stack
- Highest value wins
- Different types combine
- Pulse timing preserved

### Instrument Loss
```csharp
// Instrument breaks during song
if (instrument.Condition <= 0)
{
    MessageToCaster("Your instrument breaks!", eChatType.CT_Important);
    CancelPulsingSpell(Caster, Spell.SpellType);
}
```

### Zone Transitions
- Songs continue if possible
- Ambient music switches
- Combat music resets
- Sound positions update

### Multiple Musicians
- Songs don't interfere
- Client handles mixing
- Server tracks separately
- Group coordination possible

## Test Scenarios

1. **Instrument Testing**
   - All songs require instruments
   - Duration scales properly
   - Quality affects performance
   - Condition degradation works

2. **Song Mechanics**
   - Pulses consume power
   - Movement doesn't interrupt
   - Combat interruption correct
   - Stacking rules enforced

3. **Sound Playback**
   - Range limitations work
   - Multiple sounds handled
   - Category volumes apply
   - Transitions smooth

4. **Edge Cases**
   - Instrument breaking
   - Zone transitions
   - Multiple musicians
   - Client reconnection

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 