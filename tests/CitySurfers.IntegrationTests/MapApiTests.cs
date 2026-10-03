using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CitySurfers.Application.ActivityMap;

namespace CitySurfers.IntegrationTests;

public sealed class MapApiTests
{
    [Theory]
    [InlineData("", "today")]
    [InlineData("?period=live", "live")]
    [InlineData("?period=today", "today")]
    [InlineData("?period=month", "month")]
    public async Task MapReturnsOnlyAggregateZones(string query, string expected)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/map/activity" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ActivityMapResponse>())!;
        Assert.Equal(expected, result.Period);
        Assert.Equal(5, result.Zones.Count);
        Assert.NotEqual(default, result.GeneratedAtUtc);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "period", "generatedAtUtc", "zones" },
            json.RootElement.EnumerateObject().Select(property => property.Name));
        foreach (var zone in json.RootElement.GetProperty("zones").EnumerateArray())
            Assert.Equal(new[] { "id", "name", "latitude", "longitude", "activeRunners", "runs", "averagePaceSecondsPerKm", "activityLevel" },
                zone.EnumerateObject().Select(property => property.Name));
    }

    [Theory]
    [InlineData("year")]
    [InlineData("0")]
    [InlineData("TODAY")]
    [InlineData("")]
    public async Task InvalidPeriodIs400Problem(string period)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.GetAsync($"/api/map/activity?period={period}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
}
