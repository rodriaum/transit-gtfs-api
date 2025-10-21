using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareRulesService : IFareRulesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareRulesService> _logger;
    private readonly IPostgresService _postgresService;

    public FareRulesService(GtfsDbContext gtfsDbContext, ILogger<FareRulesService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FareRule>> GetAllAsync()
    {
        return await _gtfsDbContext.FareRules.ToListAsync();
    }

    public async Task<List<FareRule>?> GetByFareIdAsync(string fareId)
    {
        return await _gtfsDbContext.FareRules.Where(f => f.FareId == fareId).ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_rules.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.FareRules.Select(f => f.FareId.ToLower() + ":" + (f.RouteId ?? "").ToLower() + ":" + (f.OriginId ?? "").ToLower() + ":" + (f.DestinationId ?? "").ToLower() + ":" + (f.ContainsId ?? "").ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<FareRule> entities = new List<FareRule>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fareId = rowData.GetValueOrDefault("fare_id", "") ?? "";
            string routeId = rowData.GetValueOrDefault("route_id", null) ?? "";
            string originId = rowData.GetValueOrDefault("origin_id", null) ?? "";
            string destinationId = rowData.GetValueOrDefault("destination_id", null) ?? "";
            string containsId = rowData.GetValueOrDefault("contains_id", null) ?? "";
            string uniqueKey = fareId.ToLower() + ":" + routeId.ToLower() + ":" + originId.ToLower() + ":" +
                               destinationId.ToLower() + ":" + containsId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            FareRule entity = new FareRule
            {
                Id = Guid.NewGuid().ToString(),
                FareId = fareId,
                RouteId = string.IsNullOrEmpty(routeId) ? null : routeId,
                OriginId = string.IsNullOrEmpty(originId) ? null : originId,
                DestinationId = string.IsNullOrEmpty(destinationId) ? null : destinationId,
                ContainsId = string.IsNullOrEmpty(containsId) ? null : containsId
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}