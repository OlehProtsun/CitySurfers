using CitySurfers.Domain.Running;

namespace CitySurfers.Application.Running;

public interface IRunSessionStore
{
    Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(RunSession run, CancellationToken cancellationToken = default);
    // Compare the previous progress atomically so concurrent requests cannot overwrite newer rewards.
    Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
        CancellationToken cancellationToken = default);
}
