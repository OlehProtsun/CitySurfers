using CitySurfers.Application.Users;

namespace CitySurfers.Application.Running;

public sealed class RunHistoryService(ICurrentUserAccessor currentUser, IRunHistoryReader history)
{
    public async Task<IReadOnlyList<RunHistoryItem>> GetRecentAsync(int limit,
        CancellationToken cancellationToken = default) => await history.GetCompletedAsync(
            await currentUser.GetUserIdAsync(cancellationToken), limit: limit, cancellationToken: cancellationToken);
}
