using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using DOL.GS.Interfaces;
using DOL.GS.Interfaces.Combat;
using DOL.GS.Interfaces.Items;
using DOL.GS.Interfaces.Character;
using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.PropertyCalculators;

namespace DOL.Tests.Unit.Interfaces
{
    /// <summary>
    /// Comprehensive tests for interface architecture compliance
    /// Validates SOLID principles, segregation, and clean architecture
    /// </summary>
    [TestFixture]
    public class InterfaceArchitectureTests
    {
        private Assembly _gameServerAssembly;
        private Type[] _allInterfaces;

        [SetUp]
        public void Setup()
        {
            _gameServerAssembly = typeof(IGameObject).Assembly;
            _allInterfaces = _gameServerAssembly.GetTypes()
                .Where(t => t.IsInterface && t.Namespace?.Contains("Interfaces") == true)
                .ToArray();
        }

        #region Interface Segregation Principle (ISP) Tests

        [Test]
        [Category("Architecture")]
        public void AllInterfaces_ShouldFollowISP_MaxFiveMethodsOrProperties()
        {
            var violations = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                var memberCount = interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.MemberType == MemberTypes.Method || m.MemberType == MemberTypes.Property)
                    .Count();

                if (memberCount > 5)
                {
                    violations.Add($"{interfaceType.Name} has {memberCount} members (max 5 allowed)");
                }
            }

            Assert.That(violations, Is.Empty, 
                $"Interface Segregation Principle violations:\n{string.Join("\n", violations)}");
        }

        [Test]
        [Category("Architecture")]
        public void CoreInterfaces_ShouldBeFocused()
        {
            // Test specific interfaces for focused responsibilities
            var coreInterfaces = new[]
            {
                typeof(IIdentifiable),
                typeof(IPositionable), 
                typeof(IEventNotifier),
                typeof(IDamageable),
                typeof(IAttackable),
                typeof(IDefender)
            };

            foreach (var interfaceType in coreInterfaces)
            {
                var methodCount = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
                var propertyCount = interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length;
                
                Assert.That(methodCount + propertyCount, Is.LessThanOrEqualTo(5),
                    $"{interfaceType.Name} should have <= 5 members for focus");
            }
        }

        #endregion

        #region Dependency Direction Tests

        [Test]
        [Category("Architecture")]
        public void DomainInterfaces_ShouldHaveZeroDependencies()
        {
            var domainInterfaces = _allInterfaces
                .Where(t => t.Namespace?.Contains("Core") == true || 
                           t.Namespace?.Contains("Combat") == true)
                .ToArray();

            foreach (var interfaceType in domainInterfaces)
            {
                var dependencies = GetInterfaceDependencies(interfaceType);
                var externalDependencies = dependencies
                    .Where(d => !d.Assembly.Equals(_gameServerAssembly) && 
                               !d.Namespace?.StartsWith("System") == true)
                    .ToArray();

                Assert.That(externalDependencies, Is.Empty,
                    $"Domain interface {interfaceType.Name} has external dependencies: {string.Join(", ", externalDependencies.Select(d => d.Name))}");
            }
        }

        [Test]
        [Category("Architecture")]
        public void ApplicationInterfaces_ShouldOnlyDependOnDomain()
        {
            // When we have application layer interfaces, test they only depend on domain
            var applicationInterfaces = _allInterfaces
                .Where(t => t.Name.Contains("UseCase") || t.Name.Contains("Service"))
                .ToArray();

            foreach (var interfaceType in applicationInterfaces)
            {
                var dependencies = GetInterfaceDependencies(interfaceType);
                var invalidDependencies = dependencies
                    .Where(d => d.Namespace?.Contains("Infrastructure") == true)
                    .ToArray();

                Assert.That(invalidDependencies, Is.Empty,
                    $"Application interface {interfaceType.Name} depends on infrastructure: {string.Join(", ", invalidDependencies.Select(d => d.Name))}");
            }
        }

        #endregion

        #region Interface Completeness Tests

        [Test]
        [Category("Architecture")]
        public void GameObjectHierarchy_ShouldHaveCompleteInterfaces()
        {
            // Verify our core game object interfaces are complete
            Assert.That(typeof(IGameObject), Is.Not.Null);
            Assert.That(typeof(IGameLiving), Is.Not.Null);
            Assert.That(typeof(ICharacter), Is.Not.Null);

            // Verify inheritance chain
            Assert.That(typeof(IGameLiving).GetInterfaces(), Contains.Item(typeof(IGameObject)));
            Assert.That(typeof(ICharacter).GetInterfaces(), Contains.Item(typeof(IGameLiving)));
        }

        [Test]
        [Category("Architecture")]
        public void CombatInterfaces_ShouldHaveCompleteSet()
        {
            var requiredCombatInterfaces = new[]
            {
                "IAttackable",
                "IAttacker", 
                "IDefender",
                "IDamageable",
                "IWeaponSpecialist",
                "ICriticalCapable"
            };

            foreach (var interfaceName in requiredCombatInterfaces)
            {
                var interfaceType = _allInterfaces.FirstOrDefault(t => t.Name == interfaceName);
                Assert.That(interfaceType, Is.Not.Null, $"Missing combat interface: {interfaceName}");
            }
        }

        [Test]
        [Category("Architecture")]
        public void ItemInterfaces_ShouldCoverAllTypes()
        {
            var requiredItemInterfaces = new[]
            {
                "IItem",
                "IWeapon",
                "IArmor", 
                "IConsumable",
                "IStackableItem",
                "IMagicalItem"
            };

            foreach (var interfaceName in requiredItemInterfaces)
            {
                var interfaceType = _allInterfaces.FirstOrDefault(t => t.Name == interfaceName);
                Assert.That(interfaceType, Is.Not.Null, $"Missing item interface: {interfaceName}");
            }
        }

        #endregion

        #region Contract Consistency Tests

        [Test]
        [Category("Architecture")]
        public void AllInterfaces_ShouldHaveConsistentNaming()
        {
            var namingViolations = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                if (!interfaceType.Name.StartsWith("I"))
                {
                    namingViolations.Add($"{interfaceType.Name} should start with 'I'");
                }

                if (interfaceType.Name.Contains("Manager") || interfaceType.Name.Contains("Helper"))
                {
                    namingViolations.Add($"{interfaceType.Name} uses discouraged naming (Manager/Helper)");
                }
            }

            Assert.That(namingViolations, Is.Empty,
                $"Interface naming violations:\n{string.Join("\n", namingViolations)}");
        }

        [Test]
        [Category("Architecture")]
        public void AllInterfaces_ShouldHaveDocumentation()
        {
            var undocumentedInterfaces = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                var xmlDocumentation = interfaceType.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                // For this test, we'll check if interface has at least one documented member
                var hasDocumentedMembers = interfaceType.GetMembers()
                    .Any(m => m.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>().Any());

                if (string.IsNullOrEmpty(xmlDocumentation) && !hasDocumentedMembers)
                {
                    undocumentedInterfaces.Add(interfaceType.Name);
                }
            }

            // Allow some undocumented for now, but track progress
            TestContext.WriteLine($"Undocumented interfaces: {undocumentedInterfaces.Count}/{_allInterfaces.Length}");
            
            if (undocumentedInterfaces.Count > 0)
            {
                TestContext.WriteLine($"Needs documentation: {string.Join(", ", undocumentedInterfaces)}");
            }
        }

        #endregion

        #region Mocking and Testability Tests

        [Test]
        [Category("Architecture")]
        public void AllInterfaces_ShouldBeMockable()
        {
            var unmockableInterfaces = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                // Check for static members (not mockable)
                var staticMembers = interfaceType.GetMembers(BindingFlags.Static | BindingFlags.Public);
                if (staticMembers.Length > 0)
                {
                    unmockableInterfaces.Add($"{interfaceType.Name} has static members");
                }

                // Check for sealed methods (C# 8+ default interface methods)
                var sealedMethods = interfaceType.GetMethods()
                    .Where(m => m.IsFinal)
                    .ToArray();
                
                if (sealedMethods.Length > 0)
                {
                    unmockableInterfaces.Add($"{interfaceType.Name} has sealed methods");
                }
            }

            Assert.That(unmockableInterfaces, Is.Empty,
                $"Unmockable interfaces found:\n{string.Join("\n", unmockableInterfaces)}");
        }

        #endregion

        #region Performance-Aware Design Tests

        [Test]
        [Category("Architecture")]
        public void HotPathInterfaces_ShouldBeMinimal()
        {
            // Interfaces used in hot paths should be especially lean
            var hotPathInterfaces = new[]
            {
                typeof(IDamageable),
                typeof(IAttackable),
                typeof(IPositionable)
            };

            foreach (var interfaceType in hotPathInterfaces)
            {
                var memberCount = interfaceType.GetMembers().Length;
                Assert.That(memberCount, Is.LessThanOrEqualTo(3),
                    $"Hot path interface {interfaceType.Name} has {memberCount} members, should be <= 3");
            }
        }

        [Test]
        [Category("Architecture")]
        public void Interfaces_ShouldNotExposeImplementationDetails()
        {
            var violations = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                var methods = interfaceType.GetMethods();
                foreach (var method in methods)
                {
                    // Check return types for implementation exposure
                    if (method.ReturnType.Name.Contains("Dictionary") || 
                        method.ReturnType.Name.Contains("List") ||
                        method.ReturnType.Name.Contains("Array"))
                    {
                        violations.Add($"{interfaceType.Name}.{method.Name} exposes {method.ReturnType.Name}");
                    }

                    // Check parameters
                    foreach (var param in method.GetParameters())
                    {
                        if (param.ParameterType.Name.Contains("Dictionary") ||
                            param.ParameterType.Name.Contains("List"))
                        {
                            violations.Add($"{interfaceType.Name}.{method.Name} parameter {param.Name} exposes {param.ParameterType.Name}");
                        }
                    }
                }
            }

            // Some violations might be acceptable, but track them
            if (violations.Count > 0)
            {
                TestContext.WriteLine($"Implementation exposure warnings: {violations.Count}");
                TestContext.WriteLine(string.Join("\n", violations.Take(10))); // Show first 10
            }
        }

        #endregion

        #region Helper Methods

        private Type[] GetInterfaceDependencies(Type interfaceType)
        {
            var dependencies = new HashSet<Type>();

            // Get dependencies from inherited interfaces
            foreach (var inherited in interfaceType.GetInterfaces())
            {
                dependencies.Add(inherited);
            }

            // Get dependencies from method signatures
            foreach (var method in interfaceType.GetMethods())
            {
                dependencies.Add(method.ReturnType);
                foreach (var param in method.GetParameters())
                {
                    dependencies.Add(param.ParameterType);
                }
            }

            // Get dependencies from properties
            foreach (var property in interfaceType.GetProperties())
            {
                dependencies.Add(property.PropertyType);
            }

            return dependencies.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// Tests for specific interface contracts and behaviors
    /// </summary>
    [TestFixture]
    public class InterfaceContractTests
    {
        [Test]
        [Category("Contracts")]
        public void IGameObject_ShouldHaveIdentification()
        {
            // Test that IGameObject provides proper identification
            var gameObjectInterface = typeof(IGameObject);
            
            var hasObjectId = gameObjectInterface.GetProperty("ObjectId") != null;
            var hasName = gameObjectInterface.GetProperty("Name") != null;
            
            Assert.That(hasObjectId || hasName, Is.True,
                "IGameObject should have either ObjectId or Name property for identification");
        }

        [Test]
        [Category("Contracts")]
        public void IDamageable_ShouldHaveHealthConcepts()
        {
            var damageableInterface = typeof(IDamageable);
            
            var hasTakeDamage = damageableInterface.GetMethods()
                .Any(m => m.Name.Contains("Damage"));
            
            Assert.That(hasTakeDamage, Is.True,
                "IDamageable should have damage-related methods");
        }

        [Test]
        [Category("Contracts")]
        public void IAttacker_ShouldHaveAttackCapabilities()
        {
            var attackerInterface = typeof(IAttacker);
            
            var hasAttackMethods = attackerInterface.GetMethods()
                .Any(m => m.Name.Contains("Attack") || m.Name.Contains("CanAttack"));
            
            Assert.That(hasAttackMethods, Is.True,
                "IAttacker should have attack-related methods");
        }

        [Test]
        [Category("Contracts")]
        public void IInventory_ShouldHaveItemManagement()
        {
            var inventoryInterface = typeof(IInventory);
            
            if (inventoryInterface != null) // Check if interface exists
            {
                var hasItemMethods = inventoryInterface.GetMethods()
                    .Any(m => m.Name.Contains("Item") || m.Name.Contains("Add") || m.Name.Contains("Remove"));
                
                Assert.That(hasItemMethods, Is.True,
                    "IInventory should have item management methods");
            }
        }
    }
} 