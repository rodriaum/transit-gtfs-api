using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Utils;
using Route = Tranzor.Models.Route;

namespace Tranzor.Services.Gtfs.Static;

public class RoutesService : IRoutesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<RoutesService> _logger;
    private readonly IPostgresService _postgresService;

    public RoutesService(GtfsDbContext gtfsDbContext, ILogger<RoutesService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Route>> GetAllAsync()
    {
        return await _gtfsDbContext.Routes.ToListAsync();
    }

    public async Task<Route?> GetByIdAsync(string routeId)
    {
        return await _gtfsDbContext.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId);
    }

    public async Task ImportDataAsync(string directoryPath, string agencyId)
    {
        string filePath = Path.Combine(directoryPath, "routes.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Routes.Select(r => r.RouteId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Route> entities = new List<Route>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
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
                totalIgnored++;
                continue;
            }

            Route entity = new Route
            {
                Id = Guid.NewGuid().ToString(),
                RouteId = routeId,
                AgencyId = agencyId,
                RouteShortName = rowData.GetValueOrDefault("route_short_name", "") ?? "",
                RouteLongName = rowData.GetValueOrDefault("route_long_name", "") ?? "",
                RouteDesc = rowData.GetValueOrDefault("route_desc", null),
                RouteType = routeType,
                RouteUrl = rowData.GetValueOrDefault("route_url", null),
                RouteColor = rowData.GetValueOrDefault("route_color", null),
                RouteTextColor = rowData.GetValueOrDefault("route_text_color", null),
                RouteSortOrder = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("route_sort_order", null), null),
                ContinuousPickup = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("continuous_pickup", null), null),
                ContinuousDropOff =
                    NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("continuous_drop_off", null), null)
            };

            entities.Add(entity);
            existingIds.Add(routeId.ToLower());
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}