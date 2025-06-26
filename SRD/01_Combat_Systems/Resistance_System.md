# Resistance System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from ResistCalculator.cs, SpellHandler.cs, and related files
- **Implementation**: Complete

## Overview
The resistance system provides damage mitigation against both physical and magical damage types. It uses a two-layer calculation system with primary resists (items, buffs, racial) and secondary resists (realm abilities) that stack multiplicatively.

## Core Mechanics

### Damage Types and Resist Types
```csharp
public enum eDamageType : byte
{
    // Physical
    Natural = 0,
    Crush = 1,
    Slash = 2,
    Thrust = 3,
    
    // Magical
    Body = 10,
    Cold = 11,
    Energy = 12,
    Heat = 13,
    Matter = 14,
    Spirit = 15
}

// Conversion array
protected static readonly eProperty[] m_damageTypeToResistBonusConversion = 
{
    eProperty.Resist_Natural,  // Natural
    eProperty.Resist_Crush,    // Crush
    eProperty.Resist_Slash,    // Slash
    eProperty.Resist_Thrust,   // Thrust
    0, 0, 0, 0, 0, 0,         // Unused
    eProperty.Resist_Body,     // Body
    eProperty.Resist_Cold,     // Cold
    eProperty.Resist_Energy,   // Energy
    eProperty.Resist_Heat,     // Heat
    eProperty.Resist_Matter,   // Matter
    eProperty.Resist_Spirit    // Spirit
};
```

## Two-Layer Resistance System

### Layer 1: Primary Resists
```csharp
// Primary resists include:
int itemBonus = CalcValueFromItems(living, property);
int buffBonus = CalcValueFromBuffs(living, property);
int racialBonus = SkillBase.GetRaceResist(living.Race, (eResist)property);

// Buff calculation includes caps
int buff = living.BaseBuffBonusCategory[property] + 
           living.SpecBuffBonusCategory[property];
buff = Math.Min(buff, BuffBonusCap); // 26% cap for players

// Debuffs reduce buffs
int debuff = Math.Abs(living.DebuffCategory[property]) + 
             Math.Abs(living.SpecDebuffCategory[property]);
buff -= Math.Abs(debuff);

// Debuff effectiveness halved on players
if (buff < 0 && living is GamePlayer)
    buff /= 2;

// Primary result
int result = itemBonus + buffBonus;
```

### Layer 2: Secondary Resists
```csharp
// Secondary resists (realm abilities, etc.)
int abilityBonus = livingToCheck.AbilityBonus[property];

// Magic Absorption affects all magic resists
if (property is eProperty.Resist_Body through Resist_Spirit or Resist_Natural)
{
    abilityBonus += livingToCheck.AbilityBonus[eProperty.MagicAbsorption];
}

// Apply secondary as multiplicative layer
result += (int)((1 - result * 0.01) * abilityBonus);

// NPCs get additional resist from Constitution
if (living is GameNPC)
{
    double resistFromCon = StatCalculator.CalculateBuffContributionToAbsorbOrResist(
        living, eProperty.Constitution) / 8 * 100;
    result += (int)((1 - result * 0.01) * resistFromCon);
}

// Add racial bonus after multiplicative calculation
result += racialBonus;

// Hard cap at property maximum
return Math.Min(result, HardCap);
```

## Damage Calculation

### Physical Damage Resistance
```csharp
// For melee/archery attacks
double primarySecondaryResistMod = CalculateTargetResistance(
    ad.Target, ad.DamageType, armor);

// Apply resistance
damage *= primarySecondaryResistMod;
```

### Spell Damage Resistance
```csharp
public virtual double ModifyDamageWithTargetResist(AttackData ad, double damage)
{
    eDamageType damageType = DetermineSpellDamageType();
    eProperty property = ad.Target.GetResistTypeForDamage(damageType);
    
    // Primary resist layer
    int primaryResistModifier = ad.Target.GetResist(damageType);
    
    // Secondary resist layer (capped at 80%)
    int secondaryResistModifier = Math.Min(80, 
        ad.Target.SpecBuffBonusCategory[property]);
    
    // Resist Pierce reduces item bonus resists
    int resistPierce = Caster.GetModified(eProperty.ResistPierce);
    if (resistPierce > 0 && Spell.SpellType != eSpellType.Archery)
    {
        primaryResistModifier -= Math.Max(0, 
            Math.Min(ad.Target.ItemBonus[property], resistPierce));
    }
    
    // Calculate damage reduction
    double resistModifier = damage * primaryResistModifier * -0.01;
    resistModifier += (damage + resistModifier) * secondaryResistModifier * -0.01;
    damage += resistModifier;
    
    ad.Modifier = (int)Math.Floor(resistModifier);
    return damage;
}
```

## Special Resist Mechanics

### Resist Pierce
```csharp
// Resist Pierce reduces target's item bonus resists
// Does not affect racial, buff, or ability resists
if (resistPierce > 0 && Spell.SpellType != eSpellType.Archery)
{
    primaryResistModifier -= Math.Max(0, 
        Math.Min(ad.Target.ItemBonus[property], resistPierce));
}
```

### Magic Absorption
```csharp
// Magic Absorption provides resist to all magic damage types
switch (property)
{
    case eProperty.Resist_Body:
    case eProperty.Resist_Cold:
    case eProperty.Resist_Energy:
    case eProperty.Resist_Heat:
    case eProperty.Resist_Matter:
    case eProperty.Resist_Spirit:
    case eProperty.Resist_Natural:
        abilityBonus += livingToCheck.AbilityBonus[eProperty.MagicAbsorption];
        break;
}
```

### Racial Resists
```csharp
// Racial resists are added after multiplicative calculation
// This makes them more valuable as they're not reduced
int racialBonus = SkillBase.GetRaceResist(living.Race, (eResist)property);
result += racialBonus; // Added after all percentage calculations
```

## Resist Debuffs

### Duration Calculation
```csharp
protected override int CalculateEffectDuration(GameLiving target)
{
    double duration = Spell.Duration;
    
    // Spell duration bonus
    duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;
    
    // Reduced by target's resist
    duration -= duration * target.GetResist(m_spell.DamageType) * 0.01;
    
    // Minimum 1ms, maximum 4x base duration
    if (duration < 1)
        duration = 1;
    else if (duration > (Spell.Duration * 4))
        duration = Spell.Duration * 4;
        
    return (int)duration;
}
```

### Debuff Effectiveness
```csharp
// Resist debuffs get 25% specialization bonus
// Applied in SingleStatDebuff base class

// Debuffs are halved in effectiveness vs players
if (buff < 0 && living is GamePlayer)
    buff /= 2;
```

## Spell Resistance (Hit Chance)

### Base Hit Chance
```csharp
// Base hit chance calculation
double hitChance = 87.5; // 12.5% base resist

// Dual component spells have penalty
if (IsDualComponentSpell)
    hitChance -= 2.5;

// Level factors
hitChance += (spellLevel - target.Level) / 2.0;
hitChance += m_caster.GetModified(eProperty.ToHitBonus);

// Additional level bonus for NPCs
if (playerCaster == null || target is not GamePlayer)
{
    hitChance += m_caster.EffectiveLevel - target.EffectiveLevel;
    hitChance += Math.Max(0, target.attackComponent.Attackers.Count - 1) 
        * Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
}
```

### Spell Penetration
```csharp
// Piercing Magic RA
ECSGameEffect piercingMagic = effects.FirstOrDefault(
    e => e.EffectType is eEffect.PiercingMagic);
if (piercingMagic != null)
    hitChance += piercingMagic.SpellHandler.Spell.Value;

// Majestic Will
ECSGameEffect majesticWill = effects.FirstOrDefault(
    e => e.EffectType is eEffect.MajesticWill);
if (majesticWill != null)
    hitChance += majesticWill.Effectiveness * 5;
```

## Resist Caps

### Hard Caps
- **Primary + Secondary**: No hard cap on total
- **Secondary Resists**: 80% cap for spell damage
- **Buff Cap**: 26% for players (BuffBonusCap)
- **Debuff Reduction**: Halved effectiveness vs players

### Soft Caps
- Primary resists effectively soft-capped by item limits
- Racial resists bypass all soft caps

## Necromancer Pet Special Rules
```csharp
// Necro pets inherit owner's resists
GameLiving livingToCheck;

if (living is NecromancerPet necroPet && 
    necroPet.Owner is GamePlayer playerOwner)
{
    livingToCheck = playerOwner;
}
else
{
    livingToCheck = living;
}

// Pet uses owner's:
// - Item resists
// - Ability resists (RAs)
// - Other bonuses
```

## NPC Resist Calculations

### Constitution-Based Resists
```csharp
// NPCs get magic resist from Constitution
if (living is GameNPC)
{
    double resistFromCon = StatCalculator
        .CalculateBuffContributionToAbsorbOrResist(
            living, eProperty.Constitution) / 8 * 100;
            
    // Applied as third layer
    result += (int)((1 - result * 0.01) * resistFromCon);
}
```

### No Buff Caps
```csharp
// NPCs ignore buff resist caps
buff = livingToCheck is GameNPC ? 
    buff : Math.Min(buff, BuffBonusCap);
```

## Display and Updates

### Character Updates
```csharp
protected override void SendUpdates(GameLiving target)
{
    base.SendUpdates(target);
    
    if (target is GamePlayer player)
    {
        player.Out.SendCharResistsUpdate();
    }
}
```

### Combat Messages
```csharp
// Resist messages show actual resist chance
if (spellResistChance > spellResistRoll)
{
    OnSpellNegated(target, SpellNegatedReason.Resisted);
    SendSpellResistAnimation(target);
    SendSpellResistMessages(target);
}
```

## Special Cases

### Vampiir Magical Strike
```csharp
// Custom resist formula for Vamp claws
public override double CalculateSpellResistChance(GameLiving target)
{
    // Same level or lower: 0% resist
    // Each level above: +0.5% resist
    return target.Level <= Caster.Level ? 0 : 
        (target.Level - Caster.Level) / 2;
}
```

### Bolt Spells
```csharp
// Bolt spells apply 50% magic resist, 50% armor
double halfBaseDamage = damage * 0.5;

// Magic resist on first half
damage = base.ModifyDamageWithTargetResist(ad, halfBaseDamage);

// Armor on second half
if (!ad.Target.attackComponent.CheckBlock(ad))
{
    double weaponSkill = Caster.Level * 2.5 + INHERENT_WEAPON_SKILL;
    double targetArmor = CalculateTargetArmor(ad.Target, ad.ArmorHitLocation);
    damage += weaponSkill / targetArmor * halfBaseDamage;
}
```

## Test Scenarios

### Basic Resist Test
```
1. Cast spell with known damage
2. Apply 20% resist buff
3. Verify 20% damage reduction
4. Add 10% debuff
5. Verify 10% resist (20-10)
```

### Two-Layer Test
```
1. Apply 20% item resist
2. Add 10% RA resist
3. Expected: 20% + (1-0.2)*10% = 28%
4. Verify damage reduction
```

### Resist Pierce Test
```
1. Target has 30% item resist
2. Caster has 15% resist pierce
3. Effective resist: 15%
4. Verify damage calculation
```

## Edge Cases

### Negative Resists
- Debuffs can push resists negative
- Negative resists increase damage taken
- Player debuffs halved in effectiveness

### Over-Cap Resists
- No hard cap on total resist
- Diminishing returns from multiplicative stacking
- 100% resist theoretically possible but impractical

### Mixed Damage Types
- Each damage type calculated separately
- No averaging or combination
- Bolt spells special case (50/50 split)

## Change Log

### 2025-01-20
- Initial documentation created
- Two-layer system explained
- All special mechanics documented
- NPC/Pet rules included

## References
- ResistCalculator.cs: Core resist calculations
- SpellHandler.cs: Spell resistance implementation
- ResistDebuff.cs: Debuff mechanics
- GameLiving.cs: Damage type conversion 