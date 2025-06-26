# Interrupt System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The interrupt system prevents characters from performing certain actions after being attacked or taking other disruptive actions. It affects spell casting, ranged attacks, and ability usage with complex timing mechanics based on attack type and character state.

## Core Mechanics

### Interrupt Timer Architecture

#### Timer Components
```csharp
public class GameLiving
{
    private readonly object _interruptTimerLock = new object();
    
    // Properties
    public GameObject LastInterrupter { get; private set; }
    public long InterruptTime { get; private set; }
    public long SelfInterruptTime { get; private set; }
    public long InterruptRemainingDuration => Math.Max(InterruptTime, SelfInterruptTime) - GameLoop.GameLoopTime;
    
    // State checks
    public virtual bool IsBeingInterrupted => IsBeingInterruptedIgnoreSelfInterrupt || SelfInterruptTime > GameLoop.GameLoopTime;
    public virtual bool IsBeingInterruptedIgnoreSelfInterrupt => InterruptTime > GameLoop.GameLoopTime;
}
```

### Interrupt Initiation

#### StartInterruptTimer Method
```csharp
public virtual void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
{
    long newInterruptTime = GameLoop.GameLoopTime + duration;
    
    // 3% reduced interrupt chance per level difference
    if (!Util.Chance(100 + (attacker.EffectiveLevel - EffectiveLevel) * 3))
        return;
        
    lock (_interruptTimerLock)
    {
        bool wasAlreadyInterrupted = IsBeingInterrupted;
        
        // Don't update if new interrupt is shorter
        if (InterruptTime >= newInterruptTime)
            return;
            
        InterruptTime = newInterruptTime;
        LastInterrupter = attacker;
        
        // Prevent multiple interrupt executions
        if (wasAlreadyInterrupted)
            return;
    }
    
    // Perform actual interrupt
    PerformInterrupt(attacker, attackType);
}
```

### Interrupt Duration Mechanics

#### Standard Interrupt Durations
```csharp
// Base spell interrupt duration
public virtual int SpellInterruptDuration => Properties.SPELL_INTERRUPT_DURATION;  // Default: 3000ms

// Additional interrupt if interrupted again
public virtual int SpellInterruptRecastAgain => Properties.SPELL_INTERRUPT_AGAIN;  // Default: 500ms

// Self-interrupt after melee attack
public virtual int SelfInterruptDurationOnMeleeAttack => 3000;  // Players: 3000ms
```

#### NPC-Specific Interrupts
```csharp
public override void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
{
    // NPCs get longer interrupt to prevent immediate retaliation
    if (attacker != this)
    {
        if (Brain is not IControlledBrain controlledBrain || controlledBrain.GetPlayerOwner() == null)
            duration += 2500;  // Additional 2.5 seconds for NPCs
    }
    
    base.StartInterruptTimer(duration, attackType, attacker);
}
```

### Interrupt Types

#### 1. Attack-Based Interrupts
```csharp
// In AttackComponent.LivingMakeAttack()
// Target interrupt
ad.Target.StartInterruptTimer(interval, ad.AttackType, ad.Attacker);

// Self-interrupt for melee attacks
if (ad.IsMeleeAttack)
    owner.StartInterruptTimer(owner.SelfInterruptDurationOnMeleeAttack, ad.AttackType, ad.Attacker);
```

**Interrupt Durations by Attack Type**:
- **Melee Attack**: Uses weapon interval
- **Ranged Attack**: Uses weapon interval
- **Spell Damage**: Uses SpellInterruptDuration
- **Style Effects**: Varies by style

#### 2. Spell Casting Interrupts
```csharp
// Spell cast interrupt check
public override bool CasterIsAttacked(GameLiving attacker)
{
    // Uninterruptible spells
    if (Spell.Uninterruptible)
        return false;
        
    // Special immunities
    if (Caster.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration) ||
        Caster.effectListComponent.ContainsEffectForEffectType(eEffect.FacilitatePainworking) ||
        Caster.effectListComponent.ContainsEffectForEffectType(eEffect.QuickCast))
        return false;
        
    // Distance-based uninterruptibility
    if (Spell.Uninterruptible && attacker.GetDistanceTo(Caster) > UNINTERRUPTIBLE_SPELL_RANGE)
        return false;
        
    // Interrupt the cast
    Caster.LastInterruptMessage = attacker.GetName(0, true) + " attacks you and your spell is interrupted!";
    InterruptCasting();
    return true;
}
```

#### 3. Crowd Control Interrupts
Many crowd control effects apply interrupt timers:
```csharp
// Silence effect
effect.Owner.SilencedTime = effect.Owner.CurrentRegion.Time + CalculateEffectDuration(effect.Owner);
effect.Owner.StopCurrentSpellcast();
effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);

// Amnesia (instant)
if (Spell.CastTime == 0)
    target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);

// Disease
target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
```

### Ranged Attack Interrupts

#### Interrupt Window
```csharp
protected virtual bool CheckRangedAttackInterrupt(GameLiving attacker, AttackData.eAttackType attackType)
{
    // Sure Shot cannot be interrupted by non-melee
    if (rangeAttackComponent.RangedAttackType == eRangedAttackType.SureShot)
    {
        if (attackType is not eAttackType.MeleeOneHand
            and not eAttackType.MeleeTwoHand
            and not eAttackType.MeleeDualWield)
            return false;
    }
    
    // Check if past halfway point
    long elapsedTime = GameLoop.GameLoopTime - rangeAttackComponent.AttackStartTime;
    long halfwayPoint = attackComponent.AttackSpeed(ActiveWeapon) / 2;
    
    if (rangeAttackComponent.RangedAttackState is not eRangedAttackState.ReadyToFire 
        and not eRangedAttackState.None && elapsedTime > halfwayPoint)
        return false;  // Cannot interrupt after halfway
        
    attackComponent.StopAttack();
    return true;
}
```

#### NPC Ranged Interrupt Rules
```csharp
// Immobile NPCs special case
if (MaxSpeedBase == 0 && (attacker != TargetObject || !IsWithinRadius(attacker, MeleeAttackRange)))
    return false;  // Can only be interrupted by target in melee range
```

### Interrupt Immunity

#### Interrupt Immunity Sources
1. **Mastery of Concentration**: Realm ability prevents spell interruption
2. **Facilitate Painworking**: Necromancer pet interrupt immunity
3. **Quick Cast**: Cannot be interrupted during quick cast
4. **Uninterruptible Spells**: `Spell.Uninterruptible = true`
5. **Distance**: Some spells uninterruptible beyond 200 units

#### Interrupt Resistance
```csharp
// Level-based interrupt chance reduction
// 3% reduced chance per level difference
if (!Util.Chance(100 + (attacker.EffectiveLevel - EffectiveLevel) * 3))
    return;  // Interrupt resisted
```

### Self-Interrupt Mechanics

#### Melee Self-Interrupt
```csharp
// After melee attack, prevent immediate spell cast
public virtual int SelfInterruptDurationOnMeleeAttack => 3000;  // 3 seconds

// Applied after successful melee attack
if (ad.IsMeleeAttack)
    owner.StartInterruptTimer(owner.SelfInterruptDurationOnMeleeAttack, ad.AttackType, ad.Attacker);
```

#### NPC Self-Interrupt
```csharp
// NPCs use half their attack speed as self-interrupt
public override int SelfInterruptDurationOnMeleeAttack => AttackSpeed(ActiveWeapon) / 2;
```

### Attack Component Integration

#### Attack Action Interrupt Check
```csharp
public virtual bool CheckInterruptTimer()
{
    if (!_owner.IsBeingInterruptedIgnoreSelfInterrupt)
        return false;
        
    _owner.attackComponent.StopAttack();
    OnAimInterrupt(_owner.LastInterrupter);
    return true;
}
```

#### Ranged Attack Special Cases
```csharp
// Volley effect interrupt handling
AtlasOF_VolleyECSEffect volley = EffectListService.GetEffectOnTarget(this, eEffect.Volley) as AtlasOF_VolleyECSEffect;
volley?.OnAttacked();
```

### Interrupt Effects on Actions

#### Spell Casting
- Immediately cancels current spell
- Prevents new spell casts for duration
- Consumes power if cast was partially complete
- Triggers interrupt message to caster

#### Ranged Attacks
- Stops draw if before halfway point
- Cancels aim state
- Resets to no attack state
- May trigger weapon switch to melee

#### Melee Attacks
- Does not stop current attack
- Prevents immediate spell cast after
- May affect ability usage

### Special Interrupt Cases

#### Realm Ability Interrupts
```csharp
// Negative Maelstrom casting
GameEventMgr.AddHandler(caster, GamePlayerEvent.AttackFinished, new DOLEventHandler(CastInterrupted));

private void CastInterrupted(DOLEvent e, object sender, EventArgs arguments) 
{
    AttackFinishedEventArgs attackFinished = arguments as AttackFinishedEventArgs;
    if (attackFinished != null && attackFinished.AttackData.Attacker != sender)
        return;
    player.TempProperties.SetProperty(NM_CAST_SUCCESS, false);
    foreach (GamePlayer i_player in player.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
    {
        i_player.Out.SendInterruptAnimation(player);
    }
}
```

#### Instant Spell Interrupts
```csharp
// Some instant spells still apply interrupt
// Bonedancer instant debuffs
if (spell.ID is 10031 or 10032 or 10033)
{
    player.StopCurrentSpellcast();
    player.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
}
```

## System Interactions

### With Combat System
- Melee attacks apply interrupt to target
- Self-interrupt prevents spell-weaving
- Attack speed determines interrupt duration
- Critical hits don't affect interrupt duration

### With Spell System
- Casting checks interrupt state before starting
- Interrupted spells consume partial power
- Some spells immune to interruption
- Cast time affects vulnerability window

### With Crowd Control
- CC effects often apply interrupt
- Interrupt duration may exceed CC duration
- Some CC breaks on damage but interrupt remains
- Immunity timers separate from interrupt

### With Ranged Combat
- Draw time creates interrupt window
- Sure Shot resists non-melee interrupts
- Volley has special interrupt handling
- Halfway point prevents late interrupts

## Implementation Notes

### Thread Safety
- Interrupt timer updates use locking
- Prevents race conditions on multiple attacks
- Atomic operations for state checks
- Thread-safe property access

### Performance Considerations
- Interrupt checks cached where possible
- Level difference calculated once
- Lock held minimally
- Early exit conditions optimized

### Timer Resolution
- Uses GameLoop.GameLoopTime
- Millisecond precision
- Synchronized across systems
- No timer drift issues

## Test Scenarios

### Basic Interrupt Tests
1. **Melee Interrupt**: Verify spell cancellation
2. **Ranged Interrupt**: Test halfway point mechanics
3. **Self Interrupt**: Confirm cast prevention
4. **Duration Stacking**: Longer interrupts override

### Immunity Tests
1. **MoC Immunity**: No spell interruption
2. **Quick Cast**: Uninterruptible cast
3. **Level Resistance**: Higher level resist chance
4. **Distance Immunity**: Beyond 200 units

### Special Case Tests
1. **NPC Extended Duration**: +2.5 seconds
2. **Sure Shot Protection**: Melee only
3. **Volley Interruption**: Special handling
4. **CC Interrupt Application**: Proper timing

### Edge Cases
1. **Multiple Attackers**: Longest interrupt wins
2. **Simultaneous Interrupts**: Lock prevents double
3. **Death During Interrupt**: Timer cleared
4. **Zone Transition**: Timer persists

## Change Log
- Initial documentation based on interrupt system analysis
- Includes attack integration and special cases
- Documents immunity sources and resistance mechanics
- Covers NPC-specific behavior and self-interrupts 