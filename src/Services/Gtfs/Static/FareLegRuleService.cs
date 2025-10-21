using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareLegRuleService : IFareLegRuleService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareLegRuleService> _logger;
    private readonly IPostgresService _postgresService;

    public FareLegRuleService(GtfsDbContext gtfsDbContext, ILogger<FareLegRuleService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FareLegRule>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<FareLegRule>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_leg_rules.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<FareLegRule>().Select(f => f.FareLegRuleId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvImportUtil.ReadCsvAsync(filePath, _logger);
        List<FareLegRule> entities = new List<FareLegRule>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fareLegRuleId = rowData.GetValueOrDefault("fare_leg_rule_id", "") ?? "";
            string uniqueKey = fareLegRuleId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            FareLegRule entity = new FareLegRule
            {
                Id = Guid.NewGuid().ToString(),
                FareLegRuleId = fareLegRuleId,
                FareProductId = rowData.GetValueOrDefault("fare_product_id", null),
                LegGroupId = rowData.GetValueOrDefault("leg_group_id", null),
                NetworkId = rowData.GetValueOrDefault("network_id", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}