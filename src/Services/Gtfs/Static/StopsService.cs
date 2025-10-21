using System.Globalization;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Models.External;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class StopsService : IStopsService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<StopsService> _logger;
    private readonly IPostgresService _postgresService;

    public StopsService(GtfsDbContext gtfsDbContext, ILogger<StopsService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Stop>> GetAllAsync(string? cityId = null, int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;

        IQueryable<Stop> query = _gtfsDbContext.Stops;

        if (!string.IsNullOrEmpty(cityId))
        {
            List<string> stopIds = await _gtfsDbContext.StopCities
                .Where(city => city.CityId == cityId)
                .Select(city => city.StopId)
                .ToListAsync();

            if (stopIds.Count > 0)
            {
                query = query
                    .Skip(skip)
                    .Take(pageSize)
                    .Where(stop => stopIds.Contains(stop.Id));
            }
            else
            {
                return new List<Stop>();
            }
        }

        return await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }


    public async Task<Stop?> GetByIdAsync(string stopId)
    {
        return await _gtfsDbContext.Stops.FirstOrDefaultAsync(s => s.StopId == stopId);
    }

    public async Task<List<Stop>> GetNearestStopAsync(double lat, double lon, string? cityId = null, int limit = 1)
    {
        Point point = new Point(lon, lat) { SRID = Constant.Wgs84GeometryFactory.SRID };

        return await _gtfsDbContext.Stops.Where(s => s.Location != null)
            .OrderBy(s => s.Location!.Distance(point))
            .Take(limit)
            .ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "stops.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Stops.Select(s => s.StopId.ToLower()).ToListAsync()
        );

        // Get existing StopCity combinations to avoid duplicates
        HashSet<string> existingStopCities = new(
            await _gtfsDbContext.StopCities
                .Select(sc => $"{sc.StopId}_{sc.CityId}")
                .ToListAsync()
        );

        List<City> cities = await _gtfsDbContext.Cities.ToListAsync();
        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);

        List<Stop> entities = new List<Stop>();
        List<StopCity> stopCities = new List<StopCity>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string stopId = rowData.GetValueOrDefault("stop_id", "") ?? "";

            if (existingIds.Contains(stopId.ToLower()))
            {
                totalIgnored++;
                continue;
            }

            double stopLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lat", null),
                format: CultureInfo.InvariantCulture);
            double stopLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lon", null),
                format: CultureInfo.InvariantCulture);

            Stop entity = new Stop
            {
                Id = Guid.NewGuid().ToString(),
                StopId = stopId,
                StopCode = rowData.GetValueOrDefault("stop_code", null),
                StopName = rowData.GetValueOrDefault("stop_name", "") ?? "",
                StopDesc = rowData.GetValueOrDefault("stop_desc", null),
                StopLat = stopLat,
                StopLon = stopLon,
                ZoneId = rowData.GetValueOrDefault("zone_id", "") ?? "",
                StopUrl = rowData.GetValueOrDefault("stop_url", "") ?? "",
                LocationType =
                    EnumUtil.FromValue<LocationType>(
                        NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("location_type", null))),
                ParentStation = rowData.GetValueOrDefault("parent_station", null),
                StopTimezone = rowData.GetValueOrDefault("stop_timezone", null),
                WheelchairBoarding =
                    EnumUtil.FromValue<AccessibilityType>(
                        NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wheelchair_boarding", null))),
                PlatformCode = rowData.GetValueOrDefault("platform_code", null),
                Location = Constant.Wgs84GeometryFactory.CreatePoint(new Coordinate(stopLon, stopLat))
            };

            entities.Add(entity);
            existingIds.Add(stopId.ToLower());

            // Processes the cities for the stop
            foreach (City city in cities)
            {
                if (entity.Location == null) break;
                if (!city.Geom.Contains(entity.Location)) continue;

                string stopCityKey = $"{entity.Id}_{city.Id}";
                
                // Skip if this StopCity combination already exists
                if (existingStopCities.Contains(stopCityKey))
                    continue;

                StopCity stopCity = new StopCity
                {
                    Id = Guid.NewGuid().ToString(),
                    CityId = city.Id,
                    StopId = entity.Id
                };

                stopCities.Add(stopCity);
                existingStopCities.Add(stopCityKey); // Track it to avoid duplicates within this import
            }

            if (!stopCities.Any(sc => sc.StopId == entity.Id))
            {
                _logger.LogWarning("Stop {StopId} does not have a city to be assigned.", entity.StopId);
            }
        }

        // Bulk insert all entities and stop_cities after processing all rows
        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);

        // ALERT: This need to be after the insert of the stops entities, because it uses the entity.Id
        if (stopCities.Count > 0)
        {
            await _gtfsDbContext.BulkInsertAsync(stopCities);
        }
    }
}