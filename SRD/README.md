# OpenDAoC System Reference Document (SRD)

## Overview

Welcome to the **OpenDAoC System Reference Document (SRD)** - the definitive, authoritative reference for all Dark Age of Camelot game mechanics, rules, and systems. This living document serves as the single source of truth for authentic DAoC gameplay implementation.

## Purpose

The SRD ensures that OpenDAoC preserves the authentic Dark Age of Camelot experience by providing:

- **Exact game mechanics** with mathematical formulas and edge cases
- **Implementation guidance** for developers coding these systems  
- **Test specifications** for validating correct behavior
- **Cross-system interactions** to understand complex dependencies
- **Performance requirements** for server stability

## Quick Start

### For Developers
1. **Implementing a feature?** Check the relevant SRD section first
2. **Found missing info?** Research and update the SRD as part of your work
3. **Discovered new mechanics?** Document them following the SRD template
4. **Need test cases?** Use the SRD test scenarios as your starting point

### For Testers
1. **Writing tests?** Reference SRD specifications for expected behavior
2. **Found discrepancies?** Check if SRD needs updating or code needs fixing
3. **Testing edge cases?** SRD documents known edge cases and special conditions

### For Contributors
1. **Community research?** Contributions to SRD are highly valued
2. **Rules clarification?** Submit updates with your sources and verification
3. **Implementation feedback?** Help improve SRD based on practical experience

## Document Structure

### Primary Categories

```
01_Combat_Systems/     - Attack resolution, damage, defense mechanics
02_Character_Systems/  - Progression, stats, classes, specializations  
03_Magic_Systems/      - Spells, resistance, crowd control, healing
04_Item_Systems/       - Equipment, bonuses, quality, artifacts
05_Social_Systems/     - Guilds, alliances, chat, player titles
06_World_Systems/      - Housing, keeps, relics, territory control
07_Economy_Systems/    - Crafting, trade, consignment, economy balance
08_Quest_Systems/      - Quests, tasks, missions, reward calculations
09_Performance_Systems/ - Server mechanics, network, optimization rules
10_Cross_System_Interactions/ - Dependencies, events, integration points
```

### Document Template

Each SRD document follows a standardized template:

- **Document Status**: Completeness, verification status, implementation status
- **Overview**: System purpose and role in DAoC
- **Core Mechanics**: Detailed formulas, sources, requirements, edge cases
- **System Interactions**: How this system affects/is affected by others
- **Implementation Notes**: Performance, architecture, common pitfalls
- **Test Scenarios**: Specific test cases for validation
- **Change Log**: Track all updates and modifications
- **References**: Sources, documentation, implementation links

## How to Use This SRD

### Reading SRD Documents

Each document provides multiple levels of detail:

1. **Overview** - Quick understanding of the system
2. **Core Mechanics** - Implementation-ready specifications  
3. **Edge Cases** - Special conditions and exceptions
4. **Test Scenarios** - Validation requirements
5. **References** - Additional information sources

### Updating SRD Documents

#### When to Update
- **New rule discovery** - Found previously unknown mechanic
- **Rule clarification** - Existing rule needs correction/completion
- **Gap identification** - Missing information identified
- **Implementation feedback** - Code reveals new details or edge cases

#### Update Process
1. Research and verify the information
2. Update the appropriate SRD section
3. Add detailed change log entry
4. Update affected test scenarios
5. Review cross-system impacts
6. Commit with "SRD:" prefix

### Example SRD Usage

#### For Implementation
```csharp
// Developer implementing attack resolution
// References: SRD/01_Combat_Systems/Attack_Resolution.md

public AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context)
{
    // SRD Section: Attack Resolution Order
    if (CheckIntercept(attacker, defender)) return AttackResult.Intercepted;
    if (CheckEvade(defender, context.AttackerCount)) return AttackResult.Evaded;
    if (CheckParry(defender, attacker.Weapon)) return AttackResult.Parried;
    // ... continue following SRD specification
}
```

#### For Testing
```csharp
// Tester validating implementation
// References: SRD/01_Combat_Systems/Attack_Resolution.md - Test Scenario 3

[Test]
public void AttackResolution_ShouldCheckEvadeBeforeParry_WhenBothAvailable()
{
    // Test validates SRD Attack Resolution Order specification
    // Expected: Evade checked first, parry only if evade fails
}
```

## Quality Standards

### Documentation Requirements
- **Accuracy**: All information verified against authentic DAoC behavior
- **Completeness**: Cover all aspects of the system including edge cases
- **Clarity**: Written for developers who need to implement the system
- **Testability**: Provide specific test scenarios for validation
- **Traceability**: Link to sources and references

### Maintenance Standards
- **Currency**: Keep up-to-date with latest discoveries
- **Consistency**: Use standardized terminology across all documents
- **Cross-references**: Link related systems and dependencies
- **Change tracking**: Document all modifications with rationale

## Integration with Development

### Code Reviews
- Check SRD compliance during code review
- Flag discrepancies between implementation and SRD
- Require SRD updates for new features

### Testing
- Reference SRD specifications in test documentation
- Use SRD test scenarios as test case templates
- Update SRD when tests reveal new information

### Issue Tracking
- Check SRD for expected behavior when investigating bugs
- Update SRD as part of feature implementation
- Document performance requirements from SRD

## Contributing to the SRD

### Research Guidelines
- **Verify sources**: Use multiple sources when possible
- **Test thoroughly**: Validate mechanics through testing
- **Document process**: Explain how you verified the information
- **Community input**: Leverage collective DAoC knowledge

### Writing Guidelines
- **Be specific**: Provide exact formulas and calculations
- **Include edge cases**: Document special conditions and exceptions
- **Implementation focus**: Write for developers coding the system
- **Test scenarios**: Provide concrete validation examples

### Review Process
- Technical review for accuracy and completeness
- Implementation review for feasibility and performance
- Community review for authenticity (when applicable)

## Current Status

### Coverage Overview
- **Combat Systems**: 85% complete - Attack resolution, damage, defense, aggro, siege warfare documented
- **Character Systems**: 80% complete - Progression, death/resurrection, stealth systems documented
- **Magic Systems**: 90% complete - Spell mechanics, effects system, component system, pet summoning documented
- **Item Systems**: 65% complete - Equipment basics done, artifact systems pending
- **Social Systems**: 70% complete - Guild, housing, group systems documented
- **World Systems**: 60% complete - Region/zone, movement/speed mechanics documented
- **Economy Systems**: 75% complete - Money system, crafting documented, economy balance needed
- **Quest Systems**: 60% complete - Quest mechanics documented, advanced rewards pending
- **Performance Systems**: 0% complete - Not yet documented
- **Cross-System Interactions**: 0% complete - Not yet documented

### Priority Areas
1. **Performance specifications** - Ensuring server stability
2. **Cross-system interactions** - Understanding complex dependencies
3. **Item system completeness** - Artifact and special item mechanics
4. **Advanced quest rewards** - Complex reward calculations

## Getting Help

### Questions
- Check existing SRD documents first
- Review change logs for recent updates
- Consult references section for additional sources

### Issues
- Report missing information as GitHub issues
- Suggest corrections with supporting evidence
- Request clarification for unclear specifications

### Community
- Engage with DAoC community experts
- Participate in research initiatives
- Share discoveries and verifications

## Future Vision

The OpenDAoC SRD aims to become:

- **The definitive reference** for DAoC server emulation
- **A preservation tool** for authentic DAoC mechanics
- **A collaboration platform** for community knowledge
- **An implementation guide** for future developers
- **A testing foundation** for quality assurance

**Remember**: The SRD is not just documentation—it's the blueprint for preserving DAoC's authentic gameplay for future generations of players.

---

*"The goal is not to change the game, but to improve the code that runs it."*

For questions or contributions, see [Helper Docs/OpenDAoC_SRD_Structure_Plan.md](../Helper%20Docs/OpenDAoC_SRD_Structure_Plan.md) for detailed processes and guidelines. 