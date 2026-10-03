using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CitySurfers.Application.Running;

namespace CitySurfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<Periods.KrakowPeriodResolver>();
        services.AddSingleton<RunMetricsCalculator>();
        services.AddScoped<RunSessionService>();
        services.AddScoped<RunHistoryService>();
        services.AddScoped<Progress.ProgressService>();
        services.AddScoped<Leaderboards.LeaderboardService>();
        return services;
    }
}
