using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareMediaService : IFareMediaService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareMediaService> _logger;
    private readonly IPostgresService _postgresService;

    public FareMediaService(GtfsDbContext gtfsDbContext, ILogger<FareMediaService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FareMedia>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<FareMedia>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_media.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<FareMedia>().Select(f => f.FareMediaId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvImportUtil.ReadCsvAsync(filePath, _logger);
        List<FareMedia> entities = new List<FareMedia>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fareMediaId = rowData.GetValueOrDefault("fare_media_id", "") ?? "";
            string uniqueKey = fareMediaId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            FareMedia entity = new FareMedia
            {
                Id = Guid.NewGuid().ToString(),
                FareMediaId = fareMediaId,
                Name = rowData.GetValueOrDefault("name", "") ?? "",
                Description = rowData.GetValueOrDefault("description", null),
                Type = rowData.GetValueOrDefault("type", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}
