# Sound & Music System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

The sound and music system manages all audio aspects of the game including spell sounds, ambient audio, instrument-based songs (Minstrel/Bard/Skald), combat audio, UI feedback, and environmental sounds. The system includes sophisticated instrument mechanics for musical classes and complex pulsing song effects.

## Core Architecture

### Sound Type Categories

#### Sound Classification
```csharp
public enum eSoundType
{
    Craft = 0x01,      // Crafting activities
    Ambient = 0x02,    // Environmental sounds
    Spell = 0x03,      // Magic effects
    Combat = 0x04,     // Weapon/combat sounds
    UserInterface = 0x05, // UI feedback
    Music = 0x06,      // Background music
    Divers = 0x07      // Miscellaneous sounds
}
```

#### Sound Trigger Sources
- **Spell Effects**: Each spell has unique sound ID
- **Combat Actions**: Weapon swings, impacts, blocks
- **Ambient Environment**: Zone-based atmosphere
- **Musical Instruments**: Player-controlled songs
- **User Interface**: Click sounds, notifications
- **Crafting Activities**: Tool-specific sounds

### Instrument System

#### Instrument Requirements
```csharp
// Instruments are weapon-type objects in active weapon slot
public bool CheckInstrument()
{
    DbInventoryItem instrument = Caster.ActiveWeapon;
    
    // Must be instrument object type
    if (instrument == null || 
        instrument.Object_Type != (int)eObjectType.Instrument)
        return false;
        
    return true;
}
```

#### Historical Instrument Classifications (Pre-1.97)
```csharp
// DPS_AF field indicated instrument class:
// 1 = Drum (Warsongs)
// 2 = Lute (Peace songs)  
// 3 = Flute (Travel songs)
// 4 = Any instrument (post-1.97 universal)
```

#### Current Universal System
- **Any Instrument**: Plays any song type (post-1.97)
- **Quality Impact**: Affects duration and effectiveness
- **Condition Degradation**: Reduces with use
- **Level Scaling**: Higher level instruments more effective

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
    public int PulsePower { get; set; }
}
```

#### Duration Calculation Enhancement
```csharp
protected override int CalculateEffectDuration(GameLiving target)
{
    double duration = Spell.Duration;
    
    // Base spell duration modifiers
    duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
    
    // Instrument enhancement (songs only)
    if (Spell.InstrumentRequirement != 0)
    {
        DbInventoryItem instrument = Caster.ActiveWeapon;
        if (instrument != null)
        {
            // Up to 200% duration for songs
            duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level);
            
            // Quality and condition penalties
            duration *= instrument.Condition / (double)instrument.MaxCondition;
            duration *= instrument.Quality / 100.0;
        }
    }
    
    return (int)duration;
}
```

### Pulsing Song System

#### Pulse Mechanics
```csharp
public virtual void OnSpellPulse(PulsingSpellEffect effect)
{
    // Verify instrument still equipped
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
    else
    {
        effect.Cancel(false);
    }
}
```

#### Song Categories by Effect Type

**Speed Songs**:
- Continuous movement buff
- Stacks with sprint but not mounts
- Movement doesn't interrupt

**Healing Songs**:
- Periodic health restoration
- Lower healing rate but continuous
- Can be maintained while moving

**Mana Songs**:
- Power regeneration enhancement
- Percentage-based restoration
- Group and self variants

**Damage Songs**:
- Offensive pulse effects
- Area effect variants available
- Direct damage over time

**Utility Songs**:
- Resistance buffs
- Stat enhancements
- Crowd control effects

### Song Interruption Rules

#### Movement Interaction
```csharp
// Songs are NOT interrupted by movement (key difference from spells)
public virtual void CasterMoves()
{
    if (Spell.InstrumentRequirement != 0)
        return; // Songs allow movement
        
    if (Spell.MoveCast)
        return;
        
    InterruptCasting(); // Only interrupts regular spells
}
```

#### Interruption Conditions
- **Combat Damage**: Standard spell interruption
- **Mezz/Stun Effects**: Stops song playing
- **Instrument Switch**: Cancels active song
- **Power Depletion**: Automatic cancellation
- **Manual Cancellation**: Player command

#### Special Song Properties
```csharp
// Flute mezz can pulse while moving (special case)
if (newSpell.SpellType == eSpellType.Mesmerize && 
    newSpell.InstrumentRequirement != 0)
{
    // Special handling for instrument mezz
    // Can pulse while moving
}
```

### Sound Playback System

#### Sound Emission
```csharp
public void PlaySound(GamePlayer player, ushort soundID, eSoundType type)
{
    player.Out.SendPlaySound(type, soundID);
}

// Area sounds for spell effects
foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
{
    player.Out.SendPlaySound(eSoundType.Spell, spellSoundID);
}
```

#### Sound Properties
- **Unique ID**: Each sound has specific identifier
- **Category Type**: Volume control grouping
- **Range Limitation**: Area of effect radius
- **Priority System**: Handle overlapping sounds

### Instrument Slot Management

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

#### Equipment Restrictions
- **Two-Handed Only**: Instruments use both hands
- **No Shield**: Cannot equip shield while playing
- **Switch Penalties**: Weapon switching cancels songs
- **Quick-Swap Limitations**: Reduced effectiveness

### Ambient Music System

#### Zone Music Management
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

#### Music Transition Rules
- **Combat Start**: Immediate switch to combat theme
- **Combat End**: Fade to appropriate ambient
- **Zone Changes**: New zone theme loading
- **Day/Night Cycle**: Automatic atmosphere switching

## System Integration

### Spell System Integration
```csharp
// Every spell effect can have associated sound
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

### Combat System Integration
- **Hit Sounds**: Based on weapon type and material
- **Armor Impact**: Different sounds per armor type
- **Critical Emphasis**: Enhanced audio for critical hits
- **Defense Audio**: Miss/parry/block specific sounds

### Crafting System Integration
- **Tool Sounds**: Unique audio per trade skill
- **Success/Failure**: Audio feedback for outcomes
- **Quality Completion**: Chimes for exceptional results
- **Material-Specific**: Different sounds per material type

## Performance & Optimization

### Client-Side Handling
- **MPK Archives**: Sound files stored in game packages
- **Client Playback**: Audio managed by game client
- **Server Triggers**: Only sound IDs transmitted
- **No Streaming**: All audio files local to client

### Network Optimization
```csharp
// Efficient sound packet structure
const int MAX_CONCURRENT_SOUNDS = 32;

// Priority system for sound management
if (activeSounds.Count >= MAX_CONCURRENT_SOUNDS)
{
    RemoveLowestPriority();
}
```

### Bandwidth Considerations
- **Sound IDs Only**: 2 bytes per sound trigger
- **Batched Updates**: Multiple sounds in single packet
- **Client Caching**: Reduce redundant transmissions
- **Range Culling**: Only send sounds within range

## Special Music Features

### Bard/Minstrel/Skald Songs

#### Movement Enhancement
```csharp
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

#### Class-Specific Abilities
- **Minstrels**: Crowd control songs, stealth
- **Bards**: Damage songs, group regeneration
- **Skalds**: Enhancement songs, melee support

### Charm Song Mechanics
- **Duration**: Enhanced by instrument quality
- **Power Cost**: Per pulse consumption
- **Range**: Line of sight required for maintenance
- **Breaking**: Standard charm immunity rules apply

## Configuration System

### Server Properties
```csharp
// Sound system configuration
ENABLE_SOUND_SYSTEM = true
MAX_SOUND_DISTANCE = 5000
AMBIENT_MUSIC_ENABLED = true
COMBAT_MUSIC_ENABLED = true

// Instrument settings
INSTRUMENT_DEGRADATION_RATE = 0.1
UNIVERSAL_INSTRUMENTS = true // Post-1.97 setting
```

### Volume Controls
- **Master Volume**: Client-controlled overall level
- **Category Volumes**: Separate controls per sound type
- **Distance Attenuation**: Automatic volume reduction
- **Directional Audio**: 3D positioning support

## Edge Cases & Special Handling

### Overlapping Songs
- **Same Type**: Doesn't stack, highest value wins
- **Different Types**: Can combine effects
- **Pulse Timing**: Independent timing preservation
- **Caster Limitations**: One song per musician

### Instrument Loss Scenarios
```csharp
// Instrument breaks during performance
if (instrument.Condition <= 0)
{
    MessageToCaster("Your instrument breaks!", eChatType.CT_Important);
    CancelPulsingSpell(Caster, Spell.SpellType);
}
```

### Zone Transition Handling
- **Song Continuity**: Attempts to maintain songs across zones
- **Ambient Switching**: Immediate music environment change
- **Combat Reset**: Combat music state refreshed
- **Position Updates**: 3D audio positioning corrected

### Multiple Musicians
- **Independent Songs**: No interference between musicians
- **Client Mixing**: Audio engine handles multiple sources
- **Server Tracking**: Separate effect tracking per caster
- **Group Coordination**: Potential for synchronized effects

## Test Scenarios

### Instrument Validation
1. **Requirement Checking**: All songs require proper instruments
2. **Duration Scaling**: Quality and level affect performance
3. **Condition Impact**: Degradation reduces effectiveness
4. **Breaking Consequences**: Instrument destruction handling

### Song Mechanics Testing
1. **Pulse Consumption**: Power usage per pulse cycle
2. **Movement Tolerance**: Songs continue while moving
3. **Interruption Rules**: Combat stops casting, not songs
4. **Stacking Validation**: Proper effect combination rules

### Audio System Testing
1. **Range Limitations**: Sounds cut off at proper distances
2. **Concurrent Handling**: Multiple simultaneous sounds
3. **Volume Application**: Category-based volume controls
4. **Transition Smoothness**: No audio artifacts during changes

### Edge Case Coverage
1. **Instrument Failure**: Mid-song equipment breaking
2. **Zone Boundaries**: Seamless or appropriate transitions
3. **Multiple Musicians**: No conflicts or interference
4. **Client Reconnection**: Audio state restoration

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| 1.97 | Game | Universal instrument system implemented |
| Original | Game | Class-specific instrument requirements | 