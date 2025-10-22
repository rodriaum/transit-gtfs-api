using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class TripsService : ITripsService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<TripsService> _logger;
    private readonly IPostgresService _postgresService;

    public TripsService(GtfsDbContext gtfsDbContext, ILogger<TripsService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Trip>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _gtfsDbContext.Trips.Skip(skip).Take(pageSize).ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(string tripId)
    {
        return await _gtfsDbContext.Trips.FirstOrDefaultAsync(t => t.TripId == tripId);
    }

    public async Task<List<Trip>?> GetByRouteIdAsync(string routeId, int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _gtfsDbContext.Trips.Where(t => t.RouteId == routeId)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Trip>?> GetTripsBatchAsync(List<string> tripIds)
    {
        List<Trip> trips = await _gtfsDbContext.Trips
            .Where(t => tripIds.Contains(t.TripId))
            .ToListAsync();

        return tripIds
            .Select(id => trips.FirstOrDefault(t => t.TripId == id))
            .Where(t => t != null)
            .Select(t => t!)
            .ToList();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "trips.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Trips.Select(t => t.TripId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Trip> entities = new List<Trip>();

        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string tripId = rowData.GetValueOrDefault("trip_id", "") ?? "";

            if (existingIds.Contains(tripId.ToLower()))
            {
                totalIgnored++;
                continue;
            }

            int wheelchairAccessibleId =
                NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wheelchair_accessible", null), -1);
            int directionId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("direction_id", null), -1);
            int bikesAllowedId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("bikes_allowed", null), -1);

            Trip entity = new Trip
            {
                Id = Guid.NewGuid().ToString(),
                RouteId = rowData.GetValueOrDefault("route_id", "") ?? "",
                ServiceId = rowData.GetValueOrDefault("service_id", "") ?? "",
                TripId = tripId,
                TripHeadsign = rowData.GetValueOrDefault("trip_headsign", null),
                TripShortName = rowData.GetValueOrDefault("trip_short_name", null),
                DirectionId = directionId != -1 ? EnumUtil.FromValue<DirectionType>(directionId) : null,
                BlockId = rowData.GetValueOrDefault("block_id", null),
                ShapeId = rowData.GetValueOrDefault("shape_id", null),
                WheelchairAccessible = wheelchairAccessibleId != -1
                    ? EnumUtil.FromValue<TrinaryOption>(wheelchairAccessibleId)
                    : null,
                BikesAllowed = bikesAllowedId != -1 ? EnumUtil.FromValue<TrinaryOption>(bikesAllowedId) : null,
            };

            entities.Add(entity);
            existingIds.Add(tripId.ToLower());
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}