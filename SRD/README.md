# OpenDAoC System Reference Document (SRD)

## Overview
The OpenDAoC SRD is a comprehensive documentation of all game mechanics, formulas, and systems in the Dark Age of Camelot server emulator. This living document serves as the authoritative reference for developers, testers, and contributors.

## Purpose
- **Preserve Game Mechanics**: Document authentic DAoC gameplay rules and formulas
- **Development Reference**: Provide clear specifications for implementation
- **Testing Guide**: Define expected behaviors for quality assurance
- **Knowledge Base**: Centralize game mechanics understanding

## Document Structure

### Categories
1. **01_Combat_Systems** - Attack resolution, damage calculation, defense mechanics
2. **02_Character_Systems** - Progression, classes, stats, abilities
3. **03_Magic_Systems** - Spells, effects, casting mechanics
4. **04_Item_Systems** - Equipment, bonuses, artifacts
5. **05_Social_Systems** - Guilds, groups, alliances
6. **06_World_Systems** - Regions, zones, movement
7. **07_Economy_Systems** - Currency, trading, crafting
8. **08_Quest_Systems** - Quest mechanics, rewards
9. **09_Performance_Systems** - Server optimization, timing
10. **10_Cross_System_Interactions** - How systems work together

### Document Format
Each system document follows this template:

```markdown
# System Name

## Document Status
- **Last Updated**: YYYY-MM-DD
- **Status**: [Draft/In Progress/Stable]
- **Verification**: [Unverified/Code-verified/Live-tested]
- **Implementation**: [Not started/Partial/In Development/Stable]

## Overview
Brief description of the system and its purpose.

## Core Mechanics
Detailed mechanics, formulas, and rules.

## System Interactions
How this system interacts with others.

## Implementation Notes
Technical details for developers.

## Test Scenarios
Example test cases and expected outcomes.

## Change Log
History of significant updates.

## References
Source code files and external documentation.
```

### Document Status Definitions
- **Draft**: Initial documentation, may have gaps
- **In Progress**: Active documentation with known TODOs
- **Stable**: Comprehensive documentation, may still receive updates
- **Verification Status**: Level of confidence in accuracy
- **Implementation Status**: Current state in codebase

## Using the SRD

### For Developers
1. Check relevant system documentation before implementing features
2. Update documentation when discovering undocumented mechanics
3. Add test scenarios for new edge cases
4. Cross-reference with source code

### For Testers
1. Use test scenarios as verification checklist
2. Report discrepancies between documentation and behavior
3. Suggest additional test cases
4. Validate formulas with in-game testing

### For Contributors
1. Follow the document template for consistency
2. Include code references and verification sources
3. Document edge cases and exceptions
4. Update change logs

## Contribution Process

### Adding New Documentation
1. **Identify gap** - Find undocumented system or mechanic
2. **Research** - Study source code and test in-game
3. **Document** - Follow template structure
4. **Verify** - Test documented behavior
5. **Submit** - Create pull request with changes

### Updating Existing Documentation
1. **Rule clarification** - Existing rule needs correction/expansion
2. **New discovery** - Additional mechanics found
3. **Implementation change** - Code changes affect mechanics
4. **Bug documentation** - Document known issues

### Quality Standards
- **Accuracy**: All formulas and mechanics must be verified
- **Clarity**: Use clear language and examples
- **Consistency**: Follow established formatting
- **Completeness**: Cover all aspects of the system including edge cases
- **Testability**: Include concrete test scenarios

## Key Systems Overview

### Combat Systems
Core combat mechanics including:
- **Attack Resolution**: Hit/miss calculation, defense checks, multi-attacker formulas
- **Damage Calculation**: Base damage, modifiers, resistances, critical strikes
- **Defense Mechanics**: Evade, parry, block formulas with all modifiers
- **Style Mechanics**: Combat styles, positional requirements, follow-ups
- **Attack Speed & Timing**: Weapon speeds, haste effects, swing timers
- **Melee Attack System**: Complete attack flow and component interactions
- **Ranged Attack System**: Archery mechanics, draw/aim/release phases
- **Interrupt System**: Spell/ability interruption mechanics
- **Damage Add & Shield System**: Proc effects and reactive damage
- **Proc System**: Weapon and armor proc chances
- **Critical Strike System**: Crit calculations for all damage types
- **Resistance System**: Two-layer resist system
- **Aggro/Hate System**: NPC target selection
- **Siege Warfare**: Keep and siege mechanics

### Character Systems
Player character mechanics:
- **Character Progression**: Levels, experience, stats
- **Character Class System**: All classes, attributes, capabilities
- **Property System**: Stat calculations and bonuses
- **Specialization & Skills**: Skill training and effects
- **Realm Points & Ranks**: RvR progression system
- **Death & Resurrection**: Death penalties and recovery
- **Stealth & Detection**: Stealth mechanics and counter-stealth
- **Realm Abilities System**: RA types, costs, effects
- **Master Levels System**: ML progression and abilities

### Magic Systems
Spell and effect mechanics:
- **Spell Mechanics**: Casting, ranges, resistances
- **Spell Effects System**: 80+ effect types and categories
- **Effect Stacking Logic**: Complex stacking rules
- **Spell Component System**: Baseline vs spec components
- **Casting Mechanics System**: Cast times and interrupts
- **Area Effect & Targeting System**: AoE mechanics
- **Spell Lines & Schools System**: Spell organization
- **Buff Effect System**: Buff/debuff management
- **Pet & Summoning System**: Pet controls and AI

### Item Systems
Equipment and inventory:
- **Item Mechanics**: Stats, bonuses, requirements
- **Artifact System**: Artifact progression
- **Inventory System**: Storage and management

### Social Systems
Group and guild mechanics:
- **Guild System**: Ranks, permissions, management
- **Housing System**: House types, permissions, merchants
- **Group System**: Formation, experience, loot distribution

### World Systems
Game world mechanics:
- **Region & Zone Mechanics**: Zone structure, properties
- **Movement & Speed Mechanics**: Speed calculations, movement states

### Economy Systems
Economic mechanics:
- **Money & Currency System**: Currency tiers and exchange
- **Crafting System**: All crafting skills and success formulas
- **Loot System**: Drop rates and distribution

### Quest Systems
Quest and task mechanics:
- **Quest Mechanics**: Types, requirements, rewards

### Performance Systems
Server performance and optimization:
- **Server Performance System**: Game loop, threading, optimization
- **AI Brain System**: NPC behavior and FSM

### Cross-System Interactions
How systems work together:
- **Zone Transition System**: Seamless world navigation

## Maintenance

### Regular Updates
- Document new discoveries as they're found
- Update formulas when implementation changes
- Add test scenarios for bugs
- Improve clarity based on questions

### Review Process
- Technical review for accuracy
- Formatting review for consistency
- Cross-reference with related systems
- Validate with current implementation

## Current Coverage

### Combat Systems
Attack resolution, damage, defense, aggro, siege warfare, style mechanics, attack speed documented. Additional systems include melee/ranged attack flow, interrupts, procs, critical strikes, and resistances.

### Character Systems
Progression, death/resurrection, stealth, realm abilities, master levels documented. Includes complete class system and property calculations.

### Magic Systems
Spell system documented including mechanics, effects, stacking, casting, targeting, lines, and pet summoning.

### Item Systems
Equipment mechanics, artifact system, item generation, and inventory management documented.

### Social Systems
Guild, housing, and group systems documented with complete mechanics.

### World Systems
Region/zone and movement/speed mechanics documented.

### Economy Systems
Money system, crafting system, and loot distribution documented.

### Quest Systems
Quest mechanics documented with various quest types and rewards.

### Performance Systems
Server performance, optimization, game loop, and AI brain system documented.

### Cross-System Interactions
Zone transitions, area management, and system coordination documented.

## Major Discoveries

### Magic Systems
- Effect stacking uses complex IsBetterThan algorithm
- Spell components have baseline vs specialization variants
- 80+ unique effect types with 6 buff bonus categories

### Character Systems
- Realm abilities have complex prerequisite chains
- Master levels use fixed MLXP requirements
- Stealth detection uses multi-factor formulas

### Item Systems
- Artifacts require encounter credit system
- Unique level-based progression mechanics

### Performance Systems
- 10ms game loop with ECS architecture
- Object pooling with EMA sizing
- Multi-threaded service processing

### Cross-System Interactions
- Complex validation chains for transitions
- Security layers prevent exploits
- State management across systems

## Appendix

### Glossary
- **AF**: Armor Factor
- **ABS**: Absorption
- **DPS**: Damage Per Second
- **RvR**: Realm vs Realm
- **RA**: Realm Ability
- **ML**: Master Level
- **CL**: Champion Level

### Common Formulas
- **Weapon Skill**: BaseSkill * RelicBonus * SpecModifier
- **Armor Factor**: ItemAF + 12.5 + (Level * 20 / 50)
- **Damage Cap**: WeaponSkill * EnemyArmor * 3
- **Experience**: BasedOnLevel * GroupBonus * ServerBonus

### Known Issues
- Document any confirmed bugs or inconsistencies
- Note differences from live DAoC
- Track pending investigations

## Contact

For questions or contributions, see [Helper Docs/OpenDAoC_SRD_Structure_Plan.md](../Helper%20Docs/OpenDAoC_SRD_Structure_Plan.md) for detailed processes and guidelines. 