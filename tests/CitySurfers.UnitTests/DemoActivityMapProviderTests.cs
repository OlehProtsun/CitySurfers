using CitySurfers.Application.ActivityMap;
using CitySurfers.Infrastructure.ActivityMap;

namespace CitySurfers.UnitTests;

public sealed class DemoActivityMapProviderTests
{
    [Theory]
    [InlineData(ActivityPeriod.Live, "live", 12)]
    [InlineData(ActivityPeriod.Today, "today", 96)]
    [InlineData(ActivityPeriod.Month, "month", 1920)]
    public async Task ProviderReturnsDeterministicAggregatePublicZones(ActivityPeriod period, string label, int firstRuns)
    {
        var time = new FixedTime();
        var provider = new DemoKrakowActivityMapProvider(time);
        var result = await provider.GetAsync(period);
        Assert.Equal(label, result.Period);
        Assert.Equal(time.GetUtcNow(), result.GeneratedAtUtc);
        Assert.Equal(new[] { "Błonia", "Bulwary Wiślane", "Zakrzówek", "Park Jordana", "Las Wolski" },
            result.Zones.Select(zone => zone.Name));
        Assert.Equal(5, result.Zones.Select(zone => zone.Id).Distinct().Count());
        Assert.Equal(firstRuns, result.Zones[0].Runs);
        Assert.Equal(result.Zones, (await provider.GetAsync(period)).Zones);
        Assert.All(result.Zones, zone =>
        {
            Assert.InRange(zone.Latitude, 50, 50.1);
            Assert.InRange(zone.Longitude, 19.8, 20);
            Assert.True(zone.ActiveRunners > 1);
            Assert.True(zone.AveragePaceSecondsPerKm > 0);
            Assert.Contains(zone.ActivityLevel, new[] { "low", "medium", "high" });
        });
        Assert.Equal(new[] { "Id", "Name", "Latitude", "Longitude", "ActiveRunners", "Runs", "AveragePaceSecondsPerKm", "ActivityLevel" },
            typeof(ActivityZone).GetProperties().Select(property => property.Name));
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    }
}
