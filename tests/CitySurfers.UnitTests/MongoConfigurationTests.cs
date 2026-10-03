using CitySurfers.Infrastructure;
using CitySurfers.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CitySurfers.UnitTests;

public sealed class MongoConfigurationTests
{
    [Theory]
    [InlineData("", "citysurfers")]
    [InlineData("invalid-secret-uri", "citysurfers")]
    [InlineData("mongodb://localhost:27017", "")]
    [InlineData("mongodb://localhost:27017", "invalid/name")]
    public void Invalid_configuration_fails_without_echoing_connection_string(string uri, string database)
    {
        using var services = CreateServices(uri, database);
        var exception = Assert.Throws<OptionsValidationException>(() =>
            services.GetRequiredService<IOptions<MongoDbOptions>>().Value);
        if (uri.Length > 0)
            Assert.DoesNotContain(uri, exception.Message);
    }

    [Fact]
    public void Valid_configuration_is_bound()
    {
        using var services = CreateServices("mongodb://localhost:27017", "citysurfers-test");
        var options = services.GetRequiredService<IOptions<MongoDbOptions>>().Value;
        Assert.Equal("citysurfers-test", options.DatabaseName);
    }

    private static ServiceProvider CreateServices(string uri, string database)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDb:ConnectionString"] = uri,
            ["MongoDb:DatabaseName"] = database
        }).Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }
}
