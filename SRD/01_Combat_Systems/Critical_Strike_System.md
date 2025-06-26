# Critical Strike System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from AttackComponent.cs, Property Calculators, and related files
- **Implementation**: Complete

## Overview

**Game Rule Summary**: Critical strikes let you deal extra damage when you get lucky. Every attack or spell has a chance to "crit" for bonus damage - melee and archery crits do 10-100% extra damage against monsters but only 10-50% against other players to keep PvP balanced. The more critical hit items and abilities you have, the more often you'll land these devastating strikes.

The critical strike system provides a chance for attacks and spells to deal additional damage. Different systems exist for melee, archery, and spell critical strikes with distinct caps, calculations, and modifiers.

## Core Mechanics

### Critical Strike Types

**Game Rule Summary**: There are different types of critical strikes for different combat styles. Melee fighters crit with weapons, archers crit with bows, spellcasters crit with damage spells, and some can even crit with debuffs to make them more effective. Each type has its own chance calculation but they all follow similar damage rules.

#### 1. Melee Critical Strikes
```csharp
[PropertyCalculator(eProperty.CriticalMeleeHitChance)]
public class CriticalMeleeHitChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        // Berserk guarantees crits
        if (EffectListService.GetEffectOnTarget(living, eEffect.Berserk) != null)
            return 100;
            
        int chance = living.OtherBonus[property] + living.AbilityBonus[property];
        
        // Players get inherent 10% base
        if (living is GamePlayer)
            chance += 10;
            
        // 50% hardcap
        return Math.Min(chance, 50);
    }
}
```

#### 2. Archery Critical Strikes
```csharp
[PropertyCalculator(eProperty.CriticalArcheryHitChance)]
public class CriticalArcheryHitChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        int chance = living.OtherBonus[property] + living.AbilityBonus[property];
        
        // Players get inherent 10% base
        if (living is GamePlayer)
            chance += 10;
            
        // Pet bonuses
        if (living is NecromancerPet)
            chance += 10;
            
        // 50% hardcap
        return Math.Min(chance, 50);
    }
}
```

#### 3. Spell Critical Strikes
```csharp
[PropertyCalculator(eProperty.CriticalSpellHitChance)]
public class CriticalSpellHitChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        int chance = living.AbilityBonus[property];
        
        // List casters get inherent 10% base
        if (living is GamePlayer player)
        {
            if (player.CharacterClass.ClassType is eClassType.ListCaster)
                chance += 10;
        }
        
        // 50% hardcap
        return Math.Min(chance, 50);
    }
}
```

#### 4. Debuff Critical Strikes
```csharp
[PropertyCalculator(eProperty.CriticalDebuffHitChance)]
public class CriticalDebuffHitChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        int chance = living.AbilityBonus[property];
        
        // Pet inherits owner's Wild Arcana
        if (living is NecromancerPet necroPet)
            chance += owner.GetAbility<AtlasOF_WildArcanaAbility>()?.Amount ?? 0;
            
        // 50% hardcap
        return Math.Min(chance, 50);
    }
}
```

## Critical Damage Calculation

**Game Rule Summary**: When you score a critical hit, the extra damage is random within a range. The minimum is always 10% of your base damage, but the maximum depends on what you're fighting. Against monsters you can do up to 100% extra damage (doubling your hit), but against other players it's capped at 50% extra to prevent one-shot kills in PvP.

### Melee/Archery Critical Damage
```csharp
public int CalculateCriticalDamage(AttackData ad)
{
    if (!Util.Chance(ad.CriticalChance))
        return 0;
        
    if (owner is GamePlayer)
    {
        // Triple wield prevents critical hits (1.62)
        if (EffectListService.GetEffectOnTarget(ad.Target, eEffect.TripleWield) != null)
            return 0;
            
        int critMin, critMax;
        
        // Check for Berserk
        ECSGameEffect berserk = EffectListService.GetEffectOnTarget(owner, eEffect.Berserk);
        
        if (berserk != null)
        {
            int level = owner.GetAbilityLevel(Abilities.Berserk);
            // Berserk scaling:
            // Level 1: 10-25%
            // Level 2: 10-50%
            // Level 3: 10-75%
            // Level 4: 10-99%
            critMin = (int)(ad.Damage * 0.1);
            critMax = (int)(Math.Min(0.99, level * 0.25) * ad.Damage);
        }
        else
        {
            // Normal critical damage
            critMin = (int)(ad.Damage * 0.1);  // 10% minimum
            
            // Max crit damage differs vs players
            if (ad.Target is GamePlayer)
                critMax = ad.Damage / 2;  // 50% max vs players
            else
                critMax = ad.Damage;      // 100% max vs NPCs
        }
        
        critMin = Math.Max(critMin, 0);
        critMax = Math.Max(critMin, critMax);
        return Util.Random(critMin, critMax);
    }
    else
    {
        // NPC critical damage
        int maxCriticalDamage = ad.Target is GamePlayer ? ad.Damage / 2 : ad.Damage;
        int minCriticalDamage = (int)(ad.Damage * MinMeleeCriticalDamage);  // 10%
        
        if (minCriticalDamage > maxCriticalDamage)
            minCriticalDamage = maxCriticalDamage;
            
        return Util.Random(minCriticalDamage, maxCriticalDamage);
    }
}
```

### Spell Critical Damage
```csharp
// In SpellHandler.CalculateDamageToTarget
int criticalDamage = 0;
int criticalChance = Math.Min(50, m_caster.SpellCriticalChance);
double randNum = Util.RandomDouble() * 100;

if (criticalChance > randNum && finalDamage > 0)
{
    int criticalMax = ad.Target is GamePlayer ? 
        (int)finalDamage / 2 :  // 50% max vs players
        (int)finalDamage;       // 100% max vs NPCs
        
    criticalDamage = Util.Random((int)finalDamage / 10, criticalMax);
}
```

### Debuff Critical Effects

**Game Rule Summary**: Debuff critical strikes don't do extra damage, but instead make the debuff more effective. A critical debuff might slow someone more, reduce their stats more severely, or last longer. This makes debuff-focused characters more unpredictable and dangerous in the right hands.

```csharp
protected virtual double GetDebuffEffectivenessCriticalModifier()
{
    if (Util.Chance(Caster.DebuffCriticalChance))
    {
        double min = 0.1;  // 10% minimum
        double max = 1.0;  // 100% maximum
        double criticalModifier = min + Util.RandomDoubleIncl() * (max - min);
        
        // Notify player
        (Caster as GamePlayer)?.Out.SendMessage(
            $"Your {Spell.Name} critically debuffs the enemy for {criticalModifier * 100:0}% additional effect!", 
            eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            
        return 1.0 + criticalModifier;
    }
    
    return 1.0;
}
```

## Critical Strike Sources

**Game Rule Summary**: Critical strike chance comes from several sources that all stack together. You can get it from special realm abilities trained at high levels, from magical items and equipment, and some character classes have natural advantages. The total from all sources is capped at 50% so you can't guarantee critical hits.

### Realm Abilities
- **Wild Power**: Spell critical chance
- **Wild Arcana**: DoT/Debuff critical chance
- **Wild Minion**: Pet melee/archery critical chance
- **Mastery of Arms**: Melee critical chance

### Items
- **Melee Crit**: `eProperty.CriticalMeleeHitChance`
- **Archery Crit**: `eProperty.CriticalArcheryHitChance`
- **Spell Crit**: `eProperty.CriticalSpellHitChance`
- **Debuff Crit**: `eProperty.CriticalDebuffHitChance`

### Class Bonuses
- **List Casters**: +10% inherent spell crit
- **All Players**: +10% inherent melee/archery crit
- **Necromancer Pets**: +10% inherent crit

### Special Effects

**Game Rule Summary**: Some abilities completely change how critical strikes work. Berserk gives you 100% critical chance but makes you vulnerable. Triple Wield makes you immune to receiving critical hits but you also can't critically hit others. These create interesting tactical trade-offs.

- **Berserk**: 100% melee crit chance while active
- **Triple Wield**: Target immune to critical hits

## Special Cases

### Critical Shot

**Game Rule Summary**: The archer ability Critical Shot guarantees a hit but ironically cannot itself critically hit. This prevents it from being overpowered since it already provides guaranteed accuracy at the cost of slower attack speed.

```csharp
// Critical Shot archery style cannot critically hit
if (action.RangedAttackType is eRangedAttackType.Critical)
    return 0;
```

### DoT Critical Strikes

**Game Rule Summary**: Damage-over-time spells normally cannot critically hit, but certain realm abilities can change this. This keeps DoTs from being too powerful while still giving specialized characters ways to enhance them.

```csharp
// DoTs can only crit with Wild Arcana RA
// Handled by DoTSpellHandler directly
int criticalChance = this is not DoTSpellHandler ? 
    Math.Min(50, m_caster.SpellCriticalChance) : 0;
```

### Volley Attacks
- Volley attacks use standard critical mechanics
- Each hit rolls independently for critical

### Pet Critical Strikes

**Game Rule Summary**: Pets can score critical hits using their own base chances plus bonuses from their master's abilities. Necromancer pets are especially good at this, getting natural critical bonuses that other pets don't have.

```csharp
// Pets inherit owner's Wild Minion bonus
if (npc.Brain is IControlledBrain petBrain && 
    petBrain.GetPlayerOwner() is GamePlayer playerOwner)
{
    if (npc is NecromancerPet)
        chance += 10;  // Base necro pet bonus
        
    // Wild Minion RA bonus
    var wildMinion = playerOwner.GetAbility<AtlasOF_WildMinionAbility>();
    if (wildMinion != null)
        chance += wildMinion.Amount;
}
```

## Caps and Limits

**Game Rule Summary**: Critical strikes are capped to prevent them from becoming overpowered. No matter how much critical gear you have, you can never exceed 50% critical chance. Damage is also limited - you can never do more than double damage to monsters or 1.5x damage to players, keeping combat balanced.

### Hard Caps
- **All Critical Types**: 50% maximum chance
- **Player Target Damage**: 50% of base damage maximum
- **NPC Target Damage**: 100% of base damage maximum
- **Minimum Damage**: 10% of base damage

### Soft Caps
- No diminishing returns on critical chance
- No level-based scaling beyond base mechanics

## Display Messages

### Melee/Archery
```csharp
// Critical hit message
if (ad.CriticalDamage > 0)
{
    p.Out.SendMessage(
        LanguageMgr.GetTranslation(p.Client.Account.Language,
            "GamePlayer.Attack.Critical", 
            ad.Target.GetName(0, false),
            ad.CriticalDamage) + $" ({ad.CriticalChance}%)", 
        eChatType.CT_YouHit,
        eChatLoc.CL_SystemWindow);
}
```

### Spell Critical
```csharp
// Handled automatically by spell damage messages
// Critical damage included in total damage display
```

### Debuff Critical
```csharp
// Special message for critical debuffs
"Your {SpellName} critically debuffs the enemy for X% additional effect!"
```

## Combat Log Integration

### Detailed Combat Log
```csharp
if (playerCaster != null && playerCaster.UseDetailedCombatLog)
{
    if (criticalChance > 0)
    {
        playerCaster.Out.SendMessage(
            $"Spell crit chance: {criticalChance:0.##} random: {randNum:0.##}", 
            eChatType.CT_DamageAdd, 
            eChatLoc.CL_SystemWindow);
    }
}
```

## Implementation Notes

### Roll Timing
- Critical chance rolled after hit confirmed
- Damage calculated before resistances
- Applied before armor absorption

### Performance
- Single random roll per attack
- Cached property calculations
- No recursive critical checks

## Test Scenarios

### Basic Critical Test
```
1. Attack with 20% crit chance
2. Verify ~20% of hits are critical
3. Check damage range 10-100% (NPCs)
4. Check damage range 10-50% (players)
```

### Berserk Test
```
1. Activate Berserk
2. Verify 100% crit chance
3. Check scaling damage by level
4. Verify caps at 99% damage
```

### Cap Test
```
1. Stack crit to 60%+
2. Verify capped at 50%
3. Check all crit types
4. Verify no overflow
```

## Edge Cases

### Zero Damage
- No critical roll if base damage is 0
- Critical damage cannot exceed base damage
- Minimum 1 damage if critical procs

### Immune Targets
- Triple Wield prevents incoming crits
- No special immunities otherwise
- Crits work on all target types

### Stacking Effects
- Multiple crit sources stack additively
- Single cap applies to total
- No multiplicative stacking

## Change Log

### 2025-01-20
- Initial documentation created
- All critical types documented
- Damage calculations detailed
- Special cases included

## References
- AttackComponent.cs: Critical damage calculation
- CriticalMeleeHitChanceCalculator.cs: Melee crit chance
- CriticalArcheryHitChanceCalculator.cs: Archery crit chance
- CriticalSpellHitChanceCalculator.cs: Spell crit chance
- SpellHandler.cs: Spell critical implementation 