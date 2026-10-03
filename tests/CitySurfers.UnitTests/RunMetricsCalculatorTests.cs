using CitySurfers.Application.Running;

namespace CitySurfers.UnitTests;

public sealed class RunMetricsCalculatorTests
{
    [Fact]
    public void Zero_distance_has_no_pace() =>
        Assert.Null(new RunMetricsCalculator().CalculateAveragePace(0, 300));

    [Theory]
    [InlineData(1000, 300, 300)]
    [InlineData(1010, 320, 316.83168316831683)]
    [InlineData(6800, 2210, 325)]
    public void Calculates_average_pace(double distance, double duration, double expected) =>
        Assert.Equal(expected, new RunMetricsCalculator().CalculateAveragePace(distance, duration)!.Value, 8);
}
