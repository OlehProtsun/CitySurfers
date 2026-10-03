using CitySurfers.Application.Periods;
using CitySurfers.Application.Progress;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;

namespace CitySurfers.UnitTests;

public sealed class ProgressServiceTests
{
    [Fact]
    public async Task EmptyHistoryHasZeroTotalsAndNullPaces()
    {
        var result = await Service([]).GetAsync();
        Assert.Equal(new LifetimeProgress(0, 0, 0, null, 0, 0, 0, null), result.Lifetime);
        Assert.Equal(new PeriodProgress(0, 0, 0, null, 0), result.CurrentMonth);
        Assert.Equal(result.CurrentMonth, result.CurrentWeek);
        Assert.Equal(result.CurrentMonth, result.PreviousMonth);
        Assert.Equal(new ProgressComparison(0, null), result.Comparison);
    }

    [Fact]
    public async Task OneRunUsesRealMetricsAndMissingPreviousPaceHasNullDelta()
    {
        var result = await Service([Run("2026-10-03T12:00:00Z", 6800, 2210, 325, 4, 59)]).GetAsync();
        Assert.Equal(new LifetimeProgress(1, 6800, 2210, 325, 4, 59, 6800, 325), result.Lifetime);
        Assert.Equal(59, result.CurrentMonth.PointsEarned);
        Assert.Null(result.Comparison.MonthlyAveragePaceDeltaSecondsPerKm);
    }

    [Fact]
    public async Task AggregatesAreWeightedAndAssignedByLocalStartedDate()
    {
        var result = await Service([
            Run("2026-09-30T22:00:00Z", 1000, 300, 300, 1, 16),
            Run("2026-10-03T12:00:00Z", 3000, 1200, 400, 2, 30),
            Run("2026-09-30T21:59:59Z", 2000, 800, 400, 3, 41),
            Run("2026-08-01T12:00:00Z", 10000, 5000, 500, 4, 59)
        ]).GetAsync();
        Assert.Equal(4, result.Lifetime.CompletedRuns);
        Assert.Equal(16000, result.Lifetime.TotalDistanceMeters);
        Assert.Equal(7300, result.Lifetime.TotalDurationSeconds);
        Assert.Equal(456.25, result.Lifetime.AveragePaceSecondsPerKm);
        Assert.Equal(10, result.Lifetime.TotalOvertakes);
        Assert.Equal(146, result.Lifetime.TotalPointsEarned);
        Assert.Equal(10000, result.Lifetime.LongestRunDistanceMeters);
        Assert.Equal(300, result.Lifetime.FastestRunAveragePaceSecondsPerKm);
        Assert.Equal(new PeriodProgress(2, 4000, 1500, 375, 46), result.CurrentMonth);
        Assert.Equal(3, result.CurrentWeek.CompletedRuns);
        Assert.Equal(6000, result.CurrentWeek.DistanceMeters);
        Assert.Equal(2300, result.CurrentWeek.DurationSeconds);
        Assert.Equal(2300d / 6, result.CurrentWeek.AveragePaceSecondsPerKm!.Value, 6);
        Assert.Equal(87, result.CurrentWeek.PointsEarned);
        Assert.Equal(new PeriodProgress(1, 2000, 800, 400, 41), result.PreviousMonth);
        Assert.Equal(new ProgressComparison(2000, -25), result.Comparison);
    }

    [Fact]
    public async Task ZeroDistanceAndInvalidIndividualPacesAreUnavailable()
    {
        var result = await Service([Run("2026-10-03T12:00:00Z", 0, 0, null, 0, 0)]).GetAsync();
        Assert.Null(result.Lifetime.AveragePaceSecondsPerKm);
        Assert.Null(result.Lifetime.FastestRunAveragePaceSecondsPerKm);
    }

    private static RunHistoryItem Run(string start, double distance, double duration, double? pace, int overtakes, int points) =>
        new(Guid.NewGuid().ToString(), DateTimeOffset.Parse(start), DateTimeOffset.Parse(start).AddHours(1),
            distance, duration, pace, overtakes, points);

    private static ProgressService Service(IReadOnlyList<RunHistoryItem> runs) =>
        new(new CurrentUser(), new History(runs), new KrakowPeriodResolver(), new RunMetricsCalculator(), new FixedTime());

    private sealed class CurrentUser : ICurrentUserAccessor
    {
        public Task<string> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult("owner");
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    }

    private sealed class History(IReadOnlyList<RunHistoryItem> runs) : IRunHistoryReader
    {
        public Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId, DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            Assert.Equal("owner", userId);
            return Task.FromResult(runs);
        }
    }
}
