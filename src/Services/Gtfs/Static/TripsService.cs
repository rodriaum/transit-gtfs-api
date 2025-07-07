using Microsoft.EntityFrameworkCore;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class TripsService : ITripsService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<TripsService> _logger;
    private readonly IRedisService _redis;

    public TripsService(TransitDbContext dbContext, ILogger<TripsService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Trip>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _dbContext.Trips.Skip(skip).Take(pageSize).ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(string tripId)
    {
        return await _redis.GetOrSetAsync(
            $"trip-{tripId}",
            async () => await _dbContext.Trips.FirstOrDefaultAsync(t => t.TripId == tripId)
        );
    }

    public async Task<List<Trip>?> GetByRouteIdAsync(string routeId, int page = 1, int pageSize = 100)
    {
        return await _redis.GetOrSetAsync(
            $"trips-route-{routeId}-{page}-{pageSize}",
            async () =>
            {
                var skip = (page - 1) * pageSize;
                return await _dbContext.Trips.Where(t => t.RouteId == routeId)
                                             .Skip(skip)
                                             .Take(pageSize)
                                             .ToListAsync();
            }
        );
    }

    public async Task<List<Trip>?> GetTripsBatchAsync(List<string> tripIds)
    {
        return (await _redis.GetOrSetAsync(
            $"trips-batch-{string.Join("-", tripIds.OrderBy(id => id))}",
            async () =>
            {
                List<Trip> trips = await _dbContext.Trips
                    .Where(t => tripIds.Contains(t.TripId))
                    .ToListAsync();

                return tripIds
                    .Select(id => trips.FirstOrDefault(t => t.TripId == id))
                    .Where(t => t != null)
                    .ToList();
            }
        ) ?? new List<Trip?>())
        .Where(t => t != null)
        .Select(t => t!)
        .ToList();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "trips.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");

            int batchSize = 1000;
            List<Trip> entities = new List<Trip>(batchSize);
            int totalImported = 0;

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    _logger.LogWarning($"No data found in {filePath}");
                    return;
                }

                string[] headers = headerLine.Split(',');
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = line.Split(',');
                    Dictionary<string, string?> rowData = new Dictionary<string, string?>();

                    for (int j = 0; j < headers.Length; j++)
                    {
                        if (j < values.Length)
                        {
                            rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                        }
                    }

                    int wheelchairAccessibleId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wheelchair_accessible", null), -1);
                    int directionId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("direction_id", null), -1);
                    int bikesAllowedId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("bikes_allowed", null), -1);

                    Trip entity = new Trip
                    {
                        Id = Guid.NewGuid().ToString(),
                        RouteId = rowData.GetValueOrDefault("route_id", "") ?? "",
                        ServiceId = rowData.GetValueOrDefault("service_id", "") ?? "",
                        TripId = rowData.GetValueOrDefault("trip_id", "") ?? "",
                        TripHeadsign = rowData.GetValueOrDefault("trip_headsign", null),
                        TripShortName = rowData.GetValueOrDefault("trip_short_name", null),
                        DirectionId = directionId != -1 ? EnumUtil.FromValue<DirectionType>(directionId) : null,
                        BlockId = rowData.GetValueOrDefault("block_id", null),
                        ShapeId = rowData.GetValueOrDefault("shape_id", null),
                        WheelchairAccessible = wheelchairAccessibleId != -1 ? EnumUtil.FromValue<TrinaryOption>(wheelchairAccessibleId) : null,
                        BikesAllowed = bikesAllowedId != -1 ? EnumUtil.FromValue<TrinaryOption>(bikesAllowedId) : null,
                    };

                    entities.Add(entity);

                    if (entities.Count >= batchSize)
                    {
                        _dbContext.Trips.AddRange(entities);
                        await _dbContext.SaveChangesAsync();
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    _dbContext.Trips.AddRange(entities);
                    await _dbContext.SaveChangesAsync();
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            _logger.LogInformation($"Imported {totalImported} records from {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}