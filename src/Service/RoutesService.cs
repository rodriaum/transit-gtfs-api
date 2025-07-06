using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

public class RoutesService : IRoutesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<RoutesService> _logger;
    private readonly IRedisService _redis;

    public RoutesService(TransitDbContext dbContext, ILogger<RoutesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Models.Route>> GetAllAsync()
    {
        return await _dbContext.Routes.ToListAsync();
    }

    public async Task<Models.Route?> GetByIdAsync(string routeId)
    {
        return await _redis.GetOrSetAsync(
            $"route-{routeId}",
            async () => await _dbContext.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId)
        );
    }

    public async Task ImportDataAsync(string directoryPath, string? agencyKey = null)
    {
        string filePath = Path.Combine(directoryPath, "routes.txt");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }
        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<Models.Route>();
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
                int routeTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("route_type", null), -1);
                if (!EnumUtil.TryFromValue(routeTypeId, out RouteType routeType))
                {
                    _logger.LogWarning("Cannot parse route_type value: {routeTypeId}", routeTypeId);
                    continue;
                }
                var entity = new Models.Route
                {
                    Id = Guid.NewGuid().ToString(),
                    RouteId = rowData.GetValueOrDefault("route_id", "") ?? "",
                    AgencyId = rowData.GetValueOrDefault("agency_id", "") ?? (agencyKey ?? ""),
                    RouteShortName = rowData.GetValueOrDefault("route_short_name", "") ?? "",
                    RouteLongName = rowData.GetValueOrDefault("route_long_name", "") ?? "",
                    RouteDesc = rowData.GetValueOrDefault("route_desc", null),
                    RouteType = routeType,
                    RouteUrl = rowData.GetValueOrDefault("route_url", null),
                    RouteColor = rowData.GetValueOrDefault("route_color", null),
                    RouteTextColor = rowData.GetValueOrDefault("route_text_color", null),
                    RouteSortOrder = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("route_sort_order", null), null),
                    ContinuousPickup = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("continuous_pickup", null), null),
                    ContinuousDropOff = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("continuous_drop_off", null), null)
                };
                entities.Add(entity);
            }
            if (entities.Count > 0)
            {
                _dbContext.Routes.AddRange(entities);
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