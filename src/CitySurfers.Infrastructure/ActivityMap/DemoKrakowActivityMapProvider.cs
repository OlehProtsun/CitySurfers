using CitySurfers.Application.ActivityMap;

namespace CitySurfers.Infrastructure.ActivityMap;

public sealed class DemoKrakowActivityMapProvider(TimeProvider time) : IActivityMapProvider
{
    // Coordinates are fixed public-area centers, independent of users and run data.
    private static readonly ZoneCenter[] Centers =
    [
        new("blonia", "Błonia", 50.0605, 19.9076),
        new("bulwary-wislane", "Bulwary Wiślane", 50.0490, 19.9350),
        new("zakrzowek", "Zakrzówek", 50.0315, 19.9130),
        new("park-jordana", "Park Jordana", 50.0630, 19.9130),
        new("las-wolski", "Las Wolski", 50.0540, 19.8460)
    ];

    public Task<ActivityMapResponse> GetAsync(ActivityPeriod period, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (label, multiplier) = period switch
        {
            ActivityPeriod.Live => ("live", 1),
            ActivityPeriod.Today => ("today", 8),
            ActivityPeriod.Month => ("month", 160),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
        var zones = Centers.Select((center, index) => new ActivityZone(center.Id, center.Name,
            center.Latitude, center.Longitude, 6 + index * 3, (12 + index * 5) * multiplier,
            310 + index * 12 + (int)period * 5, index < 2 ? "high" : index < 4 ? "medium" : "low")).ToArray();
        return Task.FromResult(new ActivityMapResponse(label, time.GetUtcNow(), zones));
    }

    private sealed record ZoneCenter(string Id, string Name, double Latitude, double Longitude);
}
