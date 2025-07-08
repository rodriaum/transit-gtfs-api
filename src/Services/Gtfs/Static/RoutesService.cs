using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

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

    public async Task ImportDataAsync(string directoryPath, string? agencyId = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "routes.txt");

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
                await _dbContext.Routes.Select(r => r.RouteId.ToLower()).ToListAsync()
            );

            List<Models.Route> entities = new List<Models.Route>(batchSize);

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

                    string routeId = rowData.GetValueOrDefault("route_id", "") ?? "";
                    if (existingIds.Contains(routeId.ToLower()))
                    {
                        totalIgnored++;
                        continue;
                    }

                    int routeTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("route_type", null), -1);

                    if (!EnumUtil.TryFromValue(routeTypeId, out RouteType routeType))
                    {
                        _logger.LogWarning("Cannot parse route_type value: {routeTypeId}", routeTypeId);
                        continue;
                    }

                    Models.Route entity = new Models.Route
                    {
                        Id = Guid.NewGuid().ToString(),
                        RouteId = routeId,
                        AgencyId = rowData.GetValueOrDefault("agency_id", null) ?? agencyId ?? "",
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
                    existingIds.Add(routeId.ToLower());

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
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} file(s) ignored. ({{0}})",
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