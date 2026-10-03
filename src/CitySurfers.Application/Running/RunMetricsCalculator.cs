namespace CitySurfers.Application.Running;

public sealed class RunMetricsCalculator
{
    public double? CalculateAveragePace(double distanceMeters, double durationSeconds) =>
        distanceMeters > 0 ? durationSeconds / distanceMeters * 1000 : null;
}
