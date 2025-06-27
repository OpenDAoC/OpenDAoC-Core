# Dependency Injection Container Selection Decision

## Task: FDIS-001 - Research and select DI container (MS.Extensions.DI recommended)

## Decision: Microsoft.Extensions.DependencyInjection

### Rationale

After evaluating available DI containers for .NET, Microsoft.Extensions.DependencyInjection was selected as the primary dependency injection container for OpenDAoC based on the following criteria:

### Pros:
1. **Native .NET Integration**: First-party Microsoft package with excellent integration into the .NET ecosystem
2. **Performance**: Optimized for .NET applications with minimal overhead
3. **Familiarity**: Standard choice for .NET applications, reducing learning curve
4. **Lifetime Management**: Built-in support for Singleton, Scoped, and Transient lifetimes
5. **Hosting Integration**: Seamless integration with Microsoft.Extensions.Hosting
6. **Logging Integration**: Works seamlessly with Microsoft.Extensions.Logging
7. **Configuration Integration**: Works with Microsoft.Extensions.Configuration
8. **Minimal Dependencies**: Lightweight with minimal external dependencies
9. **Long-term Support**: Maintained by Microsoft with guaranteed long-term support

### Cons:
1. **Feature Set**: Less feature-rich than third-party alternatives like Autofac or Ninject
2. **Advanced Scenarios**: May require custom implementations for complex scenarios
3. **Interception**: No built-in AOP/interception capabilities

### Alternatives Considered:

#### Autofac
- **Pros**: Rich feature set, excellent documentation, AOP support
- **Cons**: Additional dependency, steeper learning curve, performance overhead

#### Ninject
- **Pros**: Fluent API, extensive features
- **Cons**: Performance concerns, maintenance status unclear

#### Castle Windsor
- **Pros**: Mature, feature-rich
- **Cons**: Complex configuration, heavy footprint

### Implementation Plan:

1. **Phase 1**: Add Microsoft.Extensions.DependencyInjection to GameServer.csproj ✅
2. **Phase 2**: Create GameServerHost with IServiceCollection setup
3. **Phase 3**: Implement service registration infrastructure
4. **Phase 4**: Create legacy adapter pattern for gradual migration
5. **Phase 5**: Performance optimization with compiled delegates

### Performance Considerations:

- Service resolution target: <100ns for hot paths
- Memory overhead budget: <5% of total application memory
- Startup time impact: <500ms additional startup time
- Compile-time optimization for critical services

### Migration Strategy:

1. **Gradual Migration**: Use adapter pattern to wrap existing static calls
2. **Backwards Compatibility**: Maintain existing API surface during transition
3. **Feature Flags**: Enable/disable DI features during rollout
4. **Performance Monitoring**: Continuous monitoring of performance impact

### Success Metrics:

- Zero static dependencies (eliminate GameServer.Instance)
- 100% constructor injection coverage
- Performance targets met
- Successful integration with existing architecture

---

**Decision Made**: January 2025  
**Status**: Approved  
**Next Task**: FDIS-002 - Create GameServerHost with IServiceCollection setup 