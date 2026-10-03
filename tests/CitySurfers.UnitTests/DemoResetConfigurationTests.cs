using CitySurfers.Infrastructure;
using CitySurfers.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CitySurfers.UnitTests;

public sealed class DemoResetConfigurationTests
{
    [Fact]
    public void ResetDefaultsToFalse()
    {
        Assert.False(new DemoDataOptions().ResetRunsOnStartup);
        using var services = Services(null, null);
        var options = services.GetRequiredService<IOptions<DemoDataOptions>>().Value;
        Assert.False(options.ResetRunsOnStartup);
        Assert.True(options.SeedOnStartup);
    }

    [Fact]
    public void ResetRequiresSeedingAndFailsStartupValidation()
    {
        using var services = Services("true", "false");
        var exception = Assert.Throws<OptionsValidationException>(() =>
            services.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains("DemoData:ResetRunsOnStartup requires DemoData:SeedOnStartup=true.", exception.Message);
    }

    [Theory]
    [InlineData("false", "false")]
    [InlineData("false", "true")]
    [InlineData("true", "true")]
    public void ValidCombinationsPassStartupValidation(string reset, string seed)
    {
        using var services = Services(reset, seed);
        services.GetRequiredService<IStartupValidator>().Validate();
        var options = services.GetRequiredService<IOptions<DemoDataOptions>>().Value;
        Assert.Equal(bool.Parse(reset), options.ResetRunsOnStartup);
        Assert.Equal(bool.Parse(seed), options.SeedOnStartup);
    }

    private static ServiceProvider Services(string? reset, string? seed)
    {
        var values = new Dictionary<string, string?>
        {
            ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDb:DatabaseName"] = "citysurfers-test"
        };
        if (reset is not null) values["DemoData:ResetRunsOnStartup"] = reset;
        if (seed is not null) values["DemoData:SeedOnStartup"] = seed;
        return new ServiceCollection().AddInfrastructure(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build()).BuildServiceProvider();
    }
}
