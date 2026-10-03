using System.Reflection;
using CitySurfers.Application.Running;
using CitySurfers.Infrastructure.Persistence.MongoDb;
using CitySurfers.Infrastructure.Persistence.MongoDb.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CitySurfers.UnitTests;

public sealed class MongoRunHistoryReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryUsesUserCompletedStatusExclusivePeriodSortAndServerLimit(bool withPeriod)
    {
        var from = DateTimeOffset.Parse("2026-10-01T00:00:00Z");
        var to = from.AddMonths(1);
        var collection = DispatchProxy.Create<IMongoCollection<RunDocument>, DriverProxy>();
        var proxy = (DriverProxy)(object)collection;
        proxy.Handler = (method, args) => method.Name switch
        {
            "get_DocumentSerializer" => BsonSerializer.SerializerRegistry.GetSerializer<RunDocument>(),
            "get_Settings" => new MongoCollectionSettings(),
            "FindAsync" => Capture(args!),
            _ => throw new NotSupportedException(method.Name)
        };
        var database = DispatchProxy.Create<IMongoDatabase, DriverProxy>();
        ((DriverProxy)(object)database).Handler = (_, _) => collection;
        var items = await new MongoRunHistoryReader(database).GetCompletedAsync("owner",
            withPeriod ? from : null, withPeriod ? to : null, 3);
        Assert.Empty(items);

        Task<IAsyncCursor<RunDocument>> Capture(object?[] args)
        {
            var render = new RenderArgs<RunDocument>(BsonSerializer.SerializerRegistry.GetSerializer<RunDocument>(),
                BsonSerializer.SerializerRegistry);
            var filter = ((FilterDefinition<RunDocument>)args[0]!).Render(render);
            Assert.Equal("owner", filter["userId"].AsString);
            Assert.Equal("completed", filter["status"].AsString);
            if (withPeriod)
            {
                Assert.Equal(from.UtcDateTime, filter["startedAtUtc"]["$gte"].ToUniversalTime());
                Assert.Equal(to.UtcDateTime, filter["startedAtUtc"]["$lt"].ToUniversalTime());
            }
            else Assert.False(filter.Contains("startedAtUtc"));
            var options = (FindOptions<RunDocument, RunDocument>)args[1]!;
            Assert.Equal(3, options.Limit);
            Assert.Equal(new BsonDocument { { "startedAtUtc", -1 }, { "_id", 1 } }, options.Sort.Render(render));
            return Task.FromResult<IAsyncCursor<RunDocument>>(new EmptyCursor());
        }
    }

    public class DriverProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }

    private sealed class EmptyCursor : IAsyncCursor<RunDocument>
    {
        public IEnumerable<RunDocument> Current => [];
        public bool MoveNext(CancellationToken cancellationToken = default) => false;
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Dispose() { }
    }
}
