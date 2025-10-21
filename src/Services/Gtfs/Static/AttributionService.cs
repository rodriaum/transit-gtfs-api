using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class AttributionService : IAttributionService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<AttributionService> _logger;
    private readonly IPostgresService _postgresService;

    public AttributionService(GtfsDbContext gtfsDbContext, ILogger<AttributionService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Attribution>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<Attribution>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath, string agencyId)
    {
        string filePath = Path.Combine(directoryPath, "attributions.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<Attribution>().Select(a =>
                (a.AgencyId ?? "") + ":" + (a.RouteId ?? "") + ":" + (a.TripId ?? "") + ":" +
                a.OrganizationName.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Attribution> entities = new List<Attribution>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string routeId = rowData.GetValueOrDefault("route_id", null) ?? "";
            string tripId = rowData.GetValueOrDefault("trip_id", null) ?? "";
            string orgName = rowData.GetValueOrDefault("organization_name", "") ?? "";
            string uniqueKey = agencyId + ":" + routeId + ":" + tripId + ":" + orgName.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            Attribution entity = new Attribution
            {
                Id = Guid.NewGuid().ToString(),
                AgencyId = agencyId,
                RouteId = routeId,
                TripId = tripId,
                OrganizationName = orgName,
                IsProducer = rowData.GetValueOrDefault("is_producer", "0") == "1",
                IsOperator = rowData.GetValueOrDefault("is_operator", "0") == "1",
                IsAuthority = rowData.GetValueOrDefault("is_authority", "0") == "1",
                AttributionUrl = rowData.GetValueOrDefault("attribution_url", null),
                AttributionEmail = rowData.GetValueOrDefault("attribution_email", null),
                AttributionPhone = rowData.GetValueOrDefault("attribution_phone", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);

            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}