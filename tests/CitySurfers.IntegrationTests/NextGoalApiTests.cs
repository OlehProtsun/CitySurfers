using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Goals;
using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Rivals;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CitySurfers.IntegrationTests;

public sealed class NextGoalApiTests
{
    [Fact]
    public async Task GoalTracksCompetitionThenFallsBackToMonthlyRival()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await AssertMonthlyGoal();
        var run = (await (await client.PostAsync("/api/runs", null)).Content.ReadFromJsonAsync<RunResponse>())!;
        var first = await Goal();
        Assert.Equal("run_overtake", first.Type);
        Assert.Equal("active_run", first.Source);
        Assert.Equal(run.Id, first.RunId);
        Assert.Equal(run.Competition.CurrentTarget!.Opponent, first.TargetDisplayName);
        Assert.Equal(run.Competition.CurrentTarget.DistanceToOvertakeMeters, first.RemainingDistanceMeters);
        Assert.Equal(run.Competition.CurrentTarget.PotentialPoints, first.PotentialPoints);
        Assert.Equal(run.Competition.Rank, first.CurrentRank);
        Assert.Equal(first.CurrentRank - 1, first.TargetRank);
        Assert.Null(first.RemainingPoints);

        var progress = await Progress(850, 255);
        var closer = await Goal();
        Assert.Equal(first.TargetDisplayName, closer.TargetDisplayName);
        Assert.Equal(progress.Competition.CurrentTarget!.DistanceToOvertakeMeters, closer.RemainingDistanceMeters);
        Assert.True(closer.RemainingDistanceMeters < first.RemainingDistanceMeters);

        progress = await Progress(1200, 360);
        var next = await Goal();
        Assert.NotEqual(first.TargetDisplayName, next.TargetDisplayName);
        Assert.Equal(progress.Competition.CurrentTarget!.Opponent, next.TargetDisplayName);
        Assert.Equal(progress.Competition.Rank, next.CurrentRank);
        await Progress(5500, 1650);
        await AssertMonthlyGoal();
        (await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish",
            new { distanceMeters = 6800, durationSeconds = 2210 })).EnsureSuccessStatusCode();
        await AssertMonthlyGoal();

        async Task<NextGoal> Goal()
        {
            var response = await client.GetAsync("/api/goals/next");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return Assert.IsType<NextGoal>((await response.Content.ReadFromJsonAsync<NextGoalResponse>())!.Goal);
        }
        async Task AssertMonthlyGoal()
        {
            var rival = (await client.GetFromJsonAsync<RivalResponse>("/api/rivals/current"))!;
            var goal = await Goal();
            Assert.Equal("rival_points", goal.Type);
            Assert.Equal("monthly_leaderboard", goal.Source);
            Assert.Equal(rival.Rival!.DisplayName, goal.TargetDisplayName);
            Assert.Equal(rival.Rival.PointsToPass, goal.RemainingPoints);
            Assert.Equal(rival.CurrentUser.Rank, goal.CurrentRank);
            Assert.Equal(rival.Rival.Rank, goal.TargetRank);
            Assert.Null(goal.RunId);
            Assert.Null(goal.RemainingDistanceMeters);
            Assert.Null(goal.PotentialPoints);
        }
        async Task<RunResponse> Progress(double distance, double duration)
        {
            var response = await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress",
                new { distanceMeters = distance, durationSeconds = duration });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<RunResponse>())!;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstRankWithoutRunTargetReturnsExplicitNulls(bool exhaustedRun)
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
        foreach (var (path, property) in new[] { ("/api/goals/next", "goal"), ("/api/rivals/current", "rival") })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty(property).ValueKind);
            if (property == "rival") Assert.Equal(1, json.RootElement.GetProperty("currentUser").GetProperty("rank").GetInt32());
        }
    }

    [Fact]
    public async Task OpenApiDescribesBothEndpointsAndResponseModels()
    {
        await using var app = new ApiFactory { EnvironmentName = "Development" };
        using var client = app.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        foreach (var (path, model, property) in new[]
        {
            ("/api/rivals/current", "RivalResponse", "rival"),
            ("/api/goals/next", "NextGoalResponse", "goal")
        })
        {
            var response = json.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get")
                .GetProperty("responses").GetProperty("200");
            var schema = response.GetProperty("content").GetProperty("application/json").GetProperty("schema");
            Assert.Equal($"#/components/schemas/{model}", schema.GetProperty("$ref").GetString());
            Assert.True(json.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(model)
                .GetProperty("properties").TryGetProperty(property, out _));
        }
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var property in new[] { "displayName", "rank", "points", "pointsGap", "pointsToPass" })
            Assert.True(schemas.GetProperty("RivalSnapshot").GetProperty("properties").TryGetProperty(property, out _));
        foreach (var property in new[] { "type", "source", "targetDisplayName", "runId", "remainingDistanceMeters",
            "remainingPoints", "potentialPoints", "currentRank", "targetRank" })
            Assert.True(schemas.GetProperty("NextGoal").GetProperty("properties").TryGetProperty(property, out _));
    }

    private sealed class FirstRankProvider : ILeaderboardProvider
    {
        public Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LeaderboardStandings(new(1, currentUserPoints), [], []));
    }
}
