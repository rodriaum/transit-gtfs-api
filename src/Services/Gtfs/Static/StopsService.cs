using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Diagnostics;
using System.Globalization;
using Tranzor.Databases;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class StopsService : IStopsService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<StopsService> _logger;
    private readonly IRedisService _redis;

    public StopsService(GTFSContext dbContext, ILogger<StopsService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Stop>> GetAllAsync(string? cityId = null)
    {
        return await (string.IsNullOrEmpty(cityId)
            ? _dbContext.Stops
            : _dbContext.Stops.Where(stop => stop.StopCities.Any(sc => sc.CityId == cityId)))
            .ToListAsync();
    }

    public async Task<Stop?> GetByIdAsync(string stopId)
    {
        return await _redis.GetOrSetAsync(
            $"stop-{stopId}",
            async () => await _dbContext.Stops.FirstOrDefaultAsync(s => s.StopId == stopId)
        );
    }

    public async Task<List<Stop>> GetNearestStopAsync(double lat, double lon, string? cityId = null, int limit = 1)
    {
        Point point = new Point(lon, lat) { SRID = Constant.GeometryFactory.SRID };

        return await (string.IsNullOrEmpty(cityId)
            ? _dbContext.Stops.Where(s => s.Location != null)
            : _dbContext.Stops.Where(s => s.Location != null && s.StopCities.Any(sc => sc.CityId == cityId)))
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
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _dbContext.Stops.Select(s => s.StopId.ToLower()).ToListAsync()
            );

            List<Stop> entities = new List<Stop>(batchSize);

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
                        Location = Constant.GeometryFactory.CreatePoint(new Coordinate(stopLon, stopLat))
                    };

                    entities.Add(entity);
                    existingIds.Add(stopId.ToLower());

                    if (entities.Count >= batchSize)
                    {
                        await _dbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _dbContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} line(s) ignored. ({{0}})",
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return;
        }
    }
}