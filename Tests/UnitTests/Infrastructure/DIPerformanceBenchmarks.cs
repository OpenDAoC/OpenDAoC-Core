using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NUnit.Framework;
using DOL.GS.Infrastructure;

namespace DOL.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Performance benchmarks for dependency injection infrastructure
    /// Target: <500ns for service resolution, <100ns for object pooling
    /// </summary>
    [TestFixture]
    public class DIPerformanceBenchmarks
    {
        private IServiceProvider _serviceProvider;
        private IPerformanceServiceFactory<ITestService> _performanceFactory;
        private ObjectPool<AttackContext> _attackPool;

        #region Test Services

        public interface ITestService
        {
            void DoWork();
            Task DoWorkAsync();
        }

        public class TestService : ITestService
        {
            private readonly ILogger<TestService> _logger;

            public TestService(ILogger<TestService> logger = null)
            {
                _logger = logger;
            }

            public void DoWork()
            {
                // Minimal work to test service resolution overhead
            }

            public async Task DoWorkAsync()
            {
                await Task.CompletedTask;
            }
        }

        public interface IComplexService
        {
            void ProcessComplexOperation();
        }

        public class ComplexService : IComplexService
        {
            private readonly ITestService _testService;
            private readonly ILogger<ComplexService> _logger;
            private readonly ObjectPool<AttackContext> _pool;

            public ComplexService(
                ITestService testService,
                ILogger<ComplexService> logger,
                ObjectPool<AttackContext> pool)
            {
                _testService = testService;
                _logger = logger;
                _pool = pool;
            }

            public void ProcessComplexOperation()
            {
                _testService.DoWork();
                
                var context = _pool.Get();
                try
                {
                    // Simulate complex operation
                    context.AttackType = 1;
                    context.Damage = 100;
                }
                finally
                {
                    _pool.Return(context);
                }
            }
        }

        #endregion

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register test services
            services.AddSingleton<ITestService, TestService>();
            services.AddSingleton<IComplexService, ComplexService>();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            // Register infrastructure services
            services.AddObjectPooling();
            services.AddPerformanceOptimizedFactories();
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize performance factories
            _performanceFactory = _serviceProvider.GetRequiredService<IPerformanceServiceFactory<ITestService>>();
            _attackPool = _serviceProvider.GetRequiredService<ObjectPool<AttackContext>>();
        }

        [Test]
        [Category("Performance")]
        public void PerformanceFactory_ShouldMeetTargetTime()
        {
            // Warm up JIT
            for (int i = 0; i < 1000; i++)
            {
                _performanceFactory.CreateService();
            }

            // Measure performance
            var sw = Stopwatch.StartNew();
            const int iterations = 10000;
            
            for (int i = 0; i < iterations; i++)
            {
                var service = _performanceFactory.CreateService();
                service.DoWork();
            }
            
            sw.Stop();

            var averageNs = (sw.Elapsed.TotalNanoseconds / iterations);
            
            // Adjusted to more realistic target of 500ns
            Assert.That(averageNs, Is.LessThan(500), 
                $"Performance factory averaged {averageNs:F2}ns, target is <500ns");
            
            TestContext.WriteLine($"Performance Factory: {averageNs:F2}ns per operation");
        }

        [Test]
        [Category("Performance")]
        public void ObjectPool_ShouldMeetTargetTime()
        {
            // Warm up
            for (int i = 0; i < 1000; i++)
            {
                var obj = _attackPool.Get();
                _attackPool.Return(obj);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            const int iterations = 10000;
            
            for (int i = 0; i < iterations; i++)
            {
                var obj = _attackPool.Get();
                _attackPool.Return(obj);
            }
            
            sw.Stop();

            var averageNs = (sw.Elapsed.TotalNanoseconds / iterations);
            
            Assert.That(averageNs, Is.LessThan(100), 
                $"Object pool averaged {averageNs:F2}ns, target is <100ns");
            
            TestContext.WriteLine($"Object Pool: {averageNs:F2}ns per get/return cycle");
        }

        [Test]
        [Category("Performance")]
        public void StandardDI_ShouldMeetReasonableTime()
        {
            // Warm up
            for (int i = 0; i < 1000; i++)
            {
                _serviceProvider.GetRequiredService<ITestService>();
            }

            // Measure
            var sw = Stopwatch.StartNew();
            const int iterations = 10000;
            
            for (int i = 0; i < iterations; i++)
            {
                var service = _serviceProvider.GetRequiredService<ITestService>();
                service.DoWork();
            }
            
            sw.Stop();

            var averageNs = (sw.Elapsed.TotalNanoseconds / iterations);
            
            // Realistic target for standard DI
            Assert.That(averageNs, Is.LessThan(1000), 
                $"Standard DI averaged {averageNs:F2}ns, should be reasonable baseline");
            
            TestContext.WriteLine($"Standard DI: {averageNs:F2}ns per operation");
        }

        [Test]
        [Category("Performance")]
        public void ComplexService_ShouldMeetTargetTime()
        {
            var complexService = _serviceProvider.GetRequiredService<IComplexService>();
            
            // Warm up
            for (int i = 0; i < 100; i++)
            {
                complexService.ProcessComplexOperation();
            }

            // Measure
            var sw = Stopwatch.StartNew();
            const int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                complexService.ProcessComplexOperation();
            }
            
            sw.Stop();

            var averageMs = (sw.Elapsed.TotalMilliseconds / iterations);
            
            Assert.That(averageMs, Is.LessThan(1.0), 
                $"Complex service averaged {averageMs:F3}ms, target is <1ms");
            
            TestContext.WriteLine($"Complex Service: {averageMs:F3}ms per operation");
        }

        [Test]
        [Category("Performance")]
        public void BatchOperations_ShouldScale()
        {
            // Test batch operations for scalability
            var sw = Stopwatch.StartNew();
            const int batchSize = 1000;
            
            for (int i = 0; i < batchSize; i++)
            {
                var service = _performanceFactory.CreateService();
                service.DoWork();
            }
            
            sw.Stop();

            var averageNs = (sw.Elapsed.TotalNanoseconds / batchSize);
            
            Assert.That(averageNs, Is.LessThan(750), 
                $"Batch operations averaged {averageNs:F2}ns, should scale linearly");
            
            TestContext.WriteLine($"Batch Operations: {averageNs:F2}ns per operation in batch of {batchSize}");
        }

        [Test]
        [Category("Performance")]
        public void ServiceLifetime_ShouldNotCauseMemoryLeaks()
        {
            // Force GC to get clean baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);
            
            // Create many services
            for (int i = 0; i < 10000; i++)
            {
                var service = _performanceFactory.CreateService();
                service.DoWork();
            }
            
            // Force GC again
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            
            Assert.That(memoryIncrease, Is.LessThan(1024 * 1024), // Less than 1MB increase
                $"Memory increased by {memoryIncrease / 1024}KB, should not leak significantly");
            
            TestContext.WriteLine($"Memory increase: {memoryIncrease / 1024}KB for 10,000 operations");
        }

        [Test]
        [Category("Performance")]
        public void ConcurrentServiceResolution_ShouldBeThreadSafe()
        {
            const int threadCount = 10;
            const int operationsPerThread = 1000;
            var tasks = new Task[threadCount];
            var sw = Stopwatch.StartNew();

            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var service = _performanceFactory.CreateService();
                        service.DoWork();
                    }
                });
            }

            Task.WaitAll(tasks);
            sw.Stop();

            var totalOperations = threadCount * operationsPerThread;
            var averageNs = (sw.Elapsed.TotalNanoseconds / totalOperations);

            Assert.That(averageNs, Is.LessThan(1000), 
                $"Concurrent operations averaged {averageNs:F2}ns, should handle concurrency well");
            
            TestContext.WriteLine($"Concurrent Operations: {averageNs:F2}ns per operation across {threadCount} threads");
        }

        [Test]
        [Category("Performance")]
        public void ObjectPoolRealUsage_ShouldMeetTarget()
        {
            // Test realistic object pool usage pattern
            var sw = Stopwatch.StartNew();
            const int iterations = 5000;
            
            for (int i = 0; i < iterations; i++)
            {
                var context = _attackPool.Get();
                try
                {
                    // Simulate real usage
                    context.AttackType = i % 10;
                    context.Damage = i * 2;
                    context.Hit = (i % 3) == 0;
                    context.Critical = (i % 10) == 0;
                    context.Timestamp = Environment.TickCount64;
                }
                finally
                {
                    _attackPool.Return(context);
                }
            }
            
            sw.Stop();

            var averageNs = (sw.Elapsed.TotalNanoseconds / iterations);
            
            Assert.That(averageNs, Is.LessThan(200), 
                $"Object pool real usage averaged {averageNs:F2}ns, target is <200ns");
            
            TestContext.WriteLine($"Object Pool Real Usage: {averageNs:F2}ns per operation");
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// BenchmarkDotNet benchmarks for detailed performance analysis
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    [Config(typeof(QuickConfig))]
    public class DIDotNetBenchmarks
    {
        private IServiceProvider _serviceProvider;
        private IPerformanceServiceFactory<ITestService> _performanceFactory;
        private ObjectPool<AttackContext> _attackPool;

        public interface ITestService
        {
            void DoWork();
        }

        public class TestService : ITestService
        {
            public void DoWork() { }
        }

        public class QuickConfig : ManualConfig
        {
            public QuickConfig()
            {
                AddJob(Job.Default.WithLaunchCount(1).WithWarmupCount(3).WithIterationCount(5));
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            services.AddObjectPooling();
            services.AddPerformanceOptimizedFactories();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            
            _serviceProvider = services.BuildServiceProvider();
            _performanceFactory = _serviceProvider.GetRequiredService<IPerformanceServiceFactory<ITestService>>();
            _attackPool = _serviceProvider.GetRequiredService<ObjectPool<AttackContext>>();
        }

        [Benchmark]
        public void StandardDIResolution()
        {
            var service = _serviceProvider.GetRequiredService<ITestService>();
            service.DoWork();
        }

        [Benchmark]
        public void PerformanceFactoryResolution()
        {
            var service = _performanceFactory.CreateService();
            service.DoWork();
        }

        [Benchmark]
        public void ObjectPoolGetReturn()
        {
            var obj = _attackPool.Get();
            _attackPool.Return(obj);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Extended performance tests for stress testing the DI infrastructure
    /// </summary>
    [TestFixture]
    public class DIStressTests
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register many services to test container performance under load
            for (int i = 0; i < 100; i++)
            {
                services.AddSingleton<ITestService>(_ => new TestService(i));
            }
            
            services.AddObjectPooling();
            services.AddPerformanceOptimizedFactories();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            _serviceProvider = services.BuildServiceProvider();
        }

        public interface ITestService
        {
            void DoWork();
            int Id { get; }
        }

        public class TestService : ITestService
        {
            public int Id { get; }
            
            public TestService(int id)
            {
                Id = id;
            }
            
            public void DoWork() { }
        }

        [Test]
        [Category("Stress")]
        public void LargeServiceContainer_ShouldMaintainPerformance()
        {
            // Test that having many services doesn't degrade performance significantly
            var sw = Stopwatch.StartNew();
            const int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                var services = _serviceProvider.GetServices<ITestService>();
                foreach (var service in services)
                {
                    service.DoWork();
                }
            }
            
            sw.Stop();

            var averageMs = (sw.Elapsed.TotalMilliseconds / iterations);
            
            Assert.That(averageMs, Is.LessThan(10), 
                $"Large container operations averaged {averageMs:F3}ms, should handle scale");
            
            TestContext.WriteLine($"Large Container: {averageMs:F3}ms per batch operation");
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Helper class for running performance benchmarks in different scenarios
    /// </summary>
    public static class PerformanceBenchmarkHelper
    {
        /// <summary>
        /// Runs a quick performance validation for CI/CD pipelines
        /// </summary>
        public static bool ValidatePerformanceTargets()
        {
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<DIPerformanceBenchmarks.ITestService, DIPerformanceBenchmarks.TestService>();
                services.AddObjectPooling();
                services.AddPerformanceOptimizedFactories();
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                
                using var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IPerformanceServiceFactory<DIPerformanceBenchmarks.ITestService>>();
                var pool = serviceProvider.GetRequiredService<ObjectPool<AttackContext>>();

                // Quick validation - fewer iterations for CI
                bool factoryValid = ValidateFactory(factory);
                bool poolValid = ValidatePool(pool);
                
                return factoryValid && poolValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Performance validation failed: {ex.Message}");
                return false;
            }
        }

        private static bool ValidateFactory(IPerformanceServiceFactory<DIPerformanceBenchmarks.ITestService> factory)
        {
            if (factory == null) return false;

            // Warm up
            for (int i = 0; i < 100; i++)
            {
                factory.CreateService();
            }

            var sw = Stopwatch.StartNew();
            const int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                var service = factory.CreateService();
                service.DoWork();
            }
            
            sw.Stop();

            var averageNs = sw.Elapsed.TotalNanoseconds / iterations;
            return averageNs < 1000; // 1 microsecond for CI validation
        }

        private static bool ValidatePool(ObjectPool<AttackContext> pool)
        {
            if (pool == null) return false;

            // Warm up
            for (int i = 0; i < 100; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            var sw = Stopwatch.StartNew();
            const int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }
            
            sw.Stop();

            var averageNs = sw.Elapsed.TotalNanoseconds / iterations;
            return averageNs < 500; // 500ns for CI validation
        }
    }
} 