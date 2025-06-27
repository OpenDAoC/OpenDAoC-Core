# Item Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs and item-related files
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Items are the primary way to increase your character's power beyond leveling up. Every piece of equipment has quality (how well it was made) and condition (how damaged it is), both of which affect how effective the item is. Higher level items are more powerful but require you to reach that level to equip them. Understanding item mechanics helps you evaluate loot, plan upgrades, and make informed decisions about repairs and replacements.

Items in DAoC form the foundation of character power through weapons, armor, and magical jewelry. The system uses quality, condition, level requirements, and various caps to balance item effectiveness.

## Core Mechanics

### Item Quality and Condition

**Game Rule Summary**: Quality represents how well an item was made and can never be changed after creation. Condition represents how damaged the item is from use and decreases every time you fight, but can be restored by repairing. Both quality and condition directly multiply your item's effectiveness - a 50% quality, 50% condition item only works at 25% effectiveness. Always keep your gear repaired for maximum performance.

#### Quality System
- Range: 0-100%
- Affects item effectiveness (damage/AF)
- Cannot be modified after creation (except by GM)
- Higher quality = better performance

#### Condition System
```
Condition: Current durability
MaxCondition: Maximum durability
ConditionPercent = (Condition / MaxCondition) * 100

Effectiveness = Quality% * ConditionPercent%
```

**Degradation**:
- Items lose condition through use
- Repair costs based on quality/level
- 0 condition = item unusable

### Level Requirements

**Game Rule Summary**: Items have level requirements that determine when you can equip them and how powerful their magical bonuses can be. You must reach the item's level to wear it, and higher level items can have stronger magical properties. The bonus level determines how powerful the magical bonuses are - a level 51 item can have much stronger stat bonuses than a level 20 item.

#### Base Requirements
- Item Level: Determines caps and requirements
- Character must meet level to equip
- Some items have specific class/realm requirements

#### Bonus Level System
```
BonusLevel = (ItemLevel - 15) / 5 * 5
Minimum: 5
Maximum: Based on item level
```
- Determines magical property strength
- Used for spellcraft calculations

### Weapon Mechanics

**Game Rule Summary**: Weapons have damage per second (DPS) and speed that determine how much damage they deal. Faster weapons hit more often but for less damage per swing, while slower weapons hit harder but less frequently. Your weapon's effective damage depends on its quality and condition, and there are level-based caps that prevent you from using overpowered weapons. Two-handed weapons get bonus damage to compensate for not having a shield.

#### DPS (Damage Per Second)
```
DPS_AF field = Base DPS * 10 (stored value)
Actual DPS = DPS_AF / 10.0
```

#### DPS Cap
```csharp
DPSCap = 1.2 + 0.3 * CharacterLevel
if (RealmLevel > 39)
    DPSCap += 0.3
    
ClampedDPS = Min(ItemDPS, DPSCap)
```

**Source**: `DetailDisplayHandler.cs:WriteClassicWeaponInfos()`

#### Effective Damage
```csharp
EffectiveDPS = ClampedDPS * Quality/100 * Condition/MaxCondition
ActualDamage = EffectiveDPS * WeaponSpeed
```

#### Weapon Speed
- SPD_ABS field = Speed * 10 (stored value)
- Actual speed = SPD_ABS / 10.0 seconds
- Affects damage per swing and style damage

#### Weapon Skill Calculation
```csharp
// Weapon damage without quality/condition
BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponBonus
SlowWeaponBonus = 1 + (WeaponSpeed - 20) * 0.003
```

### Armor Mechanics

**Game Rule Summary**: Armor provides protection through Armor Factor (AF) which reduces incoming damage, and Absorption which blocks a percentage of physical damage completely. Heavier armor types provide more protection but may restrict which classes can wear them. Like weapons, armor effectiveness depends on quality and condition, and there are level-based caps to prevent overpowered equipment. Keeping armor repaired is crucial for survival in combat.

#### Armor Factor (AF)
```
AF = Base armor protection value
Stored as DPS_AF field on armor items
```

#### AF Caps
```csharp
// Level-based cap
AFCap = CharacterLevel * 2
if (RealmLevel > 39)
    AFCap += 2

// Cloth armor special case
if (ArmorType == Cloth)
    AFCap = CharacterLevel
```

#### Effective AF Calculation
```csharp
BaseAF = Min(ItemAF, AFCap)
BaseAF += BaseBuffBonusCategory[ArmorFactor] / 6.0
EffectiveAF = BaseAF * Quality/100 * Condition/MaxCondition
FinalAF = Min(EffectiveAF, AFCap)
```

**Source**: `GamePlayer.cs:GetArmorAF()`

#### Absorption (ABS)

**Game Rule Summary**: Absorption is the percentage of physical damage that your armor completely blocks before it even hits your health. Heavier armor types have higher absorption - plate armor blocks 34% of all physical damage while cloth armor blocks none. This makes armor choice crucial for survival, as the difference between cloth and plate armor can mean taking 34% less physical damage from every hit.

Percentage of physical damage absorbed:

| Armor Type | Base ABS% |
|------------|-----------|
| Cloth      | 0%        |
| Leather    | 10%       |
| Studded    | 19%       |
| Reinforced | 19%       |
| Chain      | 27%       |
| Scale      | 27%       |
| Plate      | 34%       |

```csharp
FinalABS = Clamp((ItemABS + ArmorAbsorptionBonus) * 0.01, 0, 1)
```

### Magical Properties

**Game Rule Summary**: Items can have magical bonuses to stats, resistances, skills, and special properties. These bonuses are subject to level-based caps - higher level characters can benefit from stronger magical bonuses than low level characters. This system ensures that items remain relevant as you level up, since a high-level item with weak magical bonuses might be worse than a lower-level item with strong bonuses that you can actually use.

#### Bonus Types
- **Stats**: STR, CON, DEX, QUI, INT, PIE, EMP, CHA
- **Resists**: Slash, Thrust, Crush, Heat, Cold, Matter, Body, Spirit, Energy
- **Skills**: +Skill bonuses
- **Magic**: +Spell lines, +Focus
- **Other**: HP, Power, AF, Speed, etc.

#### Bonus Caps
Item bonuses subject to level-based caps:
```csharp
BonusCap = 1.5 * Level (for most bonuses)
StatCap = Level * 1.5 + 1
ResistCap = Level * 2 + 1
```

### Unique and Artifact Items

**Game Rule Summary**: Unique items are special pieces of equipment that you can only have one of per character, often with properties that normal items can't have. Artifacts are even more special - they require completing specific encounters to activate and can grow more powerful as you advance them. These items often define character builds and provide unique tactical options not available through normal equipment.

#### Unique Items
- One per character restriction
- Special properties beyond normal items
- Often quest or raid rewards

#### Artifacts
- Require activation through encounters
- Multiple levels of power
- Special abilities and bonuses

### Crafting and Imbuing

**Game Rule Summary**: Player-crafted items can be enhanced through spellcrafting (imbuing), which adds magical properties using gems and materials. Higher quality items can hold more magical power, while overcharging (exceeding the safe limit) risks destroying the item but allows for more powerful bonuses. This system lets players customize their equipment and create items specifically suited to their build and playstyle.

#### Item Creation
- Quality determines base effectiveness
- Material type affects properties
- Skill level affects quality range

#### Spellcrafting (Imbuing)
```csharp
// Max imbue points by quality
Quality 100: 0.625 * ItemLevel
Quality 99:  0.55 * ItemLevel
Quality 98:  0.475 * ItemLevel
Quality 97:  0.42 * ItemLevel
Quality 96:  0.353 * ItemLevel
Quality 95:  0.295 * ItemLevel
Below 95:    0.2 * ItemLevel
```

**Source**: `SpellCrafting.cs:GetItemMaxImbuePoints()`

#### Overcharge
- Can exceed max imbue points
- Risk of item destruction
- Success chance based on amount over

### Item Types

**Game Rule Summary**: Different equipment types serve different roles in combat. Weapons determine your damage output and attack speed, armor protects you from harm, and jewelry provides magical bonuses without taking up armor slots. Understanding which slots are available to your class and how different item types work together is crucial for building an effective character.

#### Weapons
- **One-Handed**: Standard damage/speed
- **Two-Handed**: Higher damage, slower
- **Dual Wield**: Two weapons
- **Ranged**: Bows, thrown weapons
- **Shields**: Defensive items

#### Armor Slots
- **Head**: Helm/Cap
- **Torso**: Chest piece
- **Arms**: Sleeves
- **Legs**: Leggings
- **Hands**: Gloves
- **Feet**: Boots

#### Jewelry
- **Neck**: Necklace
- **Cloak**: Back item
- **Jewel**: Gem/jewel
- **Belt**: Waist item
- **Rings**: 2 slots
- **Wrists**: 2 bracer slots

### Inventory Management

**Game Rule Summary**: Your inventory has limited space and carrying too much weight will slow you down. Items take up specific inventory slots and have weight that counts toward encumbrance. Managing your inventory efficiently and staying under the weight limit is important for maintaining mobility in combat and exploration.

#### Slot Locations
```
Equipped: 0x00-0x1F (armor/weapons)
Backpack: 0x28-0x4F (40 slots)
Vault: Various ranges
Horse Bags: 0x80-0x9F
```

#### Encumbrance
```csharp
Encumbered = CarriedWeight > MaxCarryWeight
SpeedPenalty = EncumbrancePercent * SpeedLossMultiplier
```

## System Interactions

### With Combat
- Weapon DPS affects damage
- Weapon speed affects attack rate
- Armor AF reduces damage taken
- Absorption mitigates physical damage

### With Stats
- Stat requirements for items
- Stat bonuses from items
- Encumbrance from strength

### With Crafting
- Quality affects imbue points
- Condition affects repair costs
- Material determines base stats

### With Realm Abilities
- Some items enhance RAs
- Artifact abilities
- Mythical bonuses

## Implementation Notes

### Database Fields
```sql
Key item fields:
- Level: Item level requirement
- DPS_AF: Damage/Armor factor * 10
- SPD_ABS: Speed/Absorption value
- Quality: 0-100 quality
- Condition: Current durability
- MaxCondition: Max durability
- Bonus1-10: Magical properties
- Bonus1Type-10Type: Property types
```

### Item Templates
- Templates define base properties
- Instances track condition/location
- Unique items have special handling

## Test Scenarios

### DPS Cap Test
```
Given: Level 50 player, 20.0 DPS weapon
DPSCap: 1.2 + 0.3 * 50 = 16.2
Result: Clamped to 16.2 DPS
```

### AF Calculation Test
```
Given: Level 50, 102 AF chest, 100% quality/condition
AFCap: 50 * 2 = 100
Result: 100 AF (capped from 102)
```

### Imbue Point Test
```
Given: Level 51 item, 99% quality
MaxImbue: 0.55 * 51 = 28.05 = 28 points
```

### Absorption Test
```
Given: Chain armor (27% base), +10% bonus
Total: 37% absorption
Damage: 100 physical = 63 damage taken
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all item mechanics formulas
- Documented caps and calculations
- Added crafting/imbuing systems

## References
- `GameServer/gameobjects/GamePlayer.cs` - Item effectiveness calculations
- `GameServer/packets/Client/168/DetailDisplayHandler.cs` - Item display logic
- `GameServer/craft/SpellCrafting.cs` - Imbuing mechanics
- `GameServer/managers/RandomObjectGeneration/` - Item generation 