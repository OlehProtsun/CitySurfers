using CitySurfers.Application.Leaderboards;

namespace CitySurfers.UnitTests;

public sealed class RivalServiceTests
{
    [Fact]
    public async Task SelectsDirectRankAboveAndPreservesMonthlyUserAndBoundaries()
    {
        var result = await new MotivationData().Rivals().GetAsync();
        Assert.Equal("month", result.Period);
        Assert.Equal(DateTimeOffset.Parse("2026-09-30T22:00:00Z"), result.PeriodStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-10-31T23:00:00Z"), result.PeriodEndUtc);
        Assert.Equal(new CurrentUserRank(18, 1438), result.CurrentUser);
        var rival = Assert.IsType<CitySurfers.Application.Rivals.RivalSnapshot>(result.Rival);
        Assert.Equal("Marta", rival.DisplayName);
        Assert.Equal(17, rival.Rank);
        Assert.Equal(1451, rival.Points);
        Assert.Equal(13, rival.PointsGap);
        Assert.Equal(14, rival.PointsToPass);
    }

    [Theory]
    [InlineData(1451, 13, 14)]
    [InlineData(1438, 0, 1)]
    [InlineData(1430, 0, 1)]
    public async Task GapAndPointsToPassHaveDistinctMinimums(long points, long gap, long toPass)
    {
        var data = new MotivationData { AroundMe = [new(17, "Rival", points, false)] };
        var rival = (await data.Rivals().GetAsync()).Rival!;
        Assert.Equal(gap, rival.PointsGap);
        Assert.Equal(toPass, rival.PointsToPass);
    }

    [Fact]
    public async Task FirstRankHasNoRival()
    {
        var data = new MotivationData { User = new(1, 4000) };
        Assert.Null((await data.Rivals().GetAsync()).Rival);
    }

    [Fact]
    public async Task FallsBackToTopWhenNearbyRowIsAbsent()
    {
        var data = new MotivationData { AroundMe = [], Top = [new(17, "Rival", 1451, false)] };
        Assert.Equal(17, (await data.Rivals().GetAsync()).Rival!.Rank);
    }

    [Fact]
    public async Task MissingCompetitorIsValidZeroState()
    {
        var data = new MotivationData { AroundMe = [], Top = [] };
        Assert.Null((await data.Rivals().GetAsync()).Rival);
    }
}
