namespace CitySurfers.Application.Users;

public interface IUserStore
{
    Task<UserData?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
