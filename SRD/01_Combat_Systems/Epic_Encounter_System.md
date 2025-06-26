# Epic Encounter System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from named boss implementations  
**Implementation Status:** Live

## Overview

The Epic Encounter System provides sophisticated boss mechanics for high-level raid content. This system implements complex multi-phase encounters with custom AI, environmental effects, and coordinated mechanics requiring group coordination.

## Core Architecture

### Epic Boss Base Classes
```csharp
public class GameEpicBoss : GameNPC
{
    public override int MaxHealth => base.MaxHealth * EpicHealthMultiplier;
    public virtual double EpicHealthMultiplier => 5.0; // 500% health
    public virtual bool ImmuneToDamageUntilEngaged => true;
    public virtual int MinimumGroupSize => 8;
    public virtual bool RequiresLOS => false; // Ignores line of sight
}

public class EpicBossBrain : StandardMobBrain
{
    protected int _phaseNumber = 1;
    protected List<GamePlayer> _engagedPlayers = new();
    protected long _lastPhaseChange = 0;
    protected Dictionary<string, long> _abilityTimers = new();
}
```

### Phase Management System
```csharp
public abstract class PhaseController
{
    public virtual void OnPhaseStart(int phase) { }
    public virtual void OnPhaseEnd(int phase) { }
    public virtual bool ShouldChangePhase() => false;
    public virtual void ProcessPhase() { }
    public virtual double GetPhaseHealthThreshold(int phase) => 0.0;
}
```

## Phase Transition Mechanics

### Health-Based Phase Transitions
```csharp
public override bool ShouldChangePhase()
{
    double healthPercent = (double)Body.Health / Body.MaxHealth;
    
    return _phaseNumber switch
    {
        1 => healthPercent <= 0.75,  // Phase 2 at 75%
        2 => healthPercent <= 0.50,  // Phase 3 at 50%
        3 => healthPercent <= 0.25,  // Phase 4 at 25%
        _ => false
    };
}
```

### Engagement Requirements
```csharp
public class EngagementController
{
    private readonly Dictionary<GamePlayer, long> _damageContributions = new();
    private const long MIN_ENGAGEMENT_DAMAGE = 10000;
    
    public bool IsPlayerEngaged(GamePlayer player)
    {
        return _damageContributions.ContainsKey(player) && 
               _damageContributions[player] > MIN_ENGAGEMENT_DAMAGE;
    }
    
    public void TrackDamageContribution(GamePlayer player, int damage)
    {
        if (!_damageContributions.ContainsKey(player))
            _damageContributions[player] = 0;
        _damageContributions[player] += damage;
    }
}
```

## Environmental Effects

### Dynamic Environmental Hazards
```csharp
public class EnvironmentalEffectManager
{
    public void CreateIceSpire(Point3D location, int duration)
    {
        var iceSpire = new GameNPC
        {
            Name = "Ice Spire",
            Model = 1234,
            Position = location,
            MaxHealth = 5000,
            Health = 5000,
            Level = 60
        };
        
        // Schedule explosion after duration
        new RegionTimer(iceSpire, (timer) => 
        {
            ExplodeIceSpire((GameNPC)timer.Owner);
            return 0; // Don't repeat
        }) { Interval = duration * 1000 };
        
        iceSpire.AddToWorld();
    }
    
    private void ExplodeIceSpire(GameNPC spire)
    {
        // Area damage around spire
        foreach (GamePlayer player in spire.GetPlayersInRadius(300))
        {
            player.TakeDamage(spire, eDamageType.Cold, 1000, 0);
            player.SendMessage("The ice spire explodes in deadly shards!", 
                eChatType.CT_System);
        }
        
        spire.RemoveFromWorld();
    }
}
```

## Named Boss Examples

### Nosdoden (Multi-Phase Necromancer)
```csharp
public class NosdodenBrain : StandardMobBrain
{
    private const int PHASE_2_HEALTH = 75;
    private const int PHASE_3_HEALTH = 50;
    private const int PHASE_4_HEALTH = 25;
    
    public override void Think()
    {
        if (!CheckProximityAggro()) return;
        
        ProcessPhaseTransitions();
        ProcessPhaseAbilities();
        
        base.Think();
    }
    
    private void ProcessPhaseTransitions()
    {
        double healthPercent = (double)Body.Health / Body.MaxHealth * 100;
        
        switch (_phaseNumber)
        {
            case 1 when healthPercent <= PHASE_2_HEALTH:
                StartPhase2();
                break;
            case 2 when healthPercent <= PHASE_3_HEALTH:
                StartPhase3();
                break;
            case 3 when healthPercent <= PHASE_4_HEALTH:
                StartPhase4();
                break;
        }
    }
    
    private void StartPhase2()
    {
        _phaseNumber = 2;
        SpawnGhostAdds(4);
        BroadcastMessage("Nosdoden calls upon the spirits of the dead!");
        
        // New abilities in phase 2
        _abilityTimers["ghost_summon"] = Body.CurrentRegion.Time + 30000;
    }
    
    private void StartPhase4()
    {
        _phaseNumber = 4;
        Body.Flags ^= GameNPC.eFlags.GHOST; // Become incorporeal
        _abilityTimers["death_nova"] = Body.CurrentRegion.Time + 10000;
        BroadcastMessage("Nosdoden enters his death throes!");
    }
}
```

### Iarnvidiur (Environmental Interaction)
```csharp
public class IarnvidiurBrain : StandardMobBrain
{
    private readonly List<GameNPC> _iceSpires = new();
    
    public override void OnAttackedByEnemy(AttackData ad)
    {
        base.OnAttackedByEnemy(ad);
        
        // 25% chance to create ice spire when damaged
        if (Util.Chance(25))
        {
            CreateIceSpire(ad.Attacker);
        }
    }
    
    private void CreateIceSpire(GameLiving target)
    {
        var spireLocation = GetRandomLocationNear(target.Position, 500);
        var iceSpire = CreateEnvironmentalHazard("Ice Spire", spireLocation);
        
        // Ice spire explodes after 15 seconds
        new RegionTimer(iceSpire, ExplodeAfterDelay) { Interval = 15000 };
        
        _iceSpires.Add(iceSpire);
    }
}
```

## Add Coordination System

### Coordinated Add Spawning
```csharp
public class AddSpawnController
{
    public void SpawnWaveOfAdds(int count, string addType, Formation formation)
    {
        var spawnPoints = formation.GenerateSpawnPoints(Body.Position, count);
        
        for (int i = 0; i < count; i++)
        {
            var add = CreateAdd(addType);
            add.Position = spawnPoints[i];
            add.TempProperties.SetProperty("BossLink", Body);
            add.AddToWorld();
        }
    }
}

// Specialized add behaviors
public class HealerHunterBrain : StandardMobBrain
{
    public override GameLiving CalculateNextAttackTarget()
    {
        // Prioritize healers
        var healers = GetPlayersInRadius(1500)
            .Where(p => IsHealer(p.CharacterClass))
            .OrderBy(p => p.Health);
            
        return healers.FirstOrDefault() ?? base.CalculateNextAttackTarget();
    }
}
```

## Loot Distribution

### Epic Encounter Rewards
```csharp
public class EpicEncounterLoot
{
    public void DistributeLoot(GameEpicBoss boss, List<GamePlayer> contributors)
    {
        var qualifiedPlayers = contributors
            .Where(p => GetDamageContribution(p) >= MIN_CONTRIBUTION_THRESHOLD)
            .ToList();
            
        if (qualifiedPlayers.Count < boss.MinimumGroupSize)
        {
            DistributeReducedLoot(boss, qualifiedPlayers);
            return;
        }
        
        // Full epic loot for qualified raid
        DistributeEpicLoot(boss, qualifiedPlayers);
    }
    
    private void DistributeEpicLoot(GameEpicBoss boss, List<GamePlayer> players)
    {
        // Guaranteed artifact drops
        var artifactDrops = GetArtifactDropsForBoss(boss.Name);
        foreach (var artifact in artifactDrops)
        {
            DistributeArtifact(artifact, players);
        }
        
        // Bonus realm points
        foreach (var player in players)
        {
            player.GainRealmPoints(CalculateEpicBossRealmPoints(boss.Level));
        }
    }
}
```

## TODO: Missing Documentation

- Advanced synchronization mechanisms between multiple bosses
- Dynamic difficulty scaling based on group size and composition
- Epic encounter lockout and cooldown systems
- Advanced environmental effect scripting framework
- Cross-encounter storyline progression mechanics
- Performance optimization for large-scale encounters

## References

- `GameServer/scripts/namedmobs/` - All named boss implementations
- `GameServer/ai/brain/StandardMobBrain.cs` - Base AI system
- `GameServer/gameobjects/GameEpicBoss.cs` - Epic boss base class
- Various named boss scripts for specific implementations 