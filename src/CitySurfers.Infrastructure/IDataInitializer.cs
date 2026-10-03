namespace CitySurfers.Infrastructure;

public interface IDataInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
