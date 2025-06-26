# SRD Expansion Summary - Second Pass (2024-01-20)

## Overview
This document summarizes the second comprehensive expansion of the OpenDAoC System Reference Document (SRD), focusing on additional game mechanics not covered in the first pass.

## New Systems Documented

### 1. Housing System (`05_Social_Systems/Housing_System.md`)
**Key Mechanics Documented:**
- House types and models by realm/size
- Rent system with lockbox and auto-payment
- Permission system (9 levels + owner)
- Vault system (4 house + 4 account vaults)
- Decoration system (interior/exterior/hookpoints)
- Consignment merchant mechanics
- Market explorer integration

**Notable Discoveries:**
- Maximum lockbox holds 4 weeks of rent
- Repossession preserves contents
- Hookpoint IDs determine guard/siege placement
- Account vaults accessible from any owned house

### 2. Crafting System (`07_Economy_Systems/Crafting_System.md`)
**Key Mechanics Documented:**
- Primary, secondary, and advanced craft types
- Success chance formulas by con level
- Skill gain calculations with capital city bonus
- Quality determination (96-100%)
- Crafting time formulas with modifiers
- Secondary skill requirements
- Repair system mechanics
- Spellcrafting overcharge rates

**Notable Discoveries:**
- 2% masterpiece chance at all skill levels
- Legendary (1000+) skill guarantees high quality
- Crafting time affected by keep bonuses and relics
- Repair requires 50% of item craft level

### 3. Aggro/Hate System (`01_Combat_Systems/Aggro_Hate_System.md`)
**Key Mechanics Documented:**
- Aggro list management and updates
- Protect ability mitigation (10/20/30%)
- Distance-based effective aggro calculation
- Pet/owner aggro split (85/15)
- Taunt mechanics (spell and style)
- Special brain types (turret, friend, commander)
- BAF (Bring A Friend) system

**Notable Discoveries:**
- Effective aggro reduces exponentially past 800 units
- Shades tracked but ignored until pets die
- Style taunt scales with damage dealt
- Maximum aggro list distance enforced

### 4. Death & Resurrection System (`02_Character_Systems/Death_Resurrection_System.md`)
**Key Mechanics Documented:**
- Death types (PvE/PvP/RvR) and penalties
- Experience loss formulas with death count modifiers
- Constitution loss scaling
- Gravestone system for XP recovery
- Resurrection sickness types
- Release locations and timers
- Special death mechanics (duels, underwater)

**Notable Discoveries:**
- First death: 1/3 XP penalty
- Second death: 2/3 XP penalty
- Resurrection grants realm points to caster
- 5 second damage immunity after resurrection

### 5. Group System (`05_Social_Systems/Group_System.md`)
**Key Mechanics Documented:**
- 8 player maximum with leader system
- Experience sharing by level range
- Challenge code thresholds
- Autosplit loot/coin mechanics
- Eligibility requirements
- Battlegroup integration
- Group mission system

**Notable Discoveries:**
- Experience requires same color range
- Challenge threshold scales with group size
- Coin split applies guild dues
- Items go to random eligible member

### 6. Stealth & Detection System (`02_Character_Systems/Stealth_Detection_System.md`)
**Key Mechanics Documented:**
- Stealth activation restrictions
- Player detection range formulas
- NPC detection with field of view
- Detection chance calculations
- Special abilities (Camouflage, Vanish, Detect Hidden)
- Movement speed modifications
- Stealth breaking conditions

**Notable Discoveries:**
- Detection range hard capped at 1900 units
- NPCs have 90° vision, 120° hearing
- See Hidden: 2700 - (36 * stealth_spec)
- Vanish prevents DoT breaking stealth

### 7. Siege Warfare System (`01_Combat_Systems/Siege_Warfare_System.md`)
**Key Mechanics Documented:**
- Ram mechanics with passenger scaling
- Ranged siege damage and ranges
- Hookpoint system and IDs
- Control mechanics and distances
- Summoning restrictions
- Damage modifiers vs players
- Special abilities (Lifter, Siege Bolt)

**Notable Discoveries:**
- Maximum 2 rams per door
- Trebuchet does 3x damage to doors
- 50% damage reduction for tank classes
- Ram protection 50-80% based on level

### 8. Pet & Summoning System (`03_Magic_Systems/Pet_Summoning_System.md`)
**Key Mechanics Documented:**
- Class-specific pet types
- Control system (aggression/walk states)
- Aggro split mechanics (85/15)
- Necromancer shade system
- Bonedancer commander/minion control
- Animist turret behaviors
- Pet brain system and commands

**Notable Discoveries:**
- Pet aggro split always 85% pet, 15% owner
- FnF turrets select targets randomly
- Commander pets synchronize minion states
- Maximum control distance: 5000 units

## Cross-System Interactions Identified

### Combat Integration
- Stealth breaks on attack
- Siege weapons can't be used while stealthed
- Pets use standard combat mechanics
- Aggro system affects all combat participants

### Group Coordination
- Group buffs affect pets
- Siege ram passengers must be grouped
- Group members always see stealthed allies
- Loot distribution follows group rules

### Economic Systems
- Housing rent uses money system
- Crafting requires appropriate tools
- Consignment merchants handle transactions
- Guild dues apply to group coin splits

## Implementation Patterns Observed

### Common Design Patterns
- Interface-based brain systems for AI
- Distance-based scaling formulas
- Percentage-based modifiers
- Hard caps on maximum values

### Performance Optimizations
- Lazy evaluation of expensive calculations
- Capped update frequencies
- Maximum list sizes enforced
- Regional limitations on effects

## Next Steps Recommended

1. **Additional Systems to Document:**
   - Keep capture mechanics
   - Relic system
   - Trading system
   - Bind point system
   - Faction mechanics

2. **Cross-References Needed:**
   - Link related systems in each document
   - Create interaction matrix
   - Document edge cases between systems

3. **Verification Required:**
   - Test formulas against live behavior
   - Verify hard-coded values
   - Confirm server property effects

## Files Created/Updated
- SRD/05_Social_Systems/Housing_System.md (NEW)
- SRD/07_Economy_Systems/Crafting_System.md (NEW)
- SRD/01_Combat_Systems/Aggro_Hate_System.md (NEW)
- SRD/02_Character_Systems/Death_Resurrection_System.md (EXISTS)
- SRD/05_Social_Systems/Group_System.md (NEW)
- SRD/02_Character_Systems/Stealth_Detection_System.md (EXISTS)
- SRD/01_Combat_Systems/Siege_Warfare_System.md (NEW)
- SRD/03_Magic_Systems/Pet_Summoning_System.md (NEW)

Total: 6 new documents, 2 existing documents found 