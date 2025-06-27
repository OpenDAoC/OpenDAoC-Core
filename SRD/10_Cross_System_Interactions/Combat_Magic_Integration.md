# Combat & Magic System Integration

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Combat and Magic work together seamlessly in DAoC through many interconnected systems. When you get hit while casting a spell, there's a chance your spell will be interrupted based on your concentration skill and the attacker's level. Some spells do both physical and magical damage (like bolt spells), where the physical portion can be blocked but the magical portion is reduced by resist. Weapon procs add magical effects to your melee attacks, while damage-add spells boost your weapon damage. Buffs and debuffs directly affect your combat effectiveness - strength buffs increase melee damage, dexterity affects casting speed, and disease prevents healing. Critical hits work the same way for both spells and weapons, and the resistance system protects against magical damage using a two-layer calculation that can be pierced by certain abilities.

The Combat and Magic systems are tightly integrated in DAoC, with spells affecting combat calculations, combat interrupting spellcasting, and shared mechanics for damage, resistance, and critical hits. This document details the complex interactions between these core systems.

## Spell Combat Integration

### Combat Spells in Melee

#### Weapon Procs
```csharp
public class WeaponProc
{
    public void TryToProc(AttackData ad)
    {
        if (Util.Chance(ProcChance))
        {
            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(
                ad.Attacker, Spell, SpellLine);
                
            spellHandler.StartSpell(ad.Target);
        }
    }
}
```

#### Damage Add Integration
```csharp
public class DamageAddECSEffect : ECSGameSpellEffect
{
    public override void OnDirectEffect(GameLiving target)
    {
        // Add magical damage to physical attacks
        int damageAdd = (int)(Spell.Damage * Effectiveness);
        
        if (target.attackComponent.AttackState)
        {
            target.TempProperties.setProperty(DAMAGE_ADD_PROPERTY, damageAdd);
        }
    }
}
```

### Spell Interruption by Combat

#### Interrupt Mechanics
```csharp
public class InterruptHandler
{
    public static void InterruptCasting(GameLiving caster, GameLiving attacker)
    {
        if (caster.castingComponent == null || !caster.castingComponent.IsCasting)
            return;
            
        // Calculate interrupt chance
        double interruptChance = CalculateInterruptChance(caster, attacker);
        
        if (Util.ChanceDouble(interruptChance))
        {
            caster.castingComponent.InterruptCasting();
            
            // Add interrupt immunity
            InterruptECSGameEffect immunity = new InterruptECSGameEffect(
                new ECSGameEffectInitParams(caster, 
                Properties.SPELL_INTERRUPT_DURATION, 1));
                
            EffectService.RequestImmediateEffect(immunity);
        }
    }
}
```

#### Interrupt Resistance
```csharp
private static double CalculateInterruptChance(GameLiving caster, GameLiving attacker)
{
    // Base 65% interrupt chance
    double chance = 0.65;
    
    // Concentration reduces interrupt chance
    int concentration = caster.GetModified(eProperty.Concentration);
    chance *= (1.0 - concentration * 0.01);
    
    // Dexterity affects interrupt resistance
    int dexterity = caster.GetModified(eProperty.Dexterity);
    chance *= (1.0 - (dexterity - 50) * 0.002);
    
    // Level difference
    int levelDiff = attacker.Level - caster.Level;
    chance += levelDiff * 0.02;
    
    return Math.Max(0.05, Math.Min(0.95, chance));
}
```

## Damage System Integration

### Physical vs Magical Damage

#### Damage Type Mixing
```csharp
public class AttackData
{
    public int PhysicalDamage { get; set; }     // Weapon + strength
    public int MagicalDamage { get; set; }      // Spell effects
    public DamageType DamageType { get; set; }  // Slash/Thrust/Crush/Magic
    
    public int TotalDamage => PhysicalDamage + MagicalDamage;
}
```

#### Bolt Spells (Mixed Damage)
```csharp
public class BoltSpellHandler : SpellHandler
{
    public override void CalculateDamageToTarget(GameLiving target)
    {
        // 50% magic damage, 50% physical damage
        int totalDamage = CalculateSpellDamage();
        
        AttackData ad = new AttackData();
        ad.PhysicalDamage = totalDamage / 2;
        ad.MagicalDamage = totalDamage / 2;
        ad.DamageType = Spell.DamageType;
        
        // Physical portion can be blocked
        if (target.attackComponent.CanBlock(ad))
        {
            ad.PhysicalDamage = 0; // Blocked
        }
        
        // Magic portion affected by magic resistance
        int magicResist = target.GetResist(eDamageType.Spirit);
        ad.MagicalDamage = (int)(ad.MagicalDamage * (1.0 - magicResist * 0.01));
        
        target.TakeDamage(ad);
    }
}
```

### Critical Hit Integration

#### Shared Critical System
```csharp
public class CriticalHitCalculator
{
    public static bool IsCriticalHit(GameLiving attacker, AttackType attackType)
    {
        double critChance = 0;
        
        switch (attackType)
        {
            case AttackType.Melee:
                critChance = attacker.GetModified(eProperty.CriticalMeleeHitChance);
                break;
            case AttackType.Ranged:
                critChance = attacker.GetModified(eProperty.CriticalArcheryHitChance);
                break;
            case AttackType.Spell:
                critChance = attacker.GetModified(eProperty.CriticalSpellHitChance);
                break;
        }
        
        return Util.ChanceDouble(critChance);
    }
    
    public static int CalculateCriticalDamage(int baseDamage, bool isPvP)
    {
        double multiplier = isPvP ? 
            Util.RandomDouble(0.1, 0.5) :    // 10-50% vs players
            Util.RandomDouble(0.1, 1.0);     // 10-100% vs NPCs
            
        return (int)(baseDamage * multiplier);
    }
}
```

## Resistance System Integration

### Unified Resistance Calculation

#### Two-Layer Resistance Model
```csharp
public class ResistanceCalculator
{
    public static int CalculateResistDamage(GameLiving target, int damage, 
                                          eDamageType damageType)
    {
        // Layer 1: Item + Racial + Buff resists
        int primaryResist = GetPrimaryResist(target, damageType);
        int layer1Damage = damage - (damage * primaryResist / 100);
        
        // Layer 2: RA + Spec buff resists
        int secondaryResist = GetSecondaryResist(target, damageType);
        secondaryResist = Math.Min(80, secondaryResist); // 80% cap
        
        int layer2Damage = layer1Damage - (layer1Damage * secondaryResist / 100);
        
        return Math.Max(1, layer2Damage); // Minimum 1 damage
    }
    
    private static int GetPrimaryResist(GameLiving target, eDamageType damageType)
    {
        int resist = 0;
        
        // Item bonuses
        resist += target.GetModified(GetResistProperty(damageType));
        
        // Racial bonuses
        resist += target.GetRacialResistance(damageType);
        
        // Buff effects
        resist += target.GetBuffBonus(GetResistProperty(damageType));
        
        return resist;
    }
}
```

### Resist Piercing
```csharp
public class ResistPiercing
{
    public static void ApplyResistPierce(GameLiving caster, GameLiving target, 
                                       ISpellHandler spellHandler)
    {
        int pierceValue = caster.GetModified(eProperty.ResistPierce);
        
        if (pierceValue > 0)
        {
            // Temporarily reduce target's primary resist
            ResistPierceECSGameEffect pierce = new ResistPierceECSGameEffect(
                new ECSGameEffectInitParams(target, pierceValue, 
                spellHandler.Spell.DamageType));
                
            EffectService.RequestStartEffect(pierce);
        }
    }
}
```

## Buff & Debuff Combat Integration

### Combat Enhancement Buffs

#### Stat Buffs in Combat
```csharp
public class StatBuffEffect : ECSGameSpellEffect
{
    public override void OnStartEffect()
    {
        // Strength buffs affect melee damage
        if (Spell.Property == eProperty.Strength)
        {
            Owner.RecalculateProperty(eProperty.MeleeDamage);
        }
        
        // Dexterity affects weapon skill and casting speed
        if (Spell.Property == eProperty.Dexterity)
        {
            Owner.RecalculateProperty(eProperty.WeaponSkill);
            Owner.RecalculateProperty(eProperty.CastingSpeed);
        }
        
        // Constitution affects hit points
        if (Spell.Property == eProperty.Constitution)
        {
            int hpGain = (int)(Spell.Value * 4); // 4 HP per CON
            Owner.MaxHealth += hpGain;
            Owner.Health += hpGain; // Heal on buff
        }
    }
}
```

#### Combat Speed Buffs
```csharp
public class CombatSpeedBuff : ECSGameSpellEffect
{
    public override void OnStartEffect()
    {
        // Affects both melee and casting speed
        Owner.RecalculateProperty(eProperty.MeleeSpeed);
        Owner.RecalculateProperty(eProperty.CastingSpeed);
        
        // Update active timers
        if (Owner.attackComponent.AttackState)
        {
            Owner.attackComponent.RecalculateAttackSpeed();
        }
        
        if (Owner.castingComponent.IsCasting)
        {
            Owner.castingComponent.UpdateCastSpeed();
        }
    }
}
```

### Debuffs in Combat

#### Disease Effects
```csharp
public class DiseaseSpellHandler : SpellHandler
{
    public override void ApplyEffectOnTarget(GameLiving target)
    {
        // Disease reduces healing effectiveness
        DiseaseECSGameEffect disease = new DiseaseECSGameEffect(
            new ECSGameEffectInitParams(target, Spell.Duration, Spell.Value));
            
        // Healing spells are 50% less effective
        disease.HealingReduction = 0.5;
        
        // Natural regeneration stopped
        disease.PreventRegeneration = true;
        
        EffectService.RequestStartEffect(disease);
    }
}
```

#### Stat Debuffs
```csharp
public class DebuffSpellHandler : SpellHandler
{
    public override int CalculateSpellResistChance(GameLiving target)
    {
        int resistChance = base.CalculateSpellResistChance(target);
        
        // Debuffs use different resist calculation
        resistChance += target.Level - Caster.Level;
        resistChance += target.GetModified(eProperty.MagicResistance);
        
        // Cap at 75% for debuffs
        return Math.Min(75, resistChance);
    }
    
    public override void OnSpellResisted(GameLiving target)
    {
        // Partial resist for debuffs
        if (Util.Chance(25)) // 25% chance for partial effect
        {
            ApplyPartialEffect(target, 0.5); // 50% effectiveness
        }
    }
}
```

## Crowd Control Integration

### CC and Combat Interaction

#### Mesmerize Break on Damage
```csharp
public class MesmerizeEffect : ECSGameSpellEffect
{
    public override void OnDamageDealt(AttackData ad)
    {
        // Any damage breaks mesmerize
        if (ad.Damage > 0)
        {
            CancelEffect(false);
            
            // Send break message
            MessageToCaster(LanguageMgr.GetTranslation(
                "SpellHandler.Mesmerize.TargetHit"));
        }
    }
}
```

#### Stun and Attack Prevention
```csharp
public class StunEffect : ECSGameSpellEffect
{
    public override bool OnAttackAttempt(GameLiving attacker)
    {
        // Prevent all attacks while stunned
        if (attacker == Owner)
        {
            MessageToLiving(Owner, "You are stunned and cannot attack!");
            return false;
        }
        
        return true;
    }
    
    public override bool OnSpellCastAttempt()
    {
        // Prevent spellcasting while stunned
        MessageToLiving(Owner, "You are stunned and cannot cast spells!");
        return false;
    }
}
```

## Spell Power Integration

### Power Management in Combat

#### Power Regeneration During Combat
```csharp
public class PowerRegeneration
{
    public static int CalculatePowerRegen(GameLiving living)
    {
        int basePowerRegen = living.MaxMana / 800; // Base 0.125% per tick
        
        // Combat reduces power regeneration
        if (living.InCombat)
        {
            basePowerRegen /= 2; // 50% reduction in combat
        }
        
        // Meditation skill bonus (out of combat only)
        if (!living.InCombat && living is GamePlayer player)
        {
            int meditation = player.GetSkillLevel(Abilities.Meditation);
            basePowerRegen += meditation / 5; // 20% per meditation level
        }
        
        // Stat bonuses
        int powerStat = living.GetModified(living.GetManaStat());
        basePowerRegen += (powerStat - 50) / 4;
        
        return Math.Max(1, basePowerRegen);
    }
}
```

#### Power Costs and Efficiency
```csharp
public class SpellPowerCalculator
{
    public static int CalculatePowerCost(GameLiving caster, ISpell spell)
    {
        int baseCost = spell.Power;
        
        // Power efficiency from items/buffs
        int powerEfficiency = caster.GetModified(eProperty.PowerEfficiency);
        baseCost = (int)(baseCost * (1.0 - powerEfficiency * 0.01));
        
        // Stat reduction
        int manaStat = caster.GetModified(caster.GetManaStat());
        baseCost = (int)(baseCost * (1.0 - (manaStat - 50) * 0.002));
        
        // Minimum 1 power
        return Math.Max(1, baseCost);
    }
}
```

## Combat Style & Spell Integration

### Style Effects with Magic

#### Magical Style Effects
```csharp
public class MagicalStyle : Style
{
    public ISpell AttachedSpell { get; set; }
    
    public override void ExecuteStyle(GameLiving attacker, GameLiving defender, 
                                    AttackData ad)
    {
        // Execute normal style damage
        base.ExecuteStyle(attacker, defender, ad);
        
        // Cast attached spell
        if (AttachedSpell != null && ad.AttackResult == AttackResult.Hit)
        {
            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(
                attacker, AttachedSpell, AttachedSpellLine);
                
            spellHandler.StartSpell(defender);
        }
    }
}
```

#### Spell-Enhanced Styles
```csharp
public class EnhancedStyleCalculator
{
    public static int CalculateStyleDamage(Style style, GameLiving attacker, 
                                         AttackData ad)
    {
        int baseDamage = style.GetDamageBonus(attacker, ad.Weapon);
        
        // Check for magical enhancement buffs
        foreach (var effect in attacker.effectListComponent.GetAllEffects())
        {
            if (effect is StyleEnhancementEffect styleEff && 
                styleEff.AffectsStyle(style))
            {
                baseDamage = (int)(baseDamage * (1.0 + styleEff.DamageMultiplier));
            }
        }
        
        return baseDamage;
    }
}
```

## Healing & Combat Integration

### Combat Healing Rules

#### Healing Aggro
```csharp
public class HealingAggroHandler
{
    public static void GenerateHealingAggro(GameLiving healer, GameLiving target, 
                                          int healAmount)
    {
        // Generate aggro on all NPCs attacking the heal target
        foreach (GameNPC npc in GetNPCsAttacking(target))
        {
            if (npc.Brain is StandardMobBrain brain)
            {
                // Healing generates aggro equal to heal amount
                brain.AddToAggroList(healer, healAmount);
            }
        }
    }
}
```

#### Combat Healing Penalties
```csharp
public class CombatHealingModifier
{
    public static double GetCombatHealingModifier(GameLiving healer, GameLiving target)
    {
        double modifier = 1.0;
        
        // Self-healing in combat reduced
        if (healer == target && healer.InCombat)
        {
            modifier *= 0.75; // 25% penalty
        }
        
        // Healing others while in melee combat
        if (healer.InCombat && healer.attackComponent.AttackState)
        {
            modifier *= 0.5; // 50% penalty
        }
        
        return modifier;
    }
}
```

## Performance Integration

### Shared Calculation Optimization

#### Property Calculator Reuse
```csharp
public class IntegratedPropertyCalculator
{
    private static readonly Dictionary<eProperty, IPropertyCalculator> _calculators 
        = new Dictionary<eProperty, IPropertyCalculator>();
        
    public static void RegisterCalculator(eProperty property, 
                                        IPropertyCalculator calculator)
    {
        _calculators[property] = calculator;
    }
    
    public static int Calculate(GameLiving living, eProperty property)
    {
        if (_calculators.TryGetValue(property, out var calculator))
        {
            return calculator.Calculate(living);
        }
        
        return living.GetBase(property);
    }
}
```

#### Effect Processing Optimization
```csharp
public class EffectProcessor
{
    public static void ProcessCombatAndMagicEffects(GameLiving living)
    {
        // Single pass through all effects
        foreach (var effect in living.effectListComponent.GetAllEffects())
        {
            // Update combat properties
            if (effect.AffectsCombat)
            {
                effect.UpdateCombatProperties();
            }
            
            // Update magic properties
            if (effect.AffectsMagic)
            {
                effect.UpdateMagicProperties();
            }
            
            // Shared property updates
            effect.UpdateSharedProperties();
        }
    }
}
```

## Configuration Integration

### Shared Configuration Properties

#### Combat & Magic Settings
```xml
<Property Name="SPELL_INTERRUPT_DURATION" Value="2000" />
<Property Name="SPELL_INTERRUPT_REUSE" Value="5000" />
<Property Name="COMBAT_HEALING_PENALTY" Value="25" />
<Property Name="POWER_REGEN_IN_COMBAT" Value="50" />
<Property Name="CRITICAL_CAP_MELEE" Value="50" />
<Property Name="CRITICAL_CAP_SPELL" Value="50" />
<Property Name="RESIST_CAP_PRIMARY" Value="75" />
<Property Name="RESIST_CAP_SECONDARY" Value="80" />
```

#### Damage Type Mappings
```csharp
public static class DamageTypeMapping
{
    public static eDamageType GetMagicEquivalent(eDamageType physicalType)
    {
        return physicalType switch
        {
            eDamageType.Slash => eDamageType.Spirit,
            eDamageType.Thrust => eDamageType.Matter,
            eDamageType.Crush => eDamageType.Body,
            _ => eDamageType.Spirit
        };
    }
}
```

## Test Scenarios

### Integration Testing

#### Combat Spell Casting
- Cast offensive spell during melee combat
- Verify spell interruption mechanics
- Test concentration effects
- Validate power costs in combat

#### Buff Integration
- Apply stat buffs before combat
- Verify damage calculations update
- Test buff removal on death
- Check stacking limitations

#### Resistance Testing
- Cast spell on resistant target
- Verify two-layer calculation
- Test resist piercing effects
- Validate damage minimums

#### Critical Hit Testing
- Test melee critical hits
- Test spell critical hits
- Verify PvP vs PvE differences
- Check critical healing

## Implementation Notes

### Thread Safety
- Property calculations are atomic
- Effect lists protected by locks
- Combat state checks are consistent

### Memory Management
- Shared calculator instances
- Effect pooling for common buffs
- Cached resistance calculations

### Network Protocol
- Combined combat/spell packets
- Efficient effect updates
- Minimal resistance notifications 