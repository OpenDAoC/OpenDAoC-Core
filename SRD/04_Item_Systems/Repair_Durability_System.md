# Repair & Durability System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from durability implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Your equipment gradually wears out through combat use and environmental exposure, reducing its effectiveness until it eventually breaks and becomes unusable. You can repair damaged items at NPC blacksmiths for gold, or learn crafting skills to repair items yourself using materials. Keeping your gear in good condition is essential - broken weapons deal less damage and damaged armor provides less protection.

The Repair & Durability System manages item degradation through use and provides mechanisms for restoration. This system creates economic sinks, encourages maintenance, and adds tactical considerations to equipment management.

## Core Architecture

### Durability Properties
```csharp
public interface IDurableItem
{
    int Durability { get; set; }        // Current durability
    int MaxDurability { get; set; }     // Maximum durability
    int Condition { get; set; }         // Current condition (0-100%)
    bool IsRepairable { get; }          // Can this item be repaired
    double RepairCost { get; }          // Base repair cost
}

public class ItemDurability
{
    public int Current { get; set; }
    public int Maximum { get; set; }
    public double ConditionPercent => (double)Current / Maximum * 100;
    public bool IsBroken => Current <= 0;
    public bool NeedsRepair => ConditionPercent < 75;
}
```

### Durability Loss Mechanics
```csharp
public class DurabilityLossCalculator
{
    public static void ProcessCombatDurabilityLoss(GamePlayer player, AttackData attackData)
    {
        if (attackData.AttackResult == AttackResult.HitStyle || 
            attackData.AttackResult == AttackResult.HitUnstyled)
        {
            // Attacker weapon durability loss
            DamageWeapon(attackData.Weapon, 1);
            
            // Defender armor durability loss
            var hitLocation = DetermineArmorHitLocation(attackData);
            var armor = player.Inventory.GetItem(hitLocation);
            if (armor != null)
            {
                DamageArmor(armor, CalculateArmorDamage(attackData));
            }
        }
    }
    
    private static void DamageWeapon(IWeapon weapon, int damage)
    {
        weapon.Durability = Math.Max(0, weapon.Durability - damage);
        
        if (weapon.Durability == 0)
        {
            weapon.Owner.SendMessage("Your weapon is broken!", eChatType.CT_Important);
        }
        else if (weapon.ConditionPercent < 25)
        {
            weapon.Owner.SendMessage("Your weapon is badly damaged!", eChatType.CT_Important);
        }
    }
}
```

## Durability Loss Factors

### Combat Degradation
```csharp
public static class CombatDurabilityRules
{
    // Weapon durability loss per attack
    public const int WEAPON_DURABILITY_LOSS_PER_HIT = 1;
    public const int WEAPON_DURABILITY_LOSS_PER_MISS = 0;
    
    // Armor durability loss when hit
    public static int CalculateArmorDurabilityLoss(AttackData attackData)
    {
        int baseLoss = 1;
        
        // Higher damage causes more durability loss
        if (attackData.Damage > 100)
            baseLoss += 1;
        if (attackData.Damage > 200)
            baseLoss += 1;
            
        // Critical hits cause extra durability loss
        if (attackData.IsCriticalHit)
            baseLoss += 1;
            
        return baseLoss;
    }
    
    // Special durability loss cases
    public static void ProcessStyleDurabilityLoss(IStyle style, IWeapon weapon)
    {
        // Some styles cause extra weapon wear
        int extraLoss = style.GetDurabilityPenalty();
        weapon.Durability = Math.Max(0, weapon.Durability - extraLoss);
    }
}
```

### Environmental Degradation
```csharp
public class EnvironmentalDurabilityLoss
{
    public static void ProcessEnvironmentalDamage(GamePlayer player)
    {
        var currentWeather = player.CurrentRegion.Weather;
        
        switch (currentWeather)
        {
            case WeatherType.HeavyRain:
                // Metal items rust faster
                DamageMetalItems(player, 1);
                break;
                
            case WeatherType.Sandstorm:
                // All equipment damaged by sand
                DamageAllEquipment(player, 1);
                break;
                
            case WeatherType.Blizzard:
                // Leather items become brittle
                DamageLeatherItems(player, 1);
                break;
        }
    }
}
```

## Repair Systems

### NPC Repair Services
```csharp
public class RepairNPC : GameNPC
{
    public override bool Interact(GamePlayer player)
    {
        ShowRepairOptions(player);
        return true;
    }
    
    private void ShowRepairOptions(GamePlayer player)
    {
        var damagedItems = GetDamagedItems(player);
        
        if (damagedItems.Count == 0)
        {
            SayTo(player, "Your equipment is in perfect condition!");
            return;
        }
        
        long totalCost = CalculateTotalRepairCost(damagedItems);
        
        SayTo(player, $"I can repair your equipment for {totalCost} gold.");
        SayTo(player, "Say 'repair all' to repair everything, or 'repair [item]' for specific items.");
    }
    
    public bool RepairAllItems(GamePlayer player)
    {
        var damagedItems = GetDamagedItems(player);
        long totalCost = CalculateTotalRepairCost(damagedItems);
        
        if (player.GetCurrentMoney() < totalCost)
        {
            SayTo(player, "You don't have enough money for repairs!");
            return false;
        }
        
        player.RemoveMoney(totalCost);
        
        foreach (var item in damagedItems)
        {
            RepairItem(item);
        }
        
        SayTo(player, "All your equipment has been repaired!");
        return true;
    }
}
```

### Player Self-Repair
```csharp
public class PlayerRepairSkill
{
    public static bool CanSelfRepair(GamePlayer player, IItem item)
    {
        // Requires crafting skill matching item type
        var requiredSkill = GetRequiredCraftingSkill(item);
        var playerSkill = player.GetCraftingSkillLevel(requiredSkill);
        
        // Need minimum skill level
        if (playerSkill < item.Level / 2)
            return false;
            
        // Need repair materials
        if (!HasRepairMaterials(player, item))
            return false;
            
        return true;
    }
    
    public static RepairResult AttemptSelfRepair(GamePlayer player, IItem item)
    {
        var skillLevel = player.GetCraftingSkillLevel(GetRequiredCraftingSkill(item));
        var repairChance = CalculateRepairChance(skillLevel, item.Level);
        
        ConsumeRepairMaterials(player, item);
        
        if (Util.Chance(repairChance))
        {
            var repairAmount = CalculateRepairAmount(skillLevel, item);
            item.Durability = Math.Min(item.MaxDurability, item.Durability + repairAmount);
            
            return new RepairResult
            {
                Success = true,
                RepairAmount = repairAmount,
                Message = "You successfully repair the item!"
            };
        }
        else
        {
            // Failed repair can damage item further
            if (Util.Chance(25))
            {
                item.Durability = Math.Max(0, item.Durability - 5);
                return new RepairResult
                {
                    Success = false,
                    Message = "Your repair attempt failed and damaged the item further!"
                };
            }
            
            return new RepairResult
            {
                Success = false,
                Message = "Your repair attempt failed."
            };
        }
    }
}
```

## Repair Costs

### Cost Calculation
```csharp
public class RepairCostCalculator
{
    public static long CalculateRepairCost(IItem item)
    {
        // Base cost depends on item level and type
        long baseCost = item.Level * GetItemTypeCostModifier(item.Type);
        
        // Damage percentage affects cost
        double damagePercent = 1.0 - (double)item.Durability / item.MaxDurability;
        long damageCost = (long)(baseCost * damagePercent);
        
        // Quality affects repair cost
        double qualityModifier = item.Quality / 100.0;
        long finalCost = (long)(damageCost * qualityModifier);
        
        return Math.Max(1, finalCost);
    }
    
    private static double GetItemTypeCostModifier(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => 3.0,
            ItemType.Armor => 2.0,
            ItemType.Shield => 2.5,
            ItemType.Jewelry => 1.5,
            ItemType.Clothing => 1.0,
            _ => 1.0
        };
    }
}
```

### Repair Material Requirements
```csharp
public class RepairMaterials
{
    private static readonly Dictionary<ItemType, List<MaterialRequirement>> _repairMaterials = new()
    {
        [ItemType.Weapon] = new List<MaterialRequirement>
        {
            new("Metal Bars", 2),
            new("Whetstones", 1)
        },
        [ItemType.Armor] = new List<MaterialRequirement>
        {
            new("Metal Bars", 1),
            new("Cloth Squares", 1)
        },
        [ItemType.Shield] = new List<MaterialRequirement>
        {
            new("Wood", 1),
            new("Metal Bars", 1)
        }
    };
    
    public static List<MaterialRequirement> GetRequiredMaterials(IItem item)
    {
        return _repairMaterials.GetValueOrDefault(item.Type, new List<MaterialRequirement>());
    }
}

public record MaterialRequirement(string MaterialName, int Quantity);
```

## Condition Effects

### Performance Degradation
```csharp
public class ConditionEffects
{
    public static double GetWeaponEffectiveness(IWeapon weapon)
    {
        double conditionPercent = (double)weapon.Durability / weapon.MaxDurability;
        
        return conditionPercent switch
        {
            >= 0.75 => 1.0,      // 100% effectiveness
            >= 0.50 => 0.95,     // 95% effectiveness
            >= 0.25 => 0.85,     // 85% effectiveness
            >= 0.10 => 0.70,     // 70% effectiveness
            > 0 => 0.50,         // 50% effectiveness
            _ => 0.0             // Broken - unusable
        };
    }
    
    public static double GetArmorEffectiveness(IArmor armor)
    {
        double conditionPercent = (double)armor.Durability / armor.MaxDurability;
        
        return conditionPercent switch
        {
            >= 0.75 => 1.0,      // Full armor factor
            >= 0.50 => 0.90,     // 90% armor factor
            >= 0.25 => 0.75,     // 75% armor factor
            >= 0.10 => 0.50,     // 50% armor factor
            > 0 => 0.25,         // 25% armor factor
            _ => 0.0             // Broken - no protection
        };
    }
}
```

## Item Breakage

### Broken Item Handling
```csharp
public class BrokenItemSystem
{
    public static void HandleItemBreakage(IItem item)
    {
        if (item.Durability > 0)
            return;
            
        // Mark item as broken
        item.IsBroken = true;
        
        // Remove from active equipment if equipped
        if (item.IsEquipped)
        {
            item.Owner.Inventory.UnequipItem(item);
            item.Owner.SendMessage($"Your {item.Name} breaks and is unequipped!", 
                eChatType.CT_Important);
        }
        
        // Apply broken equipment penalties
        ApplyBrokenEquipmentPenalties(item.Owner);
    }
    
    private static void ApplyBrokenEquipmentPenalties(GamePlayer player)
    {
        var brokenItems = player.Inventory.GetBrokenItems();
        
        // Each broken item reduces overall effectiveness
        double penaltyPercent = brokenItems.Count * 0.05; // 5% per broken item
        player.TempProperties.SetProperty("BrokenEquipmentPenalty", penaltyPercent);
    }
}
```

## Configuration

```csharp
[ServerProperty("repair", "enable_durability_system", true)]
public static bool ENABLE_DURABILITY_SYSTEM;

[ServerProperty("repair", "durability_loss_per_hit", 1)]
public static int DURABILITY_LOSS_PER_HIT;

[ServerProperty("repair", "environmental_durability_loss", true)]
public static bool ENVIRONMENTAL_DURABILITY_LOSS;

[ServerProperty("repair", "repair_cost_multiplier", 1.0)]
public static double REPAIR_COST_MULTIPLIER;

[ServerProperty("repair", "self_repair_enabled", true)]
public static bool SELF_REPAIR_ENABLED;

[ServerProperty("repair", "broken_item_penalties", true)]
public static bool BROKEN_ITEM_PENALTIES;
```

## TODO: Missing Documentation

- Advanced repair quality mechanics and masterwork restoration
- Guild-based repair services and benefits
- Artifact-specific durability and repair requirements
- Durability insurance and protection systems
- Dynamic repair costs based on server economy
- Mass repair tools and automated maintenance systems

## References

- `GameServer/gameobjects/GameInventoryItem.cs` - Item durability properties
- `GameServer/craft/Repair/` - Repair skill implementations
- Various NPC repair merchant implementations
- Combat system durability loss integration 