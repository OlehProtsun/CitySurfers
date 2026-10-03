using CitySurfers.Application.Periods;

namespace CitySurfers.UnitTests;

public sealed class KrakowPeriodResolverTests
{
    private readonly KrakowPeriodResolver resolver = new();

    [Theory]
    [InlineData("2026-01-15T23:30:00Z", "2026-01-15T23:00:00Z", "2026-01-16T23:00:00Z")]
    [InlineData("2026-07-15T22:30:00Z", "2026-07-15T22:00:00Z", "2026-07-16T22:00:00Z")]
    [InlineData("2026-03-29T12:00:00Z", "2026-03-28T23:00:00Z", "2026-03-29T22:00:00Z")]
    [InlineData("2026-10-25T12:00:00Z", "2026-10-24T22:00:00Z", "2026-10-25T23:00:00Z")]
    public void TodayUsesLocalMidnightAndDst(string now, string start, string end)
    {
        AssertRange(resolver.GetTodayUtcRange(DateTimeOffset.Parse(now)), start, end);
    }

    [Fact]
    public void WeekStartsMondayAndHandlesDst() => AssertRange(
        resolver.GetCurrentWeekUtcRange(DateTimeOffset.Parse("2026-03-29T12:00:00Z")),
        "2026-03-22T23:00:00Z", "2026-03-29T22:00:00Z");

    [Fact]
    public void MonthUsesLocalDate() => AssertRange(
        resolver.GetCurrentMonthUtcRange(DateTimeOffset.Parse("2026-09-30T22:30:00Z")),
        "2026-09-30T22:00:00Z", "2026-10-31T23:00:00Z");

    [Fact]
    public void PreviousMonthHandlesYearBoundary() => AssertRange(
        resolver.GetPreviousMonthUtcRange(DateTimeOffset.Parse("2026-01-15T12:00:00Z")),
        "2025-11-30T23:00:00Z", "2025-12-31T23:00:00Z");

    private static void AssertRange(UtcPeriodRange range, string start, string end)
    {
        Assert.Equal(DateTimeOffset.Parse(start), range.StartUtc);
        Assert.Equal(DateTimeOffset.Parse(end), range.EndUtc);
        Assert.True(range.Contains(range.StartUtc));
        Assert.False(range.Contains(range.EndUtc));
    }
}
