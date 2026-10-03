using System.Net.Http.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Leaderboards;

namespace CitySurfers.IntegrationTests;

public sealed class LeaderboardApiTests
{
    [Fact]
    public async Task BothPeriodsReflectActiveAndCompletedScoresExactlyOnce()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await AssertScores(0);
        var run = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
        (await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress", new { distanceMeters = 2800, durationSeconds = 840 }))
            .EnsureSuccessStatusCode();
        await AssertScores(30);
        (await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish", new { distanceMeters = 6800, durationSeconds = 2210 }))
            .EnsureSuccessStatusCode();
        await AssertScores(59);
        var second = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
        (await client.PatchAsJsonAsync($"/api/runs/{second.Id}/progress", new { distanceMeters = 1200, durationSeconds = 360 }))
            .EnsureSuccessStatusCode();
        await AssertScores(75);

        async Task AssertScores(long points)
        {
            foreach (var period in new[] { "today", "month" })
            {
                var result = (await client.GetFromJsonAsync<LeaderboardResponse>($"/api/leaderboards/{period}?userId=foreign"))!;
                Assert.Equal(period, result.Period);
                Assert.Equal(points, result.CurrentUser.Points);
                Assert.True(result.PeriodEndUtc > result.PeriodStartUtc);
                Assert.Equal(10, result.Top.Count);
                Assert.Equal(points, Assert.Single(result.AroundMe, row => row.IsCurrentUser).Points);
            }
        }
    }
}
