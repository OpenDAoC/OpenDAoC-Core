# Pet & Summoning System

## Document Status
- Status: Comprehensive
- Implementation: Complete

## Overview

**Game Rule Summary**: The Pet & Summoning System lets certain classes summon magical creatures to fight alongside them as loyal companions. Different classes have unique pet styles: Necromancers get permanent undead pets that share damage with their shade, Theurgists summon temporary elemental turrets, Spiritmasters have wolf spirits that follow them everywhere, and Bonedancers command entire armies of skeleton minions. You control your pets through commands like "aggressive" (attack everything), "defensive" (only fight back), or "passive" (don't fight at all), plus movement commands like "stay," "follow," and "go there." Pets are powerful allies that significantly boost your combat effectiveness, but they require management and understanding of their unique behaviors to use effectively.

The pet system allows certain classes to summon and control creatures to fight alongside them. Different classes have unique pet mechanics including permanent pets, temporary summons, and specialized control systems.

## Core Mechanics

### Pet Types

#### Class-Based Pets
- **Theurgist**: Elemental pets (temporary, stationary)
- **Animist**: Plant turrets (temporary, stationary)
- **Cabalist**: Simulacrum (temporary, mobile)
- **Spiritmaster**: Spirit pets (permanent until death)
- **Enchanter**: Underhill companion (permanent)
- **Necromancer**: Undead pets (shade + pet)
- **Bonedancer**: Bone army (commander + minions)
- **Hunter**: Charmed animals (permanent)

### Control System

#### Aggression States
```csharp
public enum eAggressionState : byte
{
    Passive = 0,    // Won't attack anything
    Defensive = 1,  // Attacks only when attacked
    Aggressive = 2  // Attacks any valid target
}
```

#### Walk States
```csharp
public enum eWalkState : byte
{
    Stay = 0,    // Pet stays in place
    Follow = 1,  // Pet follows owner
    GoTarget = 2 // Pet moves to target
}
```

#### Control Distance
- Maximum control distance: 5000 units
- Pet released if owner exceeds distance
- Region change releases pet

### Aggro Mechanics

#### Pet Aggro Split
```csharp
// When pet attacks, aggro is split:
int damage = Math.Max(1, ad.Damage + ad.CriticalDamage);

// Aggro is split between owner (15%) and pet (85%)
int aggroForOwner = (int)(damage * 0.15);
int aggroForPet = damage - aggroForOwner;
```

#### Pet Aggro Behavior
- Passive: No aggro generation
- Defensive: Only attacks current attackers
- Aggressive: Actively seeks targets in range

### Necromancer Pets

#### Shade System
- Shade created on death
- Transfers to pet on summon
- Returns on pet death
- Provides stat bonuses

#### Pet Scaling
```csharp
// Pet stats scale with owner level and shade level
// Stats based on NPCTemplate values
if (NPCTemplate.Strength > 0)
    Strength = (short) Math.Round(Strength * (NPCTemplate.Strength / 100.0));
// Similar for other stats
```

#### Taunt Mode
- Toggleable taunt effect
- Increases aggro generation
- Message on state change

### Bonedancer System

#### Commander Pet
- Controls sub-pets (minions)
- Can toggle minion assist mode
- Synchronizes aggression states
- Special taunt toggle

#### Minion Control
```csharp
// Refresh minion aggression state
if (Brain is IControlledBrain commBrain)
    foreach (IControlledBrain minBrain in ControlledNpcList)
        if (minBrain != null)
            minBrain.SetAggressionState(commBrain.AggressionState);
```

### Animist Turrets

#### Turret Types
- **Bomber**: Self-destructs on contact
- **Melee**: Close combat turret
- **Caster**: Ranged spell turret
- **Healer**: Healing/buff turret

#### Special Mechanics
- Fire-and-Forget (FnF) mode
- Ground-targeted placement
- Limited duration
- Can't be directly controlled

#### FnF Turret Behavior
```csharp
// FnF turrets randomly select targets
if (_filteredAggroList.Count > 0)
    return _filteredAggroList[Util.Random(_filteredAggroList.Count - 1)];
```

### Pet Brain System

#### Controlled Brain Interface
```csharp
public interface IControlledBrain
{
    GameLiving Owner { get; }
    eAggressionState AggressionState { get; set; }
    eWalkState WalkState { get; set; }
    bool IsMainPet { get; set; }
    void Attack(GameObject target);
    void Follow(GameObject target);
    void Stay();
    void ComeHere();
    void Goto(GameObject target);
    void UpdatePetWindow();
}
```

#### Think Cycle
- Base interval: 1500ms
- Checks for new commands
- Updates aggro list
- Processes spell casting
- Maintains follow distance

### Pet Commands

#### Basic Commands
- **Passive/Defensive/Aggressive**: Set aggression
- **Stay/Follow/Go to**: Set movement
- **Attack**: Target specific enemy
- **Release**: Dismiss pet

#### Advanced Commands
- **Cast**: Force spell cast (caster pets)
- **Taunt**: Toggle taunt mode (Necro/BD)
- **Assist**: Toggle minion assist (BD)

### Pet Buffs

#### Buff Propagation
- Group buffs affect pets in range
- Pet-targeted buffs affect nearby pets
- Buffs check pet ownership

#### Defensive Spell Targeting
```csharp
// Find targets for defensive spells
List<GameLiving> targets = new List<GameLiving>();
// Add owner
// Add owner's other pets
// Add group members' pets
// Check range and LoS
```

### Pet Death & Resurrection

#### Death Behavior
- Returns to spawn point
- Clears aggro list
- Updates pet window
- May drop items (configurable)

#### Resurrection
- Some pets can be resurrected
- Others must be resummoned
- Shade returns to Necromancer

## System Interactions

### Combat System
- Pets use standard combat mechanics
- Can block/parry/evade
- Generate aggro normally
- Can be targeted by abilities

### Spell System
- Pets can cast spells (if capable)
- Subject to interruption
- Use mana/power pool
- Can be buffed/debuffed

### Group System
- Pets not counted in group size
- Can receive group buffs
- Visible in group window
- Share group experience rules

### RvR System
- Pets grant realm points when killed
- Can capture objectives
- Affected by crowd control
- Can be banished

## Implementation Notes

### Pet Window Updates
- Health/mana/endurance status
- Stance indicators
- Target information
- Spell readiness

### Database
- NPCTemplate defines pet types
- Spell definitions for summons
- Brain assignments
- Stat scaling rules

### Network
- Pet creation packets
- Status update packets
- Command packets
- Death notifications

## Test Scenarios

### Basic Control Tests
- Summon/release commands
- Movement commands
- Aggression changes
- Max distance release

### Combat Tests
- Aggro split verification
- Taunt effectiveness
- Multiple pet coordination
- Pet vs pet combat

### Special Mechanics
- Shade transfer (Necro)
- Minion control (BD)
- Turret targeting (Animist)
- Charm breaking (Hunter)

### Edge Cases
- Region transitions
- Owner death behavior
- Multiple pets of same type
- Pet stuck scenarios

## Change Log
- Initial documentation created
- Added aggro formulas
- Documented special pet types
- Added brain system details

## References
- GameServer/ai/brain/ControlledMob/ControlledMobBrain.cs
- GameServer/gameobjects/Bonedancer/CommanderPet.cs
- GameServer/gameobjects/Necromancer/NecromancerPet.cs
- GameServer/ai/brain/Animist/ 