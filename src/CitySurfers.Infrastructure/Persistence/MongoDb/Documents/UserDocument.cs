using MongoDB.Bson.Serialization.Attributes;

namespace CitySurfers.Infrastructure.Persistence.MongoDb.Documents;

[BsonIgnoreExtraElements]
internal sealed class UserDocument
{
    [BsonId]
    public string Id { get; set; } = "";
    [BsonElement("username")]
    public string Username { get; set; } = "";
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = "";
    [BsonElement("demoPassword")]
    public string DemoPassword { get; set; } = "";
    [BsonElement("createdAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }
}
