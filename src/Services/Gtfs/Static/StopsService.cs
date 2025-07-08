using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class StopsService : IStopsService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<StopsService> _logger;
    private readonly IRedisService _redis;

    public StopsService(TransitDbContext dbContext, ILogger<StopsService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Stop>> GetAllAsync()
    {
        return await _dbContext.Stops.ToListAsync();
    }

    public async Task<Stop?> GetByIdAsync(string stopId)
    {
        return await _redis.GetOrSetAsync(
            $"stop-{stopId}",
            async () => await _dbContext.Stops.FirstOrDefaultAsync(s => s.StopId == stopId)
        );
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
            _logger.LogInformation($"Importing data from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;

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

                    Stop entity = new Stop
                    {
                        Id = Guid.NewGuid().ToString(),
                        StopId = rowData.GetValueOrDefault("stop_id", "") ?? "",
                        StopCode = rowData.GetValueOrDefault("stop_code", null),
                        StopName = rowData.GetValueOrDefault("stop_name", "") ?? "",
                        StopDesc = rowData.GetValueOrDefault("stop_desc", null),
                        StopLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lat", null), format: CultureInfo.InvariantCulture),
                        StopLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("stop_lon", null), format: CultureInfo.InvariantCulture),
                        ZoneId = rowData.GetValueOrDefault("zone_id", "") ?? "",
                        StopUrl = rowData.GetValueOrDefault("stop_url", "") ?? "",
                        LocationType = EnumUtil.FromValue<LocationType>(NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("location_type", null))),
                        ParentStation = rowData.GetValueOrDefault("parent_station", null),
                        StopTimezone = rowData.GetValueOrDefault("stop_timezone", null),
                        WheelchairBoarding = EnumUtil.FromValue<AccessibilityType>(NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wheelchair_boarding", null))),
                        PlatformCode = rowData.GetValueOrDefault("platform_code", null)
                    };

                    entities.Add(entity);

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

            string duration = TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"Imported {totalImported} records from {filePath} in {duration}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}