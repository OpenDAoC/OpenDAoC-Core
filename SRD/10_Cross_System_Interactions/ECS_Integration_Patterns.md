# ECS Integration Patterns

**Document Status:** Complete Cross-System Analysis  
**Verification:** Code-verified across all system integrations  
**Implementation Status:** Live Production

## Overview

OpenDAoC's Entity Component System (ECS) serves as the central coordination hub for all game systems. This document details how ECS integrates with every major system, the interaction patterns used, and the sophisticated coordination mechanisms that ensure consistent game state across all systems.

## Combat System Integration

### Attack Processing Flow

#### ECS-Combat Coordination
```csharp
// Game rule: Combat actions trigger multiple ECS components
public void RequestStartAttack(GameObject attackTarget = null)
{
    // Stop any current casting
    if (owner.castingComponent != null)
    {
        owner.castingComponent.InterruptCasting(false);
        ServiceObjectStore.Remove(owner.castingComponent);
    }
    
    // Register attack component for processing
    _startAttackTarget = attackTarget ?? owner.TargetObject;
    StartAttackRequested = true;
    ServiceObjectStore.Add(this);
}

// Game rule: Combat results trigger effect components
public void OnAttackComplete(AttackResult result)
{
    if (result.TriggeredEffect != null)
    {
        target.effectListComponent.AddEffect(result.TriggeredEffect);
        ServiceObjectStore.Add(target.effectListComponent);
    }
    
    // Combat may trigger movement effects (knockback, teleport)
    if (result.MovementEffect != null)
    {
        target.movementComponent.ApplyMovementEffect(result.MovementEffect);
        ServiceObjectStore.Add(target.movementComponent);
    }
}
```

#### Combat State Management
```csharp
public class AttackComponent : IServiceObject
{
    // Game rule: Attack components coordinate with multiple systems
    public void Tick()
    {
        // Validate movement hasn't broken attack range
        if (!IsWithinRange(_startAttackTarget))
        {
            InterruptAttack("Target out of range");
            ServiceObjectStore.Remove(this);
            return;
        }
        
        // Coordinate with effect system for damage bonuses
        var damageBonus = owner.effectListComponent?.GetDamageBonus(DamageType.Melee) ?? 0;
        
        // Coordinate with property system for weapon skill
        var weaponSkill = owner.GetModifiedSpecLevel(_weaponSpec);
        
        // Process attack with coordinated data
        if (!attackAction.Tick())
            ServiceObjectStore.Remove(this);
    }
}
```

## Magic System Integration

### Spell Casting Coordination

#### ECS-Magic Integration
```csharp
public class CastingComponent : IServiceObject
{
    // Game rule: Casting coordinates with multiple systems
    public void StartCasting(Spell spell, GameObject target)
    {
        // Interrupt any current attacks
        if (owner.attackComponent != null)
        {
            owner.attackComponent.StopAttack();
            ServiceObjectStore.Remove(owner.attackComponent);
        }
        
        // Coordinate with effect system for casting speed bonuses
        var castSpeedBonus = owner.effectListComponent?.GetCastingSpeedBonus() ?? 0;
        
        // Calculate modified cast time
        var modifiedCastTime = CalculateModifiedCastTime(spell, castSpeedBonus);
        
        spellToCast = spell;
        spellTarget = target;
        startCastTime = GameLoop.GameLoopTime;
        
        ServiceObjectStore.Add(this);
    }
    
    // Game rule: Spell completion triggers effect components
    private void CompleteCasting()
    {
        var spellEffect = spellToCast.CreateEffect(owner, spellTarget);
        
        if (spellTarget.effectListComponent == null)
            spellTarget.effectListComponent = new EffectListComponent(spellTarget);
        
        spellTarget.effectListComponent.AddEffect(spellEffect);
        ServiceObjectStore.Add(spellTarget.effectListComponent);
    }
}
```

### Effect Processing Integration

#### Spell Effects ECS Coordination
```csharp
public class EffectListComponent : IServiceObject
{
    // Game rule: Effects coordinate with property calculation system
    public void Tick()
    {
        bool propertiesChanged = false;
        
        foreach (var effect in _effects.ToList())
        {
            if (effect.ShouldExpire())
            {
                effect.Cancel(false);
                _effects.Remove(effect);
                propertiesChanged = true;
            }
            else if (ServiceUtils.ShouldTick(effect.NextTick))
            {
                effect.Tick();
                
                // Some effects modify properties
                if (effect.ModifiesProperties())
                    propertiesChanged = true;
            }
        }
        
        // Trigger property recalculation if needed
        if (propertiesChanged)
        {
            owner.NotifyPropertyChanged();
        }
        
        // Remove component if no active effects
        if (_effects.Count == 0 && _immunityEffects.Count == 0)
            ServiceObjectStore.Remove(this);
    }
}
```

## Character System Integration

### Property Calculation Coordination

#### ECS-Property System Integration
```csharp
public interface IPropertySource
{
    int GetBase(Property property);
    int GetItemBonus(Property property);
    int GetBuffBonus(Property property);
    int GetDebuffPenalty(Property property);
    IList<IPropertyModifier> GetModifiers(Property property);
}

// Game rule: Property calculations integrate with effect system
public class CharacterPropertySource : IPropertySource
{
    private readonly GameObject _character;
    
    public int GetBuffBonus(Property property)
    {
        if (_character.effectListComponent == null)
            return 0;
        
        // Coordinate with ECS effect system
        return _character.effectListComponent.GetBonusForProperty(property);
    }
    
    public int GetDebuffPenalty(Property property)
    {
        if (_character.effectListComponent == null)
            return 0;
        
        // Coordinate with ECS effect system
        return _character.effectListComponent.GetPenaltyForProperty(property);
    }
}
```

### Experience and Progression Integration

#### ECS-Progression Coordination
```csharp
public class CharacterProgressionService
{
    // Game rule: Progression events trigger multiple ECS systems
    public void GrantExperience(ICharacter character, long experience)
    {
        character.Experience += experience;
        
        if (ShouldLevelUp(character))
        {
            LevelUp(character);
            
            // Level up may trigger effect updates
            if (character.effectListComponent != null)
            {
                character.effectListComponent.CheckLevelDependentEffects();
                ServiceObjectStore.Add(character.effectListComponent);
            }
            
            // Level up may trigger property recalculation
            character.NotifyPropertyChanged();
        }
    }
}
```

## Movement System Integration

### Position and Zone Coordination

#### ECS-Movement Integration
```csharp
public class MovementComponent : IServiceObject
{
    // Game rule: Movement coordinates with zone and combat systems
    protected virtual void TickInternal()
    {
        // Update position
        UpdatePosition();
        
        // Check for zone transitions
        if (!Owner.IsSamePosition(_positionDuringLastSubZoneRelocationCheck) && 
            ServiceUtils.ShouldTick(_nextSubZoneRelocationCheckTick))
        {
            _nextSubZoneRelocationCheckTick = GameLoop.GameLoopTime + SUBZONE_RELOCATION_CHECK_INTERVAL;
            _positionDuringLastSubZoneRelocationCheck = new Point2D(Owner.X, Owner.Y);
            
            // Trigger zone service if needed
            Owner.SubZoneObject.CheckForRelocation();
        }
        
        // Validate combat range
        if (Owner.attackComponent != null)
        {
            ValidateCombatRange();
        }
    }
    
    private void ValidateCombatRange()
    {
        if (!IsWithinAttackRange(Owner.attackComponent.Target))
        {
            Owner.attackComponent.InterruptAttack("Movement broke attack range");
            ServiceObjectStore.Remove(Owner.attackComponent);
        }
    }
}
```

### Zone Transition Integration

#### ECS-Zone System Coordination
```csharp
public class ZoneService
{
    // Game rule: Zone transitions affect all character components
    private static void ProcessZoneTransition(LinkedListNode<GameObject> node, 
                                            SubZone currentSubZone, 
                                            SubZone destinationSubZone, 
                                            Zone destinationZone)
    {
        GameObject obj = node.Value;
        
        // Stop all active components during zone transition
        if (obj.attackComponent != null)
        {
            obj.attackComponent.StopAttack();
            ServiceObjectStore.Remove(obj.attackComponent);
        }
        
        if (obj.castingComponent != null)
        {
            obj.castingComponent.InterruptCasting(true);
            ServiceObjectStore.Remove(obj.castingComponent);
        }
        
        // Effects may be zone-dependent
        if (obj.effectListComponent != null)
        {
            obj.effectListComponent.CheckZoneDependentEffects(destinationZone);
            ServiceObjectStore.Add(obj.effectListComponent);
        }
        
        // Process the actual zone change
        currentSubZone.RemoveObject(node);
        destinationSubZone.AddObject(node);
    }
}
```

## Item System Integration

### Equipment and Bonuses Coordination

#### ECS-Item Integration
```csharp
public class InventoryService
{
    // Game rule: Equipment changes trigger property and effect updates
    public void EquipItem(ICharacter character, IItem item, EquipmentSlot slot)
    {
        var previousItem = character.Inventory.GetEquippedItem(slot);
        
        // Unequip previous item
        if (previousItem != null)
        {
            UnequipItem(character, slot);
        }
        
        // Equip new item
        character.Inventory.Equip(item, slot);
        
        // Item effects may create effect components
        foreach (var itemEffect in item.GetActiveEffects())
        {
            if (character.effectListComponent == null)
                character.effectListComponent = new EffectListComponent(character);
            
            character.effectListComponent.AddEffect(itemEffect);
            ServiceObjectStore.Add(character.effectListComponent);
        }
        
        // Trigger property recalculation
        character.NotifyPropertyChanged();
        
        // Equipment may affect ongoing actions
        if (character.castingComponent != null)
        {
            ValidateCastingRequirements(character);
        }
    }
}
```

### Artifact Integration

#### ECS-Artifact Coordination
```csharp
public class ArtifactService
{
    // Game rule: Artifact leveling triggers effect updates
    public void LevelUpArtifact(IArtifact artifact, ICharacter owner)
    {
        artifact.ArtifactLevel++;
        
        // Unlock new artifact abilities
        var newAbilities = GetUnlockedAbilities(artifact);
        
        foreach (var ability in newAbilities)
        {
            var abilityEffect = ability.CreateEffect(owner);
            
            if (owner.effectListComponent == null)
                owner.effectListComponent = new EffectListComponent(owner);
            
            owner.effectListComponent.AddEffect(abilityEffect);
            ServiceObjectStore.Add(owner.effectListComponent);
        }
        
        // Trigger property recalculation
        owner.NotifyPropertyChanged();
    }
}
```

## Guild System Integration

### Guild Benefits Coordination

#### ECS-Guild Integration
```csharp
public class GuildService
{
    // Game rule: Guild buffs integrate with effect system
    public void ApplyGuildBuff(IGuild guild, Spell guildBuff)
    {
        foreach (var member in guild.Members.Where(m => m.Character.IsOnline))
        {
            var character = member.Character;
            var buffEffect = guildBuff.CreateEffect(character, character);
            
            if (character.effectListComponent == null)
                character.effectListComponent = new EffectListComponent(character);
            
            character.effectListComponent.AddEffect(buffEffect);
            ServiceObjectStore.Add(character.effectListComponent);
        }
    }
    
    // Game rule: Guild claiming affects multiple systems
    public void ClaimKeep(IGuild guild, IKeep keep)
    {
        keep.Guild = guild;
        
        // Guild claiming may trigger realm bonuses
        foreach (var member in guild.Members)
        {
            if (member.Character.IsOnline)
            {
                UpdateRealmBonuses(member.Character);
                member.Character.NotifyPropertyChanged();
            }
        }
    }
}
```

## Crafting System Integration

### Crafting Process Coordination

#### ECS-Crafting Integration
```csharp
public class CraftingService
{
    // Game rule: Crafting creates ECS components for progress tracking
    public void StartCrafting(ICrafter crafter, IRecipe recipe)
    {
        // Stop any current actions
        if (crafter.attackComponent != null)
        {
            crafter.attackComponent.StopAttack();
            ServiceObjectStore.Remove(crafter.attackComponent);
        }
        
        if (crafter.castingComponent != null)
        {
            crafter.castingComponent.InterruptCasting(false);
            ServiceObjectStore.Remove(crafter.castingComponent);
        }
        
        // Create crafting component
        var craftComponent = new CraftComponent(crafter);
        craftComponent.recipe = recipe;
        craftComponent.startTime = GameLoop.GameLoopTime;
        craftComponent.craftingTime = CalculateCraftTime(crafter, recipe);
        
        ServiceObjectStore.Add(craftComponent);
        
        // Crafting may benefit from effect bonuses
        var craftingBonus = crafter.effectListComponent?.GetCraftingBonus() ?? 0;
        if (craftingBonus > 0)
        {
            craftComponent.ApplyBonus(craftingBonus);
        }
    }
}
```

## Quest System Integration

### Quest Progress Coordination

#### ECS-Quest Integration
```csharp
public class QuestService
{
    // Game rule: Quest objectives monitor ECS events
    public void OnKillTarget(ICharacter killer, INPC target)
    {
        var activeQuests = killer.QuestLog.GetActiveQuests();
        
        foreach (var quest in activeQuests)
        {
            if (quest.HasKillObjective(target))
            {
                quest.UpdateProgress(killer);
                
                // Quest completion may trigger effects
                if (quest.IsComplete())
                {
                    var questReward = quest.GetCompletionReward();
                    if (questReward.HasEffects())
                    {
                        foreach (var effect in questReward.Effects)
                        {
                            if (killer.effectListComponent == null)
                                killer.effectListComponent = new EffectListComponent(killer);
                            
                            killer.effectListComponent.AddEffect(effect);
                            ServiceObjectStore.Add(killer.effectListComponent);
                        }
                    }
                }
            }
        }
    }
}
```

## Housing System Integration

### House Management Coordination

#### ECS-Housing Integration
```csharp
public class HousingService
{
    // Game rule: House bonuses integrate with effect system
    public void EnterHouse(ICharacter character, IHouse house)
    {
        // Apply house bonuses
        var houseBonuses = house.GetActiveBonuses();
        
        foreach (var bonus in houseBonuses)
        {
            var bonusEffect = bonus.CreateEffect(character);
            
            if (character.effectListComponent == null)
                character.effectListComponent = new EffectListComponent(character);
            
            character.effectListComponent.AddEffect(bonusEffect);
            ServiceObjectStore.Add(character.effectListComponent);
        }
        
        // House entry may stop combat
        if (character.attackComponent != null && !house.AllowsCombat())
        {
            character.attackComponent.StopAttack();
            ServiceObjectStore.Remove(character.attackComponent);
        }
    }
}
```

## AI System Integration

### NPC Behavior Coordination

#### ECS-AI Integration
```csharp
public abstract class ABrain : IServiceObject
{
    // Game rule: AI brains coordinate with all game systems
    public virtual void Tick()
    {
        if (Body?.ObjectState != eObjectState.Active)
        {
            ServiceObjectStore.Remove(this);
            return;
        }
        
        // AI decisions may trigger combat
        if (ShouldStartAttack())
        {
            var target = SelectTarget();
            if (target != null)
            {
                if (Body.attackComponent == null)
                    Body.attackComponent = new AttackComponent(Body);
                
                Body.attackComponent.RequestStartAttack(target);
            }
        }
        
        // AI decisions may trigger spells
        if (ShouldCastSpell())
        {
            var spell = SelectSpell();
            var target = SelectSpellTarget(spell);
            
            if (Body.castingComponent == null)
                Body.castingComponent = new CastingComponent(Body);
            
            Body.castingComponent.StartCasting(spell, target);
        }
        
        // AI decisions may trigger movement
        if (ShouldMove())
        {
            var destination = SelectMovementDestination();
            Body.movementComponent.MoveTo(destination);
            ServiceObjectStore.Add(Body.movementComponent);
        }
    }
}
```

## Network System Integration

### Client Communication Coordination

#### ECS-Network Integration
```csharp
public class ClientService
{
    // Game rule: Network events trigger ECS components
    public static void ProcessPlayerInput(GameClient client, GSPacketIn packet)
    {
        switch (packet.ID)
        {
            case 0xA0: // Attack packet
                if (client.Player.attackComponent == null)
                    client.Player.attackComponent = new AttackComponent(client.Player);
                
                var target = GetTargetFromPacket(packet);
                client.Player.attackComponent.RequestStartAttack(target);
                break;
                
            case 0xB0: // Cast spell packet
                if (client.Player.castingComponent == null)
                    client.Player.castingComponent = new CastingComponent(client.Player);
                
                var spell = GetSpellFromPacket(packet);
                var spellTarget = GetSpellTargetFromPacket(packet);
                client.Player.castingComponent.StartCasting(spell, spellTarget);
                break;
                
            case 0xC0: // Movement packet
                if (client.Player.movementComponent == null)
                    client.Player.movementComponent = new PlayerMovementComponent(client.Player);
                
                var position = GetPositionFromPacket(packet);
                client.Player.movementComponent.UpdatePosition(position);
                ServiceObjectStore.Add(client.Player.movementComponent);
                break;
        }
    }
}
```

## Performance Integration Patterns

### Cross-System Optimization

#### Coordinated Processing
```csharp
// Game rule: Systems coordinate to minimize redundant calculations
public class PropertyCalculationService
{
    private static readonly Dictionary<GameObject, long> _lastCalculationTime = new();
    
    public static void RecalculateProperties(GameObject owner)
    {
        var currentTime = GameLoop.GameLoopTime;
        
        // Avoid redundant calculations within same tick
        if (_lastCalculationTime.TryGetValue(owner, out var lastTime) && 
            lastTime == currentTime)
        {
            return;
        }
        
        _lastCalculationTime[owner] = currentTime;
        
        // Coordinate with all systems that affect properties
        var effectBonuses = owner.effectListComponent?.GetAllBonuses() ?? new Dictionary<Property, int>();
        var itemBonuses = owner.inventory?.GetEquipmentBonuses() ?? new Dictionary<Property, int>();
        var guildBonuses = owner.guild?.GetActiveBonuses() ?? new Dictionary<Property, int>();
        
        // Recalculate all properties
        foreach (Property property in Enum.GetValues<Property>())
        {
            var calculator = PropertyCalculatorRegistry.Get(property);
            var newValue = calculator.Calculate(owner, effectBonuses, itemBonuses, guildBonuses);
            owner.SetProperty(property, newValue);
        }
    }
}
```

## Integration Monitoring

### Cross-System Health Checks

#### Integration Diagnostics
```csharp
public static class IntegrationDiagnostics
{
    // Game rule: Monitor integration points for consistency
    public static void ValidateSystemIntegration()
    {
        var allComponents = ServiceObjectStore.GetAllComponents();
        
        foreach (var component in allComponents)
        {
            // Validate component owner exists
            if (component.Owner == null || component.Owner.ObjectState != eObjectState.Active)
            {
                log.Warn($"Component {component.GetType()} has invalid owner");
                ServiceObjectStore.Remove(component);
                continue;
            }
            
            // Validate cross-references
            switch (component)
            {
                case AttackComponent attack:
                    ValidateAttackIntegration(attack);
                    break;
                case CastingComponent casting:
                    ValidateCastingIntegration(casting);
                    break;
                case EffectListComponent effects:
                    ValidateEffectIntegration(effects);
                    break;
            }
        }
    }
    
    private static void ValidateAttackIntegration(AttackComponent attack)
    {
        // Ensure attack target is valid
        if (attack.Target != null && attack.Target.ObjectState != eObjectState.Active)
        {
            attack.StopAttack();
            ServiceObjectStore.Remove(attack);
        }
        
        // Ensure no conflicting casting component
        if (attack.Owner.castingComponent != null)
        {
            log.Warn($"Player {attack.Owner.Name} has both attack and casting components");
        }
    }
}
```

## Conclusion

OpenDAoC's ECS integration patterns demonstrate sophisticated coordination between all game systems. The careful orchestration of component interactions, service dependencies, and cross-system communication ensures consistent game state while maintaining high performance. These integration patterns enable complex gameplay mechanics while preserving system modularity and testability.

## Change Log

- **v1.0** (2025-01-20): Complete cross-system integration analysis
  - Combat system ECS coordination
  - Magic system integration patterns
  - Character progression coordination
  - Movement and zone integration
  - Item system coordination
  - Guild, crafting, quest, housing integrations
  - AI and network system coordination
  - Performance optimization patterns
  - Integration monitoring and diagnostics

## References

- ECS_Game_Loop_Deep_Dive.md - Core ECS architecture
- ECS_Component_System.md - Component details
- ECS_Service_Layer_Architecture.md - Service coordination
- All system-specific SRD documents for detailed mechanics 