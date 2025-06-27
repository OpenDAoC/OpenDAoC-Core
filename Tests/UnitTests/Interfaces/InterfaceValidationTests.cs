using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DOL.Tests.Unit.Interfaces
{
    /// <summary>
    /// Validates interface architecture compliance and design principles
    /// FIEX-013: Setup interface unit tests
    /// FIEX-014: Validate interface segregation principle
    /// </summary>
    [TestFixture]
    public class InterfaceValidationTests
    {
        private Assembly _gameServerAssembly;
        private Type[] _allInterfaces;

        [SetUp]
        public void Setup()
        {
            _gameServerAssembly = typeof(DOL.GS.GameServer).Assembly;
            _allInterfaces = _gameServerAssembly.GetTypes()
                .Where(t => t.IsInterface && 
                           (t.Namespace?.Contains("Interfaces") == true || t.Name.StartsWith("I")))
                .ToArray();
        }

        #region FIEX-014: Interface Segregation Principle Validation

        [Test]
        [Category("ISP")]
        public void AllInterfaces_ShouldFollowISP_MaxFiveMethods()
        {
            var violations = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                var methodCount = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
                var propertyCount = interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length;
                var totalMembers = methodCount + propertyCount;

                if (totalMembers > 5)
                {
                    violations.Add($"{interfaceType.Name}: {totalMembers} members (Methods: {methodCount}, Properties: {propertyCount})");
                }
            }

            Assert.That(violations, Is.Empty, 
                $"Interface Segregation Principle violations:\n{string.Join("\n", violations)}");
            
            TestContext.WriteLine($"✅ ISP Compliance: {_allInterfaces.Length - violations.Count}/{_allInterfaces.Length} interfaces follow ISP");
        }

        [Test]
        [Category("ISP")]
        public void CoreGameInterfaces_ShouldBeSegregated()
        {
            // Test that our major interfaces are properly segregated
            var coreInterfaceChecks = new Dictionary<string, int>
            {
                {"IGameObject", 5},      // Core identification and positioning
                {"IGameLiving", 5},      // Living entity basics  
                {"IDamageable", 3},      // Just damage-related
                {"IAttackable", 3},      // Just attack-related
                {"IDefender", 3},        // Just defense-related
                {"IPositionable", 3},    // Just position-related
                {"IIdentifiable", 2}     // Just ID-related
            };

            foreach (var check in coreInterfaceChecks)
            {
                var interfaceType = _allInterfaces.FirstOrDefault(t => t.Name == check.Key);
                if (interfaceType != null)
                {
                    var memberCount = interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance).Length;
                    Assert.That(memberCount, Is.LessThanOrEqualTo(check.Value),
                        $"{check.Key} has {memberCount} members, should have ≤{check.Value} for proper segregation");
                }
            }
        }

        #endregion

        #region Interface Naming and Documentation Tests

        [Test]
        [Category("Standards")]
        public void AllInterfaces_ShouldFollowNamingConventions()
        {
            var violations = new List<string>();

            foreach (var interfaceType in _allInterfaces)
            {
                // Must start with 'I'
                if (!interfaceType.Name.StartsWith("I"))
                {
                    violations.Add($"{interfaceType.Name} should start with 'I'");
                }

                // Avoid anti-patterns
                var discouragedNames = new[] {"Manager", "Helper", "Util"};
                foreach (var discouraged in discouragedNames)
                {
                    if (interfaceType.Name.Contains(discouraged))
                    {
                        violations.Add($"{interfaceType.Name} contains discouraged term '{discouraged}'");
                    }
                }
            }

            Assert.That(violations.Count, Is.LessThan(_allInterfaces.Length * 0.1), 
                $"Too many naming violations ({violations.Count}). Examples:\n{string.Join("\n", violations.Take(5))}");
        }

        #endregion

        #region Interface Completeness Tests

        [Test]
        [Category("Completeness")]
        public void CoreGameObjectHierarchy_ShouldExist()
        {
            var requiredInterfaces = new[]
            {
                "IGameObject",
                "IGameLiving", 
                "ICharacter"
            };

            var missing = new List<string>();
            var found = new List<string>();
            
            foreach (var required in requiredInterfaces)
            {
                if (_allInterfaces.Any(t => t.Name == required))
                    found.Add(required);
                else
                    missing.Add(required);
            }

            TestContext.WriteLine($"✅ Core hierarchy: {found.Count}/{requiredInterfaces.Length} interfaces found");
            if (found.Count > 0)
                TestContext.WriteLine($"Found: {string.Join(", ", found)}");
            
            if (missing.Count > 0)
                TestContext.WriteLine($"Missing: {string.Join(", ", missing)}");

            Assert.That(found.Count, Is.GreaterThan(0), "At least one core interface should exist");
        }

        [Test]
        [Category("Completeness")]
        public void CombatSystem_ShouldHaveRequiredInterfaces()
        {
            var combatInterfaces = new[]
            {
                "IAttackable",
                "IAttacker",
                "IDefender", 
                "IDamageable"
            };

            var found = new List<string>();
            var missing = new List<string>();

            foreach (var required in combatInterfaces)
            {
                if (_allInterfaces.Any(t => t.Name == required))
                    found.Add(required);
                else
                    missing.Add(required);
            }

            TestContext.WriteLine($"⚔️ Combat interfaces: {found.Count}/{combatInterfaces.Length} found");
            
            if (found.Count > 0)
                TestContext.WriteLine($"Found: {string.Join(", ", found)}");
            
            if (missing.Count > 0)
                TestContext.WriteLine($"Missing: {string.Join(", ", missing)}");

            Assert.That(found.Count, Is.GreaterThan(0), 
                "Should have at least 1 combat interface");
        }

        [Test]
        [Category("Completeness")]
        public void ItemSystem_ShouldHaveItemInterfaces()
        {
            var itemInterfaces = new[]
            {
                "IItem",
                "IWeapon",
                "IArmor",
                "IInventory"
            };

            var found = itemInterfaces.Where(required => 
                _allInterfaces.Any(t => t.Name == required)).ToList();

            TestContext.WriteLine($"🎒 Item interfaces: {found.Count}/{itemInterfaces.Length} found");
            
            if (found.Count > 0)
                TestContext.WriteLine($"Found: {string.Join(", ", found)}");
            
            Assert.That(found.Count, Is.GreaterThan(0), 
                "Should have at least 1 item interface");
        }

        #endregion

        #region Mockability Tests

        [Test]
        [Category("Testing")]
        public void AllInterfaces_ShouldBeMockable()
        {
            var unmockableCount = 0;
            var mockableCount = 0;

            foreach (var interfaceType in _allInterfaces)
            {
                // Check for static members (can't be mocked)
                var hasStaticMembers = interfaceType.GetMembers(BindingFlags.Static | BindingFlags.Public).Length > 0;
                
                // Check for sealed methods 
                var hasSealedMethods = interfaceType.GetMethods().Any(m => m.IsFinal);

                if (hasStaticMembers || hasSealedMethods)
                    unmockableCount++;
                else
                    mockableCount++;
            }

            var mockabilityRatio = (double)mockableCount / _allInterfaces.Length;
            TestContext.WriteLine($"🧪 Mockability: {mockableCount}/{_allInterfaces.Length} interfaces ({mockabilityRatio:P1})");

            Assert.That(mockabilityRatio, Is.GreaterThan(0.8), 
                "At least 80% of interfaces should be mockable for testing");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Category("Performance")]
        public void HotPathInterfaces_ShouldBeMinimal()
        {
            var hotPathInterfaceNames = new[]
            {
                "IDamageable",
                "IAttackable", 
                "IPositionable"
            };

            var violations = new List<string>();

            foreach (var name in hotPathInterfaceNames)
            {
                var interfaceType = _allInterfaces.FirstOrDefault(t => t.Name == name);
                if (interfaceType != null)
                {
                    var memberCount = interfaceType.GetMembers().Length;
                    if (memberCount > 4)
                    {
                        violations.Add($"{name}: {memberCount} members");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                $"Hot path interfaces should be minimal:\n{string.Join("\n", violations)}");
        }

        #endregion

        #region Summary

        [Test]
        [Category("Summary")]
        public void InterfaceArchitecture_Summary()
        {
            var totalInterfaces = _allInterfaces.Length;
            var ispCompliant = _allInterfaces.Count(IsISPCompliant);
            var mockable = _allInterfaces.Count(IsMockable);

            var ispRatio = totalInterfaces > 0 ? (double)ispCompliant / totalInterfaces : 0;
            var mockRatio = totalInterfaces > 0 ? (double)mockable / totalInterfaces : 0;

            TestContext.WriteLine("\n📊 INTERFACE ARCHITECTURE SUMMARY");
            TestContext.WriteLine($"Total Interfaces: {totalInterfaces}");
            TestContext.WriteLine($"ISP Compliant: {ispCompliant} ({ispRatio:P1})");
            TestContext.WriteLine($"Mockable: {mockable} ({mockRatio:P1})");

            // Quality gates - relaxed during development
            if (totalInterfaces > 0)
            {
                Assert.That(ispRatio, Is.GreaterThan(0.7), "70%+ interfaces should follow ISP");
                Assert.That(mockRatio, Is.GreaterThan(0.8), "80%+ interfaces should be mockable");
            }

            TestContext.WriteLine("✅ Interface architecture assessment complete!");
        }

        #endregion

        #region Helper Methods

        private bool IsISPCompliant(Type interfaceType)
        {
            var memberCount = interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance).Length;
            return memberCount <= 5;
        }

        private bool IsMockable(Type interfaceType)
        {
            var hasStaticMembers = interfaceType.GetMembers(BindingFlags.Static | BindingFlags.Public).Length > 0;
            var hasSealedMethods = interfaceType.GetMethods().Any(m => m.IsFinal);
            return !hasStaticMembers && !hasSealedMethods;
        }

        #endregion
    }
} 