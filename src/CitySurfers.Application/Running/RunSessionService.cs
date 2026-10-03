using CitySurfers.Application.Users;
using CitySurfers.Domain.Running;

namespace CitySurfers.Application.Running;

public sealed record RunSessionResult(RunSession Run, CompetitionSnapshot Competition, IReadOnlyList<Overtake> Events);

public sealed class RunSessionService(ICurrentUserAccessor currentUser, IRunSessionStore store,
    IRunCompetitionProvider competition, RunMetricsCalculator metrics, TimeProvider time)
{
    public async Task<RunSessionResult> StartAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        if (await store.GetActiveByUserIdAsync(userId, cancellationToken) is not null)
            throw new RunRuleException(RunError.Conflict, "An active run already exists.");
        var run = new RunSession(Guid.NewGuid().ToString("N"), userId, competition.StartingRank, UtcNow());
        await store.AddAsync(run, cancellationToken);
        return Result(run);
    }

    public async Task<RunSessionResult> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        var run = await store.GetActiveByUserIdAsync(userId, cancellationToken)
            ?? throw NotFound();
        return Result(run);
    }

    public async Task<RunSessionResult> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Result(await GetOwnedRunAsync(id, cancellationToken));

    public Task<RunSessionResult> UpdateProgressAsync(string id, double distanceMeters, double durationSeconds,
        CancellationToken cancellationToken = default) =>
        ApplyProgressAsync(id, distanceMeters, durationSeconds, false, cancellationToken);

    public Task<RunSessionResult> FinishAsync(string id, double distanceMeters, double durationSeconds,
        CancellationToken cancellationToken = default) =>
        ApplyProgressAsync(id, distanceMeters, durationSeconds, true, cancellationToken);

    private async Task<RunSessionResult> ApplyProgressAsync(string id, double distance, double duration,
        bool finish, CancellationToken cancellationToken)
    {
        var run = await GetOwnedRunAsync(id, cancellationToken);
        var previousDistance = run.DistanceMeters;
        var previousDuration = run.DurationSeconds;
        var now = UtcNow();
        run.UpdateProgress(distance, duration, metrics.CalculateAveragePace(distance, duration), now);
        var events = competition.Evaluate(run, now);
        if (finish)
            run.Finish(now);
        await store.SaveAsync(run, previousDistance, previousDuration, cancellationToken);
        return Result(run, events);
    }

    private async Task<RunSession> GetOwnedRunAsync(string id, CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        var run = await store.GetByIdAsync(id, cancellationToken);
        return run is not null && run.UserId == userId ? run : throw NotFound();
    }

    private RunSessionResult Result(RunSession run, IReadOnlyList<Overtake>? events = null) =>
        new(run, competition.GetSnapshot(run), events ?? []);

    private DateTime UtcNow() =>
        DateTimeOffset.FromUnixTimeMilliseconds(time.GetUtcNow().ToUnixTimeMilliseconds()).UtcDateTime;
    private static RunRuleException NotFound() => new(RunError.NotFound, "Run not found.");
}
