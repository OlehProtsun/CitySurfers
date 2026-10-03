namespace CitySurfers.Application.Running;

public interface IRunHistoryReader
{
    Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId,
        DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, int? limit = null,
        CancellationToken cancellationToken = default);
}
