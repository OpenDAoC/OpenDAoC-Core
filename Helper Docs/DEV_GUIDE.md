# OpenDAoC Core Development Guidelines

## Core Architectural Principles

### SOLID Principles Compliance
- Single Responsibility: Each class focuses on one domain area
- Interface Segregation: Clients only depend on interfaces they actually use  
- Open/Closed: Easier to extend individual systems without affecting others
- Dependency Inversion: All systems depend on interfaces, not implementations

### Key Framework Patterns
- Interface-First Development: Complete contracts before implementation
- Dependency Injection: Zero GetComponent calls in production code
- Service Locator: Use a service locator for all service dependencies
- Modular Decomposition: Follow extracted subsystem patterns

## DO: Essential Practices

### Interface-First Development
- DO create interfaces before implementations
- DO use existing interface patterns from PathfinderTactics.Interfaces  
- DO check PathfinderTactics.Interfaces first before creating new types
- DO use composition and extension methods rather than duplicating existing contracts

### Dependency Injection
- DO use a service locator for all service dependencies
- DO register services through dependency injection
- DO follow the established DependencyDescriptor pattern
- DO use OnDependencyInjected for component initialization

### Code Quality Standards
- DO use established C# naming conventions (PascalCase for public, camelCase for private)
- DO prefix private fields with m_ (e.g., m_VariableName)
- DO prefix static fields with s_ (e.g., s_StaticName)
- DO use descriptive names that explain purpose

### Testing Requirements
- DO write tests for all new features using existing test base classes
- DO include rule citations in test documentation
- DO use MockDiceSystem for deterministic testing
- DO follow test hierarchies (*TestBase, *SpecificTestBase, etc.)

### DAoC Rule Compliance
- DO cite official DAoC rule sources in code comments
- DO implement authentic DAoC mechanics following official rules



## DONT: Anti-Patterns to Avoid

### Architectural Violations
- DONT use GetComponent calls in production code - use dependency injection
- DONT create God classes - follow extracted subsystem patterns
- DONT bypass the service locator for system dependencies
- DONT create circular dependencies between systems

### Code Quality Issues
- DONT create classes larger than 1,000 lines without decomposition plan
- DONT duplicate existing functionality - extend or compose instead
- DONT skip interface contracts for public APIs
- DONT use magic numbers - use a Constants class for all game values

### Testing Violations
- DONT skip test coverage for new features
- DONT use real dice rolls in tests - use MockDiceSystem
- DONT create tests without DAoC rule validation
- DONT ignore failing tests or use Assert.Pass without implementation

### DAoC Rule Violations
- DONT implement house rules without clear documentation
- DONT create mechanics that contradict official DAoC rules
- DONT skip rule citations for complex implementations
- DONT violate ORC License compliance requirements

## Quick Reference

### When Adding New Features
1. Check PathfinderTactics.Interfaces for existing contracts
2. Create interface before implementation
3. Use a service locator for dependencies
4. Write tests with DAoC rule validation
5. Follow established naming conventions

### When Refactoring Large Classes
1. Follow UTS extraction pattern
2. Identify clear domain boundaries
3. Extract focused subsystems
4. Maintain public API compatibility
5. Update tests for new structure

---

These guidelines ensure consistency with established patterns and maintain the professional quality of the OpenDAoC Core.
