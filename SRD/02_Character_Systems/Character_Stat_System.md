# Character Stat System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The character stat system manages eight primary attributes that affect combat effectiveness, spell power, and survivability. Stats are determined by race, increased through leveling, and modified by items, buffs, and abilities.

## Core Mechanics

### Primary Stats

#### The Eight Stats
1. **Strength (STR)** - Melee damage, encumbrance
2. **Constitution (CON)** - Hit points, resists
3. **Dexterity (DEX)** - Weapon skill, casting speed
4. **Quickness (QUI)** - Attack speed, evade chance
5. **Intelligence (INT)** - Spell damage (Albion)
6. **Piety (PIE)** - Spell damage (Albion/Midgard)
7. **Empathy (EMP)** - Spell damage (Hibernia)
8. **Charisma (CHA)** - Reserved for future use

### Base Stats by Race

#### Albion Races
| Race | STR | CON | DEX | QUI | INT | PIE | EMP | CHA |
|------|-----|-----|-----|-----|-----|-----|-----|-----|
| Briton | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 |
| Avalonian | 45 | 45 | 60 | 70 | 80 | 60 | 60 | 60 |
| Highlander | 70 | 70 | 50 | 50 | 60 | 60 | 60 | 60 |
| Saracen | 50 | 50 | 80 | 60 | 60 | 60 | 60 | 60 |
| Inconnu | 50 | 60 | 70 | 50 | 70 | 60 | 60 | 60 |
| Half Ogre | 90 | 70 | 40 | 40 | 60 | 60 | 60 | 60 |

#### Midgard Races
| Race | STR | CON | DEX | QUI | INT | PIE | EMP | CHA |
|------|-----|-----|-----|-----|-----|-----|-----|-----|
| Norseman | 70 | 70 | 50 | 50 | 60 | 60 | 60 | 60 |
| Troll | 100 | 70 | 35 | 35 | 60 | 60 | 60 | 60 |
| Dwarf | 60 | 80 | 50 | 50 | 60 | 60 | 60 | 60 |
| Kobold | 50 | 50 | 70 | 70 | 60 | 60 | 60 | 60 |
| Valkyn | 55 | 45 | 65 | 75 | 60 | 60 | 60 | 60 |
| Frostalf | 55 | 55 | 55 | 60 | 60 | 75 | 60 | 60 |

#### Hibernia Races
| Race | STR | CON | DEX | QUI | INT | PIE | EMP | CHA |
|------|-----|-----|-----|-----|-----|-----|-----|-----|
| Celt | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 |
| Firbolg | 90 | 60 | 40 | 40 | 60 | 60 | 70 | 60 |
| Elf | 40 | 40 | 75 | 75 | 70 | 60 | 60 | 60 |
| Lurikeen | 40 | 40 | 80 | 80 | 60 | 60 | 60 | 60 |
| Sylvan | 70 | 60 | 55 | 45 | 70 | 60 | 60 | 60 |
| Shar | 60 | 80 | 50 | 50 | 60 | 60 | 60 | 60 |

### Stat Progression

#### Level-Based Gains
Stats begin increasing at **level 6**:

```csharp
// Primary stat: Every level starting at 6
if (level >= 6 && PrimaryStat != eStat.UNDEFINED)
    player.ChangeBaseStat(PrimaryStat, 1);

// Secondary stat: Every 2 levels (6, 8, 10, 12...)
if (level >= 6 && SecondaryStat != eStat.UNDEFINED && ((level - 6) % 2 == 0))
    player.ChangeBaseStat(SecondaryStat, 1);

// Tertiary stat: Every 3 levels (6, 9, 12, 15...)
if (level >= 6 && TertiaryStat != eStat.UNDEFINED && ((level - 6) % 3 == 0))
    player.ChangeBaseStat(TertiaryStat, 1);
```

#### Total Gains (Level 1-50)
- **Primary Stat**: +45 points (levels 6-50)
- **Secondary Stat**: +22 points
- **Tertiary Stat**: +15 points

### Class Stat Assignment

#### Primary/Secondary/Tertiary by Class
| Class | Primary | Secondary | Tertiary | Mana Stat |
|-------|---------|-----------|----------|-----------|
| **Albion** |
| Armsman | STR | CON | DEX | - |
| Cleric | PIE | CON | STR | PIE |
| Mercenary | STR | DEX | CON | - |
| Paladin | STR | CON | DEX | PIE |
| Scout | DEX | QUI | STR | DEX |
| Minstrel | CHR | STR | CON | CHR |
| Infiltrator | DEX | QUI | STR | - |
| **Midgard** |
| Warrior | STR | CON | DEX | - |
| Berserker | STR | DEX | CON | - |
| Thane | STR | PIE | CON | PIE |
| Healer | PIE | CON | STR | PIE |
| Hunter | DEX | QUI | STR | DEX |
| Shadowblade | DEX | QUI | STR | - |
| **Hibernia** |
| Hero | STR | CON | DEX | - |
| Champion | STR | INT | DEX | INT |
| Ranger | DEX | QUI | STR | DEX |
| Nightshade | DEX | QUI | STR | - |
| Druid | EMP | CON | STR | EMP |
| Bard | CHR | EMP | CON | EMP |

### Stat Effects

#### Strength
- **Melee Damage**: Damage bonus = STR * 0.01
- **Encumbrance**: Max weight = STR * 8
- **Weapon Skill**: Affects melee accuracy

#### Constitution
- **Hit Points**: HP bonus = CON * HP_per_CON (varies by level)
- **Resist Rates**: Improves resist chances
- **Fatigue Recovery**: Slightly improves recovery

#### Dexterity
- **Casting Speed**: DEX affects spell casting time
- **Archery Damage**: DEX * 0.01 damage bonus (archers)
- **Shield Spec**: Increases block chance

#### Quickness
- **Attack Speed**: Reduces weapon swing time
- **Evade Chance**: Base evade component
- **Casting Speed**: Secondary effect

#### Intelligence/Piety/Empathy
- **Spell Damage**: Mana_Stat * 0.01 damage bonus
- **Power Pool**: Larger mana pool
- **Spell Effectiveness**: Various spell effects

### Stat Calculation

#### Base Formula
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    // Get base stat from race + level gains
    int baseStat = living.GetBaseStat((eStat)property);
    
    // Add item bonuses (capped)
    int itemBonus = CalcValueFromItems(living, property);
    
    // Add buff bonuses
    int buffBonus = CalcValueFromBuffs(living, property);
    
    // Subtract debuffs
    int baseDebuff = Math.Abs(living.DebuffCategory[property]);
    int specDebuff = Math.Abs(living.SpecDebuffCategory[property]);
    
    // Apply debuff effectiveness
    ApplyDebuffs(ref baseDebuff, ref specDebuff, ref buffBonus, ref baseStat);
    
    // Add ability bonuses (realm abilities)
    int abilityBonus = living.AbilityBonus[property];
    
    // Calculate final stat
    int stat = baseStat + itemBonus + buffBonus + abilityBonus;
    
    // Apply multiplicative bonuses
    stat *= living.BuffBonusMultCategory1.Get((int)property);
    
    // Minimum stat is 1
    return Math.Max(1, stat);
}
```

#### Debuff Effectiveness
- **vs Base/Item Stats**: 50% effectiveness
- **vs Buffs**: 100% effectiveness

### Stat Caps

#### Item Bonus Caps
Item stat bonuses are capped based on level:
- Levels 1-14: +0
- Levels 15-19: +5
- Levels 20-24: +10
- Levels 25-29: +15
- Levels 30-34: +20
- Levels 35-39: +25
- Levels 40-44: +30
- Levels 45-50: +35

#### Total Stat Caps
- **Base + Items**: Capped by item bonus limits
- **Buffs**: Separate cap (typically +62-85)
- **Realm Abilities**: Usually uncapped
- **Total Maximum**: ~200-250 depending on class/race

### Death Penalty

#### Constitution Loss
- **PvE Death**: Lose constitution based on level
- **Recovery**: Regain through NPC healers
- **Effect**: Reduced max HP, resists

```csharp
public virtual int TotalConstitutionLostAtDeath
{
    get { return DBCharacter.ConLostAtDeath; }
    set { DBCharacter.ConLostAtDeath = value; }
}
```

## Implementation Notes

### Stat Storage
```csharp
// Stats stored in character database
[DataElement(AllowDbNull = false)]
public int Strength { get; set; }
public int Constitution { get; set; }
public int Dexterity { get; set; }
public int Quickness { get; set; }
public int Intelligence { get; set; }
public int Piety { get; set; }
public int Empathy { get; set; }
public int Charisma { get; set; }
```

### Stat Modification
```csharp
public override void ChangeBaseStat(eStat stat, short val)
{
    int oldstat = GetBaseStat(stat);
    base.ChangeBaseStat(stat, val);
    int newstat = GetBaseStat(stat);
    
    // Always positive and not null
    if (newstat < 1) newstat = 1;
    
    // Update database
    switch (stat)
    {
        case eStat.STR: character.Strength = newstat; break;
        case eStat.DEX: character.Dexterity = newstat; break;
        // ... etc
    }
}
```

### NPC Stats
NPCs use simplified stat system:
```csharp
public virtual void AutoSetStats(DbMob dbMob = null)
{
    Strength = Properties.PET_AUTOSET_STR_BASE;
    Constitution = Properties.PET_AUTOSET_CON_BASE;
    
    if (Level > 1)
    {
        Strength += (short)(levelMinusOne * Properties.PET_AUTOSET_STR_MULTIPLIER);
        Constitution += (short)(levelMinusOne * Properties.PET_AUTOSET_CON_MULTIPLIER);
    }
}
```

## System Interactions

### Combat System
- STR/DEX affect weapon damage
- QUI determines attack speed
- CON provides survivability

### Spell System
- INT/PIE/EMP determine spell damage
- DEX affects casting speed
- Mana stat determines power pool

### Item System
- Items provide stat bonuses
- Bonuses capped by level
- Requirements may include minimum stats

### Buff System
- Stat buffs common (Str/Con, Dex/Qui)
- Base and spec buffs stack
- Debuffs reduce effectiveness

## Test Scenarios

1. **Base Stats**
   - Verify race starting stats
   - Check stat display
   - Confirm minimum values

2. **Level Progression**
   - Test stat gains at levels
   - Verify primary/secondary/tertiary
   - Check retroactive gains

3. **Buff/Debuff**
   - Apply stat buffs
   - Test debuff effectiveness
   - Verify stacking rules

4. **Item Bonuses**
   - Equip stat items
   - Check cap enforcement
   - Test bonus calculations

## Edge Cases

### Stat Overflow
- Stats capped at reasonable values
- Prevent negative stats
- Handle extreme buffs/debuffs

### Death Recovery
- Constitution loss tracking
- Recovery mechanics
- Penalty calculations

### Class Changes
- Stat reassignment on respec
- Proper stat progression
- Mana stat updates

## TODO
- Document acuity (combined stat) mechanics
- Add mythical stat bonuses details
- Clarify champion level stat gains
- Detail realm rank stat bonuses 