using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;
using Microsoft.Extensions.DependencyInjection;

namespace CitySurfers.IntegrationTests;

public sealed class RunApiTests
{
    [Fact]
    public async Task Full_flow_returns_contracts_and_persisted_summary()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/runs/active")).StatusCode);
        var start = await client.PostAsync("/api/runs", null);
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var run = (await start.Content.ReadFromJsonAsync<RunResponse>())!;
        Assert.Equal("active", run.Status);
        Assert.Equal(0, run.DistanceMeters);
        Assert.Null(run.AveragePaceSecondsPerKm);
        Assert.Equal(41, run.Competition.Rank);
        Assert.Equal("Runner_92", run.Competition.CurrentTarget!.Opponent);
        Assert.Equal(1200, run.Competition.CurrentTarget.DistanceToOvertakeMeters);
        Assert.EndsWith($"/api/runs/{run.Id}", start.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/runs", null)).StatusCode);
        Assert.Equal(run.Id, (await client.GetFromJsonAsync<RunResponse>("/api/runs/active"))!.Id);
        Assert.Equal(run.Id, (await client.GetFromJsonAsync<RunResponse>($"/api/runs/{run.Id}"))!.Id);

        var below = await Progress(client, run.Id, 1010, 320);
        Assert.Empty(below.Events);
        Assert.Equal(190, below.Competition.CurrentTarget!.DistanceToOvertakeMeters);
        Assert.Equal(316.831683, below.AveragePaceSecondsPerKm!.Value, 6);
        var crossed = await Progress(client, run.Id, 2800, 840);
        Assert.Equal(new[] { "Runner_92", "Marta" }, crossed.Events.Select(item => item.Opponent));
        Assert.All(crossed.Events, item => Assert.Equal("OVERTAKE", item.Type));
        Assert.Equal(39, crossed.Competition.Rank);
        Assert.Equal(30, crossed.Competition.SeasonPointsEarned);
        Assert.Empty((await Progress(client, run.Id, 2800, 840)).Events);

        var finish = await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish", new { distanceMeters = 6800, durationSeconds = 2210 });
        Assert.Equal(HttpStatusCode.OK, finish.StatusCode);
        var summary = (await finish.Content.ReadFromJsonAsync<PostRunSummary>())!;
        Assert.Equal(run.Id, summary.RunId);
        Assert.Equal(run.StartedAtUtc, summary.StartedAtUtc);
        Assert.NotNull(summary.FinishedAtUtc);
        Assert.Equal(325, summary.AveragePaceSecondsPerKm);
        Assert.Equal(4, summary.OvertakesCount);
        Assert.Equal(41, summary.RankBefore);
        Assert.Equal(37, summary.RankAfter);
        Assert.Equal(59, summary.SeasonPointsEarned);
        Assert.Null(summary.NextTarget);
        var loaded = (await client.GetFromJsonAsync<PostRunSummary>($"/api/runs/{run.Id}"))!;
        Assert.Equal(JsonSerializer.Serialize(summary), JsonSerializer.Serialize(loaded));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/runs/active")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/runs/{run.Id}/finish",
            new { distanceMeters = 6800, durationSeconds = 2210 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/runs/{run.Id}/progress",
            new { distanceMeters = 7000, durationSeconds = 2300 })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/runs", null)).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{\"distanceMeters\":1000}")]
    [InlineData("{\"distanceMeters\":-1,\"durationSeconds\":300}")]
    [InlineData("{\"distanceMeters\":1000,\"durationSeconds\":-1}")]
    [InlineData("{\"distanceMeters\":999,\"durationSeconds\":300}")]
    [InlineData("{\"distanceMeters\":1000,\"durationSeconds\":299}")]
    public async Task Invalid_progress_or_finish_is_400_problem_and_does_not_mutate(string body)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var start = await client.PostAsync("/api/runs", null);
        var id = (await start.Content.ReadFromJsonAsync<RunResponse>())!.Id;
        await Progress(client, id, 1000, 300);
        foreach (var path in new[] { "progress", "finish" })
        {
            using var request = new HttpRequestMessage(path == "progress" ? HttpMethod.Patch : HttpMethod.Post,
                $"/api/runs/{id}/{path}") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }
        var persisted = (await client.GetFromJsonAsync<RunResponse>($"/api/runs/{id}"))!;
        Assert.Equal(1000, persisted.DistanceMeters);
        Assert.Equal(300, persisted.DurationSeconds);
        Assert.Equal("active", persisted.Status);
    }

    [Fact]
    public async Task Missing_and_foreign_runs_return_404()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await app.Services.GetRequiredService<IRunSessionStore>().AddAsync(new RunSession("foreign", "other-user", 41, DateTime.UtcNow));
        foreach (var id in new[] { "missing", "foreign" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/runs/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/runs/{id}/progress",
                new { distanceMeters = 1200, durationSeconds = 300 })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/runs/{id}/finish",
                new { distanceMeters = 1200, durationSeconds = 300 })).StatusCode);
        }
    }

    [Fact]
    public async Task Concurrent_start_allows_only_one_active_run()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsync("/api/runs", null)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Concurrent_progress_and_finish_do_not_duplicate_rewards()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var start = await client.PostAsync("/api/runs", null);
        var id = (await start.Content.ReadFromJsonAsync<RunResponse>())!.Id;
        var updates = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PatchAsJsonAsync(
            $"/api/runs/{id}/progress", new { distanceMeters = 2800, durationSeconds = 840 })));
        var eventCount = 0;
        foreach (var response in updates)
        {
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
            if (response.StatusCode == HttpStatusCode.OK)
                eventCount += (await response.Content.ReadFromJsonAsync<RunResponse>())!.Events.Count;
        }
        Assert.Equal(2, eventCount);
        var finishes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync(
            $"/api/runs/{id}/finish", new { distanceMeters = 6800, durationSeconds = 2210 })));
        Assert.Single(finishes, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, finishes.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        var summary = (await client.GetFromJsonAsync<PostRunSummary>($"/api/runs/{id}"))!;
        Assert.Equal(4, summary.OvertakesCount);
        Assert.Equal(59, summary.SeasonPointsEarned);
    }

    private static async Task<RunResponse> Progress(HttpClient client, string id, double distanceMeters, double durationSeconds)
    {
        var response = await client.PatchAsJsonAsync($"/api/runs/{id}/progress", new { distanceMeters, durationSeconds });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RunResponse>())!;
    }
}
