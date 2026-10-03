using CitySurfers.Application.Periods;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;

namespace CitySurfers.Application.Progress;

public sealed class ProgressService(ICurrentUserAccessor currentUser, IRunHistoryReader history,
    KrakowPeriodResolver periods, RunMetricsCalculator metrics, TimeProvider time)
{
    public async Task<PersonalProgress> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var runs = await history.GetCompletedAsync(await currentUser.GetUserIdAsync(cancellationToken),
            cancellationToken: cancellationToken);
        var all = Aggregate(runs);
        var lifetime = new LifetimeProgress(all.CompletedRuns, all.DistanceMeters, all.DurationSeconds,
            all.AveragePaceSecondsPerKm, runs.Sum(run => (long)run.OvertakesCount), all.PointsEarned,
            runs.Select(run => run.DistanceMeters).DefaultIfEmpty(0).Max(),
            runs.Where(run => run.AveragePaceSecondsPerKm is > 0
                && double.IsFinite(run.AveragePaceSecondsPerKm.Value))
                .Select(run => run.AveragePaceSecondsPerKm).DefaultIfEmpty(null).Min());
        var week = InPeriod(periods.GetCurrentWeekUtcRange(now));
        var month = InPeriod(periods.GetCurrentMonthUtcRange(now));
        var previous = InPeriod(periods.GetPreviousMonthUtcRange(now));
        return new PersonalProgress(lifetime, week, month, previous,
            new ProgressComparison(month.DistanceMeters - previous.DistanceMeters,
                month.AveragePaceSecondsPerKm - previous.AveragePaceSecondsPerKm));

        PeriodProgress InPeriod(UtcPeriodRange range) => Aggregate(runs.Where(run => range.Contains(run.StartedAtUtc)));
    }

    private PeriodProgress Aggregate(IEnumerable<RunHistoryItem> source)
    {
        var runs = source.ToArray();
        var distance = runs.Sum(run => run.DistanceMeters);
        var duration = runs.Sum(run => run.DurationSeconds);
        return new PeriodProgress(runs.Length, distance, duration,
            metrics.CalculateAveragePace(distance, duration), runs.Sum(run => (long)run.PointsEarned));
    }
}
