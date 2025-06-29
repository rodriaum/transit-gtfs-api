using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using MongoDB.Driver;

namespace TransitGtfsApi.Service;

public class FareRulesService : MongoService<FareRule>, IFareRulesService
{
    private readonly IRedisService _redis;

    public FareRulesService(IMongoDatabase database, ILogger<FareRulesService> logger, IRedisService redis)
        : base(database, logger, "gtfs_fare_rules")
    {
        this._redis = redis;

        IndexKeysDefinition<FareRule> indexKeysDefinition = Builders<FareRule>.IndexKeys.Ascending(r => r.FareId);
        _collection.Indexes.CreateOne(new CreateIndexModel<FareRule>(indexKeysDefinition));
    }

    public async Task<List<FareRule>> GetAllAsync()
    {
        return await _collection.Find(Builders<FareRule>.Filter.Empty).ToListAsync();
    }

    public async Task<List<FareRule>?> GetByFareIdAsync(string fareId)
    {
        return await _redis.GetOrSetAsync(
            $"fare-rules-{fareId}",
            async () => await _collection.Find(f => f.FareId == fareId).ToListAsync()
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_rules.txt");
        await ImportFromCsvAsync(filePath, fields => new FareRule
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            FareId = fields.GetValueOrDefault("fare_id", "") ?? "",
            RouteId = fields.GetValueOrDefault("route_id", "") ?? "",
            OriginId = fields.GetValueOrDefault("origin_id", "") ?? "",
            DestinationId = fields.GetValueOrDefault("destination_id", "") ?? ""
        });
    }
}