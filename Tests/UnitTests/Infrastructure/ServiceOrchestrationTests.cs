using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using DOL.GS.Infrastructure;

namespace DOL.Tests.Unit.Infrastructure
{
    [TestFixture]
    public class ServiceOrchestrationTests
    {
        private class TestService : ServiceLifecycleBase
        {
            public bool Started { get; private set; }
            public bool Stopped { get; private set; }
            public DateTime? StartTime { get; private set; }
            public DateTime? StopTime { get; private set; }

            public TestService(string serviceName, ServicePriority priority = ServicePriority.Normal) 
                : base(serviceName, priority)
            {
            }

            protected override Task OnStartAsync()
            {
                Started = true;
                StartTime = DateTime.UtcNow;
                return Task.CompletedTask;
            }

            protected override Task OnStopAsync()
            {
                Stopped = true;
                StopTime = DateTime.UtcNow;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task GameServerHost_ShouldStartAndStopServicesInOrder()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            var criticalService = new TestService("CriticalService", ServicePriority.Critical);
            var normalService = new TestService("NormalService", ServicePriority.Normal);
            var optionalService = new TestService("OptionalService", ServicePriority.Optional);
            
            services.AddSingleton<IServiceLifecycle>(criticalService);
            services.AddSingleton<IServiceLifecycle>(normalService);
            services.AddSingleton<IServiceLifecycle>(optionalService);
            
            var host = new GameServerHostBuilder()
                .ConfigureServices(s => 
                {
                    foreach (var service in services)
                    {
                        s.Add(service);
                    }
                })
                .AddGameServerServices()
                .Build();

            // Act
            await host.StartAsync();
            await Task.Delay(10); // Small delay to ensure ordering
            await host.StopAsync();

            // Assert
            Assert.That(criticalService.Started, Is.True, "Critical service should be started");
            Assert.That(normalService.Started, Is.True, "Normal service should be started");
            Assert.That(optionalService.Started, Is.True, "Optional service should be started");
            
            Assert.That(criticalService.Stopped, Is.True, "Critical service should be stopped");
            Assert.That(normalService.Stopped, Is.True, "Normal service should be stopped");
            Assert.That(optionalService.Stopped, Is.True, "Optional service should be stopped");
            
            // Verify startup order (Critical -> Normal -> Optional)
            Assert.That(criticalService.StartTime, Is.LessThanOrEqualTo(normalService.StartTime), 
                "Critical service should start before Normal service");
            Assert.That(normalService.StartTime, Is.LessThanOrEqualTo(optionalService.StartTime), 
                "Normal service should start before Optional service");
            
            // Verify shutdown order (Optional -> Normal -> Critical)
            Assert.That(optionalService.StopTime, Is.LessThanOrEqualTo(normalService.StopTime), 
                "Optional service should stop before Normal service");
            Assert.That(normalService.StopTime, Is.LessThanOrEqualTo(criticalService.StopTime), 
                "Normal service should stop before Critical service");
        }

        [Test]
        public async Task ServiceLifetimeManager_ShouldProvideAccurateReport()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddServiceLifetimeManagement();
            
            var service1 = new TestService("Service1");
            var service2 = new TestService("Service2");
            
            services.AddSingleton<IServiceLifecycle>(service1);
            services.AddSingleton<IServiceLifecycle>(service2);
            
            var serviceProvider = services.BuildServiceProvider();
            var lifetimeManager = serviceProvider.GetRequiredService<IServiceLifetimeManager>();
            
            // Auto-register services
            var allServices = serviceProvider.GetServices<IServiceLifecycle>();
            foreach (var service in allServices)
            {
                lifetimeManager.RegisterService(service);
            }

            // Act
            var initialReport = lifetimeManager.GetReport();
            await lifetimeManager.StartServicesAsync();
            var afterStartReport = lifetimeManager.GetReport();
            await lifetimeManager.StopServicesAsync();
            var finalReport = lifetimeManager.GetReport();

            // Assert
            Assert.That(initialReport.TotalServices, Is.EqualTo(2), "Should report 2 registered services");
            Assert.That(initialReport.IsStarting, Is.False, "Should not be starting initially");
            Assert.That(initialReport.IsStopping, Is.False, "Should not be stopping initially");
            
            Assert.That(afterStartReport.TotalServices, Is.EqualTo(2), "Should still report 2 services after start");
            Assert.That(finalReport.TotalServices, Is.EqualTo(2), "Should still report 2 services after stop");
        }

        [Test]
        public async Task ServiceLifetimeManager_ShouldHandleServiceFailureGracefully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddServiceLifetimeManagement();
            
            var goodService = new TestService("GoodService");
            var badService = new FailingTestService("BadService");
            
            services.AddSingleton<IServiceLifecycle>(goodService);
            services.AddSingleton<IServiceLifecycle>(badService);
            
            var serviceProvider = services.BuildServiceProvider();
            var lifetimeManager = serviceProvider.GetRequiredService<IServiceLifetimeManager>();
            
            // Auto-register services
            var allServices = serviceProvider.GetServices<IServiceLifecycle>();
            foreach (var service in allServices)
            {
                lifetimeManager.RegisterService(service);
            }

            // Act & Assert
            // Startup should fail due to bad service
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await lifetimeManager.StartServicesAsync());
            
            // Good service should still be startable individually
            await goodService.OnServiceStartAsync();
            Assert.That(goodService.Started, Is.True, "Good service should be able to start");
            
            // Cleanup
            await goodService.OnServiceStopAsync();
        }

        private class FailingTestService : ServiceLifecycleBase
        {
            public FailingTestService(string serviceName) : base(serviceName) { }

            protected override Task OnStartAsync()
            {
                throw new InvalidOperationException("This service always fails to start");
            }

            protected override Task OnStopAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
} 