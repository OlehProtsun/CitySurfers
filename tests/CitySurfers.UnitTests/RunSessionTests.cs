using CitySurfers.Domain.Running;

namespace CitySurfers.UnitTests;

public sealed class RunSessionTests
{
    [Fact]
    public void Starts_active_and_finishes_once()
    {
        var run = new RunSession("id", "user", 41, DateTime.UtcNow);
        Assert.Equal(RunStatus.Active, run.Status);
        Assert.Empty(run.Overtakes);
        Assert.Equal(0, run.DistanceMeters);
        run.UpdateProgress(1000, 300, 300, DateTime.UtcNow);
        run.Finish(DateTime.UtcNow);
        Assert.Equal(RunStatus.Completed, run.Status);
        Assert.NotNull(run.FinishedAtUtc);
        Assert.Throws<RunRuleException>(() => run.Finish(DateTime.UtcNow));
        Assert.Throws<RunRuleException>(() => run.UpdateProgress(2000, 600, 300, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(-1, 300)]
    [InlineData(1000, -1)]
    [InlineData(999, 300)]
    [InlineData(1000, 299)]
    [InlineData(double.NaN, 300)]
    [InlineData(1000, double.PositiveInfinity)]
    public void Rejects_invalid_or_decreasing_progress(double distance, double duration)
    {
        var run = new RunSession("id", "user", 41, DateTime.UtcNow);
        run.UpdateProgress(1000, 300, 300, DateTime.UtcNow);
        Assert.Equal(RunError.Validation, Assert.Throws<RunRuleException>(() =>
            run.UpdateProgress(distance, duration, null, DateTime.UtcNow)).Error);
        Assert.Equal(1000, run.DistanceMeters);
    }
}
