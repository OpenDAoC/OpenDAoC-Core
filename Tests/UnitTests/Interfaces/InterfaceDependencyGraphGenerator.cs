using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace DOL.Tests.Unit.Interfaces
{
    /// <summary>
    /// Generates interface dependency graphs for visualization and analysis
    /// FIEX-015: Create interface dependency graphs
    /// </summary>
    [TestFixture]
    public class InterfaceDependencyGraphGenerator
    {
        private Assembly _gameServerAssembly;
        private Type[] _allInterfaces;
        private Dictionary<Type, List<Type>> _dependencyGraph;

        [SetUp]
        public void Setup()
        {
            _gameServerAssembly = typeof(DOL.GS.GameServer).Assembly;
            _allInterfaces = _gameServerAssembly.GetTypes()
                .Where(t => t.IsInterface && 
                           (t.Namespace?.Contains("Interfaces") == true || t.Name.StartsWith("I")))
                .ToArray();
            
            _dependencyGraph = BuildDependencyGraph();
        }

        #region Graph Generation Tests

        [Test]
        [Category("Documentation")]
        public void GenerateMermaidDependencyGraph()
        {
            var mermaidGraph = GenerateMermaidGraph();
            
            var outputPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "interface_dependencies.mermaid");
            File.WriteAllText(outputPath, mermaidGraph);
            
            TestContext.WriteLine($"📊 Mermaid graph generated: {outputPath}");
            TestContext.WriteLine($"Interfaces analyzed: {_allInterfaces.Length}");
            TestContext.WriteLine($"Dependencies found: {_dependencyGraph.Values.Sum(list => list.Count)}");

            // Validate graph structure
            Assert.That(mermaidGraph, Is.Not.Empty);
            Assert.That(mermaidGraph, Contains.Substring("graph TD"));
        }

        [Test]
        [Category("Documentation")]
        public void GenerateDotGraph()
        {
            var dotGraph = GenerateDotGraph();
            
            var outputPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "interface_dependencies.dot");
            File.WriteAllText(outputPath, dotGraph);
            
            TestContext.WriteLine($"🔗 DOT graph generated: {outputPath}");
            TestContext.WriteLine("Use Graphviz to render: dot -Tpng interface_dependencies.dot -o dependencies.png");

            Assert.That(dotGraph, Is.Not.Empty);
            Assert.That(dotGraph, Contains.Substring("digraph"));
        }

        [Test]
        [Category("Documentation")]
        public void GenerateMarkdownReport()
        {
            var markdownReport = GenerateMarkdownReport();
            
            var outputPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "interface_analysis.md");
            File.WriteAllText(outputPath, markdownReport);
            
            TestContext.WriteLine($"📋 Markdown report generated: {outputPath}");

            Assert.That(markdownReport, Is.Not.Empty);
            Assert.That(markdownReport, Contains.Substring("# Interface Dependency Analysis"));
        }

        #endregion

        #region Analysis Tests

        [Test]
        [Category("Analysis")]
        public void AnalyzeDependencyComplexity()
        {
            var complexityAnalysis = new Dictionary<string, object>();
            
            // Calculate metrics
            var maxDependencies = _dependencyGraph.Values.Max(deps => deps.Count);
            var avgDependencies = _dependencyGraph.Values.Average(deps => deps.Count);
            var totalInterfaces = _allInterfaces.Length;
            var totalDependencies = _dependencyGraph.Values.Sum(deps => deps.Count);
            
            // Find interfaces with most dependencies
            var topDependentInterfaces = _dependencyGraph
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(5)
                .ToList();

            // Find most depended-upon interfaces
            var dependencyCounts = new Dictionary<Type, int>();
            foreach (var deps in _dependencyGraph.Values)
            {
                foreach (var dep in deps)
                {
                    dependencyCounts[dep] = dependencyCounts.GetValueOrDefault(dep, 0) + 1;
                }
            }
            
            var topDependencies = dependencyCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .ToList();

            TestContext.WriteLine("\n📊 DEPENDENCY COMPLEXITY ANALYSIS");
            TestContext.WriteLine($"Total Interfaces: {totalInterfaces}");
            TestContext.WriteLine($"Total Dependencies: {totalDependencies}");
            TestContext.WriteLine($"Average Dependencies per Interface: {avgDependencies:F2}");
            TestContext.WriteLine($"Maximum Dependencies: {maxDependencies}");
            
            TestContext.WriteLine("\n🔗 Interfaces with Most Dependencies:");
            foreach (var item in topDependentInterfaces)
            {
                TestContext.WriteLine($"  {item.Key.Name}: {item.Value.Count} dependencies");
            }
            
            TestContext.WriteLine("\n⭐ Most Depended-Upon Interfaces:");
            foreach (var item in topDependencies)
            {
                TestContext.WriteLine($"  {item.Key.Name}: used by {item.Value} interfaces");
            }

            // Quality assertions
            Assert.That(avgDependencies, Is.LessThan(10), 
                "Average dependencies per interface should be reasonable");
            Assert.That(maxDependencies, Is.LessThan(20), 
                "No interface should have excessive dependencies");
        }

        [Test]
        [Category("Analysis")]
        public void DetectCircularDependencies()
        {
            var circularDependencies = FindCircularDependencies();
            
            TestContext.WriteLine($"\n🔄 Circular Dependency Analysis:");
            TestContext.WriteLine($"Circular dependencies found: {circularDependencies.Count}");
            
            foreach (var cycle in circularDependencies)
            {
                TestContext.WriteLine($"  Cycle: {string.Join(" → ", cycle.Select(t => t.Name))}");
            }

            Assert.That(circularDependencies, Is.Empty, 
                "No circular dependencies should exist in interface hierarchy");
        }

        [Test]
        [Category("Analysis")]
        public void AnalyzeLayerSeparation()
        {
            var layerAnalysis = AnalyzeLayers();
            
            TestContext.WriteLine("\n🏗️ LAYER SEPARATION ANALYSIS");
            
            foreach (var layer in layerAnalysis)
            {
                TestContext.WriteLine($"\n{layer.Key} Layer:");
                TestContext.WriteLine($"  Interfaces: {layer.Value.Count}");
                
                if (layer.Value.Count > 0)
                {
                    TestContext.WriteLine($"  Examples: {string.Join(", ", layer.Value.Take(3).Select(t => t.Name))}");
                }
            }

            // Check for layer violations
            var violations = DetectLayerViolations(layerAnalysis);
            if (violations.Count > 0)
            {
                TestContext.WriteLine("\n⚠️ Layer Violations:");
                foreach (var violation in violations)
                {
                    TestContext.WriteLine($"  {violation}");
                }
            }

            Assert.That(violations.Count, Is.LessThan(5), 
                "Should have minimal layer violations during refactoring");
        }

        #endregion

        #region Graph Generation Methods

        private string GenerateMermaidGraph()
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");
            sb.AppendLine("  %% Interface Dependency Graph");
            sb.AppendLine("  %% Generated by OpenDAoC Interface Analysis");
            sb.AppendLine();
            
            // Add style classes
            sb.AppendLine("  classDef coreInterface fill:#e1f5fe,stroke:#0277bd,stroke-width:2px");
            sb.AppendLine("  classDef combatInterface fill:#e8f5e8,stroke:#388e3c,stroke-width:2px");
            sb.AppendLine("  classDef itemInterface fill:#fff3e0,stroke:#f57c00,stroke-width:2px");
            sb.AppendLine("  classDef characterInterface fill:#fce4ec,stroke:#c2185b,stroke-width:2px");
            sb.AppendLine();

            // Generate nodes and edges
            var processedEdges = new HashSet<string>();
            
            foreach (var interfaceType in _allInterfaces)
            {
                var nodeName = GetMermaidNodeName(interfaceType);
                var styleClass = GetInterfaceCategory(interfaceType);
                
                if (_dependencyGraph.ContainsKey(interfaceType))
                {
                    foreach (var dependency in _dependencyGraph[interfaceType])
                    {
                        if (_allInterfaces.Contains(dependency))
                        {
                            var depNodeName = GetMermaidNodeName(dependency);
                            var edgeKey = $"{nodeName} --> {depNodeName}";
                            
                            if (!processedEdges.Contains(edgeKey))
                            {
                                sb.AppendLine($"  {nodeName} --> {depNodeName}");
                                processedEdges.Add(edgeKey);
                            }
                        }
                    }
                }
                
                // Apply style
                sb.AppendLine($"  class {nodeName} {styleClass}");
            }

            return sb.ToString();
        }

        private string GenerateDotGraph()
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph InterfaceDependencies {");
            sb.AppendLine("  rankdir=TB;");
            sb.AppendLine("  node [shape=box,style=filled];");
            sb.AppendLine();
            
            // Define node styles
            sb.AppendLine("  // Core interfaces");
            foreach (var iface in _allInterfaces.Where(t => IsCore(t)))
            {
                sb.AppendLine($"  \"{iface.Name}\" [fillcolor=lightblue];");
            }
            
            sb.AppendLine("  // Combat interfaces");
            foreach (var iface in _allInterfaces.Where(t => IsCombat(t)))
            {
                sb.AppendLine($"  \"{iface.Name}\" [fillcolor=lightgreen];");
            }
            
            sb.AppendLine("  // Item interfaces");
            foreach (var iface in _allInterfaces.Where(t => IsItem(t)))
            {
                sb.AppendLine($"  \"{iface.Name}\" [fillcolor=orange];");
            }
            
            sb.AppendLine();
            
            // Add edges
            foreach (var kvp in _dependencyGraph)
            {
                foreach (var dependency in kvp.Value)
                {
                    if (_allInterfaces.Contains(dependency))
                    {
                        sb.AppendLine($"  \"{kvp.Key.Name}\" -> \"{dependency.Name}\";");
                    }
                }
            }
            
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateMarkdownReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Interface Dependency Analysis");
            sb.AppendLine();
            sb.AppendLine($"**Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Total Interfaces**: {_allInterfaces.Length}");
            sb.AppendLine();
            
            // Summary statistics
            var totalDeps = _dependencyGraph.Values.Sum(deps => deps.Count);
            var avgDeps = _dependencyGraph.Values.Average(deps => deps.Count);
            
            sb.AppendLine("## Summary Statistics");
            sb.AppendLine();
            sb.AppendLine($"- **Total Dependencies**: {totalDeps}");
            sb.AppendLine($"- **Average Dependencies per Interface**: {avgDeps:F2}");
            sb.AppendLine($"- **ISP Compliance**: {GetISPCompliancePercentage():F1}%");
            sb.AppendLine();
            
            // Interface categories
            sb.AppendLine("## Interface Categories");
            sb.AppendLine();
            
            var categories = new[]
            {
                ("Core", _allInterfaces.Where(IsCore)),
                ("Combat", _allInterfaces.Where(IsCombat)),
                ("Character", _allInterfaces.Where(IsCharacter)),
                ("Item", _allInterfaces.Where(IsItem))
            };
            
            foreach (var (name, interfaces) in categories)
            {
                var interfaceList = interfaces.ToArray();
                sb.AppendLine($"### {name} Interfaces ({interfaceList.Length})");
                sb.AppendLine();
                
                foreach (var iface in interfaceList.OrderBy(t => t.Name))
                {
                    var depCount = _dependencyGraph.GetValueOrDefault(iface, new List<Type>()).Count;
                    sb.AppendLine($"- **{iface.Name}** ({depCount} dependencies)");
                }
                sb.AppendLine();
            }
            
            // Complexity analysis
            sb.AppendLine("## Complexity Analysis");
            sb.AppendLine();
            
            var highComplexity = _dependencyGraph
                .Where(kvp => kvp.Value.Count > 3)
                .OrderByDescending(kvp => kvp.Value.Count)
                .ToArray();
                
            if (highComplexity.Length > 0)
            {
                sb.AppendLine("### High Complexity Interfaces");
                sb.AppendLine();
                
                foreach (var kvp in highComplexity)
                {
                    sb.AppendLine($"- **{kvp.Key.Name}**: {kvp.Value.Count} dependencies");
                    foreach (var dep in kvp.Value.Take(5))
                    {
                        sb.AppendLine($"  - {dep.Name}");
                    }
                    if (kvp.Value.Count > 5)
                    {
                        sb.AppendLine($"  - ... and {kvp.Value.Count - 5} more");
                    }
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }

        #endregion

        #region Analysis Methods

        private Dictionary<Type, List<Type>> BuildDependencyGraph()
        {
            var graph = new Dictionary<Type, List<Type>>();
            
            foreach (var interfaceType in _allInterfaces)
            {
                graph[interfaceType] = GetDirectDependencies(interfaceType);
            }
            
            return graph;
        }

        private List<Type> GetDirectDependencies(Type interfaceType)
        {
            var dependencies = new HashSet<Type>();
            
            // Inherited interfaces
            foreach (var inherited in interfaceType.GetInterfaces())
            {
                dependencies.Add(inherited);
            }
            
            // Method parameter and return types
            foreach (var method in interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                AddTypeDependencies(method.ReturnType, dependencies);
                
                foreach (var param in method.GetParameters())
                {
                    AddTypeDependencies(param.ParameterType, dependencies);
                }
            }
            
            // Property types
            foreach (var property in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                AddTypeDependencies(property.PropertyType, dependencies);
            }
            
            return dependencies.Where(t => t != interfaceType).ToList();
        }

        private void AddTypeDependencies(Type type, HashSet<Type> dependencies)
        {
            if (type.IsInterface && _allInterfaces.Contains(type))
            {
                dependencies.Add(type);
            }
            
            // Handle generic types
            if (type.IsGenericType)
            {
                foreach (var genericArg in type.GetGenericArguments())
                {
                    AddTypeDependencies(genericArg, dependencies);
                }
            }
        }

        private List<List<Type>> FindCircularDependencies()
        {
            var cycles = new List<List<Type>>();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();
            
            foreach (var interfaceType in _allInterfaces)
            {
                if (!visited.Contains(interfaceType))
                {
                    var path = new List<Type>();
                    FindCyclesUtil(interfaceType, visited, recursionStack, path, cycles);
                }
            }
            
            return cycles;
        }

        private bool FindCyclesUtil(Type current, HashSet<Type> visited, HashSet<Type> recursionStack, 
            List<Type> path, List<List<Type>> cycles)
        {
            visited.Add(current);
            recursionStack.Add(current);
            path.Add(current);
            
            if (_dependencyGraph.ContainsKey(current))
            {
                foreach (var dependency in _dependencyGraph[current])
                {
                    if (!visited.Contains(dependency))
                    {
                        if (FindCyclesUtil(dependency, visited, recursionStack, path, cycles))
                            return true;
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        // Found cycle
                        var cycleStart = path.IndexOf(dependency);
                        cycles.Add(path.Skip(cycleStart).ToList());
                        return true;
                    }
                }
            }
            
            recursionStack.Remove(current);
            path.Remove(current);
            return false;
        }

        private Dictionary<string, List<Type>> AnalyzeLayers()
        {
            return new Dictionary<string, List<Type>>
            {
                {"Core", _allInterfaces.Where(IsCore).ToList()},
                {"Domain", _allInterfaces.Where(IsDomain).ToList()},
                {"Application", _allInterfaces.Where(IsApplication).ToList()},
                {"Infrastructure", _allInterfaces.Where(IsInfrastructure).ToList()}
            };
        }

        private List<string> DetectLayerViolations(Dictionary<string, List<Type>> layers)
        {
            var violations = new List<string>();
            
            // Domain should not depend on Application or Infrastructure
            foreach (var domainInterface in layers["Domain"])
            {
                var deps = _dependencyGraph.GetValueOrDefault(domainInterface, new List<Type>());
                var appDeps = deps.Intersect(layers["Application"]).ToList();
                var infraDeps = deps.Intersect(layers["Infrastructure"]).ToList();
                
                if (appDeps.Count > 0)
                    violations.Add($"{domainInterface.Name} (Domain) depends on Application: {string.Join(", ", appDeps.Select(t => t.Name))}");
                    
                if (infraDeps.Count > 0)
                    violations.Add($"{domainInterface.Name} (Domain) depends on Infrastructure: {string.Join(", ", infraDeps.Select(t => t.Name))}");
            }
            
            return violations;
        }

        #endregion

        #region Helper Methods

        private string GetMermaidNodeName(Type type)
        {
            return type.Name.Replace("I", "").Replace("<", "").Replace(">", "");
        }

        private string GetInterfaceCategory(Type type)
        {
            if (IsCore(type)) return "coreInterface";
            if (IsCombat(type)) return "combatInterface";
            if (IsItem(type)) return "itemInterface";
            if (IsCharacter(type)) return "characterInterface";
            return "coreInterface";
        }

        private bool IsCore(Type type) => 
            type.Name.Contains("GameObject") || type.Name.Contains("Identifiable") || type.Name.Contains("Positionable");

        private bool IsCombat(Type type) => 
            type.Name.Contains("Attack") || type.Name.Contains("Defend") || type.Name.Contains("Damage") || type.Name.Contains("Combat");

        private bool IsCharacter(Type type) => 
            type.Name.Contains("Character") || type.Name.Contains("Player") || type.Name.Contains("Living");

        private bool IsItem(Type type) => 
            type.Name.Contains("Item") || type.Name.Contains("Inventory") || type.Name.Contains("Weapon") || type.Name.Contains("Armor");

        private bool IsDomain(Type type) => 
            type.Namespace?.Contains("Combat") == true || type.Namespace?.Contains("Character") == true;

        private bool IsApplication(Type type) => 
            type.Name.Contains("UseCase") || type.Name.Contains("ApplicationService");

        private bool IsInfrastructure(Type type) => 
            type.Name.Contains("Repository") || type.Name.Contains("Adapter");

        private double GetISPCompliancePercentage()
        {
            var compliantCount = _allInterfaces.Count(t => 
                t.GetMembers(BindingFlags.Public | BindingFlags.Instance).Length <= 5);
            return (double)compliantCount / _allInterfaces.Length * 100;
        }

        #endregion
    }
} 