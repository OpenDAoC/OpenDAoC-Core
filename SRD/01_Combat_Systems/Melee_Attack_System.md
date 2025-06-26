# Melee Attack System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The melee attack system manages all aspects of close-combat attacks including timing, damage calculation, defense resolution, and special attack mechanics like combat styles and dual wielding. It uses an Entity Component System (ECS) architecture with attack components coordinating the complex interactions between attackers and defenders.

## Core Architecture

### Attack Component System

#### Primary Components
```csharp
public class AttackComponent : IServiceObject
{
    public GameLiving owner;
    public WeaponAction weaponAction;       // Current weapon action
    public AttackAction attackAction;       // Attack timing and control
    public ConcurrentDictionary<GameLiving, long> Attackers { get; } // Track who's attacking
}
```

#### Attack Action Flow
```csharp
public class AttackAction
{
    // Core timing intervals
    protected const int TICK_INTERVAL_FOR_NON_ATTACK = 100;  // Failed attack retry
    private const int MINIMUM_MELEE_DELAY_AFTER_RANGED_ATTACK = 750;
    
    // Attack state
    private long _nextMeleeTick;      // When next melee attack can occur
    private long _nextRangedTick;     // When next ranged attack can occur
    private bool _firstTick = true;   // Initial attack timing
}
```

### Attack Execution Pipeline

#### 1. Attack Initiation
```csharp
public bool Tick()
{
    if (!ShouldTick())
        return true;
        
    if (!CanPerformAction())
    {
        _interval = TICK_INTERVAL_FOR_NON_ATTACK;
        return true;
    }
    
    if (_owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
        TickMeleeAttack();
    else
        TickRangedAttack();
        
    return true;
}
```

**Action Restrictions**:
- Cannot attack while mezzed or stunned
- Cannot attack while engaging
- Cannot attack while casting uninterruptible spells

#### 2. Melee Attack Preparation
```csharp
protected virtual bool PrepareMeleeAttack()
{
    // Shield style weapon swap
    if (_combatStyle != null && _combatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
        _weapon = _leftWeapon;
        
    // Style cleanup based on last attack result
    bool clearOldStyles = false;
    if (LastAttackData != null)
    {
        switch (LastAttackData.AttackResult)
        {
            case eAttackResult.OutOfRange:
            case eAttackResult.TargetNotVisible:
                clearOldStyles = ServiceUtils.ShouldTick(StyleComponent.NextCombatStyleTime + 250);
                break;
            case eAttackResult.NotAllowed_ServerRules:
            case eAttackResult.TargetDead:
            case eAttackResult.NoValidTarget:
                clearOldStyles = true;
                break;
        }
    }
    
    // NPC-specific range and follow checks
    if (owner is GameNPC npc)
    {
        int meleeAttackRange = npc.MeleeAttackRange;
        
        // Calculate time to reach target
        double timeToTarget = (_target.GetDistanceTo(npc) - meleeAttackRange) / npc.MaxSpeed * 1000.0;
        
        // Switch to ranged if too far
        if (timeToTarget > TIME_TO_TARGET_THRESHOLD_BEFORE_RANGED_SWITCH)
        {
            SwitchToRangedAndTick();
            return false;
        }
    }
    
    return true;
}
```

#### 3. Weapon Action Creation
```csharp
protected virtual void PerformMeleeAttack()
{
    AttackComponent.weaponAction = new WeaponAction(
        _owner,                 // Attacker
        _target,                // Target
        _weapon,                // Main weapon
        _leftWeapon,           // Off-hand weapon
        _effectiveness,         // Damage modifier
        _attackInterval,        // Attack speed
        _combatStyle           // Combat style
    );
    AttackComponent.weaponAction.Execute();
}
```

### WeaponAction Execution

#### Attack Resolution Process
```csharp
public void Execute()
{
    // Determine dual wield and left hand attacks
    _isDualWieldAttack = IsDualWieldAttack(_attackWeapon, _leftWeapon, _owner);
    _leftHandSwingCount = _owner.attackComponent.CalculateLeftHandSwingCount(_attackWeapon, _leftWeapon);
    
    // Main hand attack
    if (!MakeMainHandAttack(_attackWeapon, _leftWeapon, _combatStyle, _effectiveness, out AttackData mainHandAttackData))
        return;
        
    // Off-hand attacks (if applicable)
    MakeOffHandAttack(out AttackData leftHandAttackData);
    
    // Process attack results
    switch (mainHandAttackData.AttackResult)
    {
        case eAttackResult.HitStyle:
            // Apply style effects
            foreach (ISpellHandler proc in mainHandAttackData.StyleEffects)
                proc.StartSpell(mainHandAttackData.Target);
            break;
            
        case eAttackResult.NoTarget:
        case eAttackResult.TargetDead:
            _owner.OnTargetDeadOrNoTarget();
            return;
            
        case eAttackResult.NotAllowed_ServerRules:
        case eAttackResult.NoValidTarget:
            _owner.attackComponent.StopAttack();
            return;
    }
    
    // Unstealth before animation
    if (_owner is GamePlayer playerOwner)
        playerOwner.Stealth(false);
        
    // Show appropriate attack animation
    ShowAttackAnimation(mainHandAttackData, _attackWeapon);
}
```

### Attack Data Structure

#### Core AttackData Class
```csharp
public class AttackData
{
    // Core properties
    public GameLiving Attacker { get; set; }
    public GameLiving Target { get; set; }
    public eAttackType AttackType { get; set; }
    public eAttackResult AttackResult { get; set; }
    
    // Damage values
    public int Damage { get; set; }
    public int StyleDamage { get; set; }
    public int CriticalDamage { get; set; }
    public int CriticalChance { get; set; }
    
    // Weapon and timing
    public DbInventoryItem Weapon { get; set; }
    public int Interval { get; set; }      // Attack speed in ms
    public bool IsOffHand { get; set; }   // Left-hand attack
    
    // Style information
    public Style Style { get; set; }
    public List<ISpellHandler> StyleEffects { get; set; }
    
    // Defense chances (stored as percentages)
    public double ParryChance { get; set; }
    public double EvadeChance { get; set; }
    public double BlockChance { get; set; }
    public double MissChance { get; set; }
    public double DefensePenetration { get; set; }
    
    // Hit location
    public eArmorSlot ArmorHitLocation { get; set; }
}
```

### Attack Type Determination

#### Attack Type Classification
```csharp
public static eAttackType GetAttackType(DbInventoryItem weapon, bool dualWield, GameLiving attacker)
{
    // Dual wield attacks (except for Savages)
    if (dualWield && (attacker is not GamePlayer player || player.CharacterClass.ID is not eCharacterClass.Savage))
        return eAttackType.MeleeDualWield;
        
    // Unarmed attacks
    if (weapon == null)
        return eAttackType.MeleeOneHand;
        
    // Weapon-based determination
    eAttackType attackType = weapon.SlotPosition switch
    {
        Slot.TWOHAND => eAttackType.MeleeTwoHand,
        Slot.RANGED => eAttackType.Ranged,
        _ => eAttackType.MeleeOneHand,
    };
    
    // Player weapon validation
    if (attacker is GamePlayer)
    {
        if (attackType is eAttackType.MeleeTwoHand && weapon.Item_Type is not Slot.TWOHAND)
            attackType = eAttackType.MeleeOneHand;
    }
    
    return attackType;
}
```

### Attack Result Calculation

#### Defense Resolution Order
```csharp
public virtual eAttackResult CalculateEnemyAttackResult(WeaponAction action, AttackData ad, DbInventoryItem attackerWeapon, ref double effectiveness)
{
    // 1. Bodyguard check
    if (CheckBodyguard())
        return eAttackResult.Bodyguarded;
        
    // 2. Phase shift (100% miss)
    if (phaseshift != null)
        return eAttackResult.Missed;
        
    // 3. Grapple
    if (grapple != null)
        return eAttackResult.Grappled;
        
    // 4. Brittle Guard
    if (brittleguard != null)
    {
        brittleguard.Cancel(false);
        return eAttackResult.Missed;
    }
    
    // 5. Intercept (redirects attack)
    if (intercept != null && !stealthStyle)
    {
        ad.Target = intercept.Source;
        return eAttackResult.HitUnstyled;
    }
    
    // 6. Calculate defense penetration
    ad.DefensePenetration = ad.Attacker.attackComponent.CalculateDefensePenetration(ad.Weapon, ad.Target.Level);
    
    // 7. Defense checks (if not disabled)
    if (!defenseDisabled)
    {
        // Evade check
        double evadeChance = owner.TryEvade(ad, lastAttackData, Attackers.Count);
        ad.EvadeChance = evadeChance * 100;
        if (evadeChance > Util.RandomDouble())
            return eAttackResult.Evaded;
            
        // Parry check (melee only)
        if (ad.IsMeleeAttack)
        {
            double parryChance = owner.TryParry(ad, lastAttackData, Attackers.Count);
            ad.ParryChance = parryChance * 100;
            if (parryChance > Util.RandomDouble())
                return eAttackResult.Parried;
        }
        
        // Block check
        if (CheckBlock(ad))
            return eAttackResult.Blocked;
    }
    
    // 8. Guard check
    if (CheckGuard(ad, stealthStyle))
        return eAttackResult.Blocked;
        
    // 9. Miss/Fumble calculation
    double missChance = GetMissChance(action, ad, lastAttackData, attackerWeapon) * 0.01;
    double fumbleChance = ad.IsMeleeAttack ? ad.Attacker.GetModified(eProperty.FumbleChance) * 0.001 : 0;
    
    if (missChance > Util.RandomDouble())
        return fumbleChance > missRoll ? eAttackResult.Fumbled : eAttackResult.Missed;
        
    // 10. Bladeturn check
    if (CheckBladeturn(ad, stealthStyle, ref effectiveness))
        return eAttackResult.Missed;
        
    return eAttackResult.HitUnstyled;
}
```

### Special Attack Mechanics

#### Dual Wield System
```csharp
public int CalculateLeftHandSwingCount(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon)
{
    // No left hand swings without weapon or shield styles
    if (leftWeapon == null)
        return 0;
        
    // H2H special case - alternating attacks
    if (mainWeapon?.Object_Type == (int)eObjectType.HandToHand)
        return owner is GamePlayer ? leftWeapon.MaxCount : 1;
        
    // Dual wield chance calculation
    int spec = CalculateSpec(leftWeapon);
    double chance = 0.25 + spec * 0.0068;
    
    // Count successful swings
    int swings = 0;
    while (Util.ChanceDouble(chance) && swings < MAX_LEFT_HAND_SWINGS)
        swings++;
        
    return swings;
}
```

#### Attack Range Calculation
```csharp
public int AttackRange
{
    get
    {
        // Base melee range
        int range = 128;  // Tested values: 125-130
        
        // Weapon-based range for players
        if (owner is GamePlayer)
        {
            if (owner.ActiveWeapon != null)
            {
                switch ((eObjectType)owner.ActiveWeapon.Object_Type)
                {
                    case eObjectType.Spear:
                    case eObjectType.PolearmWeapon:
                    case eObjectType.CelticSpear:
                    case eObjectType.TwoHandedWeapon:
                    case eObjectType.LargeWeapons:
                        // Polearms have extended range
                        // Implementation specific to weapon
                        break;
                }
            }
        }
        
        // NPC attack range
        else if (owner is GameNPC npc)
        {
            range = npc.MeleeAttackRange;
        }
        
        return range;
    }
}
```

### Attack Speed Mechanics

#### Speed Calculation
```csharp
public int AttackSpeed(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon = null)
{
    // Get base weapon speed
    double speed = mainWeapon?.SPD_ABS ?? 34;  // 3.4 seconds default
    speed *= 100;  // Convert to milliseconds
    
    // Apply quickness modifier
    speed *= 1.0 - (owner.GetModified(eProperty.Quickness) - 60) * 0.002;
    
    // Apply haste/slow effects
    if (owner.effectListComponent.ContainsEffectForEffectType(eEffect.MeleeHasteDebuff))
        speed /= 1.0 - SpeedDecreaseBuff * 0.01;
    else if (owner.effectListComponent.ContainsEffectForEffectType(eEffect.MeleeHasteBuff))
        speed /= 1.0 + MeleeHasteBuff * 0.01;
    
    // Apply melee speed bonus
    speed *= owner.GetModified(eProperty.MeleeSpeed) * 0.01;
    
    // Minimum attack speed
    int minimum = mainWeapon?.SPD_ABS > 0 ? mainWeapon.SPD_ABS * 50 : 1500;
    return (int)Math.Max(minimum, speed);
}
```

### Combat Messaging

#### Attack Result Messages
```csharp
public static void SendAttackingCombatMessages(WeaponAction action, AttackData ad)
{
    switch (ad.AttackResult)
    {
        case eAttackResult.Parried:
            message = $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is parried!";
            break;
            
        case eAttackResult.Evaded:
            message = $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is evaded!";
            break;
            
        case eAttackResult.Fumbled:
            message = $"{ad.Attacker.GetName(0, true)} fumbled!";
            break;
            
        case eAttackResult.Missed:
            message = $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and misses!";
            break;
            
        case eAttackResult.Blocked:
            message = $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is blocked!";
            
            // Special guard messages
            if (guardSource != null && guardSource != ad.Target)
            {
                if (guardSource is GamePlayer player)
                    player.Out.SendMessage($"You block {ad.Attacker.GetName(0, false)}'s attack on {ad.Target.GetName(0, false)}!", 
                        eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
            }
            break;
    }
}
```

### Attack Animation System

#### Animation Determination
```csharp
public void ShowAttackAnimation(AttackData ad, DbInventoryItem weapon)
{
    // Determine animation result byte
    byte resultByte = ad.AttackResult switch
    {
        eAttackResult.Missed => 0,
        eAttackResult.Parried => 1,
        eAttackResult.Blocked => 2,
        eAttackResult.Evaded => 3,
        eAttackResult.Fumbled => 4,
        eAttackResult.HitUnstyled => 10,
        eAttackResult.HitStyle => 11,
        _ => 0
    };
    
    // Get weapon models for animation
    int attackersWeapon = weapon?.Model ?? 0;
    int defendersWeapon = 0;
    
    // Get defender's weapon for parry animation
    if (ad.AttackResult == eAttackResult.Parried && ad.Target.ActiveWeapon != null)
        defendersWeapon = ad.Target.ActiveWeapon.Model;
        
    // Get defender's shield for block animation
    else if (ad.AttackResult == eAttackResult.Blocked)
    {
        DbInventoryItem lefthand = ad.Target.ActiveLeftWeapon;
        if (lefthand?.Object_Type == (int)eObjectType.Shield)
            defendersWeapon = lefthand.Model;
    }
    
    // Send animation to all nearby players
    foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.Out.SendCombatAnimation(_owner, ad.Target, attackersWeapon, 
            defendersWeapon, resultByte, 0, resultByte);
    }
}
```

## System Interactions

### With Style System
- Styles modify attack damage and effects
- Style chains require specific attack results
- Style procs trigger on successful hits
- Backup styles activate on defense results

### With Effect System
- Haste/slow effects modify attack speed
- Combat buffs increase damage
- Debuffs reduce defense chances
- Crowd control disables attacks

### With Defense System
- Defense checks occur in specific order
- Each defense type has unique mechanics
- Multiple attackers reduce defense effectiveness
- Special abilities bypass certain defenses

### With Interrupt System
- Melee attacks interrupt spell casting
- Self-interrupt prevents immediate spell cast after melee
- Interrupt duration based on weapon interval
- Different interrupt durations for attack types

## Implementation Notes

### Performance Optimization
- Attack actions pooled per tick
- Defense calculations cached when possible
- Animation packets batched
- Range checks use squared distance

### Thread Safety
- AttackComponent uses concurrent collections
- Attack state synchronized via locks
- Timer management thread-safe
- Effect list access protected

### Special Considerations
- NPCs use different attack timing logic
- Pets inherit owner's attack bonuses
- Siege weapons use special attack mechanics
- Keep guards have modified attack ranges

## Test Scenarios

### Basic Attack Tests
1. **Single Target Attack**: Verify basic hit/damage
2. **Attack Speed**: Validate speed calculations
3. **Attack Range**: Test melee range limits
4. **Animation Sync**: Confirm animation matches result

### Defense Integration
1. **Defense Order**: Verify checks occur in correct sequence
2. **Multi-Attacker**: Test defense reductions
3. **Special Defenses**: Validate bodyguard, intercept, etc.
4. **Stealth Attacks**: Confirm defense bypass

### Dual Wield Tests
1. **Swing Chance**: Validate left-hand swing calculation
2. **H2H Alternating**: Test hand-to-hand special case
3. **Damage Distribution**: Verify damage calculations
4. **Animation Sequencing**: Check dual wield animations

### Style Integration
1. **Style Chains**: Test opening requirements
2. **Style Effects**: Validate proc activation
3. **Backup Styles**: Test defensive style switching
4. **Growth Rate**: Verify damage scaling

## Change Log
- Initial documentation based on AttackComponent analysis
- Includes WeaponAction and AttackAction architecture
- Documents attack result calculation pipeline
- Covers special mechanics like dual wield and H2H 