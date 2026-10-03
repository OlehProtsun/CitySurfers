using System.Net;
using System.Net.Http.Json;
using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Rivals;

namespace CitySurfers.IntegrationTests;

public sealed class RivalApiTests
{
    [Fact]
    public async Task RivalContractUsesMonthlyStandingsAndCurrentUser()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/rivals/current?userId=foreign");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<RivalResponse>())!;
        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboards/month"))!;
        Assert.Equal("month", result.Period);
        Assert.Equal(board.PeriodStartUtc, result.PeriodStartUtc);
        Assert.Equal(board.PeriodEndUtc, result.PeriodEndUtc);
        Assert.Equal(board.CurrentUser, result.CurrentUser);
        var rival = Assert.IsType<RivalSnapshot>(result.Rival);
        var row = Assert.Single(board.AroundMe, row => row.Rank == board.CurrentUser.Rank - 1);
        Assert.Equal(row.DisplayName, rival.DisplayName);
        Assert.Equal(row.Rank, rival.Rank);
        Assert.Equal(row.Points, rival.Points);
        Assert.Equal(Math.Max(0, row.Points - board.CurrentUser.Points), rival.PointsGap);
        Assert.Equal(Math.Max(1, row.Points - board.CurrentUser.Points + 1), rival.PointsToPass);
    }
}
