namespace CitySurfers.Application.Users;

public interface ICurrentUserAccessor
{
    Task<string> GetUserIdAsync(CancellationToken cancellationToken = default);
}
