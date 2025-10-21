using System.Diagnostics;
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
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly ILogger<StopsService> _logger;
    private readonly IRedisService _redis;

    public StopsService(GtfsDbContext gtfsDBContext, ILogger<StopsService> logger, IRedisService redis)
    {
        _gtfsDBContext = gtfsDBContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Stop>> GetAllAsync(string? cityId = null, int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;

        IQueryable<Stop> query = _gtfsDBContext.Stops;

        if (!string.IsNullOrEmpty(cityId))
        {
            List<string> stopIds = await _gtfsDBContext.StopCities
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
        return await _redis.GetOrSetAsync(
            $"stop-{stopId}",
            async () => await _gtfsDBContext.Stops.FirstOrDefaultAsync(s => s.StopId == stopId)
        );
    }

    public async Task<List<Stop>> GetNearestStopAsync(double lat, double lon, string? cityId = null, int limit = 1)
    {
        Point point = new Point(lon, lat) { SRID = Constant.Wgs84GeometryFactory.SRID };

        return await _gtfsDBContext.Stops.Where(s => s.Location != null)
            .OrderBy(s => s.Location!.Distance(point))
            .Take(limit)
            .ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "stops.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {0}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.SqlBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = [.. await _gtfsDBContext.Stops.Select(s => s.StopId.ToLower()).ToListAsync()];
            List<City> cities = await _gtfsDBContext.Cities.ToListAsync();

            List<Stop> entities = new List<Stop>(batchSize);
            List<StopCity> stopCities = new List<StopCity>(batchSize);

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

                    string stopId = rowData.GetValueOrDefault("stop_id", "") ?? "";
                    if (existingIds.Contains(stopId.ToLower()))
                    {
                        totalIgnored++;
                        continue;
                    }

                    double stopLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lat", null), format: CultureInfo.InvariantCulture);
                    double stopLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lon", null), format: CultureInfo.InvariantCulture);

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
                        LocationType = EnumUtil.FromValue<LocationType>(NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("location_type", null))),
                        ParentStation = rowData.GetValueOrDefault("parent_station", null),
                        StopTimezone = rowData.GetValueOrDefault("stop_timezone", null),
                        WheelchairBoarding = EnumUtil.FromValue<AccessibilityType>(NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wheelchair_boarding", null))),
                        PlatformCode = rowData.GetValueOrDefault("platform_code", null),
                        Location = Constant.Wgs84GeometryFactory.CreatePoint(new Coordinate(stopLon, stopLat))
                    };

                    entities.Add(entity);
                    existingIds.Add(stopId.ToLower());

                    foreach (City city in cities)
                    {
                        if (entity.Location == null) break;
                        if (!city.Geom.Contains(entity.Location)) continue;

                        StopCity stopCity = new StopCity();

                        stopCity.Id = Guid.NewGuid().ToString();
                        stopCity.CityId = city.Id;
                        stopCity.StopId = entity.Id;

                        stopCities.Add(stopCity);
                    }

                    if (!stopCities.Any(sc => sc.StopId == entity.Id))
                    {
                        _logger.LogWarning("Stop {StopId} does not have a city to be assigned.", entity.StopId);
                    }

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDBContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();

                        await _gtfsDBContext.BulkInsertAsync(stopCities);
                        stopCities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDBContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();

                    await _gtfsDBContext.BulkInsertAsync(stopCities);
                    stopCities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in database with {2} line(s) ignored. ({3})",
                totalImported,
                filePath,
                totalIgnored,
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
        }
    }
}