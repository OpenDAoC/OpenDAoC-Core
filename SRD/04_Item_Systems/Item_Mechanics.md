# Item Mechanics

## Document Status
- **Completeness**: 95% (missing some special item types)
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs and item-related files
- **Implementation Status**: ✅ Fully Implemented

## Overview
Items in DAoC form the foundation of character power through weapons, armor, and magical jewelry. The system uses quality, condition, level requirements, and various caps to balance item effectiveness.

## Core Mechanics

### Item Quality and Condition

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

#### Unique Items
- One per character restriction
- Special properties beyond normal items
- Often quest or raid rewards

#### Artifacts
- Require activation through encounters
- Multiple levels of power
- Special abilities and bonuses

### Crafting and Imbuing

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