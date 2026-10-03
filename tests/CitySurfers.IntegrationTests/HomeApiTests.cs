using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Home;
using CitySurfers.Application.Leaderboards;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CitySurfers.IntegrationTests;

public sealed class HomeApiTests
{
    [Fact]
    public async Task HomeTracksIdleActiveAndFinishedStateWithCompactJsonContract()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var idle = await Home();
        Assert.Equal(new CurrentUserRank(41, 0), idle.Today);
        Assert.Null(idle.ActiveRun);
        Assert.Equal("rival_points", idle.NextGoal!.Type);
        Assert.Equal(9, idle.NextGoal.RemainingPoints);
        var run = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
        var fresh = await Home();
        Assert.Equal(run.Id, fresh.ActiveRun!.Id);
        Assert.Null(fresh.ActiveRun.AveragePaceSecondsPerKm);
        (await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress",
            new { distanceMeters = 600, durationSeconds = 180 })).EnsureSuccessStatusCode();
        var active = await Home();
        Assert.Equal(new ActiveRunSummary(run.Id, run.StartedAtUtc, 600, 180, 300), active.ActiveRun);
        Assert.Equal("run_overtake", active.NextGoal!.Type);
        Assert.Equal(run.Id, active.NextGoal.RunId);
        Assert.Equal(600, active.NextGoal.RemainingDistanceMeters);
        Assert.Equal(41, active.NextGoal.CurrentRank);
        Assert.Equal(40, active.NextGoal.TargetRank);
        using (var json = JsonDocument.Parse(await client.GetStringAsync("/api/home")))
        {
            Assert.Equal(new[] { "today", "activeRun", "nextGoal" },
                json.RootElement.EnumerateObject().Select(p => p.Name));
            Assert.Equal(new[] { "id", "startedAtUtc", "distanceMeters", "durationSeconds", "averagePaceSecondsPerKm" },
                json.RootElement.GetProperty("activeRun").EnumerateObject().Select(p => p.Name));
        }
        (await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish",
            new { distanceMeters = 5500, durationSeconds = 1650 })).EnsureSuccessStatusCode();
        var finished = await Home();
        Assert.Equal(new CurrentUserRank(37, 59), finished.Today);
        Assert.Null(finished.ActiveRun);
        Assert.Equal("rival_points", finished.NextGoal!.Type);
        Assert.Equal(6, finished.NextGoal.RemainingPoints);

        async Task<HomeResponse> Home()
        {
            var response = await client.GetAsync("/api/home?userId=foreign");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<HomeResponse>())!;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoRivalAndNoTargetReturns200WithNullGoal(bool exhaustedRun)
    {
        await using var app = new ApiFactory();
        await using var configured = app.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeaderboardProvider>();
            services.AddSingleton<ILeaderboardProvider, FirstRankProvider>();
        }));
        using var client = configured.CreateClient();
        if (exhaustedRun)
        {
            var run = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
            (await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress",
                new { distanceMeters = 5500, durationSeconds = 1650 })).EnsureSuccessStatusCode();
        }
        var response = await client.GetAsync("/api/home");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var home = (await response.Content.ReadFromJsonAsync<HomeResponse>())!;
        Assert.Null(home.NextGoal);
        Assert.Equal(exhaustedRun, home.ActiveRun is not null);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("nextGoal").ValueKind);
    }

    [Fact]
    public async Task OpenApiDescribesHomeAndPreservesEveryExistingApiPath()
    {
        await using var app = new ApiFactory { EnvironmentName = "Development" };
        using var client = app.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = json.RootElement.GetProperty("paths");
        foreach (var path in new[] { "/api/auth/login", "/api/runs", "/api/runs/active", "/api/runs/{runId}",
            "/api/runs/{runId}/progress", "/api/runs/{runId}/finish", "/api/runs/history", "/api/progress",
            "/api/leaderboards/today", "/api/leaderboards/month", "/api/rivals/current", "/api/goals/next", "/api/map/activity" })
            Assert.True(paths.TryGetProperty(path, out _), path);
        var schema = paths.GetProperty("/api/home").GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("#/components/schemas/HomeResponse", schema.GetProperty("$ref").GetString());
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var property in new[] { "today", "activeRun", "nextGoal" })
            Assert.True(schemas.GetProperty("HomeResponse").GetProperty("properties").TryGetProperty(property, out _));
        foreach (var property in new[] { "id", "startedAtUtc", "distanceMeters", "durationSeconds", "averagePaceSecondsPerKm" })
            Assert.True(schemas.GetProperty("ActiveRunSummary").GetProperty("properties").TryGetProperty(property, out _));
    }

    private sealed class FirstRankProvider : ILeaderboardProvider
    {
        public Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LeaderboardStandings(new(1, currentUserPoints), [], []));
    }
}
