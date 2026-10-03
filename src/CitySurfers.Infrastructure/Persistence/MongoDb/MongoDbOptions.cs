using MongoDB.Driver;

namespace CitySurfers.Infrastructure.Persistence.MongoDb;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "citysurfers";

    internal static bool HasValidConnectionString(MongoDbOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            return false;
        try
        {
            _ = MongoClientSettings.FromConnectionString(options.ConnectionString);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
