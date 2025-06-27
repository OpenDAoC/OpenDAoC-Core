# Poison & Disease System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from poison implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Poisons and diseases are damage-over-time effects that slowly harm you while active. You can apply poisons to your weapons to hurt enemies, but beware of environmental hazards and monsters with venoms. Diseases can spread between nearby players, while poisons from weapons consume charges. Both can be cured with spells or antidote items, though diseases are generally harder to cure than poisons.

The Poison & Disease System manages damage-over-time effects, contagion mechanics, and environmental hazards. This system provides tactical depth through persistent effects that require different treatment strategies.

## Core Architecture

### Poison Types
```csharp
public enum ePoisonType
{
    Physical,       // Weapon-applied poisons
    Environmental,  // Area hazards
    Spell,         // Magical poisons
    Disease,       // Contagious effects
    Venom         // Monster venoms
}

public class PoisonEffect : TimedEffect
{
    public ePoisonType PoisonType { get; set; }
    public int DamagePerTick { get; set; }
    public int TickInterval { get; set; } = 4000; // 4 seconds
    public bool IsContagious { get; set; }
    public int CureLevel { get; set; } // Difficulty to cure
}
```

## Poison Application

### Weapon Poisoning
```csharp
public class WeaponPoisonApplication
{
    public static bool ApplyPoison(GamePlayer poisoner, IWeapon weapon, IPoison poison)
    {
        if (!CanApplyPoison(poisoner, weapon, poison))
            return false;
            
        weapon.PoisonCharges = poison.MaxCharges;
        weapon.PoisonSpellID = poison.SpellID;
        weapon.PoisonMaxCharges = poison.MaxCharges;
        
        poisoner.SendMessage($"You apply {poison.Name} to your {weapon.Name}");
        return true;
    }
    
    public static void ProcessPoisonOnHit(AttackData attackData)
    {
        var weapon = attackData.Weapon;
        if (weapon?.PoisonCharges <= 0)
            return;
            
        // Poison application chance
        if (Util.Chance(POISON_PROC_CHANCE))
        {
            ApplyPoisonToTarget(attackData.Target, weapon.PoisonSpellID);
            weapon.PoisonCharges--;
            
            if (weapon.PoisonCharges <= 0)
            {
                attackData.Attacker.SendMessage("Your weapon is no longer poisoned.");
            }
        }
    }
}
```

### Environmental Poisoning
```csharp
public class EnvironmentalPoison
{
    public static void ProcessPoisonousTerrain(GamePlayer player, Point3D location)
    {
        var terrain = GetTerrainType(location);
        
        if (terrain == TerrainType.PoisonSwamp)
        {
            if (Util.Chance(25)) // 25% chance per tick
            {
                ApplyEnvironmentalPoison(player, "Swamp Poison");
            }
        }
        else if (terrain == TerrainType.ToxicWaste)
        {
            if (Util.Chance(50)) // 50% chance per tick
            {
                ApplyEnvironmentalPoison(player, "Toxic Contamination");
            }
        }
    }
}
```

## Disease Mechanics

### Contagion System
```csharp
public class DiseaseContagion
{
    public static void ProcessDiseaseSpread(GamePlayer diseased)
    {
        if (!HasContagiousDisease(diseased))
            return;
            
        var nearbyPlayers = diseased.GetPlayersInRadius(CONTAGION_RANGE);
        
        foreach (var player in nearbyPlayers)
        {
            if (Util.Chance(CONTAGION_CHANCE))
            {
                SpreadDisease(diseased, player);
            }
        }
    }
    
    private static void SpreadDisease(GamePlayer source, GamePlayer target)
    {
        var disease = GetContagiousDisease(source);
        
        // Resistance check
        if (target.GetResistance(disease.DamageType) > Util.Random(100))
        {
            target.SendMessage("You resist the disease!");
            return;
        }
        
        // Apply weakened version to target
        var spreaderDisease = CreateWeakenedDisease(disease);
        target.EffectList.Add(spreaderDisease);
        
        target.SendMessage($"You have contracted {disease.Name}!");
    }
}
```

### Disease Progression
```csharp
public class DiseaseProgression
{
    public static void ProcessDiseaseStages(DiseaseEffect disease)
    {
        disease.CurrentStage++;
        
        switch (disease.CurrentStage)
        {
            case 1: // Mild symptoms
                disease.DamagePerTick = disease.BaseDamage;
                break;
                
            case 2: // Moderate symptoms
                disease.DamagePerTick = (int)(disease.BaseDamage * 1.5);
                ApplyStatDebuff(disease.Owner, -10);
                break;
                
            case 3: // Severe symptoms
                disease.DamagePerTick = disease.BaseDamage * 2;
                ApplyStatDebuff(disease.Owner, -20);
                ApplyMovementPenalty(disease.Owner, 0.8);
                break;
                
            case 4: // Critical condition
                disease.DamagePerTick = disease.BaseDamage * 3;
                ApplyStatDebuff(disease.Owner, -30);
                ApplyMovementPenalty(disease.Owner, 0.6);
                break;
        }
    }
}
```

## Curing and Treatment

### Cure Spell Mechanics
```csharp
public class CureSpellHandler : SpellHandler
{
    public override bool StartSpell(GameLiving target)
    {
        var poisonEffects = target.EffectList.GetAllOfType<PoisonEffect>();
        var diseaseEffects = target.EffectList.GetAllOfType<DiseaseEffect>();
        
        int curedEffects = 0;
        
        // Cure poisons
        foreach (var poison in poisonEffects)
        {
            if (CanCurePoison(poison, Spell.Value))
            {
                target.EffectList.RemoveEffect(poison);
                curedEffects++;
            }
        }
        
        // Cure diseases (harder to cure)
        foreach (var disease in diseaseEffects)
        {
            if (CanCureDisease(disease, Spell.Value))
            {
                target.EffectList.RemoveEffect(disease);
                curedEffects++;
            }
        }
        
        if (curedEffects > 0)
        {
            MessageToCaster($"You cure {curedEffects} effect(s).");
            return true;
        }
        
        MessageToCaster("There are no effects to cure.");
        return false;
    }
    
    private bool CanCurePoison(PoisonEffect poison, int cureStrength)
    {
        return cureStrength >= poison.CureLevel;
    }
    
    private bool CanCureDisease(DiseaseEffect disease, int cureStrength)
    {
        // Diseases are harder to cure and may require multiple attempts
        int cureChance = Math.Min(90, cureStrength * 10 - disease.CureLevel * 5);
        return Util.Chance(cureChance);
    }
}
```

### Antidote Items
```csharp
public class AntidoteItem : UsableItem
{
    public override bool Use(GamePlayer player)
    {
        var poisonEffects = player.EffectList.GetAllOfType<PoisonEffect>();
        
        if (poisonEffects.Count == 0)
        {
            player.SendMessage("You are not poisoned.");
            return false;
        }
        
        // Remove one poison effect
        var poisonToCure = poisonEffects.OrderBy(p => p.CureLevel).First();
        player.EffectList.RemoveEffect(poisonToCure);
        
        player.SendMessage($"The antidote cures your {poisonToCure.Name}.");
        return true;
    }
}
```

## Immunity and Resistance

### Poison Immunity
```csharp
public class PoisonImmunity
{
    public static bool IsImmuneToPoison(GameLiving target, ePoisonType poisonType)
    {
        // Undead immune to diseases and most poisons
        if (target.BodyType == BodyType.Undead)
        {
            return poisonType == ePoisonType.Disease || 
                   poisonType == ePoisonType.Physical;
        }
        
        // Constructs immune to all biological effects
        if (target.BodyType == BodyType.Construct)
        {
            return poisonType != ePoisonType.Spell;
        }
        
        // Check temporary immunity
        return target.TempProperties.GetProperty("PoisonImmunity", false);
    }
    
    public static void GrantTemporaryImmunity(GameLiving target, int duration)
    {
        target.TempProperties.SetProperty("PoisonImmunity", true);
        
        new RegionTimer(target, (timer) =>
        {
            target.TempProperties.RemoveProperty("PoisonImmunity");
            if (target is GamePlayer player)
            {
                player.SendMessage("Your poison immunity has worn off.");
            }
            return 0;
        }) { Interval = duration };
    }
}
```

## Monster Venoms

### Unique Venom Effects
```csharp
public class MonsterVenoms
{
    public static readonly Dictionary<string, VenomEffect> SpecialVenoms = new()
    {
        ["SpiderVenom"] = new VenomEffect
        {
            DamagePerTick = 50,
            Duration = 30000,
            SpecialEffect = "Paralysis",
            CureLevel = 3
        },
        
        ["SnakeVenom"] = new VenomEffect
        {
            DamagePerTick = 75,
            Duration = 20000,
            SpecialEffect = "MovementSlow",
            CureLevel = 2
        },
        
        ["DragonBreath"] = new VenomEffect
        {
            DamagePerTick = 150,
            Duration = 15000,
            SpecialEffect = "HealthRegenBlock",
            CureLevel = 5
        }
    };
    
    public static void ApplyMonsterVenom(GameNPC monster, GameLiving target, string venomType)
    {
        if (!SpecialVenoms.TryGetValue(venomType, out var venom))
            return;
            
        var venomEffect = new PoisonEffect(venom.Duration)
        {
            DamagePerTick = venom.DamagePerTick,
            PoisonType = ePoisonType.Venom,
            CureLevel = venom.CureLevel,
            Name = $"{monster.Name}'s {venomType}"
        };
        
        target.EffectList.Add(venomEffect);
        
        // Apply special effects
        ApplySpecialVenomEffect(target, venom.SpecialEffect);
    }
}
```

## Configuration

```csharp
[ServerProperty("poison", "enable_poison_system", true)]
public static bool ENABLE_POISON_SYSTEM;

[ServerProperty("poison", "poison_proc_chance", 25)]
public static int POISON_PROC_CHANCE;

[ServerProperty("poison", "contagion_range", 150)]
public static int CONTAGION_RANGE;

[ServerProperty("poison", "contagion_chance", 10)]
public static int CONTAGION_CHANCE;

[ServerProperty("poison", "environmental_poison_enabled", true)]
public static bool ENVIRONMENTAL_POISON_ENABLED;
```

## TODO: Missing Documentation

- Advanced poison resistance calculations
- Cross-system integration with alchemy crafting
- Poison immunity items and artifacts
- Environmental hazard mapping system
- Advanced disease mutation mechanics

## References

- `GameServer/spells/PoisonSpellHandler.cs` - Poison spell implementation
- `GameServer/effects/PoisonEffect.cs` - Poison effect processing
- `GameServer/gameobjects/GamePlayer.cs` - Poison application methods
- Various monster scripts for venom implementations 