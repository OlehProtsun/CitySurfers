namespace CitySurfers.Application.Authentication;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
