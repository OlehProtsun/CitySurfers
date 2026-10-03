using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;

namespace CitySurfers.IntegrationTests;

internal sealed class TestRunStore : IRunSessionStore, IRunHistoryReader
{
    private readonly Dictionary<string, RunSession> runs = [];
    private readonly object gate = new();

    public Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId,
        DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, int? limit = null,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var items = runs.Values.Where(run => run.UserId == userId && run.Status == RunStatus.Completed
                && (!fromUtc.HasValue || run.StartedAtUtc >= fromUtc.Value.UtcDateTime)
                && (!toUtc.HasValue || run.StartedAtUtc < toUtc.Value.UtcDateTime))
                .OrderByDescending(run => run.StartedAtUtc).ThenBy(run => run.Id, StringComparer.Ordinal)
                .Take(limit ?? int.MaxValue)
                .Select(run => new RunHistoryItem(run.Id, run.StartedAtUtc, run.FinishedAtUtc,
                    run.DistanceMeters, run.DurationSeconds, run.AveragePaceSecondsPerKm,
                    run.Overtakes.Count, run.SeasonPointsEarned)).ToArray();
            return Task.FromResult<IReadOnlyList<RunHistoryItem>>(items);
        }
    }

    public Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        lock (gate)
            return Task.FromResult(Copy(runs.Values.FirstOrDefault(run => run.UserId == userId && run.Status == RunStatus.Active)));
    }

    public Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (gate)
            return Task.FromResult(Copy(runs.GetValueOrDefault(id)));
    }

    public Task AddAsync(RunSession run, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (runs.Values.Any(item => item.UserId == run.UserId && item.Status == RunStatus.Active))
                throw new RunRuleException(RunError.Conflict, "An active run already exists.");
            runs.Add(run.Id, Copy(run)!);
        }
        return Task.CompletedTask;
    }

    public Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var previous = runs.GetValueOrDefault(run.Id);
            if (previous is null || previous.Status != RunStatus.Active || previous.DistanceMeters != previousDistance
                || previous.DurationSeconds != previousDuration)
                throw new RunRuleException(RunError.Conflict, "The run changed.");
            runs[run.Id] = Copy(run)!;
        }
        return Task.CompletedTask;
    }

    private static RunSession? Copy(RunSession? run) => run is null ? null : RunSession.Restore(run.Id,
        run.UserId, run.Status, run.StartedAtUtc, run.UpdatedAtUtc, run.FinishedAtUtc,
        run.DistanceMeters, run.DurationSeconds, run.AveragePaceSecondsPerKm,
        run.StartingRank, run.CurrentRank, run.SeasonPointsEarned, run.Overtakes);
}
