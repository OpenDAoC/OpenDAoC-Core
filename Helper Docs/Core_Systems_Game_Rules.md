# OpenDAoC Core Systems - Game Rules Documentation

## Table of Contents
1. [Combat System](#combat-system)
2. [Character Progression](#character-progression)
3. [Class System](#class-system)
4. [Item and Equipment System](#item-and-equipment-system)
5. [Property Calculator System](#property-calculator-system)
6. [Realm and Faction System](#realm-and-faction-system)
7. [Guild System](#guild-system)
8. [Housing System](#housing-system)
9. [Keep and Siege System](#keep-and-siege-system)
10. [Crafting System](#crafting-system)
11. [Quest and Task System](#quest-and-task-system)
12. [Magic System](#magic-system)

## Combat System

### Attack Resolution
The combat system follows this order of checks:
1. **Range Check**: Verify target is within weapon range
2. **Line of Sight Check**: Ensure clear path to target  
3. **Defense Checks** (if not disabled):
   - Evade
   - Parry (melee only)
   - Block
   - Guard
4. **Hit/Miss Calculation**
5. **Fumble Check** (subset of miss)
6. **Damage Calculation** (if hit)
7. **Critical Strike Check**
8. **Damage Application**

### Base Damage Calculation

Base damage is calculated using multiple factors:

1. **Weapon Damage**:
   ```
   BaseDamage = WeaponDPS * Speed * SlowWeaponModifier
   SlowWeaponModifier = 1 + (WeaponSpeed - 2.0) * 0.03
   ```

2. **Stat Damage Bonus**:
   - Strength for melee (Str/1.2 capped at level+1)
   - Dexterity for ranged
   
3. **Two-Handed Weapon Bonus**:
   ```
   TwoHandBonus = 1.1 + (SpecLevel * 0.005)
   ```

4. **Damage Cap**:
   ```
   DamageCap = WeaponSkill * EnemyArmor * 3 * ServerModifier
   ```

### Damage Modifiers

1. **Weapon Skill Modifier**:
   ```
   WeaponSkill = BaseWeaponSkill * RelicBonus * SpecModifier
   BaseWeaponSkill = PlayerWeaponSkill + 90.68 (inherent skill)
   SpecModifier = 1 + Variance * (SpecLevel - TargetLevel) * 0.01
   Variance = 0.25 to 1.25 based on spec level
   ```

2. **Armor Factor Modifier**:
   ```
   ArmorMod = ArmorFactor / (1 - ArmorAbsorb)
   ArmorFactor = TargetAF + 20 (inherent AF) + (Level * 20 / 50) for players
   DamageMod = WeaponSkill / ArmorMod
   ```

3. **Damage Type Modifiers**:
   - Melee damage bonus: +X% from items/buffs
   - Ranged damage bonus: +X% from items/buffs
   - Spell damage bonus: +X% for certain archery types
   - PvP damage modifier: Configurable server setting (PVP_MELEE_DAMAGE)
   - PvE damage modifier: Configurable server setting (PVE_MELEE_DAMAGE)

4. **Critical Hits**:
   - **Chance**: From items/abilities (eProperty.CriticalMeleeHitChance or CriticalArcheryHitChance)
   - **Damage Range**:
     - Players: 10% to 100% of base damage vs NPCs, 10% to 50% vs players
     - NPCs: Same ranges (10-100% vs NPCs, 10-50% vs players)
   - **Berserk Modifiers**:
     - Level 1: 10-25% of base damage
     - Level 2: 10-50% of base damage
     - Level 3: 10-75% of base damage
     - Level 4: 10-99% of base damage
   - **Special Cases**:
     - Critical Shot (archery) cannot critically hit
     - Triple Wield prevents receiving critical hits

5. **Two-Handed Weapon Bonus**: Scales with specialization level
6. **Left-Hand (Dual Wield) Damage**: 62.5% base + 0.34% per Left Axe spec point
7. **Defense Penetration**: WeaponSkill * 0.08 / 100

### Defense Mechanics

#### Evade
```
Base Evade = ((Dex + Qui) / 2 - 50) * 0.05 + EvadeAbilityLevel * 5
```
- Modified by buffs/debuffs/items
- Reduced by 3% per additional attacker
- Divided by 5 against ranged attacks
- 50% cap in RvR only (EVADE_CAP property)
- Only works from front (180 degree arc) unless Advanced Evade

#### Parry
```
Base Parry = (Dex * 2 - 100) / 40 + ParrySpec / 2 + MasteryOfParry * 3 + 5
```
- Modified by buffs/debuffs/items
- Divided by (attackerCount + 1) / 2
- Halved vs two-handed weapons
- 50% cap in RvR only (PARRY_CAP property)
- Only works from front (120 degree arc)
- Requires weapon equipped (not bows)

#### Block
```
Base Block = 5% + 0.5% * ShieldSpec
```
- Modified by buffs/debuffs/items (Dex, Mastery of Blocking)
- Requires shield equipped
- Only works from front (120 degree arc) unless Engage
- Engage: Works 360 degrees vs single target, normal arc vs others
- Block cap depends on shield size:
  - Small Shield: 45% + 5% per tier = 60% max
  - Medium Shield: 50% + 5% per tier = 65% max
  - Large Shield: 60% + 5% per tier = 75% max

#### Fumble Mechanics
- **Base Chance**: Max(51 - level, debuffs + abilities) / 1000
  - Level 1: 5.0% chance
  - Level 50: 0.1% chance
- **Only applies to melee attacks** (ranged cannot fumble)
- **Fumbles are a subset of misses** - can't fumble without missing
- **Miss chance is adjusted** to be at least fumble chance
- **On Fumble**:
  - Attack interval is doubled
  - Combat styles are cleared
- **Spell Fumble**: Separate mechanic, capped at 100%

### Hit/Miss Calculation

#### Base Miss Chance
```
Miss% = 15% + LevelDifference * 0.33% + ArmorBonus
```
- **Level Difference**: Target Level - Attacker Level (min 0)
- **Armor Bonus**: 
  - PvP: Flat addition to miss chance
  - PvE: Percentage increase of miss chance

#### Style Modifiers
- Style BonusToHit: Reduces miss chance
- Previous style BonusToDefense: Increases miss chance

#### Ammo Modifiers (Archery)
Arrow/bolt quality affects accuracy:
- Rough: +15% miss chance
- Standard: No modifier
- Footed: -25% miss chance

#### Special Cases
- **Strafing**: 30% chance to force a miss (PvP only)
- **Bladeturn**: Can deflect attacks based on level difference

### Armor System

#### Armor Factor (AF)
- **Base Item AF**: Capped at 2 * level (1 * level for cloth)
- **Inherent AF**: +12.5 for all calculations
- **Player Bonus**: +Level * 20 / 50 (level 50 = +20 AF)
- **Quality/Condition**: AF * Quality% * Condition%
- **Buffs**: 
  - Base buffs capped with item AF
  - Spec buffs capped at Level * 1.875
  - Item bonuses capped at Level
  
#### Armor Absorption
Base absorption by armor type:
- Cloth: 0%
- Leather: 10%
- Reinforced/Studded: 19%
- Scale/Chain: 27%
- Plate: 34%

**Modifiers**:
- +X% from items/abilities (capped at 50%)
- Cannot go below 0% even with debuffs
- NPCs: Base = Level * 0.0054 (27% at level 50)
- Necro pets: Base = Owner Level * 0.0068 (34% at level 50)

#### Armor Effectiveness Formula
```
EffectiveArmor = ArmorFactor / (1 - Absorption)
```
At 100% absorption, armor provides infinite protection.

### Attack Speed and Timing

#### Base Attack Speed
Weapon speed in milliseconds = SPD_ABS * 100

#### Player Speed Modifiers
**Melee Weapons**:
```
Speed * (1 - (Quickness - 60) * 0.002) * MeleeSpeed / 100
```
- Minimum: 1500ms (1.5 seconds)

**Ranged Weapons (Old Archery)**:
```
Speed * (1 - (Quickness - 60) * 0.002) - (Speed * ArcherySpeed / 100)
```
- Critical Shot: Speed * 2 - (AbilityLevel - 1) * Speed / 10
- Rapid Fire: Speed * 0.5 (minimum 900ms)

**Ranged Weapons (New Archery)**:
Uses Casting Speed instead of Archery Speed

#### NPC Speed
- One-hand weapons: 3.0 seconds
- Two-hand weapons: 4.0 seconds
- Ranged weapons: 4.5 seconds
- Modified by: Speed * MeleeSpeed / 100

#### Special Timing Rules
- **Fumble**: Doubles next attack interval
- **Self-interrupt on melee**: Half of attack speed
- **Ranged to melee switch**: 750ms minimum delay
- **Non-attack rounds**: 100ms tick interval

## Character Progression

### Experience System

#### Experience Calculation
Experience gain is based on several factors:

**Base Experience**:
```
BaseXP = MobType.BaseXP * MobLevel
```

**Level Difference Modifiers**:
- Same level: 100% XP
- Higher level mobs: +15% per level above player
- Lower level mobs: -10% per level below player (minimum 5%)

**Group Experience**:
- Group bonus: +5% per additional group member
- Level spread penalty if level difference > 8 within group
- Experience shared based on level contribution

**Challenge/Camp Bonuses**:
- Challenge bonus: Additional XP for difficult encounters
- Camp bonus: Increased XP for extended hunting in same area

**Death Penalty**:
- Constitution loss on death (-5% per death, max -50%)
- Experience debt system (optional per server config)

#### Experience to Level
Experience requirements increase exponentially:
```
XPToLevel(n) = BaseXP * (Level^ExperienceMultiplier)
```
Where ExperienceMultiplier varies by server configuration.

### Stat Progression

#### Base Stat Increases
**Primary Stat** (highest class focus):
- +1 per level starting at level 6
- Total: +45 stats at level 50

**Secondary Stat** (moderate class focus):
- +1 every 2 levels starting at level 6
- Total: +22 stats at level 50

**Tertiary Stat** (minor class focus):
- +1 every 3 levels starting at level 6
- Total: +14 stats at level 50

#### Starting Stats
Starting stats are race and class dependent:
- **Race bonuses**: Each race has stat modifiers
- **Class bonuses**: Base stats vary by class type
- **Point allocation**: Limited point buy system

#### Stat Caps
**Base Stats**: 101 (base 50 + 51 from items/buffs)
**Acuity Bonus**: Additional mana stat from items
**Hit Points**: Constitution-based, class modifier applies
**Power**: Mana stat-based, class modifier applies

### Specialization Points

#### Specialization Point Gain
```
SpecPointsPerLevel = Level * ClassSpecMultiplier / 10
ClassSpecMultiplier = 10-20 (varies by class)
```

**Examples**:
- Fighter classes: Usually 10 multiplier (2 points/level at level 20)
- Hybrid classes: Usually 15 multiplier (3 points/level at level 20)
- Pure casters: Usually 20 multiplier (4 points/level at level 20)

#### Specialization Cost
Cost to train specialization points:
```
CostToLevel = Sum(1 to TargetLevel) of level
```
- Level 1: 1 point
- Level 10: 55 points total
- Level 25: 325 points total
- Level 50: 1275 points total

#### Composite Specialization
Some specializations are composite (combine multiple skills):
- Combined training cost
- Shared training pool
- Cross-spec bonuses

### Skill Point Allocation

#### Skill Point Gain
```
SkillPointsPerLevel = BaseSkillPoints + (Level - 1) * SkillMultiplier
```
Where SkillMultiplier varies by class (typically 2-4).

#### Skill Types
**Weapon Skills**:
- Trained with skill points
- Cap at 5 * (level + 1)
- Modified by specialization bonus

**Magic Skills**:
- Trained with specialization points
- Baseline + specialization level
- Focus bonus from items

**Other Skills**:
- Class abilities (Evade, Parry, etc.)
- Craft skills
- Language skills

### Champion Levels (Post-50 Progression)

#### Champion Experience
Separate experience pool for levels 51-60:
- Much higher XP requirements
- PvE and RvR sources
- Diminishing returns after 55

#### Champion Benefits
**Champion Level 1-5**:
- +1 Specialization point per level
- +5 Hit points per level
- Access to champion abilities

**Champion Level 6-10**:
- +2 Specialization points per level
- +10 Hit points per level
- Enhanced champion abilities

#### Champion Abilities
- **ML 1-5**: First champion ability line
- **ML 6-10**: Second champion ability line
- **Prerequisites**: Must complete champion quest line

### Realm Rank Progression

#### Realm Point Gain
Realm points awarded for:
- Player kills (modified by level difference)
- Keep captures and defenses
- Relic captures
- Tower/outpost captures

#### Realm Rank Benefits
**Realm Ranks 1-5**:
- +1 to all resists per rank
- +5% Hit points per rank
- Access to realm abilities

**Realm Ranks 6-10**:
- +2 to all resists per rank
- +10% Hit points per rank
- Higher tier realm abilities

**Realm Ranks 11-13**:
- +3 to all resists per rank
- +15% Hit points per rank
- Master level realm abilities

#### Realm Abilities
**Passive Abilities**:
- Augmented stats (Aug STR, Aug CON, etc.)
- Resistances (Magic Resistance, etc.)
- Regeneration (Toughness, etc.)

**Active Abilities**:
- Realm rank 5+ abilities
- Long cooldowns (5-30 minutes)
- Powerful effects for RvR

### Master Level Progression

#### Master Level Steps
Each Master Level (1-10) requires:
1. **Credit farming**: Kill specific encounters
2. **Artifact encounter**: Defeat ML boss
3. **Step completion**: Various requirements

#### Master Level Benefits
**ML 1-5**:
- Primary ML line abilities
- Stat bonuses
- Special item abilities

**ML 6-10**:
- Secondary ML line abilities
- Enhanced stat bonuses
- Powerful capstone abilities

## Class System

### Base Classes
**Albion**:
- Fighter → Armsman, Mercenary, Paladin, Reaver
- Acolyte → Cleric, Friar, Heretic  
- Rogue → Infiltrator, Minstrel, Scout
- Elementalist → Theurgist, Wizard
- Mage → Cabalist, Necromancer, Sorcerer

**Midgard**:
- Viking → Berserker, Savage, Skald, Thane, Valkyrie, Warrior
- Mystic → Bonedancer, Runemaster, Spiritmaster, Warlock
- Seer → Healer, Shaman
- Rogue → Hunter, Shadowblade

**Hibernia**:
- Guardian → Blademaster, Champion, Hero
- Naturalist → Druid, Warden
- Stalker → Nightshade, Ranger, Vampiir
- Magician → Eldritch, Enchanter, Mentalist
- Forester → Animist, Valewalker

### Class Properties
- **Mana stat**: Determines power pool (INT/PIE/EMP/MAN)
- **Weapon skill base**: 280-440 depending on class
- **Base HP**: 560-920 depending on class  
- **Armor restrictions**: Cloth/Leather/Studded/Chain/Plate
- **Specialization multiplier**: 10-20
- **Class type**: Tank/Caster/Hybrid/Stealth

### Abilities
- Class-specific abilities (Evade, Parry, etc.)
- Realm abilities (purchased with realm points)
- Champion abilities (from champion levels)
- Master level abilities

## Item and Equipment System

### Item Properties
- **Quality**: 85-100% affects damage/armor
- **Condition**: Degrades with use, affects effectiveness
- **Durability**: Maximum condition
- **DPS/AF**: Base damage/armor values
- **Speed**: Weapon attack speed (in tenths of seconds)
- **Bonus level**: 0-50, determines stat caps

### Bonus System
- **Item bonuses**: Stats, resists, skills
- **Bonus caps by level**:
  - Level 1-14: 0
  - Level 15-19: 5
  - Level 20-24: 10
  - Level 25-29: 15
  - Level 30-34: 20
  - Level 35-39: 25
  - Level 40-44: 30
  - Level 45+: 35

### Equipment Slots
- Right Hand (weapon)
- Left Hand (shield/weapon)
- Two Hand (2H weapon)
- Distance (ranged)
- Helm, Gloves, Boots, Chest, Legs, Arms, Cloak
- Neck, Jewel, Belt, Ring x2, Bracer x2
- Mythical slots

### Item Generation
- Random modifiers (ROG system)
- Unique items with special properties
- Artifacts with leveling system
- Crafted items with quality bonuses

## Property Calculator System

### Calculator Architecture

#### Base Calculator Interface
All property calculators implement `IPropertyCalculator`:
```csharp
public interface IPropertyCalculator
{
    int CalcValue(GameLiving living, eProperty property);
    int CalcValueBase(GameLiving living, eProperty property);
}
```

#### Property Sources
Property values are calculated from multiple sources:
- **Base values**: Character stats, skill levels
- **Item bonuses**: Equipment stat bonuses
- **Buff bonuses**: Spell effects (base and specialization categories)
- **Debuff penalties**: Negative spell effects
- **Ability bonuses**: Realm abilities, class abilities
- **Other bonuses**: Realm bonuses, guild bonuses

### Core Property Calculators

#### Armor Factor Calculator
Calculates effective armor factor based on target type:

**Players**:
```
ArmorFactor = Min(Level * 1.875, SpecBuffBonus) + 
              Min(Level, ItemBonus) + 
              OtherBonus - 
              Abs(DebuffPenalty)
```

**NPCs**:
```
ArmorFactor = ((1 + Level/Divisor) * (Level * Factor)) + 
              BaseBuffBonus - 
              Abs(DebuffPenalty) + 
              OtherBonus
```

**Special Cases**:
- **GameKeep Components**: `KeepLevel * KeepMod * TypeMod`
- **Necromancer Pets**: Level 50 equivalent (121 AF)
- **Summoned Pets**: Enhanced AF (175)

#### Armor Absorption Calculator
```
Absorption = BaseBuffBonus + ItemBonus + AbilityBonus - Abs(DebuffPenalty)
Maximum = 50% (hard cap)
```

**NPC Base Absorption**:
- Standard NPCs: `Level * 0.0054` (27% at level 50)
- Necromancer pets: `OwnerLevel * 0.0068` (34% at level 50)

#### Damage Modifier Calculators

**Melee Damage Calculator**:
```
MeleeDamage = AbilityBonus + BuffBonus + Min(10, ItemBonus) - Min(10, Abs(DebuffPenalty))
```

**Ranged Damage Calculator**:
- Uses identical formula to melee damage
- Separate property tracking

**Critical Hit Calculators**:
```
CriticalChance = ItemBonus + BuffBonus
Maximum = 50% (hard cap)
```

#### Resistance Calculators
Each resistance type (Heat, Cold, Matter, Body, Spirit, Energy) has a dedicated calculator:

**Primary Resistance**:
```
Resistance = BaseValue + ItemBonus + BuffBonus - DebuffPenalty
Maximum = 70% (configurable per server)
```

**Secondary Resistance**:
```
SecondaryResist = SpecBuffBonusCategory[ResistType]
Maximum = 80% (uncapped in most cases)
```

#### Stat Calculators
For each primary stat (STR, CON, DEX, QUI, INT, PIE, EMP, CHA):

**Base Calculation**:
```
FinalStat = BaseStat + 
            ItemBonus + 
            BaseBuffBonus + 
            SpecBuffBonus - 
            DebuffPenalty + 
            OtherBonus
```

**Acuity (Mana Stat) Calculation**:
```
Acuity = BaseManastat + AcuityBonus + ItemBonus + BuffBonus - DebuffPenalty
```

#### Defense Calculators

**Parry Chance Calculator**:
```
ParryChance = ((Dexterity * 2 - 100) / 4) + 
              ((ParrySpec - 1) * 5) + 
              MasteryOfParry * 3 + 
              50
```

**Block Chance Calculator**:
```
BaseBlock = 5% + 0.5% * ShieldSpec
Modified by: Dexterity, Mastery of Blocking, shield size
```

**Evade Chance Calculator**:
```
BaseEvade = ((Dex + Qui) / 2 - 50) * 0.05 + EvadeLevel * 5
```

#### Speed Calculators

**Melee Speed Calculator**:
```
FinalSpeed = BaseSpeed * (1 - (Quickness - 60) * 0.002) * MeleeSpeedModifier / 100
Minimum = 1500ms
```

**Casting Speed Calculator**:
```
CastTime = Spell.CastTime * DexterityModifier * BonusModifier
DexterityModifier = 1 - (Dexterity - 60) / 600
BonusModifier = 1 - CastingSpeedBonus * 0.01
Minimum = 40% of base cast time
```

### Bonus Categories and Stacking

#### Buff Bonus Categories
**Category 1 (BaseBuffBonusCategory)**:
- Single stat buffs
- Base enhancement spells
- Stacks with item bonuses for AF (shared cap)

**Category 2 (SpecBuffBonusCategory)**:
- Specialization line buffs
- Separate caps per property
- AF capped at Level * 1.875

**Category 3 (DebuffCategory)**:
- All debuff effects
- Negative values expected
- Can exceed positive bonus caps

**Category 4 (OtherBonus)**:
- Realm bonuses
- Guild bonuses
- Uncapped modifications

#### Item Bonus Rules
**Bonus Caps by Level**:
- Level 1-14: 0 cap
- Level 15-19: 5 cap
- Level 20-24: 10 cap
- Level 25-29: 15 cap
- Level 30-34: 20 cap
- Level 35-39: 25 cap
- Level 40-44: 30 cap
- Level 45-50: 35 cap

**Special Item Caps**:
- **Hit Points**: BonusCap * 4
- **Power**: BonusCap * 2
- **Resistances**: BonusCap * 2

#### Multiplicative Modifiers
Some properties support multiplicative modifiers:
```
FinalValue = (AdditiveTotal) * MultiplicativeModifier
```
Applied after all additive bonuses are calculated.

### Property-Specific Rules

#### ToHit Bonus
```
ToHitBonus = BaseBuffBonus + SpecBuffBonus + OtherBonus - DebuffPenalty
```
- Directly affects hit chance calculations
- No hard caps (soft caps via diminishing returns)

#### Power Regeneration
```
PowerRegen = BasePowerRegen + ItemBonus + BuffBonus - DebuffPenalty
Minimum = 0 (cannot go negative)
```

#### Spell Penetration
```
ResistPierce = ItemBonus + AbilityBonus
```
- Reduces target's primary resistances
- Applied before resistance calculations

#### Archery/Melee Speed Modifiers
**Legacy Archery Speed**:
```
ArcherySpeed = BaseSpeed * (1 - (Qui - 60) * 0.002) - (BaseSpeed * ArcherySpeedBonus / 100)
```

**New Archery (Casting Speed)**:
```
ArcherySpeed = BaseSpeed * DexterityMod * CastingSpeedMod
```

### Integration Points

#### Combat System Integration
Property calculators are called during:
- Damage calculations (armor, damage bonuses)
- Hit chance calculations (ToHit, defense chances)
- Speed calculations (attack intervals)
- Resistance calculations (damage mitigation)

#### Character Sheet Display
- Real-time calculation for UI display
- Caching for performance optimization
- Update triggers on stat/equipment changes

#### Buff/Debuff Application
- Immediate recalculation on effect changes
- Category validation and stacking rules
- Temporary vs permanent modifications

## Realm and Faction System

### Three Realms
- **Albion**: Arthurian/British theme
- **Midgard**: Norse/Viking theme
- **Hibernia**: Celtic/Nature theme

### Realm Restrictions
- Cannot communicate cross-realm
- Cannot group/guild cross-realm
- Cannot enter enemy PvE zones
- Realm timers prevent switching

### Realm Bonuses
- Darkness Falls access (most keeps)
- Relic bonuses (melee/magic/strength)
- Under-populated bonuses
- Keep/tower ownership bonuses

### RvR (Realm vs Realm)
- Frontier zones for combat
- Battlegrounds by level range
- Realm points from kills
- Realm rank progression

## Guild System

### Guild Structure
- 10 ranks (0-9)
- Customizable rank names
- Configurable permissions per rank
- Guild leader (rank 0) has all permissions

### Rank Permissions
- **Invite**: Can invite new members
- **Promote/Demote**: Can change member ranks
- **Remove**: Can kick members
- **Guild chat**: Hear/speak privileges
- **Officer chat**: Hear/speak privileges  
- **Alliance chat**: Hear/speak privileges
- **Emblem**: Can wear guild emblem
- **Claim**: Can claim keeps
- **Upgrade**: Can upgrade keep level
- **Release**: Can release keep claims
- **Buff**: Can purchase guild buffs
- **Dues**: Can set tax rate
- **Withdraw**: Can access guild bank

### Guild Features
- Guild emblems on cloaks/shields
- Guild houses with vaults
- Guild bounty/merit points
- Guild missions
- Message of the Day (MOTD)
- Officer MOTD (OMOTD)

### Alliance System
- Multiple guilds allied together
- Shared alliance chat
- Alliance leader guild
- Keep claiming coordination

## Housing System

### House Types
- **Lot markers**: Purchase locations
- **Cottages**: Model 1/5/9
- **Houses**: Model 2/6/10  
- **Villas**: Model 3/7/11
- **Mansions**: Model 4/8/12

### Housing Features
- **Vaults**: 4 house vaults + 4 account vaults
- **Consignment merchant**: With porch
- **Interior decorations**: Hookpoints
- **Exterior decorations**: Garden items
- **Guild houses**: Purchasable by guilds

### Permission System
Permission levels 1-9 configurable for:
- Enter house
- Use vaults (view/add/remove per vault)
- Change interior
- Change garden
- Use consignment merchant
- Bind inside
- Use tools/merchants
- Pay rent

### Rent System
- Weekly rent based on house value
- Lockbox for rent money
- Auto-pay from lockbox
- Repossession if rent unpaid

## Keep and Siege System

### Keep Types
- Regular keeps (level 1-10)
- Relic keeps (hold realm relics)
- Portal keeps (central teleport)

### Keep Components
- **Lord**: Main defender NPC
- **Guards**: Archers, casters, fighters
- **Doors**: Outer/inner gates
- **Walls**: Destructible sections
- **Towers**: Connected defensive structures

### Siege Warfare
- **Rams**: Damage doors
- **Catapults**: Damage walls/troops
- **Ballistae**: Anti-siege weapons
- **Trebuchets**: Long range siege
- **Boiling oil**: Door defense

### Keep Claiming
- Guild leader/officers can claim
- Upgrade paths for defense
- Guard purchasing options
- Repair costs and materials

### Territory Benefits
- Teleportation networks
- Darkness Falls access
- Realm bonuses
- Supply line mechanics

## Crafting System

### Primary Crafts
- **Weaponcrafting**: Melee weapons
- **Armorcrafting**: Metal armor
- **Tailoring**: Cloth/leather armor
- **Fletching**: Bows and ammunition
- **Siegecrafting**: Siege equipment

### Secondary Crafts
- **Metalworking**: Metal materials
- **Leatherworking**: Leather materials
- **Clothworking**: Cloth materials
- **Woodworking**: Wood materials
- **Herbcraft**: Potions/tinctures
- **Gemcutting**: Magical gems

### Advanced Crafts
- **Alchemy**: Tinctures, dyes, poisons
- **Spellcrafting**: Item enchantment

### Crafting Mechanics
```
Skill chance = 45% + (Skill - RecipeLevel) * X%
Success chance = 50-100% based on skill vs recipe
Quality = 94-100% based on skill
Crafting time = BaseTime * MaterialCount / Speed
```

### Recipe System
- Ingredients with specific materials
- Tool requirements (forge, lathe, etc.)
- Skill level requirements
- Secondary skill requirements
- Success/fail/quality outcomes

## Quest and Task System

### Quest Types
- **Standard**: Talk → Steps → Reward
- **Kill**: Kill specific mobs
- **Delivery**: Deliver items between NPCs
- **Collection**: Turn in items for rewards
- **RewardQuest**: Enhanced reward selection

### Task System
- **Kill tasks**: Hunt specific creatures
- **Delivery tasks**: Transport items
- **Craft tasks**: Create items
- Level-based availability
- Repeatable with cooldowns
- Scaled rewards by level

### Reward Calculations
- Experience: Based on level/difficulty
- Money: Scaled by task type
- Items: Level-appropriate gear
- Realm/bounty points (high level)
- Champion experience

### Mission System
- Group-based objectives
- Timed completion
- Bonus objectives
- Leaderboards
- Weekly/monthly rotations

## Game Loop Architecture

### ECS (Entity Component System)
- Entities: Game objects
- Components: Data containers
- Services: System logic
- Tick-based updates (100ms)

### Update Order
1. Movement updates
2. Combat resolution
3. Effect processing
4. Regeneration
5. AI updates
6. Network synchronization

### Performance Optimizations
- Region-based processing
- Interest management
- Update frequency scaling
- Component pooling
- Lazy evaluation 

## Magic System

### Spell Hit Chance
Base spell hit chance calculation:
```
BaseHitChance = 87.5%
```

**Modifiers**:
- **Dual Component Spells**: -2.5% penalty
- **Spell/Target Level Difference**: (spellLevel - targetLevel) / 2.0
- **PvE Level Difference**: Additional (casterEffectiveLevel - targetEffectiveLevel)
- **Multiple Attackers**: Reduces by MISSRATE_REDUCTION_PER_ATTACKERS per extra attacker
- **ToHitBonus Property**: Direct addition to hit chance
- **Piercing Magic RA**: Adds spell value to hit chance
- **Majestic Will RA**: Adds effectiveness * 5 to hit chance

**Hit Chance Penalties**:
- Below 55% hit chance: Damage reduced by (hitChance - 55) * SPELL_HITCHANCE_DAMAGE_REDUCTION_MULTIPLIER * 0.01
- Minimum effective hit chance: 55%

### Spell Damage Calculation

#### Base Damage
```
BaseDamage = Spell.Damage * StatModifier * SpecModifier
StatModifier = 1 + (ManaStat * 0.005)
SpecModifier = 1 + (ItemSpecBonus * 0.005)
```

**Special Cases**:
- **Item/Potion Effects**: No stat/spec scaling
- **Nightshade**: Uses STR + AcuityBonus instead of mana stat
- **Life Drain**: Damage * (1 + LifeDrainReturn * 0.001)

#### Damage Variance
Variance determines the random damage range:

**Standard Spells**:
```
Min = (CasterSpec - 1) / TargetLevel
Max = 1.0
```

**Special Spell Lines**:
- **Mob Spells/Nightshade**: Min = 0.6, Max = 1.0
- **Item Effects/Potions**: Min = 0.75, Max = 1.25
- **Combat Styles/RAs**: Min = 1.0, Max = 1.0 (no variance)

**Level Difference Modifier**:
```
VarianceOffset = (CasterLevel - TargetLevel) * Modifier * 0.01
Modifier = Max(2, 10 - CasterLevel * 0.16)
```
- At level 0: 10% per level difference
- At level 50+: 2% per level difference

#### Effectiveness
```
SpellDamageEffectiveness = 1 + SpellDamage% * 0.01
BuffEffectiveness = 1 + BuffEffectiveness% * 0.01 (buffs only)
DebuffEffectiveness = 1 + DebuffEffectiveness% * 0.01 * CriticalModifier (debuffs only)
```

#### Final Damage Calculation
```
1. SpellDamage = BaseDamage * (1 + RelicBonus) * Effectiveness * Variance
2. Apply hit chance penalty if < 55%
3. Apply PvP/PvE damage modifiers
4. Apply resistance reduction
5. Apply damage cap: Min(Damage, Spell.Damage * 3.0 * Effectiveness)
6. Apply critical damage
```

#### Critical Damage
- **Chance**: SpellCriticalChance property (capped at 50%)
- **Damage Range**:
  - vs NPCs: 10% to 100% of base damage
  - vs Players: 10% to 50% of base damage
- **DoTs**: Can only crit with Wild Arcana RA

### Resistance System

#### Two-Layer System
Resistances are calculated in two separate layers:

**Primary Resists** (Layer 1):
- Item bonuses
- Racial resists
- Buff resists
- RvR banner bonuses
- **Resist Pierce**: Reduces target's item bonus resists by pierce value

**Secondary Resists** (Layer 2):
- Realm ability resists (Avoidance of Magic, etc.)
- Spec buff bonuses
- Capped at 80%

**Damage Reduction**:
```
Layer1Reduction = Damage * PrimaryResist * 0.01
Layer2Reduction = (Damage - Layer1Reduction) * SecondaryResist * 0.01
FinalDamage = Damage - Layer1Reduction - Layer2Reduction
```

### Crowd Control

#### Duration Calculation
Base duration is modified by:
1. **Target Resists**: Duration * (1 - ResistPercent/100)
2. **Duration Reduction Properties**:
   - MesmerizeDurationReduction
   - StunDurationReduction
3. **Duration Limits**:
   - Minimum: 1ms
   - Maximum: Spell.Duration * 4

#### Immunity Timers
After CC expires, immunity is granted:
- **Standard CC**: 60 seconds (configurable)
- **Style Stuns**: 5x stun duration
- **Pet Stuns**: No immunity

#### Special Rules
- **NPCs < 75% Health**: Immune to mezz
- **Charge/Speed of Sound**: Immune to CC while active
- **CCImmunity Ability**: Complete immunity to CC

#### NPC Diminishing Returns
NPCs have cumulative CC resistance:
```
Duration = BaseDuration / (2 * ApplicationCount)
```
- 1st CC: 100% duration
- 2nd CC: 50% duration
- 3rd CC: 25% duration
- etc.

### Healing

#### Heal Variance
Healing has variance similar to damage spells:

**Standard Heals**:
```
MinEfficiency = 0.25 + (CasterSpec - 1) / SpellLevel
MaxEfficiency = 1.25
```
- Efficiency capped at 1.25

**Special Heal Types**:
- **Item Effects**: Min = 75%, Max = 125%
- **Potion Effects**: Min = 100%, Max = 125%
- **Combat Styles**: Fixed 125%
- **RA Heals**: No variance (100%)

#### Heal Modifiers
1. **Relic Bonus**: Same as spell damage
2. **Effectiveness**: 1 + BuffEffectiveness% * 0.01
3. **HealingEffectiveness**: Additional property modifier

#### Critical Heals
- **Chance**: CriticalHealHitChance property (capped at 50%)
- **Amount**: 10% to 100% of heal value
- Applied after all other modifiers

#### Heal Aggro
Healing generates aggro on all NPCs attacking the heal target:
- Aggro amount = Effective heal value
- Added to each attacker's aggro list

#### Spread Heal
- Prioritizes most injured group member
- Distributes healing to bring all members to same health percentage
- Total heal cap = Base heal * group member count
- Individual heal cap = Base heal * 2

### Spell Types and Special Rules

#### Bolts
- 50% magic damage, 50% physical damage
- Magic portion affected by spell resists
- Physical portion affected by target's armor
- Can be blocked (negates physical portion)

#### Damage Adds/Shields
**Variance**: Min = 0.9, Max = 0.9 * (5/3) = 1.5

**Damage Calculation**:
- Fixed damage: Spell.Damage * variance * effectiveness * interval / 1000
- Percentage damage: AttackDamage * Spell.Damage / -100

#### Life Drain
- Damage boosted by LifeDrainReturn value
- Returns percentage of damage as health to caster

#### Power Drain/Tap
- Drains target's power
- Can return power to caster based on spell configuration

### Buff/Debuff System

#### Buff Stacking
- Only highest value in each category applies
- Categories: Base buffs, Spec buffs, Item bonuses
- Base buffs and item AF capped together
- Spec AF buffs capped separately

#### Concentration
- Concentration buffs have limited range
- Range determined by BUFF_RANGE property
- Buffs drop if target moves out of range

### Spell Interruption
- Melee attacks interrupt casting
- Damage interrupts based on SpellInterruptDuration
- Some abilities grant uninterruptible casting

---
*This document is a living reference and should be updated as new mechanics are discovered or changed.* 