using CitySurfers.Application.Running;
using CitySurfers.Application.Users;
using CitySurfers.Domain.Running;
using CitySurfers.Infrastructure.Running;

namespace CitySurfers.UnitTests;

public sealed class RunSessionServiceTests
{
    private readonly MemoryStore store = new();
    private readonly FixedTime time = new();
    private RunSessionService Service(string userId = "user") =>
        new(new CurrentUser(userId), store, new DemoRunCompetitionProvider(), new RunMetricsCalculator(), time);

    [Fact]
    public async Task Start_persists_current_user_and_conflicts_on_second_start()
    {
        var service = Service();
        var result = await service.StartAsync();
        Assert.Equal("user", result.Run.UserId);
        Assert.Equal(new DateTime(2026, 10, 3, 16, 0, 0, DateTimeKind.Utc), result.Run.StartedAtUtc);
        Assert.Equal(41, result.Competition.Rank);
        Assert.Equal(1200, result.Competition.CurrentTarget!.DistanceToOvertakeMeters);
        Assert.Equal(result.Run.Id, (await service.GetActiveAsync()).Run.Id);
        Assert.Equal(RunError.Conflict, (await Assert.ThrowsAsync<RunRuleException>(() => service.StartAsync())).Error);
    }

    [Fact]
    public async Task Progress_awards_only_new_targets_and_finish_persists_summary()
    {
        var service = Service();
        var id = (await service.StartAsync()).Run.Id;
        var before = await service.UpdateProgressAsync(id, 1010, 320);
        Assert.Empty(before.Events);
        Assert.Equal(190, before.Competition.CurrentTarget!.DistanceToOvertakeMeters);
        Assert.Equal(316.831683, before.Run.AveragePaceSecondsPerKm!.Value, 6);
        var crossing = await service.UpdateProgressAsync(id, 2800, 840);
        Assert.Equal(2, crossing.Events.Count);
        Assert.Equal(39, crossing.Competition.Rank);
        Assert.Equal(30, crossing.Competition.SeasonPointsEarned);
        Assert.Empty((await service.UpdateProgressAsync(id, 2800, 840)).Events);
        time.Advance();
        var finished = await service.FinishAsync(id, 6800, 2210);
        Assert.Equal(RunStatus.Completed, finished.Run.Status);
        Assert.Equal(new DateTime(2026, 10, 3, 16, 40, 0, DateTimeKind.Utc), finished.Run.FinishedAtUtc);
        Assert.Equal(2, finished.Events.Count);
        var loaded = await Service().GetByIdAsync(id);
        Assert.Equal(59, loaded.Run.SeasonPointsEarned);
        Assert.Equal(37, loaded.Run.CurrentRank);
        Assert.Equal(325, loaded.Run.AveragePaceSecondsPerKm);
        Assert.Equal(4, loaded.Run.Overtakes.Count);
        Assert.Null(loaded.Competition.CurrentTarget);
        Assert.Equal(RunError.Conflict, (await Assert.ThrowsAsync<RunRuleException>(() =>
            service.FinishAsync(id, 6800, 2210))).Error);
        Assert.Equal(RunError.Conflict, (await Assert.ThrowsAsync<RunRuleException>(() =>
            service.UpdateProgressAsync(id, 7000, 2300))).Error);
        Assert.Equal(RunError.NotFound, (await Assert.ThrowsAsync<RunRuleException>(() => service.GetActiveAsync())).Error);
        Assert.Equal(RunStatus.Active, (await service.StartAsync()).Run.Status);
    }

    [Theory]
    [InlineData(-1, 400)]
    [InlineData(1000, -1)]
    [InlineData(999, 400)]
    [InlineData(1000, 299)]
    public async Task Invalid_update_and_finish_leave_persisted_run_unchanged(double distance, double duration)
    {
        var service = Service();
        var id = (await service.StartAsync()).Run.Id;
        await service.UpdateProgressAsync(id, 1000, 300);
        Assert.Equal(RunError.Validation, (await Assert.ThrowsAsync<RunRuleException>(() =>
            service.UpdateProgressAsync(id, distance, duration))).Error);
        Assert.Equal(RunError.Validation, (await Assert.ThrowsAsync<RunRuleException>(() =>
            service.FinishAsync(id, distance, duration))).Error);
        var loaded = await service.GetByIdAsync(id);
        Assert.Equal(1000, loaded.Run.DistanceMeters);
        Assert.Equal(300, loaded.Run.DurationSeconds);
        Assert.Equal(RunStatus.Active, loaded.Run.Status);
        Assert.Empty(loaded.Run.Overtakes);
    }

    [Fact]
    public async Task Missing_and_foreign_runs_are_not_found_for_every_operation()
    {
        var id = (await Service("other").StartAsync()).Run.Id;
        foreach (var runId in new[] { id, "missing" })
        {
            Assert.Equal(RunError.NotFound, (await Assert.ThrowsAsync<RunRuleException>(() =>
                Service().GetByIdAsync(runId))).Error);
            Assert.Equal(RunError.NotFound, (await Assert.ThrowsAsync<RunRuleException>(() =>
                Service().UpdateProgressAsync(runId, 1200, 300))).Error);
            Assert.Equal(RunError.NotFound, (await Assert.ThrowsAsync<RunRuleException>(() =>
                Service().FinishAsync(runId, 1200, 300))).Error);
        }
    }

    [Fact]
    public async Task Cancellation_reaches_store()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().StartAsync(source.Token));
    }

    private sealed class FixedTime : TimeProvider
    {
        private DateTimeOffset now = new DateTimeOffset(2026, 10, 3, 16, 0, 0, TimeSpan.Zero).AddTicks(1234);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance() => now = now.AddMinutes(40);
    }

    private sealed class CurrentUser(string id) : ICurrentUserAccessor
    {
        public Task<string> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(id);
    }

    private sealed class MemoryStore : IRunSessionStore
    {
        private readonly Dictionary<string, RunSession> runs = [];
        public Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Copy(runs.Values.FirstOrDefault(run => run.UserId == userId && run.Status == RunStatus.Active)));
        }
        public Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Copy(runs.GetValueOrDefault(id)));
        public Task AddAsync(RunSession run, CancellationToken cancellationToken = default)
        {
            runs.Add(run.Id, Copy(run)!);
            return Task.CompletedTask;
        }
        public Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
            CancellationToken cancellationToken = default)
        {
            var previous = runs[run.Id];
            if (previous.Status != RunStatus.Active || previous.DistanceMeters != previousDistance
                || previous.DurationSeconds != previousDuration)
                throw new RunRuleException(RunError.Conflict, "Concurrent change.");
            runs[run.Id] = Copy(run)!;
            return Task.CompletedTask;
        }
        private static RunSession? Copy(RunSession? run) => run is null ? null : RunSession.Restore(run.Id,
            run.UserId, run.Status, run.StartedAtUtc, run.UpdatedAtUtc, run.FinishedAtUtc,
            run.DistanceMeters, run.DurationSeconds, run.AveragePaceSecondsPerKm,
            run.StartingRank, run.CurrentRank, run.SeasonPointsEarned, run.Overtakes);
    }
}
