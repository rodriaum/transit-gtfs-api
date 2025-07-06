using System.Globalization;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

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
        string filePath = Path.Combine(directoryPath, "stops.txt");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }
        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<Stop>();
            string[] lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length <= 1)
            {
                _logger.LogWarning($"No data found in {filePath}");
                return;
            }
            string[] headers = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                var rowData = new Dictionary<string, string?>();
                for (int j = 0; j < headers.Length; j++)
                {
                    if (j < values.Length)
                        rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                }
                var entity = new Stop
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
            }
            if (entities.Count > 0)
            {
                _dbContext.Stops.AddRange(entities);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Imported {entities.Count} records from {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}