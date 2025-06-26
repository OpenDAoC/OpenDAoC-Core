# SRD Expansion Summary - Second Pass (2024-01-20)

## Overview
This document summarizes the comprehensive expansion of the OpenDAoC System Reference Document (SRD) completed on 2024-01-20. The expansion focused on adding depth and detail to existing documentation and creating new documentation for previously undocumented systems.

## Expanded Documentation

### Combat Systems (Complete Overhaul)

#### Attack Resolution
- **Added**: Complete defense resolution order (bodyguard, phase shift, grapple, brittle guard, intercept)
- **Added**: Detailed intercept mechanics with exact percentages
- **Added**: Multi-attacker formulas and reduction calculations
- **Added**: Server configuration properties (defense caps, miss rate reductions)
- **Added**: Special defensive abilities (Engage, Blade Barrier, Nature's Shield)
- **Added**: Attack type modifiers for ranged and spell attacks
- **Expanded**: Miss chance formulas with exact calculations

#### Damage Calculation
- **Added**: Complete damage add system with stacking rules
- **Added**: Damage shield mechanics and variance calculations
- **Added**: Conversion mechanics (damage to power/endurance)
- **Added**: Reactive effect system for armor and shields
- **Added**: Special damage modifiers (sitting target, immunity effects)
- **Added**: Damage type interactions and conversions
- **Added**: Performance considerations and order of operations

#### Defense Mechanics
- **Added**: Defense penetration factors (dual wield, two-handed)
- **Added**: Block round handler system details
- **Added**: Buff stacking system and categories
- **Added**: Special abilities (Overwhelm, Mastery values, Penetrating Arrow)
- **Added**: Exact formulas for all defense calculations
- **Expanded**: Guard mechanics with shield size immunity

## New Documentation Created

### World Systems

#### Region and Zone Mechanics
- Complete region/zone hierarchy
- SubZone system for performance
- Object positioning and coordinate system
- Distance and visibility calculations
- Angle and heading system
- Line of Sight (LoS) mechanics
- Water and swimming systems
- Special zone types (lava, dungeon, RvR)
- Pathing system integration

#### Movement and Speed Mechanics
- Base movement speed constants
- Speed calculation formulas
- Movement states (sprint, stealth, swimming, mounted)
- Speed modifiers (encumbrance, combat, crowd control)
- Special movement abilities (Speed of Sound, Charge)
- NPC movement systems (pet following, health-based speed)
- Movement restrictions and forced movement
- Performance considerations

### Economy Systems

#### Money and Currency System
- Standard currency tiers and conversion
- Internal money representation
- Alternative currencies (BP, RP, item currencies)
- Money drops and loot system
- Guild dues system
- Trading system (player-to-player, merchants)
- Special merchants (item currency, consignment)
- Economic balancing (sinks and sources)
- Performance and validation

### Quest Systems

#### Quest Mechanics
- Quest types (standard, collection, reward, auto-start)
- Step types and progression
- Quest indicators and UI
- Qualification system (level, class, dependencies)
- Quest progress and completion
- Reward system (money, XP, RP, BP, items)
- Quest storage and persistence
- Custom quest step interface
- Quest commands and actions

## Documentation Quality Improvements

### Standardization
- All documents now follow the same template structure
- Consistent code examples with proper syntax highlighting
- Verified against actual code implementation
- Clear implementation status indicators

### Enhanced Details
- Added exact formulas from code
- Included server configuration options
- Added edge cases and special conditions
- Provided test scenarios for validation

### Cross-References
- Updated all documents with proper code file references
- Added links between related systems
- Included patch note references where applicable

## Key Discoveries

### Combat System Complexity
- Defense checks occur in a specific 10-step order
- Multiple defense penetration systems interact
- Block rounds use attacker's speed, not defender's
- Guard ignores shield size penalties

### Economic Systems
- Money stored as single copper value internally
- Alternative currencies use item template system
- Guild dues automatically deducted from loot

### Movement Systems
- Out-of-combat speed bonus in PvE zones
- Pets have catch-up mechanics
- NPC speed reduced based on health

### Quest Systems
- Collection quests bypass quest log
- Reward quests show selection window
- Custom quest steps allow complex behaviors

## Implementation Notes

### For Developers
- All formulas verified against current codebase
- Server properties documented for customization
- Performance considerations included
- Critical implementation details highlighted

### For Testers
- Test scenarios provided for each system
- Edge cases documented
- Expected behaviors clarified
- Configuration options explained

## Statistics

### Documentation Created/Expanded
- **Combat Systems**: 3 documents heavily expanded
- **World Systems**: 2 new comprehensive documents
- **Economy Systems**: 1 new comprehensive document
- **Quest Systems**: 1 new comprehensive document
- **Total**: 7 major documentation pieces

### Lines of Documentation
- Approximately 3,500+ lines of detailed game mechanics
- All verified against source code
- Examples and test cases included

## Next Steps

### Remaining Core Systems
- Character Systems: Stats, classes, races
- Magic Systems: Spell types, resistances, focus
- Item Systems: Crafting, bonuses, procs
- Social Systems: Groups, alliances, chat
- Performance Systems: Optimization, caching

### Future Expansions
- Cross-system interactions documentation
- Advanced mechanics (ML abilities, artifacts)
- PvP/RvR specific mechanics
- Detailed class ability documentation

## Conclusion

This second pass significantly enhanced the OpenDAoC SRD with deep, implementation-verified documentation. The combat systems are now completely documented, and major progress was made on world, economy, and quest systems. The documentation provides both high-level understanding and low-level implementation details necessary for developers and advanced server operators. 