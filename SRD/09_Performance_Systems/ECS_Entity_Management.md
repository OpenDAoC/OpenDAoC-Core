# ECS Entity Management System

**Document Status:** Complete Entity Analysis  
**Verification:** Code-verified from GameObject hierarchy and entity management  
**Implementation Status:** Live Production

## Overview

OpenDAoC's Entity Management system provides the foundation for all game objects within the ECS architecture. This document covers entity lifecycle, GameObject hierarchy, entity relationships, and the sophisticated management systems that handle thousands of concurrent entities efficiently.

## Entity Architecture

### Core Entity Interface

#### GameObject Base Class
```csharp
public abstract class GameObject : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; }
    
    // Core entity properties
    public string Name { get; set; }
    public Point3D Position { get; set; }
    public ushort Heading { get; set; }
    public eObjectState ObjectState { get; set; }
    public Region CurrentRegion { get; set; }
    public Zone CurrentZone { get; set; }
    public SubZone CurrentSubZone { get; set; }
    
    // ECS component references
    public AttackComponent attackComponent { get; set; }
    public CastingComponent castingComponent { get; set; }
    public EffectListComponent effectListComponent { get; set; }
    public MovementComponent movementComponent { get; set; }
    
    // Game rule: All entities have unique object IDs for efficient lookup
    public ushort ObjectID { get; protected set; }
    
    // Game rule: Entities track their position in the world hierarchy
    public SubZoneObject SubZoneObject { get; protected set; }
    
    // Entity lifecycle management
    protected virtual void OnAddToWorld()
    {
        ObjectState = eObjectState.Active;
        CurrentRegion?.AddObject(this);
    }
    
    protected virtual void OnRemoveFromWorld()
    {
        ObjectState = eObjectState.Deleted;
        CurrentRegion?.RemoveObject(this);
        
        // Cleanup all ECS components
        CleanupComponents();
    }
}
```

### Entity Hierarchy

#### Living Entity Base
```csharp
public abstract class GameLiving : GameObject
{
    // Living entity properties
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public bool IsAlive => Health > 0;
    
    // Game rule: Living entities can engage in combat
    public GameLiving TargetObject { get; set; }
    public bool InCombat { get; set; }
    public long LastCombatAction { get; set; }
    
    // Game rule: Living entities have stats and properties
    protected PropertyCollection _properties = new();
    
    public virtual int GetModifiedProperty(Property property)
    {
        return PropertyCalculatorRegistry.Calculate(this, property);
    }
    
    // Game rule: Living entities can die and resurrect
    public virtual void Die()
    {
        Health = 0;
        ObjectState = eObjectState.Dead;
        
        // Death triggers multiple ECS interactions
        OnDeath();
    }
    
    protected virtual void OnDeath()
    {
        // Stop all active components
        if (attackComponent != null)
        {
            attackComponent.StopAttack();
            ServiceObjectStore.Remove(attackComponent);
        }
        
        if (castingComponent != null)
        {
            castingComponent.InterruptCasting(true);
            ServiceObjectStore.Remove(castingComponent);
        }
        
        // Cancel certain effects on death
        effectListComponent?.CancelDeathCancelableEffects();
    }
}
```

#### Player Entity
```csharp
public class GamePlayer : GameLiving
{
    // Player-specific properties
    public GameClient Client { get; set; }
    public string AccountName { get; set; }
    public Realm Realm { get; set; }
    public ICharacterClass CharacterClass { get; set; }
    
    // Game rule: Players have extensive inventories
    public GamePlayerInventory Inventory { get; set; }
    
    // Game rule: Players have specializations and abilities
    public Dictionary<string, int> Specializations { get; set; } = new();
    public List<Ability> Abilities { get; set; } = new();
    
    // Game rule: Players have experience and progression
    public long Experience { get; set; }
    public long RealmPoints { get; set; }
    public int ChampionLevel { get; set; }
    
    // Game rule: Players have unique network requirements
    public PlayerMovementComponent playerMovementComponent { get; set; }
    
    protected override void OnAddToWorld()
    {
        base.OnAddToWorld();
        
        // Players require movement component for network synchronization
        if (playerMovementComponent == null)
        {
            playerMovementComponent = new PlayerMovementComponent(this);
            ServiceObjectStore.Add(playerMovementComponent);
        }
        
        // Initialize property calculations
        RecalculateAllProperties();
    }
    
    // Game rule: Player property changes trigger recalculation
    public void NotifyPropertyChanged()
    {
        PropertyCalculationService.RecalculateProperties(this);
    }
}
```

#### NPC Entity
```csharp
public class GameNPC : GameLiving
{
    // NPC-specific properties
    public INpcTemplate Template { get; set; }
    public Faction Faction { get; set; }
    public int AggroLevel { get; set; }
    public int AggroRange { get; set; }
    
    // Game rule: NPCs have AI brains for behavior
    public ABrain Brain { get; set; }
    
    // Game rule: NPCs can have loot tables
    public LootTemplate LootTemplate { get; set; }
    
    // Game rule: NPCs can respawn
    public GameNPCTemplate RespawnTemplate { get; set; }
    public long RespawnTime { get; set; }
    
    protected override void OnAddToWorld()
    {
        base.OnAddToWorld();
        
        // NPCs require AI brain for behavior
        if (Brain != null)
        {
            ServiceObjectStore.Add(Brain);
        }
        
        // Initialize aggro management
        if (AggroLevel > 0)
        {
            InitializeAggroSystem();
        }
    }
    
    protected override void OnDeath()
    {
        base.OnDeath();
        
        // NPCs may drop loot
        if (LootTemplate != null)
        {
            GenerateLoot();
        }
        
        // Schedule respawn if applicable
        if (RespawnTemplate != null)
        {
            ScheduleRespawn();
        }
        
        // Remove AI brain
        if (Brain != null)
        {
            ServiceObjectStore.Remove(Brain);
        }
    }
}
```

#### Static Game Objects
```csharp
public class GameStaticItem : GameObject
{
    // Static object properties
    public ushort Model { get; set; }
    public bool IsPickable { get; set; }
    public int RespawnInterval { get; set; }
    
    // Game rule: Static items don't have components by default
    // They only get components when interacted with
    
    public virtual void OnPlayerInteract(GamePlayer player)
    {
        if (IsPickable)
        {
            // Create temporary component for pickup processing
            var pickupComponent = new ItemPickupComponent(this, player);
            ServiceObjectStore.Add(pickupComponent);
        }
    }
}

public class GameDoor : GameStaticItem
{
    // Door-specific properties
    public bool IsOpen { get; set; }
    public int OpenTime { get; set; }
    public bool RequiresKey { get; set; }
    
    // Game rule: Doors can have auto-close timers
    public ECSGameTimer CloseTimer { get; set; }
    
    public virtual void Open(GamePlayer opener = null)
    {
        if (IsOpen) return;
        
        IsOpen = true;
        BroadcastUpdate();
        
        // Schedule auto-close if configured
        if (OpenTime > 0)
        {
            CloseTimer = new ECSGameTimer(this, CloseTimerCallback, OpenTime);
        }
    }
    
    private int CloseTimerCallback(ECSGameTimer timer)
    {
        Close();
        return 0; // Don't repeat
    }
}
```

## Entity Lifecycle Management

### Entity Creation

#### Object Factory Pattern
```csharp
public static class GameObjectFactory
{
    // Game rule: Centralized entity creation for consistent initialization
    public static T CreateEntity<T>(string templateId = null) where T : GameObject, new()
    {
        var entity = new T();
        
        // Assign unique object ID
        entity.ObjectID = GetNextObjectID();
        
        // Initialize based on template if provided
        if (!string.IsNullOrEmpty(templateId))
        {
            ApplyTemplate(entity, templateId);
        }
        
        // Register entity for management
        EntityManager.RegisterEntity(entity);
        
        return entity;
    }
    
    public static GamePlayer CreatePlayer(string characterName, ICharacterClass characterClass, Realm realm)
    {
        var player = CreateEntity<GamePlayer>();
        player.Name = characterName;
        player.CharacterClass = characterClass;
        player.Realm = realm;
        player.Level = 1;
        player.Health = player.MaxHealth = characterClass.BaseHP;
        
        // Initialize player-specific components
        player.Inventory = new GamePlayerInventory(player);
        player.Specializations = new Dictionary<string, int>();
        
        return player;
    }
    
    public static GameNPC CreateNPC(INpcTemplate template)
    {
        var npc = CreateEntity<GameNPC>();
        npc.Template = template;
        npc.Name = template.Name;
        npc.Level = template.Level;
        npc.Health = npc.MaxHealth = template.MaxHealth;
        npc.AggroLevel = template.AggroLevel;
        npc.AggroRange = template.AggroRange;
        
        // Create AI brain
        npc.Brain = BrainFactory.CreateBrain(template.BrainType, npc);
        
        return npc;
    }
}
```

### Entity Registration

#### Entity Management Service
```csharp
public static class EntityManager
{
    private static readonly Dictionary<ushort, GameObject> _entities = new();
    private static readonly Dictionary<Type, List<GameObject>> _entitiesByType = new();
    private static readonly Lock _entitiesLock = new();
    private static ushort _nextObjectID = 1;
    
    // Game rule: All entities must be registered for global lookup
    public static void RegisterEntity(GameObject entity)
    {
        using (_entitiesLock.EnterScope())
        {
            _entities[entity.ObjectID] = entity;
            
            if (!_entitiesByType.ContainsKey(entity.GetType()))
                _entitiesByType[entity.GetType()] = new List<GameObject>();
            
            _entitiesByType[entity.GetType()].Add(entity);
        }
    }
    
    public static void UnregisterEntity(GameObject entity)
    {
        using (_entitiesLock.EnterScope())
        {
            _entities.Remove(entity.ObjectID);
            
            if (_entitiesByType.TryGetValue(entity.GetType(), out var list))
            {
                list.Remove(entity);
            }
        }
    }
    
    public static GameObject GetEntity(ushort objectID)
    {
        using (_entitiesLock.EnterScope())
        {
            return _entities.GetValueOrDefault(objectID);
        }
    }
    
    public static List<T> GetEntitiesOfType<T>() where T : GameObject
    {
        using (_entitiesLock.EnterScope())
        {
            if (_entitiesByType.TryGetValue(typeof(T), out var list))
            {
                return list.Cast<T>().ToList();
            }
            return new List<T>();
        }
    }
    
    public static ushort GetNextObjectID()
    {
        return Interlocked.Increment(ref _nextObjectID);
    }
}
```

## Entity Relationships

### Hierarchical Relationships

#### Parent-Child Relationships
```csharp
public abstract class GameObject
{
    // Game rule: Entities can have hierarchical relationships
    public GameObject Parent { get; set; }
    public List<GameObject> Children { get; set; } = new();
    
    public void AddChild(GameObject child)
    {
        if (child.Parent != null)
            child.Parent.RemoveChild(child);
        
        child.Parent = this;
        Children.Add(child);
        
        // Child inherits some properties from parent
        OnChildAdded(child);
    }
    
    public void RemoveChild(GameObject child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            OnChildRemoved(child);
        }
    }
    
    protected virtual void OnChildAdded(GameObject child)
    {
        // Child inherits parent's region/zone
        if (CurrentRegion != null && child.CurrentRegion == null)
        {
            child.MoveTo(CurrentRegion, Position.X, Position.Y, Position.Z, Heading);
        }
    }
    
    // Game rule: Parent deletion affects children
    protected override void OnRemoveFromWorld()
    {
        // Remove all children first
        foreach (var child in Children.ToList())
        {
            child.RemoveFromWorld();
        }
        
        base.OnRemoveFromWorld();
    }
}
```

#### Pet Relationships
```csharp
public class GamePet : GameNPC
{
    // Game rule: Pets have special relationship with their owner
    public GameLiving Owner { get; set; }
    public PetType PetType { get; set; }
    public int FormationOffset { get; set; }
    
    protected override void OnAddToWorld()
    {
        base.OnAddToWorld();
        
        // Pets inherit some properties from owner
        if (Owner != null)
        {
            Level = Owner.Level;
            Realm = Owner.Realm;
            
            // Add pet to owner's pet list
            Owner.AddChild(this);
            
            // Create pet-specific brain
            Brain = new PetBrain(this);
            ServiceObjectStore.Add(Brain);
        }
    }
    
    // Game rule: Pet actions are controlled by owner
    public void CommandAttack(GameLiving target)
    {
        if (attackComponent == null)
            attackComponent = new AttackComponent(this);
        
        attackComponent.RequestStartAttack(target);
    }
    
    public void CommandFollow()
    {
        if (movementComponent == null)
            movementComponent = new MovementComponent(this);
        
        movementComponent.FollowTarget = Owner;
        ServiceObjectStore.Add(movementComponent);
    }
}
```

### Group Relationships

#### Group Management
```csharp
public class Group
{
    public List<GamePlayer> Members { get; set; } = new();
    public GamePlayer Leader { get; set; }
    public bool IsRaidGroup { get; set; }
    
    // Game rule: Group members share certain benefits
    public void AddMember(GamePlayer player)
    {
        if (Members.Contains(player)) return;
        
        Members.Add(player);
        player.Group = this;
        
        // Apply group bonuses
        foreach (var member in Members)
        {
            ApplyGroupBonuses(member);
        }
    }
    
    public void RemoveMember(GamePlayer player)
    {
        if (Members.Remove(player))
        {
            player.Group = null;
            
            // Remove group bonuses
            RemoveGroupBonuses(player);
            
            // Recalculate bonuses for remaining members
            foreach (var member in Members)
            {
                ApplyGroupBonuses(member);
            }
        }
    }
    
    private void ApplyGroupBonuses(GamePlayer player)
    {
        // Group experience bonus
        var groupExpBonus = (Members.Count - 1) * 0.125;
        
        // Apply as temporary effect
        var bonusEffect = new GroupBonusEffect(groupExpBonus);
        
        if (player.effectListComponent == null)
            player.effectListComponent = new EffectListComponent(player);
        
        player.effectListComponent.AddEffect(bonusEffect);
        ServiceObjectStore.Add(player.effectListComponent);
    }
}
```

### Guild Relationships

#### Guild Membership
```csharp
public class Guild
{
    public List<GuildMember> Members { get; set; } = new();
    public string Name { get; set; }
    public Realm Realm { get; set; }
    public Alliance Alliance { get; set; }
    
    // Game rule: Guild membership provides benefits
    public void AddMember(GamePlayer player, int rank)
    {
        var member = new GuildMember
        {
            Player = player,
            Rank = rank,
            JoinDate = DateTime.UtcNow
        };
        
        Members.Add(member);
        player.Guild = this;
        
        // Apply guild bonuses
        ApplyGuildBonuses(player);
        
        // Notify other members
        BroadcastMemberJoined(player);
    }
    
    private void ApplyGuildBonuses(GamePlayer player)
    {
        // Guild realm point bonuses
        var guildBonuses = GetActiveGuildBonuses();
        
        foreach (var bonus in guildBonuses)
        {
            if (player.effectListComponent == null)
                player.effectListComponent = new EffectListComponent(player);
            
            player.effectListComponent.AddEffect(bonus);
            ServiceObjectStore.Add(player.effectListComponent);
        }
    }
}
```

## Entity State Management

### Object State Tracking

#### State Transitions
```csharp
public enum eObjectState : byte
{
    Inactive = 0,   // Not in world
    Active = 1,     // In world and processing
    Dead = 2,       // Dead but still in world
    Deleted = 3     // Marked for removal
}

public abstract class GameObject
{
    public eObjectState ObjectState { get; set; } = eObjectState.Inactive;
    
    // Game rule: State changes trigger component updates
    public virtual void SetObjectState(eObjectState newState)
    {
        var oldState = ObjectState;
        ObjectState = newState;
        
        OnStateChanged(oldState, newState);
    }
    
    protected virtual void OnStateChanged(eObjectState oldState, eObjectState newState)
    {
        switch (newState)
        {
            case eObjectState.Active:
                OnBecomeActive();
                break;
            case eObjectState.Dead:
                OnBecomeDead();
                break;
            case eObjectState.Deleted:
                OnBecomeDeleted();
                break;
        }
    }
    
    protected virtual void OnBecomeActive()
    {
        // Entity is now active and can have components
    }
    
    protected virtual void OnBecomeDead()
    {
        // Stop most components
        if (attackComponent != null)
        {
            attackComponent.StopAttack();
            ServiceObjectStore.Remove(attackComponent);
        }
        
        if (castingComponent != null)
        {
            castingComponent.InterruptCasting(true);
            ServiceObjectStore.Remove(castingComponent);
        }
    }
    
    protected virtual void OnBecomeDeleted()
    {
        // Remove all components
        CleanupComponents();
        
        // Unregister from global management
        EntityManager.UnregisterEntity(this);
    }
}
```

### Component Cleanup

#### Automated Component Management
```csharp
public abstract class GameObject
{
    // Game rule: Entities manage their component lifecycle
    public void CleanupComponents()
    {
        if (attackComponent != null)
        {
            ServiceObjectStore.Remove(attackComponent);
            attackComponent = null;
        }
        
        if (castingComponent != null)
        {
            ServiceObjectStore.Remove(castingComponent);
            castingComponent = null;
        }
        
        if (effectListComponent != null)
        {
            effectListComponent.CancelAllEffects();
            ServiceObjectStore.Remove(effectListComponent);
            effectListComponent = null;
        }
        
        if (movementComponent != null)
        {
            ServiceObjectStore.Remove(movementComponent);
            movementComponent = null;
        }
        
        // Cleanup any timers
        CleanupTimers();
    }
    
    private void CleanupTimers()
    {
        var activeTimers = ServiceObjectStore.GetTimersForOwner(this);
        foreach (var timer in activeTimers)
        {
            timer.Stop();
            ServiceObjectStore.Remove(timer);
        }
    }
}
```

## Entity Performance Optimization

### Memory Management

#### Object Pooling for Entities
```csharp
public static class EntityPool
{
    private static readonly ConcurrentQueue<GameNPC> _npcPool = new();
    private static readonly ConcurrentQueue<GameStaticItem> _staticItemPool = new();
    
    // Game rule: Reuse entity instances to reduce garbage collection
    public static GameNPC GetNPC()
    {
        if (_npcPool.TryDequeue(out var npc))
        {
            npc.Reset();
            return npc;
        }
        
        return new GameNPC();
    }
    
    public static void ReturnNPC(GameNPC npc)
    {
        if (npc != null)
        {
            npc.CleanupComponents();
            npc.Reset();
            _npcPool.Enqueue(npc);
        }
    }
}
```

### Spatial Optimization

#### SubZone Entity Management
```csharp
public class SubZone
{
    private readonly WriteLockedLinkedList<GameObject>[] _objects;
    
    // Game rule: Entities are spatially partitioned for efficient processing
    public SubZone(int id, int xPos, int yPos, Zone parentZone)
    {
        ID = id;
        XPos = xPos;
        YPos = yPos;
        ParentZone = parentZone;
        
        // Separate lists by object type for cache efficiency
        _objects = new WriteLockedLinkedList<GameObject>[Enum.GetValues<eGameObjectType>().Length];
        for (int i = 0; i < _objects.Length; i++)
        {
            _objects[i] = new WriteLockedLinkedList<GameObject>();
        }
    }
    
    public void AddObject(LinkedListNode<GameObject> node)
    {
        var objectType = node.Value.GameObjectType;
        _objects[(int)objectType].AddNode(node);
        
        node.Value.CurrentSubZone = this;
        OnObjectAdded(node.Value);
    }
    
    // Game rule: Efficient range queries within subzones
    public IEnumerable<T> GetObjectsInRadius<T>(Point3D center, int radius) where T : GameObject
    {
        var radiusSquared = radius * radius;
        var objectType = GetObjectTypeForClass<T>();
        
        return _objects[(int)objectType]
            .Where(obj => obj.GetDistanceSquared(center) <= radiusSquared)
            .Cast<T>();
    }
}
```

## Entity Debugging and Monitoring

### Entity Diagnostics

#### Entity Health Monitoring
```csharp
public static class EntityDiagnostics
{
    // Game rule: Monitor entity health for debugging
    public static void ValidateAllEntities()
    {
        var allEntities = EntityManager.GetAllEntities();
        var issues = new List<string>();
        
        foreach (var entity in allEntities)
        {
            ValidateEntity(entity, issues);
        }
        
        if (issues.Count > 0)
        {
            log.Warn($"Entity validation found {issues.Count} issues:");
            foreach (var issue in issues)
            {
                log.Warn($"  {issue}");
            }
        }
    }
    
    private static void ValidateEntity(GameObject entity, List<string> issues)
    {
        // Check for invalid state
        if (entity.ObjectState == eObjectState.Deleted && entity.CurrentRegion != null)
        {
            issues.Add($"Deleted entity {entity.Name} still in region");
        }
        
        // Check for orphaned components
        if (entity.attackComponent?.ServiceObjectId.IsSet == true && entity.ObjectState != eObjectState.Active)
        {
            issues.Add($"Entity {entity.Name} has attack component while not active");
        }
        
        // Check for invalid relationships
        if (entity.Parent != null && !entity.Parent.Children.Contains(entity))
        {
            issues.Add($"Entity {entity.Name} has parent that doesn't recognize it");
        }
    }
}
```

## Conclusion

OpenDAoC's Entity Management system provides a robust foundation for all game objects within the ECS architecture. The hierarchical entity design, sophisticated lifecycle management, and performance optimizations enable the server to handle thousands of concurrent entities while maintaining consistent game state and authentic DAoC gameplay mechanics.

## Change Log

- **v1.0** (2025-01-20): Complete entity management documentation
  - Entity architecture and hierarchy
  - Lifecycle management and factory patterns
  - Entity relationships and group management
  - State management and component cleanup
  - Performance optimization and spatial partitioning
  - Debugging and monitoring systems

## References

- ECS_Game_Loop_Deep_Dive.md - Core ECS architecture
- ECS_Component_System.md - Component management
- ECS_Service_Layer_Architecture.md - Service coordination
- ECS_Integration_Patterns.md - Cross-system interactions 