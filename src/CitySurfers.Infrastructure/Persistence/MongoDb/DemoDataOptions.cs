namespace CitySurfers.Infrastructure.Persistence.MongoDb;

public sealed class DemoDataOptions
{
    public const string SectionName = "DemoData";
    public bool SeedOnStartup { get; set; } = true;
    public bool ResetRunsOnStartup { get; set; }
}
