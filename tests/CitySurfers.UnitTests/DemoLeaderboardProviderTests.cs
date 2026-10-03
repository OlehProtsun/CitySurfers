using CitySurfers.Application.Leaderboards;
using CitySurfers.Infrastructure.Leaderboards;

namespace CitySurfers.UnitTests;

public sealed class DemoLeaderboardProviderTests
{
    private readonly DemoLeaderboardProvider provider = new();

    [Theory]
    [InlineData(0, 41)]
    [InlineData(16, 40)]
    [InlineData(30, 39)]
    [InlineData(41, 38)]
    [InlineData(59, 37)]
    [InlineData(10000, 1)]
    public async Task UserScoreDeterminesRankAndIsInsertedExactlyOnce(long points, int rank)
    {
        var result = await provider.GetAsync(LeaderboardPeriod.Today, points);
        Assert.Equal(new CurrentUserRank(rank, points), result.CurrentUser);
        var user = Assert.Single(result.AroundMe, row => row.IsCurrentUser);
        Assert.Equal(rank, user.Rank);
        Assert.Equal(points, user.Points);
        Assert.Equal(10, result.Top.Count);
        Assert.True(result.AroundMe.Count <= 5);
        Assert.Equal(result.Top.OrderByDescending(row => row.Points), result.Top);
        Assert.Equal(result.AroundMe.OrderByDescending(row => row.Points), result.AroundMe);
        Assert.Equal(Enumerable.Range(1, 10), result.Top.Select(row => row.Rank));
    }

    [Fact]
    public async Task TiesAreDeterministicAndPopulationsDifferByPeriod()
    {
        var first = await provider.GetAsync(LeaderboardPeriod.Today, 64);
        var second = await provider.GetAsync(LeaderboardPeriod.Today, 64);
        Assert.Equal(first.Top, second.Top);
        Assert.Equal(first.AroundMe, second.AroundMe);
        Assert.Equal(37, first.CurrentUser.Rank);
        Assert.Equal(64, first.AroundMe.Single(row => row.Rank == 36).Points);
        var month = await provider.GetAsync(LeaderboardPeriod.Month, 64);
        Assert.NotEqual(first.CurrentUser.Rank, month.CurrentUser.Rank);
        Assert.NotEqual(first.Top[0].Points, month.Top[0].Points);
    }
}
