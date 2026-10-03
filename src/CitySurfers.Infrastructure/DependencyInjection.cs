using CitySurfers.Application.Authentication;
using CitySurfers.Application.Users;
using CitySurfers.Infrastructure.Authentication;
using CitySurfers.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CitySurfers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(MongoDbOptions.HasValidConnectionString, "MongoDb:ConnectionString must be a valid MongoDB URI.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName)
                && options.DatabaseName.IndexOfAny(['/', '\\', '.', '"', '$', ' ', '\0']) < 0,
                "MongoDb:DatabaseName must be a valid non-empty database name.")
            .ValidateOnStart();
        services.AddOptions<DemoDataOptions>()
            .Bind(configuration.GetSection(DemoDataOptions.SectionName));
        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });
        services.AddScoped<IUserStore, MongoUserStore>();
        services.AddScoped<IAuthService, DemoAuthService>();
        services.AddScoped<IDataInitializer, DemoDataSeeder>();
        services.AddHealthChecks().AddCheck<MongoDbHealthCheck>("mongodb", tags: ["ready"]);
        return services;
    }

    public static async Task InitializeInfrastructureAsync(
        this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<IDataInitializer>().InitializeAsync(cancellationToken);
        }
        catch (OptionsValidationException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Driver diagnostics may contain deployment credentials.
            throw new InvalidOperationException("MongoDB initialization failed. Check database configuration and connectivity.");
        }
    }
}
