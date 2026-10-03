using System.Net;
using System.Net.Http.Json;
using CitySurfers.Api.Contracts;
using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;
using Microsoft.Extensions.DependencyInjection;

namespace CitySurfers.IntegrationTests;

public sealed class HistoryApiTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("51")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task InvalidLimitReturnsProblem(string limit)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.GetAsync($"/api/runs/history?limit={limit}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task HistoryIsCompletedOwnedNewestFirstAndLimited()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Empty((await client.GetFromJsonAsync<RunHistoryResponse>("/api/runs/history"))!.Items);
        var store = app.Services.GetRequiredService<IRunSessionStore>();
        var now = DateTime.UtcNow;
        for (var i = 0; i < 12; i++)
        {
            var run = new RunSession($"run-{i}", "persisted-user", 41, now.AddDays(-i - 1));
            run.Finish(now);
            await store.AddAsync(run);
        }
        var foreign = new RunSession("foreign", "other", 41, now);
        foreign.Finish(now);
        await store.AddAsync(foreign);
        await store.AddAsync(new RunSession("active", "persisted-user", 41, now));
        var items = (await client.GetFromJsonAsync<RunHistoryResponse>("/api/runs/history"))!.Items;
        Assert.Equal(10, items.Count);
        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"run-{i}"), items.Select(item => item.Id));
        Assert.Single((await client.GetFromJsonAsync<RunHistoryResponse>("/api/runs/history?limit=1"))!.Items);
        Assert.Equal(12, (await client.GetFromJsonAsync<RunHistoryResponse>("/api/runs/history?limit=50"))!.Items.Count);
    }
}
