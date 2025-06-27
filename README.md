# OpenDAoC - Clean Architecture Refactoring Branch
[![Build and Release](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml)

## 🚀 Refactoring Initiative - Source of Truth

**📋 PRIMARY DOCUMENTATION**: [OpenDAoC Refactoring Progress Tracker](Helper%20Docs/OpenDAoC_Refactoring_Progress_Tracker.md)

This is the **definitive source of truth** for the OpenDAoC Clean Architecture refactoring project. All progress, tasks, metrics, and decisions are tracked there.

### Current Status
- **Phase**: Phase 1 - Foundation Layer
- **Progress**: 30/480 tasks (6.3% complete)
- **Week**: Week 2 COMPLETE ✅ | Week 3 READY TO START
- **Architecture Focus**: Clean Architecture with DI and Interface-First Design

### Quick Links
- 📊 **[Progress Tracker](Helper%20Docs/OpenDAoC_Refactoring_Progress_Tracker.md)** - Complete project status and task tracking
- 📈 **[Architecture Progress Summary](Helper%20Docs/Architecture_Progress_Summary.md)** - High-level progress overview  
- 🏗️ **[Development Standards v3](Helper%20Docs/OpenDAoC_Development_Standards_v3.mdc)** - Comprehensive architecture standards
- 📖 **[DI Migration Guide](Helper%20Docs/OpenDAoC_DI_Migration_Guide.md)** - Step-by-step dependency injection migration

## 📚 Key Supporting Documentation

### Architecture & Standards
- **[Development Standards v3](Helper%20Docs/OpenDAoC_Development_Standards_v3.mdc)** - Complete clean architecture standards and patterns
- **[Architecture Alignment Guide](Helper%20Docs/OpenDAoC_Architecture_Alignment_Guide.md)** - Architecture principles and guidelines
- **[Core Systems Interface Design](Helper%20Docs/Core_Systems_Interface_Design.md)** - Interface design patterns and hierarchy

### Migration & Implementation
- **[DI Migration Guide](Helper%20Docs/OpenDAoC_DI_Migration_Guide.md)** - Comprehensive dependency injection migration guide
- **[DI Container Selection](Helper%20Docs/DI_Container_Selection_Decision.md)** - Technical decision for Microsoft.Extensions.DependencyInjection
- **[Comprehensive Refactoring Plan](Helper%20Docs/Comprehensive_Refactoring_Plan.md)** - Detailed refactoring strategy

### Code Quality & Reviews
- **[Week 2 Code Review](Helper%20Docs/Week2_Interface_Extraction_Code_Review.md)** - Latest interface extraction review (B+ rating)
- **[Code Review Architecture Assessment](Helper%20Docs/Code_Review_Architecture_Assessment.md)** - Architecture quality evaluation
- **[Code Review Guide](Helper%20Docs/OpenDAoC_Code_Review_Guide.md)** - Standards for code reviews

### Testing & Quality Assurance
- **[Core Systems Testing Framework](Helper%20Docs/Core_Systems_Testing_Framework.md)** - Testing infrastructure and patterns
- **[Test Review Guide](Helper%20Docs/OpenDAoC_Test_Review_Guide.md)** - Comprehensive testing standards

### Game Systems Documentation
- **[Systems Reference Document (SRD)](SRD/README.md)** - Complete game mechanics documentation
- **[Core Systems Game Rules](Helper%20Docs/Core_Systems_Game_Rules.md)** - Game rule implementations

## 🎯 Refactoring Goals

### Architecture Transformation
- **Clean Architecture**: Proper layer separation with zero violations
- **Dependency Injection**: 100% DI coverage, eliminate static dependencies  
- **Interface-First Design**: 95%+ interface coverage for all public APIs
- **Performance**: <0.5ms combat calculations, zero-allocation hot paths

### Scalability Targets
- **Single Server**: 2,000 concurrent players
- **Distributed**: 10,000+ concurrent players
- **Zero Downtime**: Gradual migration with feature flags

### Quality Standards
- **Test Coverage**: 90%+ for business logic
- **Code Complexity**: <7 cyclomatic complexity average
- **Documentation**: Comprehensive architecture and implementation docs

## 📋 Current Phase Details

### ✅ Week 1: DI Infrastructure Setup (COMPLETE)
- Dependency injection container setup
- Service lifetime management
- Legacy adapter patterns
- Performance-optimized factories

### ✅ Week 2: Interface Extraction (COMPLETE)  
- Complete interface hierarchy (IGameObject, IGameLiving, ICharacter)
- Interface segregation principle compliance
- Adapter pattern implementations
- Unit tests and dependency validation

### 🔄 Week 3: Layer Architecture Setup (IN PROGRESS)
- Physical layer separation (Domain, Application, Infrastructure, Presentation)
- Dependency rule enforcement
- Cross-layer mapping infrastructure
- Use case interfaces and repository patterns

## 📈 Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Interface Coverage | ~15% | 95% | 🔴 In Progress |
| DI Coverage | ~5% | 100% | 🔴 In Progress |
| Layer Violations | Many | 0 | 🔴 Not Started |
| Static Dependencies | 1,000+ | 0 | 🔴 In Progress |
| Test Coverage | ~10% | 90% | 🔴 In Progress |

## 🛠️ Development Workflow

### For Contributors
1. **Check Progress Tracker** - Always start with the [Progress Tracker](Helper%20Docs/OpenDAoC_Refactoring_Progress_Tracker.md)
2. **Follow Standards** - Adhere to [Development Standards v3](Helper%20Docs/OpenDAoC_Development_Standards_v3.mdc)
3. **Interface-First** - Design interfaces before implementations
4. **Test-Driven** - Write tests first, aim for 90%+ coverage
5. **Update Documentation** - Keep progress tracker and docs current

### Branch Strategy
- **Main Branch**: Stable production code
- **Refactor Branch**: Active clean architecture development
- **Feature Flags**: Safe rollout of new architecture components

---

## About OpenDAoC

OpenDAoC is an emulator for Dark Age of Camelot (DAoC) servers, originally a fork of the [DOLSharp](https://github.com/Dawn-of-Light/DOLSharp) project.

Now undergoing a complete architectural transformation with ECS architecture and clean architecture principles, OpenDAoC ensures performance and scalability for thousands of players, providing a robust platform for creating and managing DAoC servers.

While the project focuses on recreating the DAoC 1.65 experience, it can be adapted for any patch level.

## Documentation

The easiest way to get started with OpenDAoC is to use Docker. Check out the `docker-compose.yml` file in the repository root for an example setup.

For detailed instructions and additional setup options, refer to the full [OpenDAoC Documentation](https://www.opendaoc.com/docs/).

## Releases

Releases for OpenDAoC are available at [OpenDAoC Releases](https://github.com/OpenDAoC/OpenDAoC-Core/releases).

OpenDAoC is also available as a Docker image, which can be pulled from the following registries:

- [GitHub Container Registry](https://ghcr.io/opendaoc/opendaoc-core) (recommended): `ghcr.io/opendaoc/opendaoc-core/opendaoc:latest`
- [Docker Hub](https://hub.docker.com/repository/docker/claitz/opendaoc/): `claitz/opendaoc:latest`

For detailed instructions and additional setup options, refer to the documentation.

## Companion Repositories

Several companion repositories are part of the [OpenDAoC project](https://github.com/OpenDAoC).

Some of the main repositories include:

- [OpenDAoC Database v1.65](https://github.com/OpenDAoC/OpenDAoC-Database)
- [Account Manager](https://github.com/OpenDAoC/opendaoc-accountmanager)
- [Client Launcher](https://github.com/OpenDAoC/OpenDAoC-Launcher)

## License

OpenDAoC is licensed under the [GNU General Public License (GPL)](https://choosealicense.com/licenses/gpl-3.0/) v3 to serve the DAoC community and promote open-source development.  
See the [LICENSE](LICENSE) file for more details.
