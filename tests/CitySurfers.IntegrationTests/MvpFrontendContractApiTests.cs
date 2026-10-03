using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitySurfers.IntegrationTests;

public sealed class MvpFrontendContractApiTests
{
    [Fact]
    public async Task FrontendSequencePreservesJsonContractsAndDedicatedScreens()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();

        var login = await Json(await client.PostAsJsonAsync("/api/auth/login", new { username = "demo", password = "1234" }));
        Fields(login, "id", "username", "displayName");
        Assert.Equal("demo", login.GetProperty("username").GetString());
        var home = await Get("/api/home");
        Fields(home, "today", "activeRun", "nextGoal");
        Assert.Equal(JsonValueKind.Null, home.GetProperty("activeRun").ValueKind);
        Assert.Equal(41, home.GetProperty("today").GetProperty("rank").GetInt32());

        var run = await Json(await client.PostAsync("/api/runs", null), HttpStatusCode.Created);
        Fields(run, "id", "status", "startedAtUtc", "updatedAtUtc", "distanceMeters", "durationSeconds",
            "averagePaceSecondsPerKm", "competition", "events");
        var id = run.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal("active", run.GetProperty("status").GetString());
        Assert.Equal(id, (await Get("/api/runs/active")).GetProperty("id").GetString());
        Assert.Equal(id, (await Get($"/api/runs/{id}")).GetProperty("id").GetString());
        foreach (var step in new[] { (1200, 360, 16, 40), (2500, 750, 30, 39), (4000, 1200, 41, 38), (5500, 1650, 59, 37) })
        {
            var progress = await Json(await client.PatchAsJsonAsync($"/api/runs/{id}/progress",
                new { distanceMeters = step.Item1, durationSeconds = step.Item2 }));
            Assert.Equal(id, progress.GetProperty("id").GetString());
            Assert.Equal(step.Item1, progress.GetProperty("distanceMeters").GetDouble());
            Assert.Equal(step.Item2, progress.GetProperty("durationSeconds").GetDouble());
            var competition = progress.GetProperty("competition");
            Fields(competition, "rank", "seasonPointsEarned", "currentTarget");
            Assert.Equal(step.Item3, competition.GetProperty("seasonPointsEarned").GetInt32());
            Assert.Equal(step.Item4, competition.GetProperty("rank").GetInt32());
            var events = progress.GetProperty("events");
            Assert.Equal(1, events.GetArrayLength());
            Fields(events[0], "type", "opponent", "rankBefore", "rankAfter", "pointsAwarded");
            Assert.Equal("OVERTAKE", events[0].GetProperty("type").GetString());
            home = await Get("/api/home");
            Assert.Equal(id, home.GetProperty("activeRun").GetProperty("id").GetString());
            var goal = home.GetProperty("nextGoal");
            Fields(goal, "type", "source", "targetDisplayName", "runId", "remainingDistanceMeters",
                "remainingPoints", "potentialPoints", "currentRank", "targetRank");
            if (step.Item4 > 37)
                Assert.Equal("run_overtake", goal.GetProperty("type").GetString());
        }

        var summary = await Json(await client.PostAsJsonAsync($"/api/runs/{id}/finish",
            new { distanceMeters = 6800, durationSeconds = 2210 }));
        Fields(summary, "runId", "startedAtUtc", "finishedAtUtc", "distanceMeters", "durationSeconds",
            "averagePaceSecondsPerKm", "overtakesCount", "overtakes", "rankBefore", "rankAfter", "seasonPointsEarned", "nextTarget");
        Assert.Equal(id, summary.GetProperty("runId").GetString());
        Assert.Equal(6800, summary.GetProperty("distanceMeters").GetDouble());
        Assert.Equal(2210, summary.GetProperty("durationSeconds").GetDouble());
        Assert.Equal(59, summary.GetProperty("seasonPointsEarned").GetInt32());
        Assert.Equal(37, summary.GetProperty("rankAfter").GetInt32());
        Assert.Equal(4, summary.GetProperty("overtakesCount").GetInt32());
        Assert.Equal(id, (await Get($"/api/runs/{id}")).GetProperty("runId").GetString());
        home = await Get("/api/home");
        Assert.Equal(JsonValueKind.Null, home.GetProperty("activeRun").ValueKind);
        Assert.Equal("rival_points", home.GetProperty("nextGoal").GetProperty("type").GetString());
        Assert.Equal(6, home.GetProperty("nextGoal").GetProperty("remainingPoints").GetInt64());

        foreach (var period in new[] { "today", "month" })
        {
            var board = await Get($"/api/leaderboards/{period}");
            Fields(board, "period", "periodStartUtc", "periodEndUtc", "currentUser", "top", "aroundMe");
            Assert.Equal(period, board.GetProperty("period").GetString());
            Assert.Equal(59, board.GetProperty("currentUser").GetProperty("points").GetInt64());
            Fields(board.GetProperty("top")[0], "rank", "displayName", "points", "isCurrentUser");
        }
        var personal = await Get("/api/progress");
        Fields(personal, "lifetime", "currentWeek", "currentMonth", "previousMonth", "comparison");
        Assert.Equal(1, personal.GetProperty("lifetime").GetProperty("completedRuns").GetInt32());
        var history = await Get("/api/runs/history");
        Assert.Equal(1, history.GetProperty("items").GetArrayLength());
        Fields(history.GetProperty("items")[0], "id", "startedAtUtc", "finishedAtUtc", "distanceMeters",
            "durationSeconds", "averagePaceSecondsPerKm", "overtakesCount", "pointsEarned");
        Assert.Equal(id, history.GetProperty("items")[0].GetProperty("id").GetString());
        var map = await Get("/api/map/activity?period=today");
        Fields(map, "period", "generatedAtUtc", "zones");
        Fields(map.GetProperty("zones")[0], "id", "name", "latitude", "longitude", "activeRunners", "runs",
            "averagePaceSecondsPerKm", "activityLevel");
        var rival = await Get("/api/rivals/current");
        Fields(rival, "period", "periodStartUtc", "periodEndUtc", "currentUser", "rival");
        Fields(rival.GetProperty("rival"), "displayName", "rank", "points", "pointsGap", "pointsToPass");
        Assert.Equal(6, rival.GetProperty("rival").GetProperty("pointsToPass").GetInt64());
        var next = await Get("/api/goals/next");
        Assert.Equal(6, next.GetProperty("goal").GetProperty("remainingPoints").GetInt64());

        async Task<JsonElement> Get(string path) => await Json(await client.GetAsync(path));
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        using (response)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.Clone();
        }
    }

    private static void Fields(JsonElement json, params string[] fields)
    {
        foreach (var field in fields)
            Assert.True(json.TryGetProperty(field, out _), $"Missing JSON field: {field}");
    }
}
