using MongoDB.Driver;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Service;

public class TripsService : MongoService<Trip>, ITripsService
{
    private readonly IRedisService _redis;

    public TripsService(IMongoDatabase database, ILogger<TripsService> logger, IRedisService redis)
        : base(database, logger, "gtfs_trips")
    {
        _redis = redis;

        IndexKeysDefinition<Trip> indexKeysDefinition = Builders<Trip>.IndexKeys.Ascending(t => t.TripId);
        _collection.Indexes.CreateOne(new CreateIndexModel<Trip>(indexKeysDefinition));

        indexKeysDefinition = Builders<Trip>.IndexKeys.Ascending(t => t.RouteId);
        _collection.Indexes.CreateOne(new CreateIndexModel<Trip>(indexKeysDefinition));

        indexKeysDefinition = Builders<Trip>.IndexKeys.Ascending(t => t.ServiceId);
        _collection.Indexes.CreateOne(new CreateIndexModel<Trip>(indexKeysDefinition));
    }

    public async Task<List<Trip>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;

        return await _collection.Find(Builders<Trip>.Filter.Empty)
                                .Skip(skip)
                                .Limit(pageSize)
                                .ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(string tripId)
    {
        return await _redis.GetOrSetAsync(
            $"trip-{tripId}",
            async () => await _collection.Find(t => t.TripId == tripId).FirstOrDefaultAsync()
        );
    }

    public async Task<List<Trip>?> GetByRouteIdAsync(string routeId, int page = 1, int pageSize = 100)
    {
        return await _redis.GetOrSetAsync(
            $"trips-route-{routeId}-{page}-{pageSize}",
            async () =>
            {
                var skip = (page - 1) * pageSize;
                return await _collection.Find(t => t.RouteId == routeId)
                                        .Skip(skip)
                                        .Limit(pageSize)
                                        .ToListAsync();
            }
        );
    }

    public async Task<List<Trip?>?> GetTripsBatchAsync(List<string> tripIds)
    {
        return await _redis.GetOrSetAsync(
            $"trips-batch-{string.Join("-", tripIds.OrderBy(id => id))}",
            async () =>
            {
                var filter = Builders<Trip>.Filter.In(t => t.TripId, tripIds);

                var trips = await _collection.Find(filter).ToListAsync();

                return tripIds
                    .Select(id => trips.FirstOrDefault(t => t.TripId == id))
                    .Where(t => t != null)
                    .ToList();
            }
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "trips.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return;
        }

        await ImportFromCsvAsync(filePath, fields =>
        {
            int wheelchairAccessibleId = NumberUtil.ParseIntSafe(fields.GetValueOrDefault("wheelchair_accessible", null), -1);
            int directionId = NumberUtil.ParseIntSafe(fields.GetValueOrDefault("direction_id", null), -1);
            int bikesAllowedId = NumberUtil.ParseIntSafe(fields.GetValueOrDefault("bikes_allowed", null), -1);

            return new Trip
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                RouteId = fields.GetValueOrDefault("route_id", "") ?? "",
                ServiceId = fields.GetValueOrDefault("service_id", "") ?? "",
                TripId = fields.GetValueOrDefault("trip_id", "") ?? "",
                TripHeadsign = fields.GetValueOrDefault("trip_headsign", "") ?? "",
                WheelchairAccessible = wheelchairAccessibleId != -1 ? EnumUtil.FromValue<TrinaryOption>(wheelchairAccessibleId) : null,
                DirectionId = directionId != -1 ? EnumUtil.FromValue<DirectionType>(directionId) : null,
                BlockId = fields.GetValueOrDefault("block_id", "") ?? "",
                ShapeId = fields.GetValueOrDefault("shape_id", "") ?? "",
                BikesAllowed = bikesAllowedId != -1 ? EnumUtil.FromValue<TrinaryOption>(bikesAllowedId) : null,
            };
        });
    }
}