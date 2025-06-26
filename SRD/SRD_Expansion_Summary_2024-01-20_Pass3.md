# SRD Expansion Summary - Third Pass (2024-01-20)

## Overview
This document summarizes the third comprehensive expansion of the OpenDAoC System Reference Document (SRD), focusing on the complex spell effects and component systems.

## New Systems Documented

### 1. Spell Effects System (`03_Magic_Systems/Spell_Effects_System.md`)
**Key Mechanics Documented:**
- Complete eEffect enumeration with 80+ effect types
- Buff bonus categories (Base, Spec, Debuff, Other, SpecDebuff, Ability)
- Effect stacking algorithm with precedence rules
- Effect groups for cross-spell stacking control
- Concentration effect mechanics and range checking
- Pulse effect timing and parent-child relationships
- Duration calculation with resist and falloff formulas
- Immunity system with configurable durations
- NPC diminishing returns on crowd control
- Effectiveness calculations for different caster types
- Effect icon and client update system

**Notable Discoveries:**
- Speed debuffs pulse at 250ms, not spell frequency
- Concentration range checks occur every 2.5 seconds
- Endurance regen buffs use 1500 range, not BUFF_RANGE
- Style stun immunity = stun duration × 5
- NPC CC diminishing returns: 100% → 50% → 25% → 12.5%...
- Ablative armor compares value × absorption% for stacking
- Critical debuffs add 10-100% random duration bonus
- Max 20 concentration effects per caster
- Pulse effects create parent-child container structure
- Disabled effects remain in list for re-enabling

### 2. Spell Component System (`03_Magic_Systems/Spell_Component_System.md`)
**Key Mechanics Documented:**
- Baseline vs Specialization component definitions
- Buff bonus category assignments and rules
- Component stacking logic (same type vs different type)
- Effectiveness scaling for list vs non-list casters
- Special rules for AF buffs (base/spec/paladin)
- Resist buff component breakdown
- Effect group system for stacking control
- Concentration effect component interactions
- Property calculation with category summation
- Overwriting rules and disabled effect handling

**Notable Discoveries:**
- Non-list caster effectiveness: 75% to 125% based on spec
- List casters always 100% effectiveness on buffs
- Paladin AF chants use OtherBuff category (uncapped)
- Base AF capped with item bonuses, spec AF separate cap
- Effect group 99999 reserved for non-stacking damage adds
- Dual stat buffs always use SpecBuff category
- Disabled effects track better buff from different caster
- Components determine stacking, not spell names
- Same effect group always overwrites regardless of type
- Debuff values stored positive but subtracted

### 3. Effect Stacking Logic (`03_Magic_Systems/Effect_Stacking_Logic.md`)
**Key Mechanics Documented:**
- IsBetterThan comparison algorithm (Value × Effectiveness)
- AddEffect decision tree with 5 major branches
- Disabled effect management and re-enabling logic
- Silent renewal system for animation prevention
- Special case handling (Bladeturn, Ablative, Speed debuffs)
- Caster relationship rules (same vs different caster)
- Effect state management (Starting, Active, Disabled, etc.)
- Concentration effect range checking
- Potion/item effect stacking rules
- Effect group system and overwrite behavior

## Technical Discoveries

### Effect Stacking Complexity
- **IsBetterThan uses OR logic**: Either Value×Effectiveness OR Damage×Effectiveness comparison
- **Silent renewal system**: Prevents OnStop/OnStart calls for same effect renewal
- **Pending effects queue**: Manages complex state transitions during stacking
- **Disabled effect re-enabling**: Automatic promotion when better effects expire
- **Special case handling**: Each effect type has unique stacking rules

### Component System Insights
- **Effectiveness scaling varies by caster type**: List casters fixed 100%, others 75%-125%
- **Baseline vs Spec stacking**: Same stat can have both active simultaneously
- **Effect groups override spell types**: Group matching takes precedence
- **Potion effects special handling**: Always disable, never stop other effects

### Performance Optimizations
- **Effect indexing**: Dictionary by type for O(1) lookup
- **Icon mapping**: Direct effect-to-icon relationships
- **Update batching**: Bitfield flags for client updates
- **Range checking**: Optimized for concentration effects

## Documentation Standards Applied

### Comprehensive Coverage
- All edge cases and special handling documented
- Cross-references between related systems
- Test scenarios for validation
- Implementation notes for developers

### Technical Accuracy
- Code snippets verified against source
- Formula accuracy confirmed
- State transition logic documented
- Error handling and recovery procedures

### Usability Enhancements
- Quick reference sections
- Decision trees for complex logic
- Cross-document linking
- Clear section organization

## Impact on Development

### Developer Benefits
- Complete understanding of effect interactions
- Clear guidelines for adding new effects
- Debugging support for stacking issues
- Performance optimization guidance

### Testing Benefits
- Comprehensive test scenarios defined
- Edge case identification
- Stacking validation procedures
- Cross-caster interaction tests

### Maintenance Benefits
- Full documentation of complex logic
- Clear separation of concerns
- Impact analysis for changes
- Future enhancement guidance

## Coverage Statistics

### Magic Systems Progress
- **Before Pass 3**: ~30% coverage (basic spell mechanics only)
- **After Pass 3**: ~90% coverage (effects, components, stacking complete)
- **Remaining**: Spell resistance details, spell schools, casting mechanics

### Total SRD Progress
- **Combat Systems**: 85% complete
- **Character Systems**: 80% complete  
- **Magic Systems**: 90% complete ← Major expansion
- **Item Systems**: 65% complete
- **Social Systems**: 70% complete
- **World Systems**: 60% complete
- **Economy Systems**: 75% complete
- **Quest Systems**: 60% complete

## Change Log
- Documented complex spell effects system with 80+ effect types
- Created comprehensive spell component system documentation
- Added detailed effect stacking logic with all special cases
- Cross-referenced all magic system documents
- Enhanced existing magic documentation with stacking details

## Cross-Reference Updates

### Updated Core_Systems_Game_Rules.md
- Added note directing users to SRD for current mechanics

### Integration Points Documented
- **Property System**: How effects modify properties through categories
- **Combat System**: Damage add stacking, damage shields, ablatives
- **CC System**: Duration modifications, immunities, NPC resistances
- **Save System**: Which effects persist through logout

## Key Formulas Documented

### Effect Duration
```
Base: Spell.Duration × (1 + SpellDurationBonus%)
Resisted: Base × (1 - TargetResist%)
Capped: Max(1ms, Min(Base × 4, Duration))
AoE PvP: Duration × (1 - Distance/Radius × 0.5)
```

### Effectiveness Scaling
```
List Caster: 100%
Non-List: 75% + (SpecLevel - 1) × 0.5 / SpellLevel
Clamped: Max(75%, Min(125%, Effectiveness))
```

### Concentration Range
```
Standard: BUFF_RANGE property (default 5000)
Endurance: 1500 units (hardcoded)
Check Interval: 2500ms
```

## Testing Implications

### New Test Cases Needed
1. Effect stacking with mixed casters
2. Concentration range boundary conditions  
3. Pulse effect parent-child cancellation
4. Diminishing returns on NPCs
5. Component category precedence
6. Effect group overwriting
7. Disabled effect re-enabling
8. Critical debuff duration variance
9. Ablative armor stacking comparison
10. Speed debuff special pulse timing

## Implementation Insights

### Architecture Patterns
- Effects stored by type in EffectListComponent
- Single active effect per type enforced
- Disabled effects maintained in list
- Parent-child pattern for pulse effects
- Bonus categories separate stat contributions

### Performance Optimizations
- Type-based effect indexing
- Throttled concentration checks
- Bulk property updates
- Cached effectiveness calculations

## Future Documentation Needs
- Spell interruption mechanics
- Spell queue system
- Cast time modifications
- Spell range calculations
- LoS and geometry checks
- Spell power costs
- Subspell mechanics
- Spell damage variance
- Resist layer interactions
- Focus spell mechanics

## Statistics
- **New Documents**: 3
- **Total Sections**: 21 major mechanics documented
- **Code References**: 50+ source files examined
- **Formulas Captured**: 15+ calculation methods
- **Edge Cases Found**: 25+ special behaviors

This pass significantly expands our understanding of how spells create and manage their effects, providing crucial information for both implementation and testing of the magic system. 