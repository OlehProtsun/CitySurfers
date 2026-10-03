using System.Net.Http.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Progress;

namespace CitySurfers.IntegrationTests;

public sealed class ProgressApiTests
{
    [Fact]
    public async Task ProgressReflectsOnlyCompletedPersistedRuns()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(0, (await client.GetFromJsonAsync<PersonalProgress>("/api/progress"))!.Lifetime.CompletedRuns);
        var run = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
        await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress", new { distanceMeters = 2800, durationSeconds = 840 });
        Assert.Equal(0, (await client.GetFromJsonAsync<PersonalProgress>("/api/progress"))!.Lifetime.TotalPointsEarned);
        var finished = await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish", new { distanceMeters = 6800, durationSeconds = 2210 });
        finished.EnsureSuccessStatusCode();
        var result = (await client.GetFromJsonAsync<PersonalProgress>("/api/progress"))!;
        Assert.Equal(new LifetimeProgress(1, 6800, 2210, 325, 4, 59, 6800, 325), result.Lifetime);
        Assert.Equal(59, result.CurrentMonth.PointsEarned);
        Assert.Equal(59, result.CurrentWeek.PointsEarned);
        Assert.Null(result.Comparison.MonthlyAveragePaceDeltaSecondsPerKm);
    }
}
