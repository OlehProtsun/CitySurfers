namespace CitySurfers.Application.ActivityMap;

public interface IActivityMapProvider
{
    Task<ActivityMapResponse> GetAsync(ActivityPeriod period, CancellationToken cancellationToken = default);
}
