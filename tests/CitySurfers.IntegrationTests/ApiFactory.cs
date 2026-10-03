using CitySurfers.Application.Users;
using CitySurfers.Application.Running;
using CitySurfers.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CitySurfers.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public bool DatabaseAvailable { get; init; } = true;
    public bool StoreThrows { get; init; }
    public string EnvironmentName { get; init; } = "Production";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "citysurfers-test",
                ["Cors:AllowedOrigins:0"] = "https://frontend.example"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserStore>();
            services.AddSingleton<IUserStore>(new TestUserStore(StoreThrows));
            services.RemoveAll<IRunSessionStore>();
            services.AddSingleton<IRunSessionStore, TestRunStore>();
            services.RemoveAll<IDataInitializer>();
            services.AddSingleton<IDataInitializer, NoOpInitializer>();
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration("mongodb",
                    new StubHealthCheck(DatabaseAvailable), null, ["ready"]));
            });
        });
    }

    private sealed class NoOpInitializer : IDataInitializer
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubHealthCheck(bool available) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(available ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
    }

    private sealed class TestUserStore(bool throws) : IUserStore
    {
        public Task<UserData?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            if (throws)
                throw new InvalidOperationException("private-database-secret");
            return Task.FromResult(username == "demo"
                ? new UserData("persisted-user", "demo", "Persisted Test Runner", "1234")
                : null);
        }
    }
}
