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

## Combat System

### Attack Resolution Order
1. Intercept check
2. Evade check
3. Parry check
4. Block (Shield) check
5. Guard check
6. Hit/Miss determination
7. Bladeturn check

### Hit/Miss Calculation
- **Base miss chance**: 18% (reduced from 23% in patch 1.117C)
- **To-Hit bonus**: Reduces miss chance directly
- **Level difference**: ±1.33% per level (PvE only)
- **Multiple attackers**: -0.5% miss per additional attacker (configurable via MISSRATE_REDUCTION_PER_ATTACKERS)
- **Weapon bonus**: Capped by attacker level
- **Armor bonus**: Capped by defender level, applied differently for PvP vs PvE
  - PvP: Direct addition/subtraction to miss chance
  - PvE: Percentage modification of miss chance
- **Style bonuses**: Applied from attack style and previous defensive style
- **Ammo modifiers** (multiplicative with base miss chance):
  - Rough: +15% miss chance
  - Standard: No modification  
  - Footed: -25% miss chance

### Damage Calculation

#### Base Damage Formula
```
BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponModifier
SlowWeaponModifier = 1 + (WeaponSpeed - 20) * 0.003
```

#### Damage Modifiers
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
   - Minimum critical damage: 10% of base damage
   - Maximum critical damage: 50% vs players, 100% vs NPCs
   - Critical chance from items/abilities, capped at 50%

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
- Modified by Dex (0.1% per point above 60)
- Modified by shield quality and condition
- 60% cap in RvR only (BLOCK_CAP property)
- Shield size determines max simultaneous blocks:
  - Small: 1 attacker
  - Medium: 2 attackers
  - Large: 3 attackers
- Halved vs dual wield attacks
- Only works from front (120 degree arc)

### Style Combat
- **Positional requirements**: Side (90°), back (90°), front (180°)
- **Opening requirements**: Parry, block, evade, hit, miss, etc.
- **Attack result requirements**: Must match previous attack result
- **Growth rate for style damage**: 
  ```
  StyleDamage = GrowthRate * Spec * AttackSpeed / UnstyledDamageCap * UnstyledDamage
  ```
- **Minimum style damage**: 1 for styles with positive growth rate
- **Endurance cost**: Based on weapon speed
- **Style effects and procs**: Chance-based spell effects
- **To-hit bonus**: Reduces miss chance directly
- **Defense bonus**: Increases attacker's miss chance on next attack

#### Stealth Opener Damage (Static Formulas)
- **Backstab I**: Cap = ~5 + Critical Strike Spec * 14/3
- **Backstab II**: Cap = 45 + Critical Strike Spec * 6
- **Perforate Artery (1H)**: Cap = 75 + Critical Strike Spec * 9
- **Perforate Artery (2H)**: Cap = 75 + Critical Strike Spec * 12

### Spell Damage

#### Base Spell Damage
```
SpellDamage = DelveValue * (1 + StatModifier * 0.005) * (1 + SpecBonus * 0.005)
StatModifier = Primary stat (INT/PIE/etc) based on class
SpecBonus = Item bonuses to spell line (casters only)
```

#### Spell Resistance
```
HitChance = 87.5% base
+/- (SpellLevel - TargetLevel) / 2
+ ToHitBonus
+ PiercingMagic effects
Modified by level difference (PvE)
Modified by multiple attackers (PvE)
```

#### Damage Variance
- Baseline: 75-125% of spell damage
- Mastery of Magic: Reduces lower bound
- Wild Power: Increases variance range

## Character Progression

### Experience System
- Level 1-50 progression
- Experience needed per level stored in database
- Group experience bonuses
- Camp/challenge bonuses
- Rested experience

### Stat Progression
- **Primary stat**: +1 per level starting at level 6
- **Secondary stat**: +1 every 2 levels starting at level 6
- **Tertiary stat**: +1 every 3 levels starting at level 6
- **Starting stats**: Race and class dependent

### Specialization Points
- Points per level = Level * SpecMultiplier / 10
- SpecMultiplier varies by class (10-20)
- Bonus spec points from realm ranks

### Champion Levels
- 1-10 champion levels post-50
- Mini-lines for specialization
- Class-specific champion abilities

### Realm Ranks
- RR1-RR13 progression
- Realm points from RvR combat
- Realm abilities purchasable with points
- RR5 ability at realm rank 5

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

### Calculator Types
Each property has a dedicated calculator that determines the final value:
- Base value (from stats/skills)
- Item bonuses
- Buff bonuses (base and spec)
- Debuff penalties
- Realm bonuses
- Multiplicative modifiers

### Stacking Rules
- **Buffs**: Highest value in each category wins
- **Item bonuses**: Additive up to caps
- **Debuffs**: Generally subtractive
- **Multiplicative**: Applied after additive

### Common Properties
- Armor Factor (AF)
- Armor Absorption
- Melee/Magic resists
- Stat bonuses (STR/CON/DEX/QUI/INT/PIE/EMP/CHA)
- Skill bonuses (+Parry, +Shield, etc.)
- Speed modifiers (melee/cast/archery)
- Critical hit chances
- Power/endurance regeneration

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